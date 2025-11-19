using System.Text;

namespace CppToCsConverter.Core.Parsers.ParameterParsing;

/// <summary>
/// Splits a C++ parameter list into individual parameter blocks.
/// Respects comment context, parentheses depth, and angle bracket depth (templates).
/// </summary>
public class ParameterBlockSplitter : IParameterBlockSplitter
{
    public List<ParameterBlock> SplitIntoBlocks(string parameterListText)
    {
        if (string.IsNullOrWhiteSpace(parameterListText))
            return new List<ParameterBlock>();

        var blocks = new List<ParameterBlock>();
        var currentBlock = new StringBuilder();
        var blockIndex = 0;
        
        var inCStyleComment = false;
        var inCppStyleComment = false;
        var parenthesesDepth = 0;
        var angleBracketDepth = 0;
        var inString = false;
        var escapeNext = false;
        
        var currentLineStart = 0; // Track where current line starts
        var blockStartsOnNewLine = false;
        var blockLeadingIndent = 0;
        var seenNonWhitespaceInBlock = false;

        for (var i = 0; i < parameterListText.Length; i++)
        {
            var ch = parameterListText[i];
            var nextCh = i + 1 < parameterListText.Length ? parameterListText[i + 1] : '\0';

            // Track newlines for line break detection
            if (ch == '\n')
            {
                currentBlock.Append(ch);
                
                // C++ style comment ends at newline
                if (inCppStyleComment)
                {
                    inCppStyleComment = false;
                }
                
                currentLineStart = i + 1;
                
                // If we haven't seen non-whitespace in this block yet, next content starts on new line
                if (!seenNonWhitespaceInBlock)
                {
                    blockStartsOnNewLine = true;
                }
                continue;
            }

            // Handle string literals (to ignore commas and quotes in strings)
            if (inString)
            {
                currentBlock.Append(ch);
                if (escapeNext)
                {
                    escapeNext = false;
                }
                else if (ch == '\\')
                {
                    escapeNext = true;
                }
                else if (ch == '"')
                {
                    inString = false;
                }
                continue;
            }

            // Detect string start
            if (ch == '"' && !inCStyleComment && !inCppStyleComment)
            {
                inString = true;
                currentBlock.Append(ch);
                continue;
            }

            // Handle C++ style comments
            if (inCppStyleComment)
            {
                currentBlock.Append(ch);
                // C++ comment ends at newline (already handled above)
                continue;
            }

            // Detect C++ comment start
            if (!inCStyleComment && ch == '/' && nextCh == '/')
            {
                inCppStyleComment = true;
                currentBlock.Append(ch);
                continue;
            }

            // Handle C-style comments
            if (inCStyleComment)
            {
                currentBlock.Append(ch);
                if (ch == '*' && nextCh == '/')
                {
                    currentBlock.Append(nextCh);
                    i++; // Skip the '/'
                    inCStyleComment = false;
                }
                continue;
            }

            // Detect C-style comment start
            if (ch == '/' && nextCh == '*')
            {
                inCStyleComment = true;
                currentBlock.Append(ch);
                continue;
            }

            // Track parentheses depth (for default values like func(1, 2))
            if (ch == '(')
            {
                parenthesesDepth++;
                currentBlock.Append(ch);
                continue;
            }

            if (ch == ')')
            {
                parenthesesDepth--;
                currentBlock.Append(ch);
                continue;
            }

            // Track angle bracket depth (for templates like vector<int, int>)
            if (ch == '<')
            {
                angleBracketDepth++;
                currentBlock.Append(ch);
                continue;
            }

            if (ch == '>')
            {
                angleBracketDepth--;
                currentBlock.Append(ch);
                continue;
            }

            // Track leading indentation
            if (!seenNonWhitespaceInBlock && (ch == ' ' || ch == '\t'))
            {
                if (blockStartsOnNewLine)
                {
                    blockLeadingIndent++;
                }
                currentBlock.Append(ch);
                continue;
            }

            // First non-whitespace character in block
            if (!seenNonWhitespaceInBlock && ch != ' ' && ch != '\t' && ch != '\n' && ch != '\r')
            {
                seenNonWhitespaceInBlock = true;
            }

            // Check if this is a parameter separator comma
            if (ch == ',' && parenthesesDepth == 0 && angleBracketDepth == 0 && !inCppStyleComment)
            {
                // Look ahead after the comma to determine what belongs to this parameter vs the next
                // Rules:
                // 1. Single-line comment (// ...) always belongs to previous parameter (terminated by linebreak)
                // 2. Multi-line comment (/* ... */) followed by only whitespace and linebreak belongs to previous
                // 3. Multi-line comment (/* ... */) followed by non-whitespace belongs to NEXT parameter
                
                var lookAheadIndex = i + 1;
                var trailingContent = new StringBuilder();
                var consumedNewlineInTrailing = false;
                
                // Skip initial whitespace after comma
                while (lookAheadIndex < parameterListText.Length && 
                       (parameterListText[lookAheadIndex] == ' ' || parameterListText[lookAheadIndex] == '\t'))
                {
                    trailingContent.Append(parameterListText[lookAheadIndex]);
                    lookAheadIndex++;
                }
                
                if (lookAheadIndex < parameterListText.Length)
                {
                    var lookAheadCh = parameterListText[lookAheadIndex];
                    var nextLookAheadCh = lookAheadIndex + 1 < parameterListText.Length ? 
                        parameterListText[lookAheadIndex + 1] : '\0';
                    
                    // Check for single-line comment
                    if (lookAheadCh == '/' && nextLookAheadCh == '/')
                    {
                        trailingContent.Append(lookAheadCh);
                        trailingContent.Append(nextLookAheadCh);
                        lookAheadIndex += 2;
                        
                        // Include everything until newline
                        while (lookAheadIndex < parameterListText.Length && parameterListText[lookAheadIndex] != '\n')
                        {
                            trailingContent.Append(parameterListText[lookAheadIndex]);
                            lookAheadIndex++;
                        }
                        
                        // Include the newline if present
                        if (lookAheadIndex < parameterListText.Length)
                        {
                            trailingContent.Append(parameterListText[lookAheadIndex]);
                            lookAheadIndex++;
                            consumedNewlineInTrailing = true;
                        }
                        
                        // Single-line comment always belongs to previous parameter
                        currentBlock.Append(trailingContent);
                        i = lookAheadIndex - 1;
                    }
                    // Check for multi-line comment
                    else if (lookAheadCh == '/' && nextLookAheadCh == '*')
                    {
                        var commentContent = new StringBuilder();
                        commentContent.Append(lookAheadCh);
                        commentContent.Append(nextLookAheadCh);
                        lookAheadIndex += 2;
                        
                        // Find the end of the comment
                        while (lookAheadIndex < parameterListText.Length)
                        {
                            var commentCh = parameterListText[lookAheadIndex];
                            var nextCommentCh = lookAheadIndex + 1 < parameterListText.Length ? 
                                parameterListText[lookAheadIndex + 1] : '\0';
                            
                            commentContent.Append(commentCh);
                            lookAheadIndex++;
                            
                            if (commentCh == '*' && nextCommentCh == '/')
                            {
                                commentContent.Append(nextCommentCh);
                                lookAheadIndex++;
                                break;
                            }
                        }
                        
                        // Now check what follows the comment: whitespace+linebreak or non-whitespace?
                        var afterCommentIndex = lookAheadIndex;
                        var hasOnlyWhitespaceUntilLinebreak = true;
                        
                        while (afterCommentIndex < parameterListText.Length)
                        {
                            var afterCh = parameterListText[afterCommentIndex];
                            
                            if (afterCh == '\n')
                            {
                                // Multi-line comment followed by linebreak belongs to previous parameter
                                trailingContent.Append(commentContent);
                                // Include whitespace and linebreak
                                for (int idx = lookAheadIndex; idx <= afterCommentIndex; idx++)
                                {
                                    trailingContent.Append(parameterListText[idx]);
                                }
                                currentBlock.Append(trailingContent);
                                i = afterCommentIndex;
                                consumedNewlineInTrailing = true;
                                break;
                            }
                            else if (afterCh != ' ' && afterCh != '\t' && afterCh != '\r')
                            {
                                // Non-whitespace after comment - comment belongs to NEXT parameter
                                hasOnlyWhitespaceUntilLinebreak = false;
                                // Don't append anything to current block
                                break;
                            }
                            
                            afterCommentIndex++;
                        }
                        
                        // If we reached end of string with only whitespace, treat as linebreak case
                        if (afterCommentIndex >= parameterListText.Length && hasOnlyWhitespaceUntilLinebreak)
                        {
                            trailingContent.Append(commentContent);
                            currentBlock.Append(trailingContent);
                            i = lookAheadIndex - 1;
                        }
                    }
                }
                
                // Create block from accumulated content
                var blockText = currentBlock.ToString();
                if (!string.IsNullOrWhiteSpace(blockText.Trim()))
                {
                    blocks.Add(new ParameterBlock(
                        blockText,
                        blockIndex++,
                        blockStartsOnNewLine,
                        blockLeadingIndent));
                }

                // Reset for next block
                currentBlock.Clear();
                blockStartsOnNewLine = consumedNewlineInTrailing;  // Next block starts on new line if we consumed one
                blockLeadingIndent = 0;
                seenNonWhitespaceInBlock = false;
                continue;
            }

            // Regular character
            currentBlock.Append(ch);
        }

        // Add final block if any content remains
        var finalBlockText = currentBlock.ToString();
        if (!string.IsNullOrWhiteSpace(finalBlockText))
        {
            blocks.Add(new ParameterBlock(
                finalBlockText,
                blockIndex,
                blockStartsOnNewLine,
                blockLeadingIndent));
        }

        return blocks;
    }
}
