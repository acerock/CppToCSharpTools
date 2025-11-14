using System;
using System.Linq;
using System.Text;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Core.Utils
{
    /// <summary>
    /// Manages indentation for comments and method bodies in C# code generation.
    /// Provides consistent and context-aware indentation based on C# structure nesting.
    /// </summary>
    public static class IndentationManager
    {
        /// <summary>
        /// Standard indentation unit (4 spaces)
        /// </summary>
        public const string IndentUnit = "    ";

        /// <summary>
        /// Indentation levels for different C# constructs (file-scoped namespaces)
        /// </summary>
        public static class Levels
        {
            public const int FileScope = 0;      // File-scoped namespace (no indentation)
            public const int Class = 0;          // class/interface declaration (0 spaces)
            public const int ClassMember = 1;    // fields, properties, methods (4 spaces)
            public const int MethodBody = 2;     // method implementation (8 spaces)
        }

        /// <summary>
        /// Calculates the original indentation level from a text block by examining the first non-empty line
        /// </summary>
        /// <param name="textBlock">The text block to analyze</param>
        /// <returns>Number of leading spaces in the first non-empty line</returns>
        public static int DetectOriginalIndentation(string textBlock)
        {
            if (string.IsNullOrEmpty(textBlock))
                return 0;

            var lines = textBlock.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var firstNonEmptyLine = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            
            if (firstNonEmptyLine == null)
                return 0;

            // Count leading spaces before first printable character
            int leadingSpaces = 0;
            foreach (char c in firstNonEmptyLine)
            {
                if (c == ' ')
                    leadingSpaces++;
                else if (c == '\t')
                    leadingSpaces += 4; // Convert tabs to spaces (standard 4-space tab)
                else
                    break;
            }

            return leadingSpaces;
        }

        /// <summary>
        /// Reindents a block of text to a target indentation level, preserving relative indentation
        /// </summary>
        /// <param name="textBlock">The text block to reindent</param>
        /// <param name="originalIndentation">Original indentation level of the block</param>
        /// <param name="targetLevel">Target indentation level (using Levels constants)</param>
        /// <returns>Reindented text block</returns>
        public static string ReindentBlock(string textBlock, int originalIndentation, int targetLevel)
        {
            if (string.IsNullOrEmpty(textBlock))
                return string.Empty;

            var targetIndentation = GetIndentationForLevel(targetLevel);
            var lines = textBlock.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var result = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (i > 0)
                    result.Append("\n");

                if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line - add target indentation 
                    result.Append(GetIndentationForLevel(targetLevel));
                }
                else
                {
                    // Calculate relative indentation by removing original base indentation
                    var trimmedLine = line.TrimStart(' ', '\t');
                    var lineIndentation = line.Length - trimmedLine.Length;
                    var relativeIndentation = Math.Max(0, lineIndentation - originalIndentation);
                    
                    // Apply target indentation plus relative indentation
                    var finalIndentation = targetIndentation + new string(' ', relativeIndentation);
                    result.Append(finalIndentation + trimmedLine);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets the indentation string for a specific level
        /// </summary>
        /// <param name="level">Indentation level</param>
        /// <returns>Indentation string (spaces)</returns>
        public static string GetIndentationForLevel(int level)
        {
            return new string(' ', level * 4);
        }

        /// <summary>
        /// Reindents comments for a method at the appropriate level based on method context
        /// </summary>
        /// <param name="comments">List of comment lines</param>
        /// <param name="originalIndentation">Original indentation of comments in source</param>
        /// <returns>Properly indented comment block</returns>
        public static string ReindentMethodComments(System.Collections.Generic.List<string> comments, int originalIndentation)
        {
            if (!comments.Any())
                return string.Empty;

            var commentBlock = string.Join("\n", comments);
            return ReindentBlock(commentBlock, originalIndentation, Levels.ClassMember);
        }

        /// <summary>
        /// Reindents method body for proper C# formatting
        /// </summary>
        /// <param name="methodBody">Method body content</param>
        /// <param name="originalIndentation">Original indentation of method body in source</param>
        /// <returns>Properly indented method body</returns>
        public static string ReindentMethodBody(string methodBody, int originalIndentation)
        {
            if (string.IsNullOrEmpty(methodBody))
                return string.Empty;

            var reindented = ReindentBlock(methodBody, originalIndentation, Levels.MethodBody);
            return CleanupMultipleEmptyLines(reindented);
        }
        
        /// <summary>
        /// Removes excessive consecutive empty lines, keeping only single empty lines for readability
        /// </summary>
        /// <param name="text">Text to clean up</param>
        /// <returns>Cleaned up text with single empty lines</returns>
        private static string CleanupMultipleEmptyLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var cleanedLines = new List<string>();
            var previousLineEmpty = false;
            
            foreach (var line in lines)
            {
                var isCurrentLineEmpty = string.IsNullOrWhiteSpace(line);
                
                if (isCurrentLineEmpty)
                {
                    // Only keep empty line if previous line wasn't empty
                    if (!previousLineEmpty)
                    {
                        cleanedLines.Add(string.Empty); // Add truly empty line
                    }
                    // Skip multiple consecutive empty lines
                }
                else
                {
                    // Non-empty line, add it as is
                    cleanedLines.Add(line);
                }
                
                previousLineEmpty = isCurrentLineEmpty;
            }
            
            return string.Join("\n", cleanedLines);
        }

        /// <summary>
        /// Determines if a method body is a one-liner (opening and closing braces on same line)
        /// </summary>
        /// <param name="methodBody">Method body to check</param>
        /// <returns>True if it's a one-liner method</returns>
        public static bool IsOneLinerMethod(string methodBody)
        {
            if (string.IsNullOrEmpty(methodBody))
                return false;

            var trimmed = methodBody.Trim();
            
            // Check if it starts with { and ends with } and doesn't contain line breaks in between
            return trimmed.StartsWith("{") && 
                   trimmed.EndsWith("}") && 
                   !trimmed.Substring(1, trimmed.Length - 2).Contains('\n') &&
                   !trimmed.Substring(1, trimmed.Length - 2).Contains('\r');
        }
    }
}