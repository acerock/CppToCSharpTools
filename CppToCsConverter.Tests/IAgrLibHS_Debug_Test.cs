using Xunit;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    public class IAgrLibHSDebugTest
    {
        [Fact]
        public void DebugGLOBALCONSTANTS_ParsedMembers()
        {
            // Arrange - Use actual IAgrLibHS.h file
            var headerPath = @"D:\BatchNetTools\CppToCSharpTools\Work\AgrLibHS\IAgrLibHS.h";
            
            if (!File.Exists(headerPath))
            {
                // Skip test if file doesn't exist
                return;
            }

            // Act - Parse the header file
            var parser = new CppHeaderParser();
            var structs = parser.ParseStructsFromHeaderFile(headerPath);
            
            var globalConstants = structs.FirstOrDefault(s => s.Name == "GLOBALCONSTANTS");
            
            Assert.NotNull(globalConstants);
            
            // Debug output - struct level info
            System.Console.WriteLine($"=== GLOBALCONSTANTS struct ===");
            System.Console.WriteLine($"Name: '{globalConstants.Name}'");
            System.Console.WriteLine($"Type: {globalConstants.Type}");
            System.Console.WriteLine($"OriginalDefinition length: {globalConstants.OriginalDefinition.Length}");
            System.Console.WriteLine($"PrecedingComments count: {globalConstants.PrecedingComments.Count}");
            if (globalConstants.PrecedingComments.Any())
            {
                System.Console.WriteLine("=== Struct PrecedingComments ===");
                foreach (var comment in globalConstants.PrecedingComments)
                {
                    System.Console.WriteLine($"  - '{comment}'");
                }
            }
            System.Console.WriteLine($"=== OriginalDefinition (first 500 chars) ===");
            var origDef = globalConstants.OriginalDefinition.Length > 500 
                ? globalConstants.OriginalDefinition.Substring(0, 500) 
                : globalConstants.OriginalDefinition;
            System.Console.WriteLine(origDef);
            System.Console.WriteLine("=== End OriginalDefinition ===");
            System.Console.WriteLine();
            
            // Debug output - print all members
            System.Console.WriteLine($"=== GLOBALCONSTANTS struct has {globalConstants.Members.Count} members ===");
            
            int memberIndex = 0;
            foreach (var member in globalConstants.Members)
            {
                System.Console.WriteLine($"Member {memberIndex++}: Type='{member.Type}', Name='{member.Name}'");
                
                if (member.Type.Contains("#define") || member.Name.Contains("#define"))
                {
                    System.Console.WriteLine($"  ^^^ ERROR: Member contains #define!");
                }
                
                if (member.Type.Contains("typedef") || member.Name.Contains("typedef"))
                {
                    System.Console.WriteLine($"  ^^^ ERROR: Member contains typedef!");
                }
                
                // Print preceding comments if any
                if (member.PrecedingComments.Any())
                {
                    System.Console.WriteLine($"  PrecedingComments ({member.PrecedingComments.Count}):");
                    foreach (var comment in member.PrecedingComments)
                    {
                        System.Console.WriteLine($"    - '{comment}'");
                        if (comment.Contains("#define") || comment.Contains("typedef"))
                        {
                            System.Console.WriteLine($"      ^^^ WARNING: Comment contains C++ code!");
                        }
                    }
                }
                
                // Print postfix comment if any
                if (!string.IsNullOrEmpty(member.PostfixComment))
                {
                    System.Console.WriteLine($"  PostfixComment: '{member.PostfixComment}'");
                    if (member.PostfixComment.Contains("#define") || member.PostfixComment.Contains("typedef"))
                    {
                        System.Console.WriteLine($"    ^^^ WARNING: Postfix comment contains C++ code!");
                    }
                }
            }
            
            // Check that no member contains raw C++ code
            foreach (var member in globalConstants.Members)
            {
                Assert.DoesNotContain("#define", member.Type);
                Assert.DoesNotContain("#define", member.Name);
                Assert.DoesNotContain("typedef", member.Type);
                Assert.DoesNotContain("typedef", member.Name);
                
                // CRITICAL: Check PostfixComment doesn't contain C++ code
                if (!string.IsNullOrEmpty(member.PostfixComment))
                {
                    Assert.DoesNotContain("#define", member.PostfixComment);
                    Assert.DoesNotContain("typedef", member.PostfixComment);
                }
            }
        }
    }
}
