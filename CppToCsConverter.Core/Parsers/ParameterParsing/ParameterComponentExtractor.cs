using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Core.Parsers.ParameterParsing;

/// <summary>
/// Extracts components (type, name, default value, comments) from a parameter block.
/// </summary>
public class ParameterComponentExtractor : IParameterComponentExtractor
{
    public CppParameter ExtractComponents(ParameterBlock block)
    {
        var rawText = block.RawText;
        
        // Step 1: Extract and remove comments, tracking their positions
        var (textWithoutComments, prefixComments, suffixComments) = ExtractComments(rawText);
        
        // Step 2: Parse the remaining text for type, name, and default value
        var (typeStr, nameStr, defaultValueStr) = ParseTypeNameDefault(textWithoutComments);
        
        // Step 3: Extract modifiers from the type string
        var isConst = typeStr.Contains("const");
        var isPointer = typeStr.Contains("*");
        var isReference = typeStr.Contains("&") && !typeStr.Contains("&&");
        
        // Step 4: Extract base type by removing modifiers
        var baseType = typeStr
            .Replace("const", "")
            .Replace("&", "")
            .Replace("*", "")
            .Trim();
        
        // Step 5: Generate canonical signature for matching
        var canonicalSignature = GenerateCanonicalSignature(typeStr);
        
        // Step 6: Build CppParameter directly
        var cppParam = new CppParameter
        {
            Type = baseType,
            Name = nameStr,
            DefaultValue = defaultValueStr,  // Keep null if no default value
            IsConst = isConst,
            IsPointer = isPointer,
            IsReference = isReference,
            CanonicalSignature = canonicalSignature,
            HasLineBreak = block.StartsOnNewLine,
            OriginalIndent = block.LeadingIndent,
            OriginalText = typeStr + (string.IsNullOrEmpty(nameStr) ? "" : " " + nameStr)
        };

        // Add positioned comments
        cppParam.PositionedComments = new List<ParameterComment>(prefixComments);
        cppParam.PositionedComments.AddRange(suffixComments);

        // Legacy InlineComments for backward compatibility
        cppParam.InlineComments = cppParam.PositionedComments.Select(pc => pc.CommentText).ToList();

        return cppParam;
    }

    private (string textWithoutComments, List<ParameterComment> prefixComments, List<ParameterComment> suffixComments) 
        ExtractComments(string text)
    {
        var prefixComments = new List<ParameterComment>();
        var suffixComments = new List<ParameterComment>();
        var cleaned = new StringBuilder();
        
        var inCStyleComment = false;
        var inCppStyleComment = false;
        var currentComment = new StringBuilder();
        var hasSeenNonCommentContent = false;
        
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            var nextCh = i + 1 < text.Length ? text[i + 1] : '\0';
            
            // Handle C++ style comment
            if (inCppStyleComment)
            {
                currentComment.Append(ch);
                if (ch == '\n')
                {
                    // End of C++ comment
                    suffixComments.Add(new ParameterComment
                    {
                        CommentText = currentComment.ToString().Trim(),
                        Position = CommentPosition.Suffix
                    });
                    currentComment.Clear();
                    inCppStyleComment = false;
                }
                continue;
            }
            
            // Detect C++ comment start
            if (!inCStyleComment && ch == '/' && nextCh == '/')
            {
                inCppStyleComment = true;
                currentComment.Append(ch);
                continue;
            }
            
            // Handle C-style comment
            if (inCStyleComment)
            {
                currentComment.Append(ch);
                if (ch == '*' && nextCh == '/')
                {
                    currentComment.Append(nextCh);
                    i++; // Skip the '/'
                    
                    // Classify as prefix or suffix based on whether we've seen content
                    var comment = new ParameterComment
                    {
                        CommentText = currentComment.ToString().Trim(),
                        Position = hasSeenNonCommentContent ? CommentPosition.Suffix : CommentPosition.Prefix
                    };
                    
                    if (hasSeenNonCommentContent)
                        suffixComments.Add(comment);
                    else
                        prefixComments.Add(comment);
                    
                    currentComment.Clear();
                    inCStyleComment = false;
                }
                continue;
            }
            
            // Detect C-style comment start
            if (ch == '/' && nextCh == '*')
            {
                inCStyleComment = true;
                currentComment.Append(ch);
                continue;
            }
            
            // Regular content
            if (ch != ' ' && ch != '\t' && ch != '\n' && ch != '\r' && ch != ',')
            {
                hasSeenNonCommentContent = true;
            }
            
            cleaned.Append(ch);
        }
        
        // If still in a C++ comment at end (no newline), add it
        if (inCppStyleComment && currentComment.Length > 0)
        {
            suffixComments.Add(new ParameterComment
            {
                CommentText = currentComment.ToString().Trim(),
                Position = CommentPosition.Suffix
            });
        }
        
