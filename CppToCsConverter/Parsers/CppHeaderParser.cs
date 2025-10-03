using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Models;

namespace CppToCsConverter.Parsers
{
    public class CppHeaderParser
    {
        private readonly Regex _classRegex = new Regex(@"(?:class|struct)\s+(?:__declspec\s*\([^)]+\)\s+)?(\w+)(?:\s*:\s*(?:public|private|protected)\s+(\w+))?", RegexOptions.Compiled);
        private readonly Regex _methodRegex = new Regex(@"(?:(virtual)\s+)?(?:(static)\s+)?(?:(\w+(?:\s*\*|\s*&)?(?:::\w+)?)\s+)?([~]?\w+)\s*\(.*?\)(?:\s*(const))?(?:\s*:\s*([^{]*))?(?:\s*=\s*0)?(?:\s*\{.*?\})?", RegexOptions.Compiled | RegexOptions.Singleline);
        private readonly Regex _memberRegex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*(\d*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(?://.*)?$", RegexOptions.Compiled);
        private readonly Regex _accessSpecifierRegex = new Regex(@"^(private|protected|public)\s*:", RegexOptions.Compiled);

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
                Console.WriteLine($"Error parsing header file {filePath}: {ex.Message}");
                return new List<CppClass>();
            }
        }

        private List<CppClass> ParseAllClassesFromLines(string[] lines, string fileName)
        {
            var classes = new List<CppClass>();
            int i = 0;
            
            while (i < lines.Length)
            {
                var foundClass = ParseNextClassFromLines(lines, ref i);
                if (foundClass != null)
                {
                    classes.Add(foundClass);
                }
                else
                {
                    i++;
                }
            }
            
            return classes;
        }

        private CppClass? ParseNextClassFromLines(string[] lines, ref int startIndex)
        {
            CppClass? currentClass = null;
            AccessSpecifier currentAccess = AccessSpecifier.Private;
            bool inClass = false;
            bool foundClassEnd = false;

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("/*"))
                    continue;

                // Check for class declaration
                var classMatch = _classRegex.Match(line);
                if (classMatch.Success && !inClass)
                {

                    currentClass = new CppClass
                    {
                        Name = classMatch.Groups[1].Value,
                        IsPublicExport = line.Contains("__declspec(dllexport)")
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
                var methodLine = CollectMultiLineMethodDeclaration(lines, ref i);
                var methodMatch = _methodRegex.Match(methodLine);
                if (methodMatch.Success)
                {
                    try
                    {
                        var method = ParseMethod(methodMatch, currentAccess, methodLine, currentClass.Name);
                        if (method != null)
                        {

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
                    var member = new CppMember
                    {
                        Type = memberMatch.Groups[3].Value.Trim(),
                        Name = memberMatch.Groups[4].Value,
                        AccessSpecifier = currentAccess,
                        IsStatic = memberMatch.Groups[1].Success,
                        IsArray = memberMatch.Groups[5].Success,
                        ArraySize = memberMatch.Groups[5].Success ? memberMatch.Groups[5].Value : string.Empty
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

        private CppMethod? ParseMethod(Match methodMatch, AccessSpecifier currentAccess, string collectedMethodLine, string className)
        {
            var method = new CppMethod
            {
                Name = methodMatch.Groups[4].Value,
                ReturnType = methodMatch.Groups[3].Success ? methodMatch.Groups[3].Value.Trim() : "void",
                AccessSpecifier = currentAccess,
                IsVirtual = methodMatch.Groups[1].Success,
                IsStatic = methodMatch.Groups[2].Success,
                IsConst = methodMatch.Groups[5].Success,
                HasInlineImplementation = collectedMethodLine.Contains("{") // Use string matching for detection
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
                // Extract just the method body content between braces
                var fullMethod = collectedMethodLine;
                var openBrace = fullMethod.IndexOf('{');
                var closeBrace = fullMethod.LastIndexOf('}');
                
                if (openBrace >= 0 && closeBrace > openBrace)
                {
                    var methodBody = fullMethod.Substring(openBrace + 1, closeBrace - openBrace - 1);
                    // Replace tab characters with four spaces and trim leading/trailing whitespace
                    method.InlineImplementation = methodBody.Replace("\t", "    ").Trim();
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
            
            // If the line has an opening brace, we need to collect until the matching closing brace
            int braceLevel = currentLine.Count(c => c == '{') - currentLine.Count(c => c == '}');
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
    }
}