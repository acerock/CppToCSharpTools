using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // Extract source directory from the file paths for namespace resolution
            string sourceDirectory = ExtractSourceDirectoryFromFiles(headerFiles, sourceFiles);
            _converter.ConvertFiles(headerFiles, sourceFiles, outputDirectory, sourceDirectory);
        }

        /// <summary>
        /// Extracts the source directory from the provided file paths for namespace resolution.
        /// </summary>
        /// <param name="headerFiles">Array of header file paths</param>
        /// <param name="sourceFiles">Array of source file paths</param>
        /// <returns>The common directory path or empty string if cannot be determined</returns>
        private string ExtractSourceDirectoryFromFiles(string[] headerFiles, string[] sourceFiles)
        {
            // Combine all file paths
            var allFiles = headerFiles.Concat(sourceFiles).Where(f => !string.IsNullOrEmpty(f)).ToList();
            
            if (!allFiles.Any())
                return "";

            // Get directory of the first file as base
            var baseDirectory = Path.GetDirectoryName(allFiles.First());
            
            if (string.IsNullOrEmpty(baseDirectory))
                return "";

            // Find the common directory among all files
            foreach (var file in allFiles.Skip(1))
            {
                var currentDir = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(currentDir))
                    continue;
                    
                // Find common path between baseDirectory and currentDir
                baseDirectory = FindCommonDirectory(baseDirectory, currentDir);
                
                if (string.IsNullOrEmpty(baseDirectory))
                    break;
            }

            return baseDirectory ?? "";
        }

        /// <summary>
        /// Finds the common directory path between two directory paths.
        /// </summary>
        /// <param name="path1">First directory path</param>
        /// <param name="path2">Second directory path</param>
        /// <returns>The common directory path or empty string</returns>
        private string FindCommonDirectory(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
                return "";

            var parts1 = path1.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parts2 = path2.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            var commonParts = new List<string>();
            int minLength = Math.Min(parts1.Length, parts2.Length);
            
            for (int i = 0; i < minLength; i++)
            {
                if (string.Equals(parts1[i], parts2[i], StringComparison.OrdinalIgnoreCase))
                {
                    commonParts.Add(parts1[i]);
                }
                else
                {
                    break;
                }
            }
            
            return commonParts.Any() ? string.Join(Path.DirectorySeparatorChar.ToString(), commonParts) : "";
        }
    }
}