        return (cleaned.ToString(), prefixComments, suffixComments);
    }

    private (string type, string name, string? defaultValue) ParseTypeNameDefault(string text)
    {
        // Remove trailing comma and whitespace
        text = text.TrimEnd(',', ' ', '\t', '\n', '\r');
        
        // Check for default value (=)
        string? defaultValue = null;
        var equalsIndex = FindDefaultValueSeparator(text);
        if (equalsIndex >= 0)
        {
            defaultValue = text.Substring(equalsIndex + 1).Trim();
            text = text.Substring(0, equalsIndex).Trim();
        }
        
        // Now we have: type + name (e.g., "const TAttId& attId" or "CAgrMT* pmtTable")
        var (type, name) = SplitTypeAndName(text);
        
        return (type, name, defaultValue);
    }

    private int FindDefaultValueSeparator(string text)
    {
        // Find '=' that's not inside parentheses, brackets, or angle brackets
        var depth = 0;
        var angleDepth = 0;
        var bracketDepth = 0;
        
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            
            if (ch == '(') depth++;
            else if (ch == ')') depth--;
            else if (ch == '<') angleDepth++;
            else if (ch == '>') angleDepth--;
            else if (ch == '[') bracketDepth++;
            else if (ch == ']') bracketDepth--;
            else if (ch == '=' && depth == 0 && angleDepth == 0 && bracketDepth == 0)
            {
                return i;
            }
        }
        
        return -1;
    }

    private (string type, string name) SplitTypeAndName(string text)
    {
        text = text.Trim();
        
        // If empty, return empty
        if (string.IsNullOrWhiteSpace(text))
            return (string.Empty, string.Empty);
        
        // Strategy: The parameter name is the last "word" that's not a modifier (* & const)
        // Exception: If text ends with * or &, there's no name (just type)
        // Work backwards to find the name
        
        var tokens = TokenizeParameter(text);
        if (tokens.Count == 0)
            return (text, string.Empty);
        
        // Check if the last token is a modifier - if so, there's no name
        var lastToken = tokens[tokens.Count - 1];
        if (lastToken == "*" || lastToken == "&" || lastToken == "const")
        {
            // No name, entire text is the type
            return (text.Trim(), string.Empty);
        }
        
        // Find the parameter name (last non-modifier, non-bracket token)
        string name = string.Empty;
        int nameIndex = -1;
        
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            var token = tokens[i];
            // Skip array brackets attached to name
            if (token.StartsWith("[") && token.EndsWith("]"))
                continue;
            
            // Skip modifiers
            if (token == "*" || token == "&" || token == "const")
                continue;
            
            // This is the name
            name = token;
            nameIndex = i;
            break;
        }
        
        // Type is everything except the name
        string type;
        if (nameIndex >= 0)
        {
            // Reconstruct type from tokens before the name + any modifiers after
            var typeTokens = new List<string>();
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i == nameIndex)
                    continue; // Skip the name itself
                    
                // Skip array brackets (they're part of name in C++)
                if (tokens[i].StartsWith("[") && tokens[i].EndsWith("]"))
                    continue;
                    
                typeTokens.Add(tokens[i]);
            }
            
            // Join tokens intelligently: no space before * or &
            var typeBuilder = new StringBuilder();
            for (int i = 0; i < typeTokens.Count; i++)
            {
                var token = typeTokens[i];
                
                // Add space before token unless:
                // - It's the first token
                // - It's a * or & (pointer/reference modifier)
                // - Previous token was a * or &
                if (i > 0 && token != "*" && token != "&" && 
                    typeTokens[i-1] != "*" && typeTokens[i-1] != "&")
                {
                    typeBuilder.Append(' ');
                }
                
                typeBuilder.Append(token);
            }
            
            type = typeBuilder.ToString().Trim();
        }
        else
        {
            // No name found, entire text is the type
            type = text.Trim();
            name = string.Empty;
        }
        
        return (type, name);
    }

    private List<string> TokenizeParameter(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inAngleBrackets = 0;
        var inArrayBrackets = false;
        
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            
            // Handle array brackets as single token
            if (ch == '[')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                inArrayBrackets = true;
                current.Append(ch);
                continue;
            }
            
            if (inArrayBrackets)
            {
                current.Append(ch);
                if (ch == ']')
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    inArrayBrackets = false;
                }
                continue;
            }
            
            // Track template depth
            if (ch == '<')
            {
                inAngleBrackets++;
                current.Append(ch);
                continue;
            }
            
            if (ch == '>')
            {
                inAngleBrackets--;
                current.Append(ch);
                continue;
            }
            
            // Inside templates, include everything
            if (inAngleBrackets > 0)
            {
                current.Append(ch);
                continue;
            }
            
            // Whitespace splits tokens
            if (char.IsWhiteSpace(ch))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }
            
            // * and & are separate tokens
            if (ch == '*' || ch == '&')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                tokens.Add(ch.ToString());
                continue;
            }
            
            current.Append(ch);
        }
        
        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }
        
        return tokens;
    }

    private string GenerateCanonicalSignature(string type)
    {
        // Normalize whitespace and const positioning for matching
        // Goal: "const TAttId&" matches "TAttId const &" matches "const TAttId  &"
        
        var tokens = TokenizeParameter(type);
        var normalized = new List<string>();
        
        // Move const to the front
        var hasConst = tokens.Contains("const");
        if (hasConst)
        {
            normalized.Add("const");
        }
        
        // Add non-const, non-modifier tokens
        foreach (var token in tokens)
        {
            if (token == "const")
                continue; // Already added at front
            if (token == "*" || token == "&")
                continue; // Add these at the end
                
            normalized.Add(token);
        }
        
        // Add modifiers at the end in consistent order
        if (tokens.Contains("*"))
            normalized.Add("*");
        if (tokens.Contains("&"))
            normalized.Add("&");
        
        // Join with single space
        return string.Join(" ", normalized);
    }
}
