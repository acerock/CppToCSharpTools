using Xunit;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    public class PostfixCommentBugTest
    {
        [Fact]
        public void Member19_PostfixComment_Should_Not_Contain_Define_Or_Typedef()
        {
            // Arrange
            var headerPath = @"D:\BatchNetTools\CppToCSharpTools\Work\AgrLibHS\IAgrLibHS.h";
            
            if (!File.Exists(headerPath))
            {
                return; // Skip if file doesn't exist
            }

            // Act
            var parser = new CppHeaderParser();
            var structs = parser.ParseStructsFromHeaderFile(headerPath);
            var globalConstants = structs.FirstOrDefault(s => s.Name == "GLOBALCONSTANTS");
            
            Assert.NotNull(globalConstants);
            
            // CRITICAL: Check total member count
            System.Console.WriteLine($"=== Total members: {globalConstants.Members.Count} ===");
            
            Assert.True(globalConstants.Members.Count >= 20, "Expected at least 20 members");
            
            var member19 = globalConstants.Members[19]; // attPrnLFRelId
            var member20 = globalConstants.Members[20]; // dimClient
            
            // Debug output for member 19
            System.Console.WriteLine($"=== Member 19 FULL DUMP ===");
            System.Console.WriteLine($"Type: '{member19.Type}'");
            System.Console.WriteLine($"Name: '{member19.Name}'");
            System.Console.WriteLine($"AccessSpecifier: '{member19.AccessSpecifier}'");
            System.Console.WriteLine($"PostfixComment length: {member19.PostfixComment?.Length ?? 0}");
            if (!string.IsNullOrEmpty(member19.PostfixComment))
            {
                // Check if postfix comment has newlines (multi-line)
                if (member19.PostfixComment.Contains("\n") || member19.PostfixComment.Contains("\r"))
                {
                    System.Console.WriteLine($"  ^^^ WARNING: PostfixComment contains newlines!");
                    System.Console.WriteLine($"  PostfixComment FULL ({member19.PostfixComment.Length} chars):");
                    System.Console.WriteLine(member19.PostfixComment);
                    System.Console.WriteLine("=== END PostfixComment ===");
                }
                else
                {
                    System.Console.WriteLine($"PostfixComment: '{member19.PostfixComment}'");
                }
            }
            System.Console.WriteLine($"PrecedingComments count: {member19.PrecedingComments.Count}");
            System.Console.WriteLine($"RegionStart: '{member19.RegionStart ?? "null"}'");
            System.Console.WriteLine($"RegionEnd length: {member19.RegionEnd?.Length ?? 0}");
            if (!string.IsNullOrEmpty(member19.RegionEnd))
            {
                if (member19.RegionEnd.Length > 500)
                {
                    System.Console.WriteLine($"RegionEnd first 500 chars: '{member19.RegionEnd.Substring(0, 500)}'");
                    System.Console.WriteLine($"... (total {member19.RegionEnd.Length} chars)");
                }
                else
                {
                    System.Console.WriteLine($"RegionEnd: '{member19.RegionEnd}'");
                }
                
                if (member19.RegionEnd.Contains("#define") || member19.RegionEnd.Contains("typedef"))
                {
                    System.Console.WriteLine("^^^ FOUND IT! RegionEnd contains C++ code!");
                }
            }
            
            System.Console.WriteLine();
            
            // Debug output for member 20
            System.Console.WriteLine($"Member 20: {member20.Type} {member20.Name}");
            System.Console.WriteLine($"PostfixComment length: {member20.PostfixComment?.Length ?? 0}");
            if (!string.IsNullOrEmpty(member20.PostfixComment))
            {
                System.Console.WriteLine($"PostfixComment first 500 chars: '{member20.PostfixComment.Substring(0, System.Math.Min(500, member20.PostfixComment.Length))}'");
                
                if (member20.PostfixComment.Length > 500)
                {
                    System.Console.WriteLine($"... (total {member20.PostfixComment.Length} chars)");
                }
            }
            System.Console.WriteLine($"PrecedingComments count: {member20.PrecedingComments.Count}");
            if (member20.PrecedingComments.Any())
            {
                System.Console.WriteLine("Preceding comments:");
                foreach (var comment in member20.PrecedingComments)
                {
                    System.Console.WriteLine($"  - '{comment.Substring(0, System.Math.Min(100, comment.Length))}'");
                }
            }
            
            // Assert
            Assert.DoesNotContain("#define", member19.PostfixComment ?? "");
            Assert.DoesNotContain("typedef", member19.PostfixComment ?? "");
            Assert.DoesNotContain("#define", member20.PostfixComment ?? "");
            Assert.DoesNotContain("typedef", member20.PostfixComment ?? "");
        }
    }
}
