using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Logging;
using CppToCsConverter.Core.Parsers.ParameterParsing;

namespace CppToCsConverter.Core.Parsers
{
    public class CppSourceParser
    {
        private readonly ILogger _logger;
        private readonly CppParameterParser _parameterParser;
        private readonly Regex _methodImplementationRegex = new Regex(
            @"(?:(\w+(?:\s*\*|\s*&)?)\s+)?(\w+)\s*::\s*([~]?\w+)\s*\(([^)]*)\)(?:\s*(const))?\s*\{", 
            RegexOptions.Compiled | RegexOptions.Multiline);
        
        // Regex to detect local methods (functions without class scope regulator ::)
        private readonly Regex _localMethodRegex = new Regex(
            @"(?:^|\n)\s*(?:(\w+(?:\s*\*|\s*&)?)\s+)?([~]?\w+)\s*\(([^)]*)\)(?:\s*(const))?\s*\{",
            RegexOptions.Compiled | RegexOptions.Multiline);
        
        private readonly Regex _staticMemberInitRegex = new Regex(
            @"(?:(const)\s+)?(?:(\w+)\s+)?(\w+)\s*::\s*(\w+)(?:\s*\[\s*\])?(?:\s*\[\s*(\d*)\s*\])?\s*=\s*([^;]+);", 
            RegexOptions.Compiled);

        private readonly Regex _pragmaRegionRegex = new Regex(@"^\s*#pragma\s+(region|endregion)(?:\s+(.*))?$", RegexOptions.Compiled);
        private readonly Regex _defineRegex = new Regex(@"^\s*#define\s+(\w+)(?:\s+(.*))?$", RegexOptions.Compiled);

        public CppSourceParser(ILogger? logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _parameterParser = new CppParameterParser(
                new ParameterBlockSplitter(),
                new ParameterComponentExtractor()
            );
        }

        public (List<CppMethod> Methods, List<CppStaticMemberInit> StaticInits) ParseSourceFile(string filePath)
        {
            var (methods, staticInits, _) = ParseSourceFileWithDefines(filePath);
            return (methods, staticInits);
        }

        public (List<CppMethod> Methods, List<CppStaticMemberInit> StaticInits, List<CppDefine> Defines) ParseSourceFileWithDefines(string filePath)
        {
            var sourceFile = ParseSourceFileComplete(filePath);
            return (sourceFile.Methods, sourceFile.StaticMemberInits, sourceFile.Defines);
        }

        public CppSourceFile ParseSourceFileComplete(string filePath)
        {
            var sourceFile = new CppSourceFile
            {
                FileName = Path.GetFileNameWithoutExtension(filePath)
            };
            
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Parse file top comments first
                sourceFile.FileTopComments.AddRange(ParseFileTopComments(lines));
                
                // Parse structs defined in source file
                var headerParser = new CppHeaderParser(_logger);
                sourceFile.Structs.AddRange(headerParser.ParseStructsFromLines(lines));
                
                // Parse method implementations using the original approach
                sourceFile.Methods.AddRange(ParseMethodImplementations(content, sourceFile.FileName));
                
                // Move struct constructors/methods from Methods list to their respective structs
                MoveStructMethodsToStructs(sourceFile.Methods, sourceFile.Structs);
                
                // Add comments and regions to the parsed methods
                AddCommentsAndRegionsToMethods(lines, sourceFile.Methods);
                
                // Parse static member initializations
                sourceFile.StaticMemberInits.AddRange(ParseStaticMemberInitializations(content));
                
                // Parse define statements
                sourceFile.Defines.AddRange(ParseDefineStatementsFromLines(lines, sourceFile.FileName));
                
                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing source file {filePath}: {ex.Message}");
                return sourceFile;
            }
        }

        private List<CppDefine> ParseDefineStatementsFromLines(string[] lines, string fileName)
        {
            var defines = new List<CppDefine>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Check if this is a define statement
                var defineMatch = _defineRegex.Match(line);
                if (defineMatch.Success)
                {
                    // Only collect defines that have a value (ignore defines with no value like #define ARCHIVER_H_)
                    if (defineMatch.Groups[2].Success && !string.IsNullOrWhiteSpace(defineMatch.Groups[2].Value))
                    {
                        // Collect comments before the define
                        var precedingComments = CollectPrecedingComments(lines, i);
                        
                        var define = new CppDefine
                        {
                            Name = defineMatch.Groups[1].Value,
                            Value = defineMatch.Groups[2].Value.Trim(),
                            FullDefinition = line,
                            PrecedingComments = precedingComments,
                            SourceFileName = fileName,
                            IsFromHeader = false
                        };
                        
                        defines.Add(define);
                    }
                }
            }
            
