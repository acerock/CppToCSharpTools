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
    public class CppHeaderParser
    {
        private readonly ILogger _logger;
        private readonly CppParameterParser _parameterParser;
        private readonly Regex _classRegex = new Regex(@"(?:class|struct)\s+(?:__declspec\s*\([^)]+\)\s+)?(\w+)(?:\s*:\s*(?:public|private|protected)\s+(\w+))?", RegexOptions.Compiled);
        private readonly Regex _methodRegex = new Regex(@"(?:(virtual)\s+)?(?:(static)\s+)?(?:((?:const\s+)?\w+(?:::\w+)?[\*&]*)\s+)?([~]?\w+)\s*\(.*?\)(?:\s*(const))?(?:\s*:\s*([^{]*))?(?:\s*=\s*0)?(?:\s*\{.*?\})?", RegexOptions.Compiled | RegexOptions.Singleline);
        private readonly Regex _memberRegex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*([^]]*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(.*)$", RegexOptions.Compiled);
        private readonly Regex _accessSpecifierRegex = new Regex(@"^(private|protected|public)\s*:\s*(.*)$", RegexOptions.Compiled);
        private readonly Regex _pragmaRegionRegex = new Regex(@"^\s*#pragma\s+(region|endregion)(?:\s+(.*))?$", RegexOptions.Compiled);
        private readonly Regex _defineRegex = new Regex(@"^\s*#define\s+(\w+)(?:\s+(.*))?$", RegexOptions.Compiled);
        
        // Regex for typedef struct name extraction (e.g., "} MyStruct;")
        private readonly Regex _typedefNameRegex = new Regex(@"^\s*}\s*(\w+)\s*;\s*$", RegexOptions.Compiled);
        
        // Deprecated struct-specific regex patterns (kept for backward compatibility with ParseStructsFromHeaderFile)
        private readonly Regex _simpleStructRegex = new Regex(@"^\s*struct\s+(\w+)", RegexOptions.Compiled);
        private readonly Regex _typedefStructRegex = new Regex(@"^\s*typedef\s+struct\s*$", RegexOptions.Compiled);
        private readonly Regex _typedefStructTagRegex = new Regex(@"^\s*typedef\s+struct\s+(\w+)\s*$", RegexOptions.Compiled);

        public CppHeaderParser(ILogger? logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _parameterParser = new CppParameterParser(
                new ParameterBlockSplitter(),
                new ParameterComponentExtractor()
            );
        }

        public List<CppClass> ParseHeaderFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                return ParseAllClassesFromLines(lines, Path.GetFileNameWithoutExtension(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing header file {filePath}: {ex.Message}");
                return new List<CppClass>();
            }
        }

        private List<CppClass> ParseAllClassesFromLines(string[] lines, string fileName)
        {
            var classes = new List<CppClass>();
            
            // First pass: collect all define statements outside of class definitions
            var headerDefines = ParseDefineStatementsFromLines(lines, fileName);
            
            int i = 0;
            while (i < lines.Length)
            {
                var foundClass = ParseNextClassFromLines(lines, ref i, fileName);
                if (foundClass != null)
                {
                    classes.Add(foundClass);
                }
                else
                {
                    i++;
                }
            }
            
            // Associate header defines with the appropriate class
            AssociateHeaderDefinesWithClasses(classes, headerDefines, fileName);
            
            return classes;
        }

        private void AssociateHeaderDefinesWithClasses(List<CppClass> classes, List<CppDefine> headerDefines, string fileName)
        {
            if (!headerDefines.Any() || !classes.Any())
                return;
                
            // Find the main class that should get the defines:
            // 1. If there's a class whose name matches the filename, use that
            // 2. Otherwise, if there's only one class, use that  
            // 3. Otherwise, use the first non-interface class
            // 4. Otherwise, use the first class
            // IMPORTANT: Skip structs - they should not get header defines
            
            CppClass? targetClass = null;
            
            // Try to find class matching filename (but not if it's a struct)
            targetClass = classes.FirstOrDefault(c => !c.IsStruct && c.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            
            // If no filename match, try single class (but not if it's a struct)
            if (targetClass == null && classes.Count == 1 && !classes[0].IsStruct)
            {
                targetClass = classes[0];
            }
            
            // If multiple classes, prefer non-interface, non-struct classes
            if (targetClass == null)
            {
                targetClass = classes.FirstOrDefault(c => !c.IsInterface && !c.IsStruct) ?? classes.FirstOrDefault(c => !c.IsStruct);
            }
            
            // Associate defines with the target class (if found and not a struct)
            if (targetClass != null && !targetClass.IsStruct)
            {
                targetClass.HeaderDefines.AddRange(headerDefines);
            }
            else if (targetClass != null && targetClass.IsStruct)
            {
                // Structs don't get header defines
            }
            else
            {
                // No target class found for defines
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
                        
                        // Extract postfix comment from value (e.g., "2 // comment" -> value="2", comment="// comment")
                        string rawValue = defineMatch.Groups[2].Value.Trim();
                        string cleanValue = rawValue;
                        string postfixComment = string.Empty;
                        
                        // Check for // comments
                        int slashCommentIndex = rawValue.IndexOf("//");
                        if (slashCommentIndex >= 0)
                        {
                            postfixComment = rawValue.Substring(slashCommentIndex).Trim();
                            cleanValue = rawValue.Substring(0, slashCommentIndex).Trim();
                        }
                        
                        // Check for /* */ comments (only if no // comment found)
                        if (string.IsNullOrEmpty(postfixComment))
                        {
                            int blockCommentIndex = rawValue.IndexOf("/*");
                            if (blockCommentIndex >= 0)
                            {
                                postfixComment = rawValue.Substring(blockCommentIndex).Trim();
                                cleanValue = rawValue.Substring(0, blockCommentIndex).Trim();
                            }
                        }
                        
                        var define = new CppDefine
                        {
                            Name = defineMatch.Groups[1].Value,
                            Value = cleanValue,
                            PostfixComment = postfixComment,
                            FullDefinition = line,
                            PrecedingComments = precedingComments,
                            SourceFileName = fileName,
                            IsFromHeader = true
                        };
                        
                        defines.Add(define);
                    }
                }
            }
            
            return defines;
        }

        private CppClass? ParseNextClassFromLines(string[] lines, ref int startIndex, string fileName)
        {
            CppClass? currentClass = null;
            AccessSpecifier currentAccess = AccessSpecifier.Private;
            bool inClass = false;
            bool foundClassEnd = false;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                

                
                // Skip empty lines but NOT comments (we need to collect them)
                if (string.IsNullOrEmpty(line))
                    continue;

                // Skip comments and pragma directives that are not regions - they will be collected by comment parsing methods
                if ((line.StartsWith("//") || line.StartsWith("/*")) && !inClass)
                {
    
                    continue;
                }
                    
                if (line.StartsWith("#") && !line.Contains("pragma region"))
                    continue;

                // Check for class/struct declaration
                var classMatch = _classRegex.Match(line);
                bool isTypedefStruct = line.TrimStart().StartsWith("typedef struct");
                
                // Exclude lines ending with semicolon that are variable declarations (e.g., "struct tm time;")
                // but not class/struct definitions (which end with "};")
                bool isVariableDeclaration = line.TrimEnd().EndsWith(";") && !line.TrimEnd().EndsWith("};");
                
                // Also check for typedef struct without a name (will get name from closing brace)
                if ((classMatch.Success || isTypedefStruct) && !inClass && !isVariableDeclaration)
                {
                    // Collect comments before the class/struct declaration
                    var precedingComments = CollectPrecedingComments(lines, i);

                    currentClass = new CppClass
                    {
                        Name = classMatch.Success ? classMatch.Groups[1].Value : "UnnamedStruct", // Temporary name, will be updated from closing brace if typedef struct
                        IsStruct = line.Contains("struct"),
                        IsPublicExport = line.Contains("__declspec(dllexport)"),
                        PrecedingComments = precedingComments
                    };

                    if (classMatch.Success && classMatch.Groups[2].Success)
                    {
                        currentClass.BaseClasses.Add(classMatch.Groups[2].Value);
                    }

                    // Check if it's an interface (contains pure virtual methods) - but only for classes, not structs
                    currentClass.IsInterface = !currentClass.IsStruct && IsInterface(lines, i);
                    
                    currentAccess = currentClass.DefaultAccessSpecifier;
                    inClass = true;
                    

                    continue;
                }

                if (!inClass || currentClass == null)
                    continue;

                // Check for class ending pattern
                if (line == "};" || line.Trim().StartsWith("}") || (line == "}" && (i + 1 >= lines.Length || !lines[i + 1].Trim().StartsWith("else"))))
                {
                    // For typedef struct, extract the name from the closing line (e.g., "} MyStruct;")
                    if (currentClass.IsStruct && line.Contains("}"))
                    {
                        var typedefNameMatch = _typedefNameRegex.Match(line);
                        if (typedefNameMatch.Success)
                        {
                            currentClass.Name = typedefNameMatch.Groups[1].Value;
                        }
                    }
                    
                    // This is likely the class ending brace
                    startIndex = i + 1;
                    foundClassEnd = true;
                    break;
                }

                // Check for access specifiers (both standalone and inline with declarations)
                var accessMatch = _accessSpecifierRegex.Match(line);
                if (accessMatch.Success)
                {
                    Enum.TryParse<AccessSpecifier>(accessMatch.Groups[1].Value, true, out currentAccess);
                    
                    // Check if there's content after the access specifier (inline declaration)
                    var remainingContent = accessMatch.Groups[2].Value.Trim();
                    if (!string.IsNullOrEmpty(remainingContent))
                    {
                        // Reprocess the line with the remaining content as if it were a normal line
                        line = remainingContent;
                        // Don't continue, let it fall through to member/method parsing
                    }
                    else
                    {
                        // Standalone access specifier, continue to next line
                        continue;
                    }
                }

                // Skip lines that are clearly inside method bodies (contain return, if, etc.)
                // But don't skip method declarations that start with a method name and have opening braces
                if (line.Trim().StartsWith("return ") || line.Trim().StartsWith("if ") || 
                    (line.Contains("{") && !line.Contains("}") && !line.Contains("(") && !line.Trim().StartsWith("~")))
                    continue;

                // Check if this line has parentheses only in comments (skip method parsing for such lines)
                bool hasCommentOnlyParentheses = false;
                if (line.Contains("(") && (line.Contains("//") || line.Contains("/*")))
                {
                    var commentIndex = Math.Min(
                        line.IndexOf("//") >= 0 ? line.IndexOf("//") : int.MaxValue,
                        line.IndexOf("/*") >= 0 ? line.IndexOf("/*") : int.MaxValue
                    );
                    
                    if (commentIndex < int.MaxValue)
                    {
                        var codeBeforeComment = line.Substring(0, commentIndex);
                        if (!codeBeforeComment.Contains("("))
                        {
                            hasCommentOnlyParentheses = true;
                        }
                    }
                }

                // Parse methods (handle multi-line declarations) - but skip if parentheses are only in comments
                if (!hasCommentOnlyParentheses)
                {
                    var originalIndex = i;
                    var methodLine = CollectMultiLineMethodDeclaration(lines, ref i);
                    
                    // Normalize spacing around pointers and references in return type for regex matching
                    // This converts "CAgrMT *GetMethod" to "CAgrMT* GetMethod" for consistent regex matching
                    var normalizedMethodLine = NormalizePointerSpacing(methodLine);
                    
                    var methodMatch = _methodRegex.Match(normalizedMethodLine);
                    

                    
                    if (methodMatch.Success)
                    {
                        try
                        {
                            // Pass BOTH the original and normalized lines so we can extract return type from original
                            var method = ParseMethod(methodMatch, currentAccess, methodLine, normalizedMethodLine, currentClass.Name, fileName);
                            if (method != null)
                            {
                                // Collect comments and region markers for method from .h file
                                var (headerComments, headerIndentation) = CollectPrecedingCommentsWithIndentation(lines, originalIndex);
                                method.HeaderComments = headerComments;
                                method.HeaderCommentIndentation = headerIndentation;
                                
                                var (regionStart, regionEnd) = ParseRegionMarkers(lines, originalIndex, i);
                                method.HeaderRegionStart = regionStart;
                                method.HeaderRegionEnd = regionEnd;

                                currentClass.Methods.Add(method);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing method '{methodMatch.Groups[4].Value}': {ex.Message}");
                        }
                        continue;
                    }
                }

                // Parse members - only if it looks like a proper member declaration
                var memberMatch = _memberRegex.Match(line);
                

                
                if (memberMatch.Success)
                {
                    if (!line.TrimStart().StartsWith("return ") && !line.TrimStart().StartsWith("if ") && !line.TrimStart().StartsWith("for ") && !line.TrimStart().StartsWith("while "))
                    {
                        // Collect comments and region markers for member
                        var precedingComments = CollectPrecedingComments(lines, i);
                        var (regionStart, regionEnd) = ParseRegionMarkers(lines, i, i);
                        
                        // Extract postfix comment from the member line
                        var postfixComment = string.Empty;
                        var initialPostfixText = memberMatch.Groups[7].Value;
                        
                        // Check if this is a multi-line comment that spans multiple lines
                        if (initialPostfixText.Trim().StartsWith("/*") && !initialPostfixText.Contains("*/"))
                        {
                            // Collect the complete multi-line comment
                            postfixComment = CollectMultiLineComment(lines, i, initialPostfixText);
                        }
                        else
                        {
                            postfixComment = ExtractPostfixComment(initialPostfixText);
                        }
                        
                        var member = new CppMember
                        {
                            Type = memberMatch.Groups[3].Value.Trim(),
                            Name = memberMatch.Groups[4].Value,
                            AccessSpecifier = currentAccess,
                            IsStatic = memberMatch.Groups[1].Success,
                            IsConst = memberMatch.Groups[2].Success,
                            IsArray = memberMatch.Groups[5].Success,
                            ArraySize = memberMatch.Groups[5].Success ? memberMatch.Groups[5].Value : string.Empty,
                            InitializationValue = memberMatch.Groups[6].Success ? memberMatch.Groups[6].Value.Trim() : string.Empty,
                            PrecedingComments = precedingComments,
                            PostfixComment = postfixComment,
                            RegionStart = regionStart,
                            RegionEnd = regionEnd
                        };

                        currentClass.Members.Add(member);
                    }
                }
            }

            // If we reach here, we've processed all lines without finding the end of the class
            // This shouldn't happen with well-formed C++ code, but handle it gracefully
            if (currentClass != null && !foundClassEnd)
            {
                startIndex = lines.Length; // Reached end of file
            }
            
            return currentClass;
        }

        private CppMethod? ParseMethod(Match methodMatch, AccessSpecifier currentAccess, string originalMethodLine, string normalizedMethodLine, string className, string fileName)
        {
            // Extract return type from ORIGINAL line to preserve C++ formatting (spaces before * or &)
            string returnType = ExtractReturnTypeFromOriginalLine(originalMethodLine, methodMatch.Groups[4].Value);
            
            var method = new CppMethod
            {
                Name = methodMatch.Groups[4].Value,
                ReturnType = returnType,
                AccessSpecifier = currentAccess,
                IsVirtual = methodMatch.Groups[1].Success,
                IsStatic = methodMatch.Groups[2].Success,
                IsConst = methodMatch.Groups[5].Success,
                HasInlineImplementation = originalMethodLine.Contains("{"), // Use string matching for detection
                ClassName = className // Fix: Set the ClassName for proper method matching
            };
            

            // Check if it's a constructor or destructor
            method.IsConstructor = !method.Name.StartsWith("~") && method.Name == className && string.IsNullOrEmpty(methodMatch.Groups[3].Value.Trim());
            method.IsDestructor = method.Name.StartsWith("~");
            


            // Handle member initializer list for constructors
            if (method.IsConstructor && methodMatch.Groups[6].Success && !string.IsNullOrWhiteSpace(methodMatch.Groups[6].Value))
            {
                method.MemberInitializerList = ParseMemberInitializerList(methodMatch.Groups[6].Value.Trim());
            }
            


            if (method.HasInlineImplementation)
            {
                // Set TargetFileName for inline implementations (from header file)
                method.TargetFileName = fileName;
                
                // Extract just the method body content between braces
                var fullMethod = originalMethodLine;
                var openBrace = fullMethod.IndexOf('{');
                var closeBrace = fullMethod.LastIndexOf('}');
                
                if (openBrace >= 0 && closeBrace > openBrace)
                {
                    var methodBody = fullMethod.Substring(openBrace + 1, closeBrace - openBrace - 1);
                    // Replace tab characters with four spaces
                    methodBody = methodBody.Replace("\t", "    ");
                    
                    // Normalize indentation: find minimum indentation and remove it from all lines
                    method.InlineImplementation = NormalizeIndentation(methodBody);
                }
                else
                {
                    // No method body found in regex groups, use empty string
                    method.InlineImplementation = "";
                }
            }

            // Parse parameters - always use balanced parentheses extraction for robust parsing
            string parametersString = "";
            
            // Always use our balanced parentheses extractor for consistent results
            var methodName = methodMatch.Groups[4].Value;
            int methodNameIndex = originalMethodLine.IndexOf(methodName);
            if (methodNameIndex >= 0)
            {
                parametersString = ExtractBalancedParameters(originalMethodLine, methodNameIndex);
            }
            
            method.Parameters = ParseParameters(parametersString);

            // Check if it's pure virtual by checking the collected method line
            // We can't rely on lines[lineIndex] anymore since we collected multi-line declarations
            method.IsPureVirtual = originalMethodLine.Contains("= 0");

            return method;
        }

        private List<CppParameter> ParseParameters(string parametersString)
        {
            if (string.IsNullOrWhiteSpace(parametersString))
                return new List<CppParameter>();

            // Pre-process to remove completely commented-out parameters (like /* agrint &oldParameter,*/)
            // This handles cases where entire parameters are commented out, not just parameter comments
            parametersString = RemoveCommentedOutParameters(parametersString);

            // Use new parameter parser
            return _parameterParser.ParseParameters(parametersString);
        }

        private string CollectMultiLineMethodDeclaration(string[] lines, ref int lineIndex)
        {
            var currentLine = lines[lineIndex];
            

            
            // If the line doesn't look like the start of a method, return it as-is
            var trimmedLine = currentLine.Trim();
            if (!trimmedLine.Contains("virtual") && !trimmedLine.Contains("static") && !trimmedLine.Contains("("))
            {
                return currentLine;
            }
            
            // Special case: If line has parentheses but they appear to be in comments (after // or /*), treat as member
            if (trimmedLine.Contains("//") || trimmedLine.Contains("/*"))
            {
                var commentIndex = Math.Min(
                    trimmedLine.IndexOf("//") >= 0 ? trimmedLine.IndexOf("//") : int.MaxValue,
                    trimmedLine.IndexOf("/*") >= 0 ? trimmedLine.IndexOf("/*") : int.MaxValue
                );
                
                if (commentIndex < int.MaxValue)
                {
                    var codeBeforeComment = trimmedLine.Substring(0, commentIndex);
                    if (!codeBeforeComment.Contains("("))
                    {
                        // Parentheses are only in comments, not a method
                        return currentLine;
                    }
                }
            }
            
            var methodBuilder = new StringBuilder();
            methodBuilder.Append(currentLine);
            
            // If the line already ends with a semicolon, it's a declaration only
            if (currentLine.TrimEnd().EndsWith(";"))
            {
                return methodBuilder.ToString();
            }
            
            // Check if this is a single-line inline method (balanced braces on same line)
            int braceLevel = currentLine.Count(c => c == '{') - currentLine.Count(c => c == '}');
            if (braceLevel == 0 && currentLine.Contains("{") && currentLine.Contains("}"))
            {
                // Single-line inline method - return as-is without collecting more lines
                return methodBuilder.ToString();
            }
            
            bool insideMethodBody = braceLevel > 0;
            
            // If no opening brace, look for semicolon or opening brace
            if (braceLevel == 0)
            {
                // Look for the end of the method declaration (semicolon or opening brace)
                for (int i = lineIndex + 1; i < lines.Length; i++)
                {
                    var nextLine = lines[i];
                    
                    // Check if we're entering a method body
                    if (nextLine.Contains("{"))
                    {
                        insideMethodBody = true;
                    }
                    
                    // Use proper line breaks for method bodies, spaces for declarations
                    if (insideMethodBody)
                    {
                        methodBuilder.Append("\n" + nextLine);
                    }
                    else
                    {
                        methodBuilder.Append(" " + nextLine.Trim());
                    }
                    
                    braceLevel += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                    
                    // If we hit a semicolon and no braces, it's a declaration
                    if (nextLine.TrimEnd().EndsWith(";") && braceLevel == 0)
                    {
                        lineIndex = i;
                        break;
                    }
                    
                    // If we have braces, continue to collect until they're balanced
                    if (braceLevel > 0)
                    {
                        // Continue to next iteration to collect more lines
                        continue;
                    }
                    else if (braceLevel == 0 && nextLine.Contains("}"))
                    {
                        // Found the closing brace
                        lineIndex = i;
                        break;
                    }
                    
                    // Prevent infinite loops - if we hit a class boundary, stop
                    if (nextLine.Trim().StartsWith("class ") || nextLine.Trim() == "}" || nextLine.Trim() == "};")
                    {
                        break;
                    }
                }
            }
            else
            {
                // We already have an opening brace, collect until braces are balanced
                // Preserve exact formatting by using original line breaks
                for (int i = lineIndex + 1; i < lines.Length; i++)
                {
                    var nextLine = lines[i];
                    
                    // Add line break and preserve the original line exactly
                    methodBuilder.Append("\n" + nextLine);
                    
                    braceLevel += nextLine.Count(c => c == '{') - nextLine.Count(c => c == '}');
                    
                    if (braceLevel == 0)
                    {
                        // Found the matching closing brace
                        lineIndex = i;
                        break;
                    }
                    
                    // Enhanced boundary detection - be more careful about class boundaries
                    var trimmed = nextLine.Trim();
                    if (trimmed.StartsWith("class ") || trimmed.StartsWith("struct ") ||
                        (trimmed == "};" && braceLevel < 0) ||
                        (trimmed.StartsWith("private:") || trimmed.StartsWith("public:") || trimmed.StartsWith("protected:")))
                    {
                        // We've likely hit a class boundary before finding the method end
                        // Back up one line to not include the boundary marker
                        lineIndex = i - 1;
                        break;
                    }
                }
            }
            
            return methodBuilder.ToString();
        }

        private bool IsInterface(string[] lines, int startIndex)
        {
            // Look ahead to see if there are pure virtual methods
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Only consider "= 0;" at the end of method declarations, not in method bodies
                // Pure virtual methods end with "= 0;" and are function declarations
                if (line.EndsWith("= 0;") && 
                    (line.Contains("virtual") || 
                     (line.Contains("(") && line.Contains(")") && !line.Contains("{") && !line.Contains("."))))
                {

                    return true;
                }
                
                // Stop at the end of the class
                if (line.Contains("};") && line.Count(c => c == '}') > line.Count(c => c == '{'))
                    break;
            }

            return false;
        }

        private List<CppMemberInitializer> ParseMemberInitializerList(string initializerList)
        {
            var initializers = new List<CppMemberInitializer>();
            
            if (string.IsNullOrWhiteSpace(initializerList))
                return initializers;

            // Split by commas, but be careful with nested parentheses
            var parts = SplitMemberInitializers(initializerList);
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (string.IsNullOrEmpty(trimmedPart))
                    continue;

                // Parse "memberName(value)" or "memberName{value}"
                var match = Regex.Match(trimmedPart, @"(\w+)\s*[\(\{]([^\)\}]*?)[\)\}]");
                if (match.Success)
                {
                    initializers.Add(new CppMemberInitializer
                    {
                        MemberName = match.Groups[1].Value,
                        InitializationValue = match.Groups[2].Value.Trim()
                    });
                }
            }

            return initializers;
        }

        private List<string> SplitMemberInitializers(string initializerList)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var parenthesesLevel = 0;
            var bracesLevel = 0;

            foreach (char c in initializerList)
            {
                switch (c)
                {
                    case '(':
                        parenthesesLevel++;
                        current.Append(c);
                        break;
                    case ')':
                        parenthesesLevel--;
                        current.Append(c);
                        break;
                    case '{':
                        bracesLevel++;
                        current.Append(c);
                        break;
                    case '}':
                        bracesLevel--;
                        current.Append(c);
                        break;
                    case ',':
                        if (parenthesesLevel == 0 && bracesLevel == 0)
                        {
                            result.Add(current.ToString());
                            current.Clear();
                        }
                        else
                        {
                            current.Append(c);
                        }
                        break;
                    default:
                        current.Append(c);
                        break;
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        private string ExtractBalancedParameters(string methodLine, int startPos)
        {
            // Find the opening parenthesis and extract parameters with balanced parentheses
            int openParenIndex = methodLine.IndexOf('(', startPos);
            if (openParenIndex == -1) return "";

            int parenLevel = 0;
            int startParamIndex = openParenIndex + 1;
            bool inQuotes = false;
            char quoteChar = '\0';
            
            for (int i = openParenIndex; i < methodLine.Length; i++)
            {
                char c = methodLine[i];
                
                // Handle quotes
                if (!inQuotes && (c == '"' || c == '\''))
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (inQuotes && c == quoteChar)
                {
                    // Check if it's escaped
                    if (i > 0 && methodLine[i - 1] == '\\')
                    {
                        // Escaped quote, continue
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else if (!inQuotes)
                {
                    // Only count parentheses when not inside quotes
                    if (c == '(') parenLevel++;
                    else if (c == ')') 
                    {
                        parenLevel--;
                        if (parenLevel == 0)
                        {
                            // Found the matching closing parenthesis
                            return methodLine.Substring(startParamIndex, i - startParamIndex);
                        }
                    }
                }
            }
            
            // If we get here, parentheses weren't balanced - return what we have
            return methodLine.Substring(startParamIndex);
        }

        private (string parameters, string remaining) ExtractParametersAndRemaining(string methodLine, int startPos)
        {
            // Find the opening parenthesis and extract parameters with balanced parentheses
            int openParenIndex = methodLine.IndexOf('(', startPos);
            if (openParenIndex == -1) return ("", methodLine);

            int parenLevel = 0;
            int startParamIndex = openParenIndex + 1;
            
            for (int i = openParenIndex; i < methodLine.Length; i++)
            {
                char c = methodLine[i];
                if (c == '(') parenLevel++;
                else if (c == ')') 
                {
                    parenLevel--;
                    if (parenLevel == 0)
                    {
                        // Found the matching closing parenthesis
                        string parameters = methodLine.Substring(startParamIndex, i - startParamIndex);
                        string remaining = i + 1 < methodLine.Length ? methodLine.Substring(i + 1) : "";
                        return (parameters, remaining);
                    }
                }
            }
            
            // If we get here, parentheses weren't balanced - return what we have
            return (methodLine.Substring(startParamIndex), "");
        }

        private List<string> SplitParametersRespectingParentheses(string parametersString)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            int parenLevel = 0;
            int angleLevel = 0;
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < parametersString.Length; i++)
            {
                char c = parametersString[i];
                
                // Handle quotes
                if (!inQuotes && (c == '"' || c == '\''))
                {
                    inQuotes = true;
                    quoteChar = c;
                    current.Append(c);
                }
                else if (inQuotes && c == quoteChar)
                {
                    // Check if it's escaped
                    if (i > 0 && parametersString[i - 1] == '\\')
                    {
                        current.Append(c);
                    }
                    else
                    {
                        inQuotes = false;
                        current.Append(c);
                    }
                }
                else if (inQuotes)
                {
                    current.Append(c);
                }
                else
                {
                    // Not in quotes, handle brackets and commas
                    if (c == '(') parenLevel++;
                    else if (c == ')') parenLevel--;
                    else if (c == '<') angleLevel++;
                    else if (c == '>') angleLevel--;
                    else if (c == ',' && parenLevel == 0 && angleLevel == 0)
                    {
                        // This comma is a parameter separator
                        if (current.Length > 0)
                        {
                            result.Add(current.ToString().Trim());
                            current.Clear();
                        }
                        continue; // Don't add the comma to current
                    }
                    
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString().Trim());
            }

            return result;
        }

        // Comment and region parsing methods
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
                var line = lines[lookbackIndex];
                var trimmedLine = line.Trim();
                
                // Check for single line comments
                if (trimmedLine.StartsWith("//"))
                {
                    commentBlock.Insert(0, line); // Keep original indentation temporarily
                    lookbackIndex--;
                    continue;
                }
                
                // Check for end of multi-line comment
                if (trimmedLine.EndsWith("*/"))
                {
                    // Check if this is a pure comment line or a code line with postfix comment
                    // Pure comment: starts with //, /*, or * (continuation line)
                    // Code with postfix: has non-comment content before the /*
                    bool isPureCommentLine = trimmedLine.StartsWith("//") || 
                                            trimmedLine.StartsWith("/*") || 
                                            trimmedLine.StartsWith("*") ||
                                            !trimmedLine.Contains("/*"); // Multi-line comment continuation without /* on this line
                    
                    if (isPureCommentLine)
                    {
                        inMultiLineComment = true;
                        commentBlock.Insert(0, line); // Keep original indentation temporarily
                        
                        // If this line also starts the comment, we're done with this block
                        if (trimmedLine.StartsWith("/*"))
                        {
                            inMultiLineComment = false;
                            lookbackIndex--;
                            continue;
                        }
                        
                        lookbackIndex--;
                        continue;
                    }
                    else
                    {
                        // Line ends with */ but has code before /* 
                        // This is a code line with postfix comment - stop collecting
                        break;
                    }
                }
                
                // Check for start of multi-line comment
                if (inMultiLineComment && trimmedLine.StartsWith("/*"))
                {
                    commentBlock.Insert(0, line); // Keep original indentation temporarily
                    inMultiLineComment = false;
                    lookbackIndex--;
                    continue;
                }
                
                // Inside multi-line comment
                if (inMultiLineComment)
                {
                    commentBlock.Insert(0, line); // Keep original indentation temporarily
                    lookbackIndex--;
                    continue;
                }
                
                // Check for empty lines between comment blocks
                if (string.IsNullOrWhiteSpace(trimmedLine))
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
                        // Only continue if the peeked line is a PURE comment (not a member with postfix comment)
                        bool isPureComment = (peekLine.StartsWith("//")) || 
                                            (peekLine.StartsWith("/*") && peekLine.EndsWith("*/")) ||
                                            (peekLine.StartsWith("*") && peekLine.EndsWith("*/"));
                        
                        if (isPureComment)
                        {
                            // There are more comments, include the empty line
                            commentBlock.Insert(0, line);
                            lookbackIndex--;
                            continue;
                        }
                    }
                }
                
                // Not a comment line, stop collecting
                break;
            }
            
            // Process collected comments - remove minimum indentation while preserving relative indentation
            if (commentBlock.Any())
            {
                // Capture original indentation from first comment line
                var firstComment = commentBlock[0];
                originalIndentation = firstComment.Length - firstComment.TrimStart().Length;
                
                // Find minimum indentation from non-empty lines
                int minIndent = commentBlock
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Min(l => l.Length - l.TrimStart().Length);
                
                // Remove minimum indentation from all lines to preserve relative indentation
                comments.AddRange(commentBlock.Select(l => 
                    string.IsNullOrWhiteSpace(l) ? "" : (l.Length >= minIndent ? l.Substring(minIndent) : l)));
            }
            
            return (comments, originalIndentation);
        }

        private List<string> CollectPrecedingComments(string[] lines, int currentIndex)
        {
            var (comments, _) = CollectPrecedingCommentsWithIndentation(lines, currentIndex);
            return comments;
        }

        private string CollectMultiLineComment(string[] lines, int startLineIndex, string initialCommentText)
        {
            var commentBuilder = new StringBuilder();
            commentBuilder.Append(initialCommentText);

            // Look for the closing */ in subsequent lines
            for (int i = startLineIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                commentBuilder.Append(" " + line.Trim());
                
                if (line.Contains("*/"))
                {
                    // Found the end of the comment
                    break;
                }
            }
            
            return commentBuilder.ToString();
        }

        private string ExtractPostfixComment(string postfixText)
        {
            if (string.IsNullOrWhiteSpace(postfixText))
                return string.Empty;

            var trimmedText = postfixText.Trim();
            if (string.IsNullOrEmpty(trimmedText))
                return string.Empty;

            // Handle single-line comment
            if (trimmedText.StartsWith("//"))
            {
                return trimmedText;
            }

            // Handle multi-line comment that starts and ends on the same line
            if (trimmedText.StartsWith("/*") && trimmedText.Contains("*/"))
            {
                return trimmedText;
            }

            // Handle multi-line comment that starts but doesn't end on the same line
            if (trimmedText.StartsWith("/*") && !trimmedText.Contains("*/"))
            {
                return trimmedText; // Return the starting part, will be collected by calling code if needed
            }

            return string.Empty;
        }

        private (string regionStart, string regionEnd) ParseRegionMarkers(string[] lines, int startIndex, int endIndex)
        {
            string regionStart = string.Empty;
            string regionEnd = string.Empty;
            
            // Look for region start before the construct
            for (int i = startIndex - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var regionMatch = _pragmaRegionRegex.Match(line);
                if (regionMatch.Success && regionMatch.Groups[1].Value.Equals("region", StringComparison.OrdinalIgnoreCase))
                {
                    var description = regionMatch.Groups[2].Success ? regionMatch.Groups[2].Value.Trim() : string.Empty;
                    regionStart = $"//#region {description}".Trim();
                    break;
                }
                
                // Skip access specifiers when searching for regions
                if (line.Equals("public:", StringComparison.OrdinalIgnoreCase) ||
                    line.Equals("protected:", StringComparison.OrdinalIgnoreCase) ||
                    line.Equals("private:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                // If we hit non-empty, non-comment line, stop looking
                if (!line.StartsWith("//") && !line.StartsWith("/*") && !line.EndsWith("*/"))
                {
                    break;
                }
            }
            
            // Look for region end after the construct
            for (int i = endIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var regionMatch = _pragmaRegionRegex.Match(line);
                if (regionMatch.Success && regionMatch.Groups[1].Value.Equals("endregion", StringComparison.OrdinalIgnoreCase))
                {
                    var comment = regionMatch.Groups[2].Success ? regionMatch.Groups[2].Value.Trim() : string.Empty;
                    regionEnd = $"//#endregion {comment}".Trim();
                    break;
                }
                
                // If we hit a non-empty line that's not a comment, stop looking
                if (!line.StartsWith("//") && !line.StartsWith("/*") && !line.EndsWith("*/"))
                {
                    break;
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

        /// <summary>
        /// Parses structs from header file and returns them with original C++ syntax preserved
        /// </summary>
        [Obsolete("This method is deprecated. Use ParseHeaderFile instead and filter for IsStruct=true.")]
        public List<CppStruct> ParseStructsFromHeaderFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                return ParseStructsFromLines(lines);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing structs from header file {filePath}: {ex.Message}");
                return new List<CppStruct>();
            }
        }

        public List<CppStruct> ParseStructsFromLines(string[] lines)
        {
            var structs = new List<CppStruct>();
            int i = 0;
            
            while (i < lines.Length)
            {
                var foundStruct = ParseNextStructFromLines(lines, ref i);
                if (foundStruct != null)
                {
                    structs.Add(foundStruct);
                }
                else
                {
                    i++;
                }
            }
            
            return structs;
        }

        private CppStruct? ParseNextStructFromLines(string[] lines, ref int startIndex)
        {
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Skip empty lines and non-struct lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("#"))
                {
                    continue;
                }

                // Check for struct patterns
                CppStruct? structResult = null;
                
                // Exclude lines ending with semicolon that are variable declarations (e.g., "struct tm time;")
                // but not struct definitions (which end with "};")
                bool isVariableDeclaration = line.TrimEnd().EndsWith(";") && !line.TrimEnd().EndsWith("};");
                
                if (!isVariableDeclaration)
                {
                    // Pattern 1: struct MyStruct
                    var simpleMatch = _simpleStructRegex.Match(line);
                    if (simpleMatch.Success)
                    {
                        structResult = ParseSimpleStruct(lines, ref i, simpleMatch.Groups[1].Value);
                    }
                    
                    // Pattern 2: typedef struct
                    var typedefMatch = _typedefStructRegex.Match(line);
                    if (typedefMatch.Success)
                    {
                        structResult = ParseTypedefStruct(lines, ref i);
                    }
                    
                    // Pattern 3: typedef struct MyTag  
                    var typedefTagMatch = _typedefStructTagRegex.Match(line);
                    if (typedefTagMatch.Success)
                    {
                        structResult = ParseTypedefStructTag(lines, ref i, typedefTagMatch.Groups[1].Value);
                    }
                }

                if (structResult != null)
                {
                    startIndex = i + 1;
                    return structResult;
                }
            }
            
            startIndex = lines.Length;
            return null;
        }

        private CppStruct ParseSimpleStruct(string[] lines, ref int startIndex, string structName)
        {
            var structLines = new List<string>();
            var precedingComments = CollectPrecedingComments(lines, startIndex);
            
            // Collect the complete struct definition
            int braceCount = 0;
            bool foundOpenBrace = false;
            
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                structLines.Add(line);
                
                // Count braces to find the end
                foreach (char c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpenBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                    }
                }
                
                // Check for end of struct
                if (foundOpenBrace && braceCount == 0 && line.TrimEnd().EndsWith(";"))
                {
                    startIndex = i;
                    break;
                }
            }
            
            var cppStruct = new CppStruct
            {
                Name = structName,
                Type = StructType.Simple,
                OriginalDefinition = string.Join(Environment.NewLine, structLines).Trim(),
                PrecedingComments = precedingComments
            };
            
            ParseStructMembers(cppStruct, structLines.ToArray());
            return cppStruct;
        }

        private CppStruct ParseTypedefStruct(string[] lines, ref int startIndex)
        {
            var structLines = new List<string>();
            var precedingComments = CollectPrecedingComments(lines, startIndex);
            
            // Collect the complete typedef struct definition
            int braceCount = 0;
            bool foundOpenBrace = false;
            string structName = "";
            
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                structLines.Add(line);
                
                // Count braces
                foreach (char c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpenBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                    }
                }
                
                // Check for end with struct name
                if (foundOpenBrace && braceCount == 0)
                {
                    var nameMatch = _typedefNameRegex.Match(line);
                    if (nameMatch.Success)
                    {
                        structName = nameMatch.Groups[1].Value;
                        startIndex = i;
                        break;
                    }
                }
            }
            
            var cppStruct = new CppStruct
            {
                Name = structName,
                Type = StructType.Typedef,
                OriginalDefinition = string.Join(Environment.NewLine, structLines).Trim(),
                PrecedingComments = precedingComments
            };
            
            ParseStructMembers(cppStruct, structLines.ToArray());
            return cppStruct;
        }

        private CppStruct ParseTypedefStructTag(string[] lines, ref int startIndex, string tagName)
        {
            var structLines = new List<string>();
            var precedingComments = CollectPrecedingComments(lines, startIndex);
            
            // Collect the complete typedef struct tag definition
            int braceCount = 0;
            bool foundOpenBrace = false;
            string structName = "";
            
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                structLines.Add(line);
                
                // Count braces
                foreach (char c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpenBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                    }
                }
                
                // Check for end with struct name
                if (foundOpenBrace && braceCount == 0)
                {
                    var nameMatch = _typedefNameRegex.Match(line);
                    if (nameMatch.Success)
                    {
                        structName = nameMatch.Groups[1].Value;
                        startIndex = i;
                        break;
                    }
                }
            }
            
            var cppStruct = new CppStruct
            {
                Name = structName,
                Type = StructType.TypedefTag,
                OriginalDefinition = string.Join(Environment.NewLine, structLines).Trim(),
                PrecedingComments = precedingComments
            };
            
            ParseStructMembers(cppStruct, structLines.ToArray());
            return cppStruct;
        }

        /// <summary>
        /// Parses member fields from a struct definition
        /// </summary>
        private void ParseStructMembers(CppStruct cppStruct, string[] lines)
        {
            bool insideBraces = false;
            List<string> precedingComments = new List<string>();
            int braceDepth = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();
                
                // Track brace depth
                foreach (char c in trimmedLine)
                {
                    if (c == '{')
                    {
                        braceDepth++;
                        insideBraces = true;
                    }
                    else if (c == '}')
                    {
                        braceDepth--;
                    }
                }
                
                // Exit when we leave the struct body completely
                if (insideBraces && braceDepth == 0)
                {
                    break;
                }
                
                // Skip if not inside braces yet
                if (!insideBraces)
                    continue;
                
                // Remove brace characters for processing
                var contentLine = trimmedLine.Replace("{", "").Replace("}", "").Trim();
                
                // Collect comments
                if (contentLine.StartsWith("//") || contentLine.StartsWith("/*") || contentLine.StartsWith("*"))
                {
                    precedingComments.Add(trimmedLine); // Use trimmed version - indentation will be added during C# generation
                    continue;
                }
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(contentLine))
                {
                    if (precedingComments.Any())
                        precedingComments.Add(""); // Preserve empty lines in comment blocks
                    continue;
                }
                
                // Skip pragma region lines
                if (contentLine.Contains("#pragma region") || contentLine.Contains("#pragma endregion"))
                    continue;
                
                // Parse member field: type name; (but only at brace depth 1 - i.e., directly in struct body, not in methods)
                if (braceDepth == 1 && contentLine.Contains(";") && !contentLine.StartsWith("typedef"))
                {
                    var member = ParseStructMemberField(contentLine, precedingComments);
                    if (member != null)
                    {
                        cppStruct.Members.Add(member);
                    }
                    precedingComments.Clear();
                }
            }
        }

        /// <summary>
        /// Parses a single struct member field from a line like "bool MyBoolField;" or "agrint MyIntField; // comment"
        /// </summary>
        private CppMember? ParseStructMemberField(string line, List<string> precedingComments)
        {
            var cleanLine = line.Trim();
            
            // Check for postfix comment first
            string postfixComment = "";
            int commentIndex = cleanLine.IndexOf("//");
            if (commentIndex == -1)
                commentIndex = cleanLine.IndexOf("/*");
                
            if (commentIndex >= 0)
            {
                postfixComment = cleanLine.Substring(commentIndex).Trim();
                cleanLine = cleanLine.Substring(0, commentIndex).Trim();
            }
            
            // Now remove trailing semicolon after comment is extracted
            cleanLine = cleanLine.TrimEnd(';').Trim();
            
            // Parse type and name
            var parts = cleanLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;
            
            // Last part is the name, everything before is the type
            var name = parts[parts.Length - 1].TrimEnd('*', '&'); // Remove pointer/reference markers from name
            var type = string.Join(" ", parts.Take(parts.Length - 1));
            
            // Check if name has pointer/reference in it
            if (parts[parts.Length - 1].Contains("*") || parts[parts.Length - 1].Contains("&"))
            {
                type += parts[parts.Length - 1].Replace(name, "").Trim();
            }
            
            return new CppMember
            {
                Name = name,
                Type = type,
                AccessSpecifier = AccessSpecifier.Internal, // Struct members default to internal
                PrecedingComments = new List<string>(precedingComments),
                PostfixComment = postfixComment,
                IsStatic = false
            };
        }

        /// <summary>
        /// Determines if a line is a struct declaration that should be handled as a struct, not a class
        /// </summary>
        private bool IsStructDeclaration(string line)
        {
            var trimmedLine = line.Trim();
            
            // Check for the three struct patterns
            return _simpleStructRegex.IsMatch(trimmedLine) || 
                   _typedefStructRegex.IsMatch(trimmedLine) || 
                   _typedefStructTagRegex.IsMatch(trimmedLine);
        }
        
        /// <summary>
        /// Removes completely commented-out parameters from the parameter string
        /// Handles cases like "param1, /* oldParam, */ param2" -> "param1, param2"
        /// </summary>
        private string RemoveCommentedOutParameters(string parametersString)
        {
            var result = new StringBuilder();
            var i = 0;
            
            while (i < parametersString.Length)
            {
                // Check for start of multi-line comment
                if (i < parametersString.Length - 1 && parametersString[i] == '/' && parametersString[i + 1] == '*')
                {
                    var commentStart = i;
                    i += 2; // Skip /*
                    
                    // Find the end of the comment and capture its content
                    var commentContent = new StringBuilder();
                    while (i <= parametersString.Length - 2)
                    {
                        if (parametersString[i] == '*' && parametersString[i + 1] == '/')
                        {
                            i += 2; // Skip */
                            break;
                        }
                        commentContent.Append(parametersString[i]);
                        i++;
                    }
                    
                    // Only remove the comment if it contains a comma (indicating it's a commented-out parameter)
                    if (commentContent.ToString().Contains(','))
                    {
                        // This is likely a commented-out parameter, skip it entirely
                        // Also skip any trailing comma and whitespace after the commented-out parameter
                        while (i < parametersString.Length && (parametersString[i] == ',' || char.IsWhiteSpace(parametersString[i])))
                        {
                            i++;
                        }
                    }
                    else
                    {
                        // This is a parameter comment (no comma), preserve it
                        var commentLength = i - commentStart;
                        result.Append(parametersString.Substring(commentStart, commentLength));
                    }
                }
                else
                {
                    result.Append(parametersString[i]);
                    i++;
                }
            }
            
            return result.ToString().Trim();
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
                    while (i <= parameterText.Length - 2)
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
                    
                    // Add to clean content for parameter name detection (preserve spaces for word counting)
                    cleanContentSoFar.Append(ch);
                    
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
        /// <summary>
        /// Extracts the return type from the original method line to preserve C++ formatting (spaces before * or &).
        /// Uses the method name to identify where the return type ends.
        /// </summary>
        private string ExtractReturnTypeFromOriginalLine(string originalMethodLine, string methodName)
        {
            // Find the method name in the original line
            int methodNameIndex = originalMethodLine.IndexOf(methodName + "(");
            if (methodNameIndex == -1)
                methodNameIndex = originalMethodLine.IndexOf(methodName + " (");
            
            if (methodNameIndex == -1)
                return "void"; // Can't find method name, default to void

            // Extract everything before the method name
            string beforeMethodName = originalMethodLine.Substring(0, methodNameIndex).Trim();

            // Remove modifiers to get the return type
            beforeMethodName = System.Text.RegularExpressions.Regex.Replace(beforeMethodName, @"^\s*(virtual|static)\s+", "");
            beforeMethodName = System.Text.RegularExpressions.Regex.Replace(beforeMethodName, @"^\s*(virtual|static)\s+", ""); // Handle both virtual and static

            beforeMethodName = beforeMethodName.Trim();

            // If nothing left, it's a constructor/destructor (no return type)
            if (string.IsNullOrWhiteSpace(beforeMethodName) || beforeMethodName.StartsWith("~"))
                return "void";

            return beforeMethodName;
        }


        /// <summary>
        /// Normalizes spacing around pointers and references in method declarations to ensure consistent regex matching.
        /// Converts "Type *method" or "Type * method" to "Type* method" (no space before *, space after)
        /// Converts "Type &method" or "Type & method" to "Type& method" (no space before &, space after)
        /// This only applies to return types, not within parameter lists.
        /// </summary>
        private string NormalizePointerSpacing(string methodLine)
        {
            // Find the opening parenthesis to identify where parameters start
            int paramStartIndex = methodLine.IndexOf('(');
            if (paramStartIndex == -1)
                return methodLine; // No parameters, return as-is

            // Only normalize the part before the parameters (the return type and method name part)
            string beforeParams = methodLine.Substring(0, paramStartIndex);
            string fromParams = methodLine.Substring(paramStartIndex);

            // Normalize pointer spacing: "Type *" or "Type * " -> "Type* "
            // Normalize reference spacing: "Type &" or "Type & " -> "Type& "
            // Use regex to find type followed by optional spaces, then * or &, then method name
            beforeParams = System.Text.RegularExpressions.Regex.Replace(
                beforeParams,
                @"(\w+)\s+([\*&]+)\s*(\w+)",  // Type, spaces, pointer/ref, optional spaces, methodName
                "$1$2 $3"  // Type+pointer/ref, space, methodName
            );

            return beforeParams + fromParams;
        }
    }
}

