using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Core
{
    /// <summary>
    /// Main entry point for C++ to C# conversion functionality.
    /// Provides a simplified API for converting C++ header and source files to C# equivalents.
    /// </summary>
    public class CppToCsConverterApi
    {
        private readonly CppToCsStructuralConverter _converter;

        /// <summary>
        /// Initializes a new instance of the CppToCsConverterApi.
        /// </summary>
        public CppToCsConverterApi()
        {
            _converter = new CppToCsStructuralConverter();
        }

        /// <summary>
        /// Converts C++ files from a source directory to C# equivalents.
        /// </summary>
        /// <param name="sourceDirectory">The directory containing C++ files to convert</param>
        /// <param name="outputDirectory">The directory where C# files will be generated</param>
        public void ConvertDirectory(string sourceDirectory, string outputDirectory)
        {
            _converter.ConvertDirectory(sourceDirectory, outputDirectory);
        }

        /// <summary>
        /// Converts specific C++ files from a source directory to C# equivalents.
        /// </summary>
        /// <param name="sourceDirectory">The directory containing C++ files to convert</param>
        /// <param name="specificFiles">Array of specific files to convert</param>
        /// <param name="outputDirectory">The directory where C# files will be generated</param>
        public void ConvertSpecificFiles(string sourceDirectory, string[] specificFiles, string outputDirectory)
        {
            _converter.ConvertSpecificFiles(sourceDirectory, specificFiles, outputDirectory);
        }

        /// <summary>
        /// Converts specific C++ header and source files to C# equivalents.
        /// </summary>
        /// <param name="headerFiles">Array of header file paths to process</param>
        /// <param name="sourceFiles">Array of source file paths to process</param>
        /// <param name="outputDirectory">The directory where C# files will be generated</param>
        public void ConvertFiles(string[] headerFiles, string[] sourceFiles, string outputDirectory)
        {
            _converter.ConvertFiles(headerFiles, sourceFiles, outputDirectory);
        }
    }
}