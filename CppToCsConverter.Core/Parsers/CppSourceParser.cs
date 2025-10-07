using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public CppSourceParser(ILogger? logger = null)
        {
            _logger = logger ?? new ConsoleLogger();
        }

        public (List<CppMethod> Methods, List<CppStaticMemberInit> StaticInits) ParseSourceFile(string filePath)
        {
            var methods = new List<CppMethod>();
            var staticInits = new List<CppStaticMemberInit>();
            
            try
            {
                var content = File.ReadAllText(filePath);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Parse method implementations using the original approach
                methods.AddRange(ParseMethodImplementations(content, fileName));
                
                // Add comments and regions to the parsed methods
                AddCommentsAndRegionsToMethods(lines, methods);
                
                // Parse static member initializations
                staticInits.AddRange(ParseStaticMemberInitializations(content));
                
                return (methods, staticInits);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing source file {filePath}: {ex.Message}");
                return (methods, staticInits);
            }
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
    }
}