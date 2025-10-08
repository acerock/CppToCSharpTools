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
    public class CppHeaderParser
    {
        private readonly ILogger _logger;
        private readonly Regex _classRegex = new Regex(@"(?:class|struct)\s+(?:__declspec\s*\([^)]+\)\s+)?(\w+)(?:\s*:\s*(?:public|private|protected)\s+(\w+))?", RegexOptions.Compiled);
        private readonly Regex _methodRegex = new Regex(@"(?:(virtual)\s+)?(?:(static)\s+)?(?:(\w+(?:\s*\*|\s*&)?(?:::\w+)?)\s+)?([~]?\w+)\s*\(.*?\)(?:\s*(const))?(?:\s*:\s*([^{]*))?(?:\s*=\s*0)?(?:\s*\{.*?\})?", RegexOptions.Compiled | RegexOptions.Singleline);
        private readonly Regex _memberRegex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*(\d*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(?://.*)?$", RegexOptions.Compiled);
        private readonly Regex _accessSpecifierRegex = new Regex(@"^(private|protected|public)\s*:", RegexOptions.Compiled);
        private readonly Regex _pragmaRegionRegex = new Regex(@"^\s*#pragma\s+(region|endregion)(?:\s+(.*))?$", RegexOptions.Compiled);
        private readonly Regex _defineRegex = new Regex(@"^\s*#define\s+(\w+)(?:\s+(.*))?$", RegexOptions.Compiled);
        
        // Struct parsing regex patterns for the three types
        private readonly Regex _simpleStructRegex = new Regex(@"^\s*struct\s+(\w+)\s*$", RegexOptions.Compiled);
        private readonly Regex _typedefStructRegex = new Regex(@"^\s*typedef\s+struct\s*$", RegexOptions.Compiled);
        private readonly Regex _typedefStructTagRegex = new Regex(@"^\s*typedef\s+struct\s+(\w+)\s*$", RegexOptions.Compiled);
        private readonly Regex _typedefNameRegex = new Regex(@"^\s*}\s*(\w+)\s*;\s*$", RegexOptions.Compiled);

        public CppHeaderParser(ILogger? logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
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
                    // Associate collected defines with this class
                    foundClass.HeaderDefines.AddRange(headerDefines);
                    classes.Add(foundClass);
                }
                else
                {
                    i++;
                }
            }
            
            return classes;
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
                    // Collect comments before the define
                    var precedingComments = CollectPrecedingComments(lines, i);
                    
                    var define = new CppDefine
                    {
                        Name = defineMatch.Groups[1].Value,
                        Value = defineMatch.Groups[2].Success ? defineMatch.Groups[2].Value.Trim() : string.Empty,
                        FullDefinition = line,
                        PrecedingComments = precedingComments,
                        SourceFileName = fileName
                    };
                    
                    defines.Add(define);
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

                // Check for class declaration (but skip struct declarations that should be handled as structs)
                var classMatch = _classRegex.Match(line);
                if (classMatch.Success && !inClass && !IsStructDeclaration(line))
                {
                    // Collect comments before the class declaration
                    var precedingComments = CollectPrecedingComments(lines, i);

                    currentClass = new CppClass
                    {
                        Name = classMatch.Groups[1].Value,
                        IsPublicExport = line.Contains("__declspec(dllexport)"),
                        PrecedingComments = precedingComments
                    };

                    if (classMatch.Groups[2].Success)
                    {
                        currentClass.BaseClasses.Add(classMatch.Groups[2].Value);
                    }

                    // Check if it's an interface (contains pure virtual methods)
                    currentClass.IsInterface = IsInterface(lines, i);
                    
                    currentAccess = currentClass.DefaultAccessSpecifier;
                    inClass = true;
                    

                    continue;
                }

                if (!inClass || currentClass == null)
                    continue;

                // Check for class ending pattern
                if (line == "};" || (line == "}" && (i + 1 >= lines.Length || !lines[i + 1].Trim().StartsWith("else"))))
                {
                    // This is likely the class ending brace
                    startIndex = i + 1;
                    foundClassEnd = true;
                    break;
                }

                // Check for access specifiers
                var accessMatch = _accessSpecifierRegex.Match(line);
                if (accessMatch.Success)
                {
                    Enum.TryParse<AccessSpecifier>(accessMatch.Groups[1].Value, true, out currentAccess);
                    continue;
                }

                // Skip lines that are clearly inside method bodies (contain return, if, etc.)
                // But don't skip method declarations that start with a method name and have opening braces
                if (line.Trim().StartsWith("return ") || line.Trim().StartsWith("if ") || 
                    (line.Contains("{") && !line.Contains("}") && !line.Contains("(") && !line.Trim().StartsWith("~")))
                    continue;

                // Parse methods (handle multi-line declarations)
                var originalIndex = i;
                var methodLine = CollectMultiLineMethodDeclaration(lines, ref i);
                var methodMatch = _methodRegex.Match(methodLine);
                if (methodMatch.Success)
                {
                    try
                    {
                        var method = ParseMethod(methodMatch, currentAccess, methodLine, currentClass.Name, fileName);
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

                // Parse members - only if it looks like a proper member declaration
                var memberMatch = _memberRegex.Match(line);
                if (memberMatch.Success && !line.TrimStart().StartsWith("return ") && !line.TrimStart().StartsWith("if ") && !line.TrimStart().StartsWith("for ") && !line.TrimStart().StartsWith("while "))
                {
                    // Collect comments and region markers for member
                    var precedingComments = CollectPrecedingComments(lines, i);
                    var (regionStart, regionEnd) = ParseRegionMarkers(lines, i, i);
                    
                    var member = new CppMember
                    {
                        Type = memberMatch.Groups[3].Value.Trim(),
                        Name = memberMatch.Groups[4].Value,
                        AccessSpecifier = currentAccess,
                        IsStatic = memberMatch.Groups[1].Success,
                        IsArray = memberMatch.Groups[5].Success,
                        ArraySize = memberMatch.Groups[5].Success ? memberMatch.Groups[5].Value : string.Empty,
                        PrecedingComments = precedingComments,
                        RegionStart = regionStart,
                        RegionEnd = regionEnd
                    };
                    currentClass.Members.Add(member);
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

        private CppMethod? ParseMethod(Match methodMatch, AccessSpecifier currentAccess, string collectedMethodLine, string className, string fileName)
        {
            var method = new CppMethod
            {
                Name = methodMatch.Groups[4].Value,
                ReturnType = methodMatch.Groups[3].Success ? methodMatch.Groups[3].Value.Trim() : "void",
                AccessSpecifier = currentAccess,
                IsVirtual = methodMatch.Groups[1].Success,
                IsStatic = methodMatch.Groups[2].Success,
                IsConst = methodMatch.Groups[5].Success,
                HasInlineImplementation = collectedMethodLine.Contains("{"), // Use string matching for detection
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
                var fullMethod = collectedMethodLine;
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
            int methodNameIndex = collectedMethodLine.IndexOf(methodName);
            if (methodNameIndex >= 0)
            {
                parametersString = ExtractBalancedParameters(collectedMethodLine, methodNameIndex);
            }
            
            method.Parameters = ParseParameters(parametersString);

            // Check if it's pure virtual by checking the collected method line
            // We can't rely on lines[lineIndex] anymore since we collected multi-line declarations
            method.IsPureVirtual = collectedMethodLine.Contains("= 0");

            return method;
        }

        private List<CppParameter> ParseParameters(string parametersString)
        {
            var parameters = new List<CppParameter>();
            
            if (string.IsNullOrWhiteSpace(parametersString))
                return parameters;

            var paramParts = SplitParametersRespectingParentheses(parametersString);
            
            foreach (var part in paramParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var parameter = new CppParameter();
                
                // Check for default value
                var defaultIndex = trimmed.LastIndexOf('=');
                if (defaultIndex > 0)
                {
                    parameter.DefaultValue = trimmed.Substring(defaultIndex + 1).Trim();
                    trimmed = trimmed.Substring(0, defaultIndex).Trim();
                }

                // Parse const, type, reference/pointer, and name
                var constMatch = Regex.Match(trimmed, @"^(const\s+)?(.+?)\s*([&*]*)\s+(\w+)$");
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
                    var words = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 2)
                    {
                        parameter.Type = string.Join(" ", words.Take(words.Length - 1));
                        parameter.Name = words.Last();
                    }
                }

                parameters.Add(parameter);
            }

            return parameters;
        }

        private string CollectMultiLineMethodDeclaration(string[] lines, ref int lineIndex)
        {
            var currentLine = lines[lineIndex];
            
            // If the line doesn't look like the start of a method, return it as-is
            if (!currentLine.Trim().Contains("virtual") && !currentLine.Trim().Contains("static") && !currentLine.Trim().Contains("("))
            {
                return currentLine;
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
        public List<CppStruct> ParseStructsFromHeaderFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                return ParseAllStructsFromLines(lines);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing structs from header file {filePath}: {ex.Message}");
                return new List<CppStruct>();
            }
        }

        private List<CppStruct> ParseAllStructsFromLines(string[] lines)
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
            
            return new CppStruct
            {
                Name = structName,
                Type = StructType.Simple,
                OriginalDefinition = string.Join(Environment.NewLine, structLines).Trim(),
                PrecedingComments = precedingComments
            };
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
            
            return new CppStruct
            {
                Name = structName,
                Type = StructType.Typedef,
                OriginalDefinition = string.Join(Environment.NewLine, structLines).Trim(),
                PrecedingComments = precedingComments
            };
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
            
            return new CppStruct
            {
                Name = structName,
                Type = StructType.TypedefTag,
                OriginalDefinition = string.Join(Environment.NewLine, structLines).Trim(),
                PrecedingComments = precedingComments
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
    }
}