            return defines;
        }

        private void AddCommentsAndRegionsToMethods(string[] lines, List<CppMethod> methods)
        {
            // For each method, find its line in the source file and collect comments
            foreach (var method in methods)
            {
                // Look for method declaration line (may span multiple lines)
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // Check if this line contains the method signature
                    if (line.Contains($"{method.ClassName}::{method.Name}") && line.Contains("("))
                    {
                        // Collect comments before method with indentation
                        var (sourceComments, sourceIndentation) = CollectPrecedingCommentsWithIndentation(lines, i);
                        method.SourceComments = sourceComments;
                        method.SourceCommentIndentation = sourceIndentation;
                        
                        // Capture implementation indentation from the method body
                        if (!string.IsNullOrEmpty(method.ImplementationBody))
                        {
                            method.ImplementationIndentation = CppToCsConverter.Core.Utils.IndentationManager.DetectOriginalIndentation(method.ImplementationBody);
                        }
                        
                        // Look for region markers around method
                        var (regionStart, regionEnd) = ParseSourceRegionMarkers(lines, i, methods, method.OrderIndex);
                        method.SourceRegionStart = regionStart;
                        method.SourceRegionEnd = regionEnd;
                        
                        break; // Found this method, move to next one
                    }
                }
            }
        }

        private List<CppMethod> ParseMethodImplementations(string content, string fileName)
        {
            var methods = new List<CppMethod>();
            var matches = _methodImplementationRegex.Matches(content);
            
            int orderIndex = 0;
            foreach (Match match in matches)
            {
                var method = new CppMethod
                {
                    ReturnType = match.Groups[1].Success ? match.Groups[1].Value.Trim() : "void",
                    ClassName = match.Groups[2].Value,
                    Name = match.Groups[3].Value,
                    IsConst = match.Groups[5].Success,
                    OrderIndex = orderIndex++
                };

                // Check if constructor or destructor
                method.IsConstructor = !method.Name.StartsWith("~") && method.Name == method.ClassName;
                method.IsDestructor = method.Name.StartsWith("~");

                // Parse parameters from implementation
                var parametersString = match.Groups[4].Value;
                method.Parameters = ParseParametersFromImplementation(parametersString);

                // Extract method body
                method.ImplementationBody = ExtractMethodBody(content, match.Index + match.Length);
                
                // Set TargetFileName for .cpp implementations
                method.TargetFileName = fileName;

                methods.Add(method);
            }

            // Handle multi-line method signatures that the regex missed
            methods.AddRange(ParseMultiLineMethodImplementations(content, fileName, methods, orderIndex));

            // Parse local methods (functions without class scope regulator ::)
            var localMethods = ParseLocalMethods(content, fileName, methods);
            methods.AddRange(localMethods);

            // Recalculate order indices based on actual file positions
            RecalculateMethodOrderIndices(content, methods);
            
            // Sort methods by their recalculated order indices
            methods = methods.OrderBy(m => m.OrderIndex).ToList();

            return methods;
        }

        private List<CppMethod> ParseMultiLineMethodImplementations(string content, string fileName, List<CppMethod> existingMethods, int startOrderIndex)
        {
            var methods = new List<CppMethod>();
            var lines = content.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Look for pattern: ClassName::MethodName ( - allow params on same line or next line
                var methodHeaderMatch = Regex.Match(line, @"^(?:(\w+(?:\s*\*|\s*&)?)\s+)?(\w+)\s*::\s*([~]?\w+)\s*\(");
                if (methodHeaderMatch.Success)
                {
                    var className = methodHeaderMatch.Groups[2].Value;
                    var methodName = methodHeaderMatch.Groups[3].Value;
                    var returnType = methodHeaderMatch.Groups[1].Success ? methodHeaderMatch.Groups[1].Value.Trim() : "void";
                    
                    // Find the closing parenthesis and opening brace
                    var parametersString = "";
                    
                    // Check if there are parameters on the same line as the opening paren
                    int openParenIndex = line.IndexOf('(');
                    int currentLine = i;
                    int parenCount = 0;
                    bool foundClosingParen = false;
                    
                    // Start counting parens from the opening paren
                    if (openParenIndex >= 0)
                    {
                        for (int charIndex = openParenIndex; charIndex < line.Length; charIndex++)
                        {
                            char c = line[charIndex];
                            if (c == '(') parenCount++;
                            else if (c == ')')
                            {
                                parenCount--;
                                if (parenCount == 0)
                                {
                                    // Found closing paren on same line - extract parameters between parens
                                    parametersString = line.Substring(openParenIndex + 1, charIndex - openParenIndex - 1) + "\n";
                                    foundClosingParen = true;
                                    break;
                                }
                            }
                        }
                        
                        // If closing paren not on first line, add rest of first line to parametersString
                        if (!foundClosingParen && openParenIndex < line.Length - 1)
                        {
                            parametersString = line.Substring(openParenIndex + 1) + "\n";
                        }
                    }
                    
                    currentLine = i + 1;
                    bool foundOpeningBrace = false;
                    int braceLineIndex = -1;
                    
                    // Collect parameters across multiple lines
                    while (currentLine < lines.Length && parenCount > 0 && !foundClosingParen)
                    {
                        var paramLine = lines[currentLine];
                        
                        // Count parentheses to find the closing paren position
                        int closingParenPos = -1;
                        for (int charIndex = 0; charIndex < paramLine.Length; charIndex++)
                        {
                            char c = paramLine[charIndex];
                            if (c == '(') parenCount++;
                            else if (c == ')')
                            {
                                parenCount--;
                                if (parenCount == 0)
                                {
                                    // Found the closing paren - only include content before it
                                    closingParenPos = charIndex;
                                    foundClosingParen = true;
                                    break;
                                }
                            }
                        }
                        
                        // Add the line content (up to closing paren if found)
                        if (closingParenPos >= 0)
                        {
                            parametersString += paramLine.Substring(0, closingParenPos) + "\n";
                        }
                        else
                        {
                            parametersString += paramLine + "\n"; // Add newline to preserve line structure
                        }
                        
                        if (foundClosingParen)
                            break;
                        
                        currentLine++;
                    }
                    
                    if (!foundClosingParen)
                        continue;
                    
                    // Look for const modifier and opening brace after the closing paren
                    // Check the current line first (where closing paren was found) for opening brace
                    bool isConst = false;
                    var currentBraceLine = lines[currentLine - 1].Trim(); // currentLine is one past the closing paren line
                    if (currentBraceLine.Contains("const"))
                    {
                        isConst = true;
                    }
                    if (currentBraceLine.Contains("{"))
                    {
                        foundOpeningBrace = true;
                        braceLineIndex = currentLine - 1;
                    }
                    
                    // If not found on same line, look on subsequent lines
                    if (!foundOpeningBrace)
                    {
                        while (currentLine < lines.Length && !foundOpeningBrace)
                        {
                            var braceLine = lines[currentLine].Trim();
                            if (braceLine.Contains("const") && !foundOpeningBrace)
                            {
                                isConst = true;
                            }
                            if (braceLine.Contains("{"))
                            {
                                foundOpeningBrace = true;
                                braceLineIndex = currentLine;
                                break;
                            }
                            currentLine++;
                        }
                    }
                    
                    if (foundOpeningBrace)
                    {
                        // Create the method
                        var method = new CppMethod
                        {
                            ReturnType = returnType,
                            ClassName = className,
                            Name = methodName,
                            IsConst = isConst,
                            OrderIndex = startOrderIndex + methods.Count
                        };

                        // Check if constructor or destructor
                        method.IsConstructor = !method.Name.StartsWith("~") && method.Name == method.ClassName;
                        method.IsDestructor = method.Name.StartsWith("~");

                        // Parse parameters
                        method.Parameters = ParseParametersFromImplementation(parametersString);

                        // Check if we already parsed this exact method (same name + parameters)
                        // This prevents duplicates while allowing overloads
                        bool isDuplicate = methods.Any(m => 
                            m.ClassName == className && 
                            m.Name == methodName &&
                            m.Parameters.Count == method.Parameters.Count &&
                            ParametersEqual(m.Parameters, method.Parameters)) ||
                            existingMethods.Any(m => 
                                m.ClassName == className && 
                                m.Name == methodName &&
                                m.Parameters.Count == method.Parameters.Count &&
                                ParametersEqual(m.Parameters, method.Parameters));
                        
                        if (isDuplicate)
                            continue;

                        // Extract method body
                        // Calculate the position of the opening brace based on line numbers
                        int braceIndex = -1;
                        int charPos = 0;
                        for (int lineIdx = 0; lineIdx <= braceLineIndex && lineIdx < lines.Length; lineIdx++)
                        {
                            if (lineIdx == braceLineIndex)
                            {
                                // Find the brace in this specific line
                                int braceInLine = lines[lineIdx].IndexOf('{');
                                if (braceInLine >= 0)
                                {
                                    braceIndex = charPos + braceInLine;
                                    break;
                                }
                            }
                            charPos += lines[lineIdx].Length + 1; // +1 for the newline character
                        }
                        
                        if (braceIndex >= 0)
                        {
                            method.ImplementationBody = ExtractMethodBody(content, braceIndex + 1);
                        }
                        
                        // Set TargetFileName
                        method.TargetFileName = fileName;

                        methods.Add(method);
                    }
                }
            }
            
            return methods;
        }

        private List<CppParameter> ParseParametersFromImplementation(string parametersString)
        {
            if (string.IsNullOrWhiteSpace(parametersString))
                return new List<CppParameter>();

            // Use new parameter parser
            return _parameterParser.ParseParameters(parametersString);
        }

        private string[] SplitParameters(string parametersString)
        {
            var parameters = new List<string>();
            var current = new System.Text.StringBuilder();
            int depth = 0;
            bool inCStyleComment = false;
            bool inCppStyleComment = false;
            
            for (int i = 0; i < parametersString.Length; i++)
            {
                char c = parametersString[i];
                
                // Check for start of C++ style comment (//)
                if (!inCStyleComment && !inCppStyleComment && c == '/' && i + 1 < parametersString.Length && parametersString[i + 1] == '/')
                {
                    inCppStyleComment = true;
                    current.Append(c);
                    continue;
                }
                
                // Check for end of C++ style comment (newline)
                if (inCppStyleComment && (c == '\n' || c == '\r'))
                {
                    inCppStyleComment = false;
                    current.Append(c);
                    
                    // After a C++ style comment ends (at newline), this is a good place to split parameters
                    // if we're not inside nested parens/angles and the current content looks complete
                    if (depth == 0 && current.ToString().Trim().Length > 0)
                    {
                        // Check if there's a previous comma in the current parameter
                        // If so, this comment was a trailing comment and the newline marks the parameter end
                        var currentStr = current.ToString();
                        var lastCommaIndex = currentStr.LastIndexOf(',');
                        if (lastCommaIndex >= 0)
                        {
                            // There's a comma followed by a comment and newline - this is a parameter boundary
                            // Add this parameter and start fresh
                            parameters.Add(current.ToString());
                            current.Clear();
                            continue;
                        }
                    }
                    continue;
                }
                
                // Check for start of C-style comment
                if (!inCStyleComment && !inCppStyleComment && c == '/' && i + 1 < parametersString.Length && parametersString[i + 1] == '*')
                {
                    inCStyleComment = true;
                    current.Append(c);
                    continue;
                }
                
                // Check for end of C-style comment
                if (inCStyleComment && c == '*' && i + 1 < parametersString.Length && parametersString[i + 1] == '/')
                {
                    inCStyleComment = false;
                    current.Append(c);
                    continue;
                }
                
                // If we're inside a comment, just append the character
                if (inCStyleComment || inCppStyleComment)
                {
                    current.Append(c);
                    continue;
                }
                
                // Normal parameter splitting logic (only when not in comment)
                if (c == '(' || c == '<')
                    depth++;
                else if (c == ')' || c == '>')
                    depth--;
                else if (c == ',' && depth == 0)
                {
                    // Add the comma to current parameter
                    current.Append(c);
                    
                    // Check if there's a trailing // comment on this line
                    // Look ahead to see if there's a // before the next newline
                    bool hasTrailingComment = false;
                    int commentStart = -1;
                    for (int j = i + 1; j < parametersString.Length; j++)
                    {
                        char lookAhead = parametersString[j];
                        if (lookAhead == '\n' || lookAhead == '\r')
                        {
                            // Newline before //, so no trailing comment
                            break;
                        }
                        if (lookAhead == '/' && j + 1 < parametersString.Length && parametersString[j + 1] == '/')
                        {
                            // Found // comment before newline - this is a trailing comment
                            hasTrailingComment = true;
                            commentStart = j;
                            break;
                        }
                        if (!char.IsWhiteSpace(lookAhead))
                        {
                            // Non-whitespace, non-comment content - this is the next parameter
                            break;
                        }
                    }
                    
                    if (!hasTrailingComment)
                    {
                        // Normal comma split - no trailing comment
                        parameters.Add(current.ToString());
                        current.Clear();
                        continue;
                    }
                    else
                    {
                        // Has trailing comment - include whitespace and comment, then split after newline
                        // Add whitespace before comment
                        for (int j = i + 1; j < commentStart; j++)
                        {
                            current.Append(parametersString[j]);
                        }
                        // Add the comment
                        i = commentStart;
                        while (i < parametersString.Length && parametersString[i] != '\n' && parametersString[i] != '\r')
                        {
                            current.Append(parametersString[i]);
                            i++;
                        }
                        // Add newline if present
                        if (i < parametersString.Length && (parametersString[i] == '\n' || parametersString[i] == '\r'))
                        {
                            current.Append(parametersString[i]);
                        }
                        // Split here
                        parameters.Add(current.ToString());
                        current.Clear();
                        continue;
                    }
                }
                
                current.Append(c);
            }
            
            if (current.Length > 0)
                parameters.Add(current.ToString());
            
            // Fix misassigned suffix comments: move leading comments from parameters to suffix of previous parameter
            return FixSuffixCommentAssignment(parameters.ToArray());
        }

        private string[] FixSuffixCommentAssignment(string[] parameters)
        {
            var fixedParameters = new List<string>();
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i].Trim();
                
                // Check if this parameter starts with a comment that might be a misplaced suffix comment
                if (i > 0 && (param.StartsWith("/*") || param.StartsWith("//")))
                {
                    // Extract the comment and remainder
                    string comment = "";
                    string remainder = param;
                    
                    if (param.StartsWith("/*"))
                    {
                        var endIndex = param.IndexOf("*/");
                        if (endIndex >= 0)
                        {
                            comment = param.Substring(0, endIndex + 2);
                            remainder = param.Substring(endIndex + 2).Trim();
                        }
                    }
                    else if (param.StartsWith("//"))
                    {
                        var newlineIndex = param.IndexOf('\n');
                        if (newlineIndex >= 0)
                        {
                            comment = param.Substring(0, newlineIndex);
                            remainder = param.Substring(newlineIndex + 1).Trim();
                        }
                        else
                        {
                            comment = param;
                            remainder = "";
                        }
                    }
                    
                    // Only move the comment if it looks like a misplaced suffix comment
                    // Heuristics to distinguish:
                    // - "/* IN */ const CString& param1" (don't move - legitimate short prefix)
                    // - "/*IN/OUT: Memory table with open cursor pointing to specific row*/ double &dValue" (move - long misplaced suffix)
                    var isLikelyMisplacedSuffix = !string.IsNullOrWhiteSpace(remainder) && 
                                           comment.Length > 20 && // Long comments are likely documentation (suffix)
                                           (comment.Contains(":") || comment.Contains("to ") || 
                                            comment.Contains("for ") || comment.Contains("with ") ||
                                            comment.Contains("Return") || comment.Contains("Set ")) && // Documentation keywords
                                           remainder.Length > 10 && // Substantial parameter content
                                           (remainder.Contains("&") || remainder.Contains("*") || 
                                            remainder.Split(' ', '\t').Length >= 2); // Looks like type + name
                    
                    var shouldMoveComment = isLikelyMisplacedSuffix;
                    
                    if (shouldMoveComment && !string.IsNullOrEmpty(comment) && fixedParameters.Count > 0)
                    {
                        // Move comment to previous parameter as suffix
                        fixedParameters[fixedParameters.Count - 1] += " " + comment;
                        
                        // Add remainder as current parameter
                        if (!string.IsNullOrWhiteSpace(remainder))
                        {
                            fixedParameters.Add(remainder);
                        }
                    }
                    else
                    {
                        // Don't move - keep as is (legitimate prefix comment)
                        fixedParameters.Add(param);
                    }
                }
                else
                {
                    fixedParameters.Add(param);
                }
            }
            
            return fixedParameters.ToArray();
        }

        private string ExtractMethodBody(string content, int startIndex)
        {
            int braceCount = 0;
            int startBrace = -1;
            
            // Find the opening brace (should be at or near startIndex)
            for (int i = startIndex - 10; i < content.Length && i < startIndex + 50; i++)
            {
                if (content[i] == '{')
                {
                    startBrace = i;
                    braceCount = 1;
                    break;
                }
            }
            
            if (startBrace == -1)
                return string.Empty;
            
            // Find the closing brace
            for (int i = startBrace + 1; i < content.Length; i++)
            {
                if (content[i] == '{')
                    braceCount++;
                else if (content[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        // Extract the method body (without the outer braces)
                        var methodBody = content.Substring(startBrace + 1, i - startBrace - 1);
                        // Replace tab characters with four spaces
                        methodBody = methodBody.Replace("\t", "    ");
                        
                        // Normalize indentation: find minimum indentation and remove it from all lines
                        return NormalizeIndentation(methodBody);
                    }
                }
            }
            
            return string.Empty;
        }

        private List<CppMethod> ParseLocalMethods(string content, string fileName, List<CppMethod> existingMethods)
        {
            var localMethods = new List<CppMethod>();
            
            // First, determine which class methods are implemented in this file
            // Local methods belong to the class with the most methods implemented in the same .cpp file
            // Exclude interface classes (typically start with 'I' and have fewer implementations)
            var classCounts = existingMethods
                .Where(m => !IsLikelyInterfaceClass(m.ClassName))  // Exclude interface classes
                .GroupBy(m => m.ClassName)
                .ToDictionary(g => g.Key, g => g.Count());
            var targetClassName = classCounts.OrderByDescending(kvp => kvp.Value)
                                            .FirstOrDefault().Key ?? "Unknown";
            
            // Find all potential local method matches
            var matches = _localMethodRegex.Matches(content);
            var lines = content.Split('\n');
            
            foreach (Match match in matches)
            {
                var returnType = match.Groups[1].Success ? match.Groups[1].Value.Trim() : "void";
                var methodName = match.Groups[2].Value;
                var parametersString = match.Groups[3].Value;
                var isConst = match.Groups[4].Success;
                
                // Skip if this looks like it might be a class method (contains ::)
                if (methodName.Contains("::"))
                    continue;
                
                // Skip if method name contains common non-method patterns
                if (methodName.Equals("if", StringComparison.OrdinalIgnoreCase) ||
                    methodName.Equals("while", StringComparison.OrdinalIgnoreCase) ||
                    methodName.Equals("for", StringComparison.OrdinalIgnoreCase) ||
                    methodName.Equals("switch", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                // Check if this is already found as a class method (to avoid duplicates)
                if (existingMethods.Any(m => m.Name == methodName))
                    continue;
                
                // Determine order index based on position in file
                int lineNumber = content.Substring(0, match.Index).Split('\n').Length - 1;
                int orderIndex = GetMethodOrderIndex(content, match.Index, existingMethods);
                
                // Create local method
                var localMethod = new CppMethod
                {
                    ReturnType = returnType,
                    Name = methodName,
                    IsConst = isConst,
                    IsLocalMethod = true,
                    IsStatic = true,
                    AccessSpecifier = AccessSpecifier.Private,
                    ClassName = targetClassName, // Associate with the class from this file
                    TargetFileName = fileName,
                    OrderIndex = orderIndex
                };

                // Parse parameters
                localMethod.Parameters = ParseParametersFromImplementation(parametersString);

                // Extract method body
                localMethod.ImplementationBody = ExtractMethodBody(content, match.Index + match.Length);
                
                // Only add if we successfully extracted a method body (not just forward declaration)
                if (!string.IsNullOrWhiteSpace(localMethod.ImplementationBody))
                {
                    localMethods.Add(localMethod);
                }
            }
            
            return localMethods;
        }
        
        /// <summary>
        /// Determines if a class name likely represents an interface class
        /// </summary>
        private bool IsLikelyInterfaceClass(string className)
        {
            // Interfaces typically start with 'I' followed by a capital letter
            return className.StartsWith("I") && className.Length > 1 && char.IsUpper(className[1]);
        }
        
        private int GetMethodOrderIndex(string content, int matchIndex, List<CppMethod> existingMethods)
        {
            // Count how many existing methods appear before this position in the file
            int orderIndex = 0;
            
            foreach (var method in existingMethods)
            {
                // Find the position of each existing method in the content
                var methodPattern = $"{method.ClassName}::{method.Name}";
                var methodIndex = content.IndexOf(methodPattern);
                if (methodIndex >= 0 && methodIndex < matchIndex)
                {
                    orderIndex++;
                }
            }
            
            return orderIndex;
        }

        private void RecalculateMethodOrderIndices(string content, List<CppMethod> methods)
        {
            // Create a list of (method, position) pairs
            var methodPositions = new List<(CppMethod method, int position)>();
            
            foreach (var method in methods)
            {
                int position = -1;
                
                if (method.IsLocalMethod)
                {
                    // For local methods, find by method name and opening brace
                    var pattern = $"{method.Name}\\s*\\([^)]*\\)\\s*\\{{";
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var match = regex.Match(content);
                    if (match.Success)
                    {
                        position = match.Index;
                    }
                }
                else
                {
                    // For class methods, find by ClassName::MethodName
                    var pattern = $"{Regex.Escape(method.ClassName)}::{Regex.Escape(method.Name)}";
                    position = content.IndexOf(pattern);
                }
                
                if (position >= 0)
                {
                    methodPositions.Add((method, position));
                }
            }
            
            // Sort by file position and assign new order indices
            var sortedMethods = methodPositions
                .OrderBy(mp => mp.position)
                .Select((mp, index) => new { mp.method, orderIndex = index })
                .ToList();
            
            // Update the OrderIndex for each method
            foreach (var item in sortedMethods)
            {
                item.method.OrderIndex = item.orderIndex;
            }
        }

        private List<CppStaticMemberInit> ParseStaticMemberInitializations(string content)
        {
            var staticInits = new List<CppStaticMemberInit>();
            var matches = _staticMemberInitRegex.Matches(content);
            
            foreach (Match match in matches)
            {
                var isConst = match.Groups[1].Success;
                var type = match.Groups[2].Success ? match.Groups[2].Value : "auto";
                var className = match.Groups[3].Value;
                var memberName = match.Groups[4].Value;
                var arraySize = match.Groups[5].Success ? match.Groups[5].Value : string.Empty;
                var initValue = match.Groups[6].Value.Trim();
                
                staticInits.Add(new CppStaticMemberInit
                {
                    ClassName = className,
                    MemberName = memberName,
                    InitializationValue = initValue,
                    IsArray = !string.IsNullOrEmpty(arraySize) || initValue.Trim().StartsWith("{"),
                    ArraySize = arraySize,
                    Type = type,
                    IsConst = isConst
                });
                
                Console.WriteLine($"Found static member initialization: {className}::{memberName} = {initValue}");
            }
            
            return staticInits;
        }

        // Comment and region parsing methods for .cpp files
        private (List<string> comments, int indentation) CollectPrecedingCommentsWithIndentation(string[] lines, int currentIndex)
        {
            var comments = new List<string>();
            int originalIndentation = 0;
            int lookbackIndex = currentIndex - 1;
            
            // Skip empty lines immediately before
            while (lookbackIndex >= 0 && string.IsNullOrWhiteSpace(lines[lookbackIndex]))
            {
                lookbackIndex--;
            }
            
            // Collect comment blocks working backwards
            var commentBlock = new List<string>();
            bool inMultiLineComment = false;
            
            while (lookbackIndex >= 0)
            {
                var line = lines[lookbackIndex].Trim();
                
                // Check for single line comments
                if (line.StartsWith("//"))
                {
                    commentBlock.Insert(0, lines[lookbackIndex]);
                    lookbackIndex--;
                    continue;
                }
                
                // Check for end of multi-line comment
                if (line.EndsWith("*/"))
                {
                    inMultiLineComment = true;
                    commentBlock.Insert(0, lines[lookbackIndex]);
                    
                    // If this line also starts the comment, we're done with this block
                    if (line.StartsWith("/*"))
                    {
                        inMultiLineComment = false;
                        lookbackIndex--;
                        continue;
                    }
                    
                    lookbackIndex--;
                    continue;
                }
                
                // Check for start of multi-line comment
                if (inMultiLineComment && line.StartsWith("/*"))
                {
                    commentBlock.Insert(0, lines[lookbackIndex]);
                    inMultiLineComment = false;
                    lookbackIndex--;
                    continue;
                }
                
                // Inside multi-line comment
                if (inMultiLineComment)
                {
                    commentBlock.Insert(0, lines[lookbackIndex]);
                    lookbackIndex--;
                    continue;
                }
                
                // Check for empty lines between comment blocks
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Peek ahead to see if there are more comments
                    int peekIndex = lookbackIndex - 1;
                    while (peekIndex >= 0 && string.IsNullOrWhiteSpace(lines[peekIndex]))
                    {
                        peekIndex--;
                    }
                    
                    if (peekIndex >= 0)
                    {
                        var peekLine = lines[peekIndex].Trim();
                        if (peekLine.StartsWith("//") || peekLine.EndsWith("*/"))
                        {
                            // There are more comments, include the empty line
                            commentBlock.Insert(0, lines[lookbackIndex]);
                            lookbackIndex--;
                            continue;
                        }
                    }
                }
                
                // Not a comment line, stop collecting
                break;
            }
            
            // Add the collected comment block to results if any
            comments.AddRange(commentBlock);
            
            // Capture original indentation from first comment line
            if (comments.Any())
            {
                var firstComment = comments[0];
                originalIndentation = firstComment.Length - firstComment.TrimStart().Length;
            }
            
            return (comments, originalIndentation);
        }

        private List<string> CollectPrecedingComments(string[] lines, int currentIndex)
        {
            var (comments, _) = CollectPrecedingCommentsWithIndentation(lines, currentIndex);
            return comments;
        }

        private (string regionStart, string regionEnd) ParseSourceRegionMarkers(string[] lines, int currentIndex, List<CppMethod> existingMethods, int currentOrderIndex)
        {
            string regionStart = string.Empty;
            string regionEnd = string.Empty;
            
            // Look for region start before current method
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var regionMatch = _pragmaRegionRegex.Match(line);
                if (regionMatch.Success && regionMatch.Groups[1].Value.Equals("region", StringComparison.OrdinalIgnoreCase))
                {
                    var description = regionMatch.Groups[2].Success ? regionMatch.Groups[2].Value.Trim() : string.Empty;
                    regionStart = $"#region {description}".Trim();
                    break;
                }
                
                // If we hit a method implementation or non-empty, non-comment line, stop looking
                if (_methodImplementationRegex.IsMatch(line) || (!line.StartsWith("//") && !line.StartsWith("/*") && !line.EndsWith("*/")))
                {
                    break;
                }
            }
            
            // Look for region end after the current method (look ahead to find the corresponding endregion)
            if (!string.IsNullOrEmpty(regionStart))
            {
                // Find the end of the current method first
                int methodEndIndex = currentIndex;
                int braceCount = 0;
                bool inMethodBody = false;
                
                for (int i = currentIndex; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    if (line.Contains("{"))
                    {
                        inMethodBody = true;
                        braceCount += line.Count(c => c == '{');
                    }
                    
                    if (inMethodBody && line.Contains("}"))
                    {
                        braceCount -= line.Count(c => c == '}');
                        if (braceCount <= 0)
                        {
                            methodEndIndex = i;
                            break;
                        }
                    }
                }
                
                // Now look for region end after the method
                for (int i = methodEndIndex + 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var regionMatch = _pragmaRegionRegex.Match(line);
                    if (regionMatch.Success && regionMatch.Groups[1].Value.Equals("endregion", StringComparison.OrdinalIgnoreCase))
                    {
                        var description = regionMatch.Groups[2].Success ? regionMatch.Groups[2].Value.Trim() : string.Empty;
                        regionEnd = $"#endregion{(string.IsNullOrEmpty(description) ? "" : " " + description)}";
                        break;
                    }
                    
                    // If we hit another method or non-comment line, stop looking
                    if (_methodImplementationRegex.IsMatch(line))
                    {
                        break;
                    }
                }
            }
            
            return (regionStart, regionEnd);
        }
        
        /// <summary>
        /// Normalizes the indentation of a method body by finding the minimum indentation
        /// of all non-empty lines and removing that amount from all lines.
        /// This ensures consistent relative indentation regardless of the original context.
        /// </summary>
        /// <param name="methodBody">The raw method body content</param>
        /// <returns>The method body with normalized indentation</returns>
        private string NormalizeIndentation(string methodBody)
        {
            if (string.IsNullOrWhiteSpace(methodBody))
                return string.Empty;
                
            var lines = methodBody.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            
            // Find the minimum indentation among all non-empty lines
            int minIndentation = int.MaxValue;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    int leadingSpaces = 0;
                    foreach (char c in line)
                    {
                        if (c == ' ')
                            leadingSpaces++;
                        else
                            break;
                    }
                    minIndentation = Math.Min(minIndentation, leadingSpaces);
                }
            }
            
            // If no non-empty lines found, return empty
            if (minIndentation == int.MaxValue)
                return string.Empty;
                
            // Remove the minimum indentation from all lines
            var normalizedLines = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Keep empty lines as empty
                    normalizedLines.Add(string.Empty);
                }
                else
                {
                    // Remove the minimum indentation
                    var normalizedLine = line.Length > minIndentation 
                        ? line.Substring(minIndentation) 
                        : line.TrimStart();
                    normalizedLines.Add(normalizedLine);
                }
            }
            
            // Join lines and trim trailing whitespace
            return string.Join("\n", normalizedLines).Trim();
        }

        private List<string> ParseFileTopComments(string[] lines)
        {
            var fileTopComments = new List<string>();
            bool insideMultiLineComment = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Skip empty lines at the beginning
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                // Check if we're entering a multi-line comment
                if (line.StartsWith("/*"))
                {
                    insideMultiLineComment = true;
                    fileTopComments.Add(lines[i]); // Use original line with whitespace
                    
                    // Check if the comment ends on the same line
                    if (line.Contains("*/"))
                    {
                        insideMultiLineComment = false;
                    }
                    continue;
                }
                
                // Check if we're inside a multi-line comment
                if (insideMultiLineComment)
                {
                    fileTopComments.Add(lines[i]); // Use original line with whitespace
                    
                    // Check if this line ends the multi-line comment
                    if (line.Contains("*/"))
                    {
                        insideMultiLineComment = false;
                    }
                    continue;
                }
                
                // Check for single-line comments
                if (line.StartsWith("//"))
                {
                    fileTopComments.Add(lines[i]); // Use original line with whitespace
                    continue;
                }
                
                // If we hit an #include, #define, or any non-comment line, stop
                if (line.StartsWith("#include", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#define", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#pragma", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#if", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#endif", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#ifndef", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("#ifdef", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                
                // Any other non-comment line means we're done with file top comments
                break;
            }
            
            return fileTopComments;
        }
        
        /// <summary>
        /// Extracts positioned comments from parameter text while preserving the clean parameter declaration
        /// </summary>
        private (string cleanText, List<ParameterComment> positionedComments) ExtractPositionedCommentsFromParameter(string parameterText)
        {
            var positionedComments = new List<ParameterComment>();
            var cleanText = new StringBuilder();
            var i = 0;
            
            // Track if we've seen the parameter name to determine prefix vs suffix
            // We'll consider comments prefix until we see both type and name parts
            var hasSeenParameterName = false;
            var cleanContentSoFar = new StringBuilder();
            
            while (i < parameterText.Length)
            {
                // Check for single-line comment
                if (i < parameterText.Length - 1 && parameterText[i] == '/' && parameterText[i + 1] == '/')
                {
                    var commentStart = i;
                    // Find end of line or end of string
                    while (i < parameterText.Length && parameterText[i] != '\n')
                        i++;
                    
                    var comment = parameterText.Substring(commentStart, i - commentStart).Trim();
                    var position = hasSeenParameterName ? CommentPosition.Suffix : CommentPosition.Prefix;
                    positionedComments.Add(new ParameterComment { CommentText = comment, Position = position });
                    
                    // Add newline if present
                    if (i < parameterText.Length && parameterText[i] == '\n')
                    {
                        cleanText.Append('\n');
                        i++;
                    }
                }
                // Check for multi-line comment
                else if (i < parameterText.Length - 1 && parameterText[i] == '/' && parameterText[i + 1] == '*')
                {
                    var commentStart = i;
                    i += 2; // Skip /*
                    
                    // Find end of comment
                    while (i < parameterText.Length - 1)
                    {
                        if (parameterText[i] == '*' && parameterText[i + 1] == '/')
                        {
                            i += 2; // Skip */
                            break;
                        }
                        i++;
                    }
                    
                    var comment = parameterText.Substring(commentStart, i - commentStart).Trim();
                    var position = hasSeenParameterName ? CommentPosition.Suffix : CommentPosition.Prefix;
                    positionedComments.Add(new ParameterComment { CommentText = comment, Position = position });
                }
                else
                {
                    var ch = parameterText[i];
                    cleanText.Append(ch);
                    
                    // Add to clean content for parameter name detection (include spaces for proper word detection)
                    cleanContentSoFar.Append(ch);
                    
                    // Try to detect if we've seen a parameter name
                    // A parameter typically has format: [const] Type[*|&] paramName
                    // We'll check if we have at least one word that could be a type and another that could be a name
                    if (!hasSeenParameterName)
                    {
                        var cleanSoFar = cleanContentSoFar.ToString().Trim();
                        // Simple heuristic: if we have multiple words and the current clean content ends with identifier characters
                        if (cleanSoFar.Contains(' ') || cleanSoFar.Contains('&') || cleanSoFar.Contains('*'))
                        {
                            var words = cleanSoFar.Split(new[] { ' ', '*', '&', '<', '>', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length >= 2) // We have type and possibly name
                            {
                                hasSeenParameterName = true;
                            }
                        }
                    }
                    
                    i++;
                }
            }
            
            // Remove trailing comma from clean text (comma is parameter separator, not part of declaration)
            var finalCleanText = cleanText.ToString().TrimEnd();
            if (finalCleanText.EndsWith(','))
            {
                finalCleanText = finalCleanText.Substring(0, finalCleanText.Length - 1).TrimEnd();
            }
            
            return (finalCleanText, positionedComments);
        }

        /// <summary>
        /// Extracts inline comments from parameter text while preserving the clean parameter declaration (legacy method)
        /// </summary>
        private (string cleanText, List<string> comments) ExtractInlineCommentsFromParameter(string parameterText)
        {
            var (cleanText, positionedComments) = ExtractPositionedCommentsFromParameter(parameterText);
            var comments = positionedComments.Select(pc => pc.CommentText).ToList();
            return (cleanText, comments);
        }
        
        /// <summary>
        /// Checks if two parameter lists are equal by comparing normalized types
        /// </summary>
        private bool ParametersEqual(List<CppParameter> params1, List<CppParameter> params2)
        {
            if (params1.Count != params2.Count)
                return false;
                
            for (int i = 0; i < params1.Count; i++)
            {
                var type1 = NormalizeParameterType(params1[i].Type);
                var type2 = NormalizeParameterType(params2[i].Type);
                
                if (type1 != type2)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Normalizes parameter type for comparison (removes const, &, *, spaces)
        /// </summary>
        private string NormalizeParameterType(string type)
        {
            return type.Trim()
                .Replace(" ", "")
                .Replace("const", "")
                .Replace("&", "")
                .Replace("*", "")
                .ToLowerInvariant();
        }
        
        /// <summary>
        /// Moves methods that belong to structs from the methods list to the struct's Methods collection
        /// </summary>
        private void MoveStructMethodsToStructs(List<CppMethod> methods, List<CppStruct> structs)
        {
            var methodsToRemove = new List<CppMethod>();
            
            foreach (var structDef in structs)
            {
                // Find methods where the method name matches the struct name (constructor)
                // or methods that are local to this struct
                foreach (var method in methods.ToList())
                {
                    // Check if this is a constructor for the struct (method name == struct name and it's a local method)
                    if (method.Name == structDef.Name && method.IsLocalMethod)
                    {
                        // Mark as constructor and update properties
                        method.IsConstructor = true;
                        method.IsLocalMethod = false; // No longer a local method, it's a struct constructor
                        method.IsStatic = false; // Constructors are not static
                        method.AccessSpecifier = AccessSpecifier.Public; // Struct constructors default to public
                        method.ClassName = structDef.Name; // Associate with the struct
                        
                        // Set inline implementation so the body gets generated
                        if (!string.IsNullOrEmpty(method.ImplementationBody))
                        {
                            method.HasInlineImplementation = true;
                            method.InlineImplementation = method.ImplementationBody;
                        }
                        
                        // Add to struct's methods collection
                        structDef.Methods.Add(method);
                        methodsToRemove.Add(method);
                    }
                }
            }
            
            // Remove the methods that were moved to structs
            foreach (var method in methodsToRemove)
            {
                methods.Remove(method);
            }
        }
    }
}