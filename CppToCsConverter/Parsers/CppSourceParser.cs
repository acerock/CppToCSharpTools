using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CppToCsConverter.Models;

namespace CppToCsConverter.Parsers
{
    public class CppSourceParser
    {
        private readonly Regex _methodImplementationRegex = new Regex(
            @"(?:(\w+(?:\s*\*|\s*&)?)\s+)?(\w+)\s*::\s*([~]?\w+)\s*\(([^)]*)\)(?:\s*(const))?\s*\{", 
            RegexOptions.Compiled | RegexOptions.Multiline);
        
        private readonly Regex _staticMemberInitRegex = new Regex(
            @"(?:(\w+)\s+)?(\w+)\s*::\s*(\w+)\s*=\s*([^;]+);", 
            RegexOptions.Compiled);

        public List<CppMethod> ParseSourceFile(string filePath)
        {
            var methods = new List<CppMethod>();
            
            try
            {
                var content = File.ReadAllText(filePath);
                
                // Parse method implementations
                methods.AddRange(ParseMethodImplementations(content));
                
                // Parse static member initializations
                ParseStaticMemberInitializations(content);
                
                return methods;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing source file {filePath}: {ex.Message}");
                return methods;
            }
        }

        private List<CppMethod> ParseMethodImplementations(string content)
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

                methods.Add(method);
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
                
                // In implementation, no default values should be present
                // Parse const, type, reference/pointer, and name
                var constMatch = Regex.Match(trimmed, @"^(const\s+)?(.+?)(\s*[&*])?\s+(\w+)$");
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
            
            for (int i = 0; i < parametersString.Length; i++)
            {
                char c = parametersString[i];
                
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
                        return methodBody.Trim();
                    }
                }
            }
            
            return string.Empty;
        }

        private void ParseStaticMemberInitializations(string content)
        {
            var matches = _staticMemberInitRegex.Matches(content);
            
            foreach (Match match in matches)
            {
                // This would be used to track static member initializations
                // For now, we'll just identify them
                var type = match.Groups[1].Success ? match.Groups[1].Value : "auto";
                var className = match.Groups[2].Value;
                var memberName = match.Groups[3].Value;
                var initValue = match.Groups[4].Value.Trim();
                
                Console.WriteLine($"Found static member initialization: {className}::{memberName} = {initValue}");
            }
        }
    }
}