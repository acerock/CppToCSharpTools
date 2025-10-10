using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests - testing real sample file
    /// </summary>
    public class MemberParsingDebugTests6
    {
        private readonly CppHeaderParser _headerParser;

        public MemberParsingDebugTests6()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_RealSampleFile()
        {
            // Act
            var classes = _headerParser.ParseHeaderFile(@"d:\dev\CppToCSharpTools\SamplesAndExpectations\CSample.h");

            // Assert and Debug
            System.Console.WriteLine($"Classes Count: {classes.Count}");
            
            foreach (var cls in classes)
            {
                System.Console.WriteLine($"Class Name: {cls.Name}");
                System.Console.WriteLine($"Members Count: {cls.Members.Count}");
                
                foreach (var member in cls.Members)
                {
                    System.Console.WriteLine($"Member: '{member.Type}' '{member.Name}'");
                }
            }
        }
    }
}