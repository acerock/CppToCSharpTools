using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Logging;

namespace CppToCsConverter.Core.Parsers
{
    public class CppSourceParser
    {
        private readonly ILogger _logger;
        private readonly Regex _methodImplementationRegex = new Regex(
            @"(?:(\w+(?:\s*\*|\s*&)?)\s+)?(\w+)\s*::\s*([~]?\w+)\s*\(([^)]*)\)(?:\s*(const))?\s*\{", 
            RegexOptions.Compiled | RegexOptions.Multiline);
        
        private readonly Regex _staticMemberInitRegex = new Regex(
            @"(?:(const)\s+)?(?:(\w+)\s+)?(\w+)\s*::\s*(\w+)(?:\s*\[\s*\])?(?:\s*\[\s*(\d*)\s*\])?\s*=\s*([^;]+);", 
            RegexOptions.Compiled);

        private readonly Regex _pragmaRegionRegex = new Regex(@"^\s*#pragma\s+(region|endregion)(?:\s+(.*))?$", RegexOptions.Compiled);
        private readonly Regex _defineRegex = new Regex(@"^\s*#define\s+(\w+)(?:\s+(.*))?$", RegexOptions.Compiled);

        public CppSourceParser(ILogger? logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
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
                
                // Parse method implementations using the original approach
                sourceFile.Methods.AddRange(ParseMethodImplementations(content, sourceFile.FileName));
                
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
                            SourceFileName = fileName
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

            return methods;
        }

        private List<CppMethod> ParseMultiLineMethodImplementations(string content, string fileName, List<CppMethod> existingMethods, int startOrderIndex)
        {
            var methods = new List<CppMethod>();
            var lines = content.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Look for pattern: ClassName::MethodName (
                var methodHeaderMatch = Regex.Match(line, @"^(?:(\w+(?:\s*\*|\s*&)?)\s+)?(\w+)\s*::\s*([~]?\w+)\s*\($");
                if (methodHeaderMatch.Success)
                {
                    var className = methodHeaderMatch.Groups[2].Value;
                    var methodName = methodHeaderMatch.Groups[3].Value;
                    var returnType = methodHeaderMatch.Groups[1].Success ? methodHeaderMatch.Groups[1].Value.Trim() : "void";
                    
                    // Skip if we already found this method
                    bool alreadyFound = existingMethods.Any(m => 
                        m.ClassName == className && 
                        m.Name == methodName);
                        
                    if (alreadyFound)
                        continue;
                    
                    // Find the closing parenthesis and opening brace
                    var parametersString = "";
                    int currentLine = i + 1;
                    int parenCount = 1; // We already have one opening paren
                    bool foundClosingParen = false;
                    bool foundOpeningBrace = false;
                    int braceLineIndex = -1;
                    
                    // Collect parameters across multiple lines
                    while (currentLine < lines.Length && parenCount > 0)
                    {
                        var paramLine = lines[currentLine];
                        parametersString += paramLine;
                        
                        // Count parentheses to handle nested comments with parens
                        foreach (char c in paramLine)
                        {
                            if (c == '(') parenCount++;
                            else if (c == ')') parenCount--;
                        }
                        
                        if (parenCount == 0)
                        {
                            foundClosingParen = true;
                            break;
                        }
                        
                        currentLine++;
                    }
                    
                    if (!foundClosingParen)
                        continue;
                    
                    // Look for const modifier and opening brace after the closing paren
                    currentLine++;
                    bool isConst = false;
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
            var parameters = new List<CppParameter>();
            
            if (string.IsNullOrWhiteSpace(parametersString))
                return parameters;

            var paramParts = SplitParameters(parametersString);
            
            foreach (var part in paramParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var parameter = new CppParameter();
                parameter.OriginalText = part.Trim(); // Store original for reconstruction
                
                // Extract positioned comments while preserving their positions
                var (cleanText, positionedComments) = ExtractPositionedCommentsFromParameter(part);
                parameter.PositionedComments = positionedComments;
                
                // Also populate legacy InlineComments for backward compatibility
                parameter.InlineComments = positionedComments.Select(pc => pc.CommentText).ToList();
                
                var cleanTrimmed = cleanText.Trim();
                
                // In implementation, no default values should be present
                // Parse const, type, reference/pointer, and name
                var constMatch = Regex.Match(cleanTrimmed, @"^(const\s+)?(.+?)(\s*[&*])?\s+(\w+)$");
                if (constMatch.Success)
                {
                    parameter.IsConst = constMatch.Groups[1].Success;
                    parameter.Type = constMatch.Groups[2].Value.Trim();
                    var refPointer = constMatch.Groups[3].Value.Trim();
                    parameter.IsReference = refPointer.Contains("&");
                    parameter.IsPointer = refPointer.Contains("*");
                    parameter.Name = constMatch.Groups[4].Value;
                }
                else
                {
                    // Fallback parsing
                    var words = cleanTrimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 2)
                    {
                        parameter.Type = string.Join(" ", words.Take(words.Length - 1));
                        parameter.Name = words.Last();
                    }
                    else if (words.Length == 1)
                    {
                        // Just a type, generate a parameter name
                        parameter.Type = words[0];
                        parameter.Name = "param";
                    }
                }

                parameters.Add(parameter);
            }

            return parameters;
        }

        private string[] SplitParameters(string parametersString)
        {
            var parameters = new List<string>();
            var current = new System.Text.StringBuilder();
            int depth = 0;
            bool inCStyleComment = false;
            
            for (int i = 0; i < parametersString.Length; i++)
            {
                char c = parametersString[i];
                
                // Check for start of C-style comment
                if (!inCStyleComment && c == '/' && i + 1 < parametersString.Length && parametersString[i + 1] == '*')
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
                if (inCStyleComment)
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
                    parameters.Add(current.ToString());
                    current.Clear();
                    continue;
                }
                
                current.Append(c);
            }
            
            if (current.Length > 0)
                parameters.Add(current.ToString());
            
            return parameters.ToArray();
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
                    
                    // Add to clean content for parameter name detection
                    if (!char.IsWhiteSpace(ch))
                    {
                        cleanContentSoFar.Append(ch);
                    }
                    
                    // Try to detect if we've seen a parameter name
                    // A parameter typically has format: [const] Type[*|&] paramName
                    // We'll check if we have at least one word that could be a type and another that could be a name
                    if (!hasSeenParameterName && !char.IsWhiteSpace(ch))
                    {
                        var cleanSoFar = cleanContentSoFar.ToString().Trim();
                        // Simple heuristic: if we have multiple words and the current clean content ends with identifier characters
                        if (cleanSoFar.Contains(' ') || cleanSoFar.Contains('&') || cleanSoFar.Contains('*'))
                        {
                            var words = cleanSoFar.Split(new[] { ' ', '*', '&', '<', '>', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length >= 2) // We have type and possibly name
                            {
                                hasSeenParameterName = true;
                            }
                        }
                    }
                    
                    i++;
                }
            }
            
            return (cleanText.ToString(), positionedComments);
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
    }
}