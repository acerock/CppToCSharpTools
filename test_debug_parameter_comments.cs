using System;
using System.IO;
using CppToCsConverter.Core.Parsers;

class DebugParameterComments
{
    static void Main()
    {
        var headerContent = @"
class TestClass
{
public:
    void TestMethod(const CString& param1 /* OUT */);
};";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, headerContent);

        try
        {
            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(tempFile);

            Console.WriteLine($"Classes found: {classes.Count}");
            if (classes.Count > 0)
            {
                var testClass = classes[0];
                Console.WriteLine($"Methods found: {testClass.Methods.Count}");
                
                if (testClass.Methods.Count > 0)
                {
                    var method = testClass.Methods[0];
                    Console.WriteLine($"Method name: {method.Name}");
                    Console.WriteLine($"Parameters found: {method.Parameters.Count}");
                    
                    if (method.Parameters.Count > 0)
                    {
                        var param = method.Parameters[0];
                        Console.WriteLine($"Parameter name: {param.Name}");
                        Console.WriteLine($"Parameter type: {param.Type}");
                        Console.WriteLine($"Parameter original text: {param.OriginalText}");
                        Console.WriteLine($"PositionedComments count: {param.PositionedComments?.Count ?? -1}");
                        Console.WriteLine($"InlineComments count: {param.InlineComments?.Count ?? -1}");
                        
                        if (param.PositionedComments != null && param.PositionedComments.Count > 0)
                        {
                            for (int i = 0; i < param.PositionedComments.Count; i++)
                            {
                                var comment = param.PositionedComments[i];
                                Console.WriteLine($"  Comment {i}: '{comment.CommentText}' Position: {comment.Position}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No positioned comments found");
                        }
                    }
                }
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}