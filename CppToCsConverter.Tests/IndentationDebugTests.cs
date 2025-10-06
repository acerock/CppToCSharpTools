using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Utils;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class IndentationDebugTests
    {
        [Fact]
        public void TestHeaderImplementationCombination()
        {
            // Test the specific header/implementation combination from user request
            var headerContent = @"class CSomeClass
{
    int memberTwo;

    public:
        int GetMemberTwo() const;
};";

            var cppContent = @"int CSomeClass::GetMemberTwo() const
{
    /* Sample method body */
    if (memberTwo != -1)
        memberTwo += 1;
    
    return memberTwo;
}";

            var tempHeaderFile = Path.GetTempFileName() + ".h";
            var tempCppFile = Path.GetTempFileName() + ".cpp";
            
            try
            {
                File.WriteAllText(tempHeaderFile, headerContent);
                File.WriteAllText(tempCppFile, cppContent);

                // Parse header to get method signature
                var headerParser = new CppHeaderParser();
                var classes = headerParser.ParseHeaderFile(tempHeaderFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSomeClass");
                var headerMethod = cppClass?.Methods.FirstOrDefault(m => m.Name == "GetMemberTwo");

                // Parse cpp to get implementation
                var cppParser = new CppSourceParser();
                var (cppMethods, _) = cppParser.ParseSourceFile(tempCppFile);
                var implMethod = cppMethods.FirstOrDefault(m => m.Name == "GetMemberTwo");

                Console.WriteLine("=== HEADER/IMPLEMENTATION COMBINATION TEST ===");
                Console.WriteLine($"Header Method: {headerMethod?.Name} (const: {headerMethod?.IsConst})");
                Console.WriteLine($"Impl Method: {implMethod?.Name} (const: {implMethod?.IsConst})");
                Console.WriteLine($"Implementation found: {!string.IsNullOrEmpty(implMethod?.ImplementationBody)}");
                Console.WriteLine();

                // Debug raw cpp content
                Console.WriteLine("=== RAW CPP CONTENT ===");
                Console.WriteLine("Raw content characters:");
                for (int i = 0; i < Math.Min(cppContent.Length, 150); i++)
                {
                    char c = cppContent[i];
                    if (c == '\r') Console.Write("\\r");
                    else if (c == '\n') Console.Write("\\n");
                    else Console.Write(c);
                }
                Console.WriteLine();
                Console.WriteLine();
                
                var lines = cppContent.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("/* Sample") ||
                        lines[i].Contains("if (memberTwo") ||
                        lines[i].Contains("memberTwo +=") ||
                        lines[i].Contains("return memberTwo"))
                    {
                        Console.WriteLine($"Raw line {i}: '{lines[i]}' (length: {lines[i].Length}, leading spaces: {lines[i].Length - lines[i].TrimStart().Length})");
                    }
                }
                Console.WriteLine();

                if (implMethod?.ImplementationBody != null)
                {
                Console.WriteLine($"ImplementationBody extracted:");
                Console.WriteLine($"'{implMethod.ImplementationBody}'");
                Console.WriteLine($"Length: {implMethod.ImplementationBody.Length}");
                
                // Debug the exact character sequence
                Console.WriteLine("=== CHARACTER BY CHARACTER ===");
                for (int i = 0; i < Math.Min(implMethod.ImplementationBody.Length, 100); i++)
                {
                    char c = implMethod.ImplementationBody[i];
                    if (c == '\r') Console.Write("\\r");
                    else if (c == '\n') Console.Write("\\n");
                    else Console.Write(c);
                }
                Console.WriteLine();
                Console.WriteLine();                    Console.WriteLine("=== EXTRACTED LINES ===");
                    var extractedLines = implMethod.ImplementationBody.Split('\n');
                    for (int i = 0; i < extractedLines.Length; i++)
                    {
                        var leadingSpaces = extractedLines[i].Length - extractedLines[i].TrimStart().Length;
                        Console.WriteLine($"Line {i}: '{extractedLines[i]}' (length: {extractedLines[i].Length}, leading: {leadingSpaces})");
                    }
                    Console.WriteLine();

                    Console.WriteLine("=== INDENTATION ANALYSIS ===");
                    Console.WriteLine($"ImplementationIndentation: {implMethod.ImplementationIndentation}");

                    var reindented = IndentationManager.ReindentMethodBody(implMethod.ImplementationBody, implMethod.ImplementationIndentation);
                    Console.WriteLine($"ReindentedMethodBody:");
                    Console.WriteLine($"'{reindented}'");
                    Console.WriteLine();

                    Console.WriteLine("=== FINAL REINDENTED LINES ===");
                    var reindentedLines = reindented.Split('\n');
                    for (int i = 0; i < reindentedLines.Length; i++)
                    {
                        var leadingSpaces = reindentedLines[i].Length - reindentedLines[i].TrimStart().Length;
                        Console.WriteLine($"Line {i}: '{reindentedLines[i]}' (length: {reindentedLines[i].Length}, leading: {leadingSpaces})");
                    }
                }
            }
            finally
            {
                if (File.Exists(tempHeaderFile)) File.Delete(tempHeaderFile);
                if (File.Exists(tempCppFile)) File.Delete(tempCppFile);
            }
        }

        [Fact] 
        public void DebugCppMethodBodyIndentation()
        {
            // Test the EXACT file from the real CSample.cpp
            var realCppFile = @"d:\dev\CppToCSharpTools\Work\Sample\CSample.cpp";
            
            if (!File.Exists(realCppFile))
            {
                Console.WriteLine($"Real cpp file not found: {realCppFile}");
                return;
            }

            var parser = new CppSourceParser();
            var (methods, _) = parser.ParseSourceFile(realCppFile);
            var method = methods.FirstOrDefault(m => m.Name == "MethodP1");
                
            Console.WriteLine("=== DEBUG CPP METHOD BODY INDENTATION ===");
            Console.WriteLine($"Method: {method?.Name}");
            Console.WriteLine($"ClassName: {method?.ClassName}");
            Console.WriteLine($"HasImplementationBody: {!string.IsNullOrEmpty(method?.ImplementationBody)}");
            
            // Read the raw file content to compare
            var rawContent = File.ReadAllText(realCppFile);
            Console.WriteLine("=== RAW CPP FILE CONTENT (MethodP1 section) ===");
            var lines = rawContent.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("if (dimPd.IsEmpty())") || 
                    lines[i].Contains("return bError") || 
                    lines[i].Contains("return lLimitHorizon"))
                {
                    Console.WriteLine($"Raw line {i}: '{lines[i]}' (length: {lines[i].Length}, leading spaces: {lines[i].Length - lines[i].TrimStart().Length})");
                }
            }
            Console.WriteLine();
            
            Console.WriteLine($"ImplementationBody extracted content:");
            Console.WriteLine($"'{method?.ImplementationBody}'");
            Console.WriteLine($"Length: {method?.ImplementationBody?.Length ?? 0}");
            Console.WriteLine();
            
            if (method?.ImplementationBody != null)
            {
                Console.WriteLine("=== EXTRACTED LINES WITH LENGTHS ===");
                var extractedLines = method.ImplementationBody.Split('\n');
                for (int i = 0; i < extractedLines.Length; i++)
                {
                    var leadingSpaces = extractedLines[i].Length - extractedLines[i].TrimStart().Length;
                    Console.WriteLine($"Line {i}: '{extractedLines[i]}' (length: {extractedLines[i].Length}, leading: {leadingSpaces})");
                }
                Console.WriteLine();
                
                Console.WriteLine("=== INDENTATION ANALYSIS ===");
                Console.WriteLine($"ImplementationIndentation: {method.ImplementationIndentation}");
                
                var reindented = IndentationManager.ReindentMethodBody(method.ImplementationBody, method.ImplementationIndentation);
                Console.WriteLine($"ReindentedMethodBody:");
                Console.WriteLine($"'{reindented}'");
                
                Console.WriteLine("=== REINDENTED LINES ===");
                var reindentedLines = reindented.Split('\n');
                for (int i = 0; i < reindentedLines.Length; i++)
                {
                    var leadingSpaces = reindentedLines[i].Length - reindentedLines[i].TrimStart().Length;
                    Console.WriteLine($"Line {i}: '{reindentedLines[i]}' (length: {reindentedLines[i].Length}, leading: {leadingSpaces})");
                }
            }
        }

        [Fact]
        public void DebugInlineMethodIndentation()
        {
            // Test the exact content from CSample.h that has wrong indentation
            var headerContent = @"class CSample
{
private:
    int MethodPrivInl1(const TDimValue& dim1)
    {
        if (dim1.IsEmpty()) 
            return 0;
        
        return 42;
    }
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                var method = cppClass?.Methods.FirstOrDefault(m => m.Name == "MethodPrivInl1");
                
                Console.WriteLine("=== DEBUG INLINE METHOD INDENTATION ===");
                Console.WriteLine($"Method: {method?.Name}");
                Console.WriteLine($"HasInlineImplementation: {method?.HasInlineImplementation}");
                
                // Let's also debug what the raw content looks like before parsing
                Console.WriteLine("=== RAW HEADER CONTENT ===");
                var lines = headerContent.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("if (dim1.IsEmpty())") || 
                        lines[i].Contains("return 0;") || 
                        lines[i].Contains("return 42;"))
                    {
                        Console.WriteLine($"Raw line {i}: '{lines[i]}' (length: {lines[i].Length}, leading spaces: {lines[i].Length - lines[i].TrimStart().Length})");
                    }
                }
                Console.WriteLine();
                
                Console.WriteLine($"InlineImplementation extracted content:");
                Console.WriteLine($"'{method?.InlineImplementation}'");
                Console.WriteLine($"Length: {method?.InlineImplementation?.Length ?? 0}");
                Console.WriteLine();
                
                if (method?.InlineImplementation != null)
                {
                    Console.WriteLine("=== EXTRACTED LINES WITH LENGTHS ===");
                    var extractedLines = method.InlineImplementation.Split('\n');
                    for (int i = 0; i < extractedLines.Length; i++)
                    {
                        var leadingSpaces = extractedLines[i].Length - extractedLines[i].TrimStart().Length;
                        Console.WriteLine($"Line {i}: '{extractedLines[i]}' (length: {extractedLines[i].Length}, leading: {leadingSpaces})");
                    }
                    Console.WriteLine();
                    
                    Console.WriteLine("=== INDENTATION ANALYSIS ===");
                    var originalIndentation = IndentationManager.DetectOriginalIndentation(method.InlineImplementation);
                    Console.WriteLine($"DetectedOriginalIndentation: {originalIndentation}");
                    
                    var reindented = IndentationManager.ReindentMethodBody(method.InlineImplementation, originalIndentation);
                    Console.WriteLine($"ReindentedMethodBody:");
                    Console.WriteLine($"'{reindented}'");
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}