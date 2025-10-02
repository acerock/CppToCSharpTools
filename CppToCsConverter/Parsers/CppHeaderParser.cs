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
        private readonly Regex _methodRegex = new Regex(@"(?:(virtual)\s+)?(?:(static)\s+)?(?:(\w+(?:\s*\*|\s*&)?(?:::\w+)?)\s+)?([~]?\w+)\s*\(([^)]*)\)(?:\s*(const))?(?:\s*:\s*[^{]*)?(?:\s*=\s*0)?(?:\s*\{([^}]*)\})?", RegexOptions.Compiled);
        private readonly Regex _memberRegex = new Regex(@"^\s*(?:(static)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*=\s*([^;]+))?;\s*(?://.*)?$", RegexOptions.Compiled);
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
            int braceLevel = 0;
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

                // Track brace levels
                int openBraces = line.Count(c => c == '{');
                int closeBraces = line.Count(c => c == '}');
                braceLevel += openBraces - closeBraces;
                

                
                if (braceLevel <= 0 && closeBraces > 0)
                {
                    // End of class - we've closed all braces and we're back to level 0
;
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
                        Type = memberMatch.Groups[2].Value.Trim(),
                        Name = memberMatch.Groups[3].Value,
                        AccessSpecifier = currentAccess,
                        IsStatic = memberMatch.Groups[1].Success
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
                IsConst = methodMatch.Groups[6].Success,
                HasInlineImplementation = methodMatch.Groups[7].Success
            };
            

            


            // Check if it's a constructor or destructor
            method.IsConstructor = !method.Name.StartsWith("~") && method.Name == className && string.IsNullOrEmpty(methodMatch.Groups[3].Value.Trim());
            method.IsDestructor = method.Name.StartsWith("~");

            if (method.HasInlineImplementation)
            {
                method.InlineImplementation = methodMatch.Groups[7].Value;
            }

            // Parse parameters
            var parametersString = methodMatch.Groups[5].Value;
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

            var paramParts = parametersString.Split(',');
            
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
            
            // If the line already ends with a semicolon or brace, it's complete
            if (currentLine.TrimEnd().EndsWith(";") || currentLine.TrimEnd().EndsWith("}"))
            {
                return methodBuilder.ToString();
            }
            
            // Look for the end of the method declaration
            for (int i = lineIndex + 1; i < lines.Length; i++)
            {
                var nextLine = lines[i];
                methodBuilder.Append(" " + nextLine.Trim());
                
                // Check if we've reached the end of the method declaration
                if (nextLine.TrimEnd().EndsWith(";") || nextLine.TrimEnd().EndsWith("}"))
                {
                    lineIndex = i; // Update the line index to skip processed lines
                    break;
                }
                
                // Prevent infinite loops - if we hit a class boundary, stop
                if (nextLine.Trim().StartsWith("class ") || nextLine.Trim() == "}" || nextLine.Trim() == "};")
                {
                    break;
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
    }
}