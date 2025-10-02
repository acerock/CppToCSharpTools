using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Models;
using CppToCsConverter.Parsers;
using CppToCsConverter.Generators;

namespace CppToCsConverter.Core
{
    public class CppToCsStructuralConverter
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;
        private readonly CsClassGenerator _classGenerator;
        private readonly CsInterfaceGenerator _interfaceGenerator;

        public CppToCsStructuralConverter()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
            _classGenerator = new CsClassGenerator();
            _interfaceGenerator = new CsInterfaceGenerator();
        }

        public void ConvertDirectory(string sourceDirectory, string outputDirectory)
        {
            Console.WriteLine($"Converting C++ files from: {sourceDirectory}");
            Console.WriteLine($"Output directory: {outputDirectory}");

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Find all .h and .cpp files
            var headerFiles = Directory.GetFiles(sourceDirectory, "*.h", SearchOption.AllDirectories);
            var sourceFiles = Directory.GetFiles(sourceDirectory, "*.cpp", SearchOption.AllDirectories);

            ConvertFiles(headerFiles, sourceFiles, outputDirectory);
        }

        public void ConvertSpecificFiles(string sourceDirectory, string[] fileNames, string outputDirectory)
        {
            Console.WriteLine($"Converting specific C++ files from: {sourceDirectory}");
            Console.WriteLine($"Files to convert: {string.Join(", ", fileNames)}");
            Console.WriteLine($"Output directory: {outputDirectory}");

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Build full paths for specified files
            var headerFiles = new List<string>();
            var sourceFiles = new List<string>();

            foreach (var fileName in fileNames)
            {
                var fullPath = Path.Combine(sourceDirectory, fileName);
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Warning: File '{fileName}' not found in '{sourceDirectory}'");
                    continue;
                }

                var extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".h")
                {
                    headerFiles.Add(fullPath);
                }
                else if (extension == ".cpp")
                {
                    sourceFiles.Add(fullPath);
                }
                else
                {
                    Console.WriteLine($"Warning: Unsupported file type '{extension}' for file '{fileName}'");
                }
            }

            ConvertFiles(headerFiles.ToArray(), sourceFiles.ToArray(), outputDirectory);
        }

        private void ConvertFiles(string[] headerFiles, string[] sourceFiles, string outputDirectory)
        {
            Console.WriteLine($"Found {headerFiles.Length} header files and {sourceFiles.Length} source files");
            Console.WriteLine($"Output directory path: '{outputDirectory}'");
            
            // Ensure output directory exists
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Console.WriteLine($"Creating output directory: {outputDirectory}");
                    Directory.CreateDirectory(outputDirectory);
                }
                else
                {
                    Console.WriteLine($"Output directory already exists: {outputDirectory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating output directory: {ex.Message}");
                throw;
            }

            // Parse all files
            var parsedHeaders = new Dictionary<string, CppClass>();
            var parsedSources = new Dictionary<string, List<CppMethod>>();

            // Parse header files
            var headerFileClasses = new Dictionary<string, List<CppClass>>();
            foreach (var headerFile in headerFiles)
            {
                Console.WriteLine($"Parsing header: {Path.GetFileName(headerFile)}");
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var fileName = Path.GetFileNameWithoutExtension(headerFile);
                headerFileClasses[fileName] = classes;
                
                // Also add to the main dictionary for backward compatibility
                foreach (var cppClass in classes)
                {
                    parsedHeaders[cppClass.Name] = cppClass;
                    Console.WriteLine($"Found class/struct: {cppClass.Name} in {Path.GetFileName(headerFile)}");
                }
            }

            // Parse source files
            foreach (var sourceFile in sourceFiles)
            {
                Console.WriteLine($"Parsing source: {Path.GetFileName(sourceFile)}");
                var methods = _sourceParser.ParseSourceFile(sourceFile);
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                parsedSources[fileName] = methods;
            }

            // Generate C# files - one per header file containing all its classes
            foreach (var headerFileKvp in headerFileClasses)
            {
                var fileName = headerFileKvp.Key;
                var classes = headerFileKvp.Value;
                
                if (classes.Count == 0)
                    continue;
                    
                Console.WriteLine($"Generating C# file: {fileName}.cs with {classes.Count} class(es)");
                
                var csFileContent = GenerateCsFileWithMultipleClasses(fileName, classes, parsedSources, outputDirectory);
                var csFileName = Path.Combine(outputDirectory, $"{fileName}.cs");
                
                try
                {
                    Console.WriteLine($"Writing C# file: {csFileName}");
                    File.WriteAllText(csFileName, csFileContent);
                    Console.WriteLine($"Generated C# file: {fileName}.cs (Size: {csFileContent.Length} chars)");
                    
                    // Verify file was written
                    if (File.Exists(csFileName))
                    {
                        var fileInfo = new FileInfo(csFileName);
                        Console.WriteLine($"File verified: {csFileName} (Size: {fileInfo.Length} bytes)");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: File was not created: {csFileName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR writing C# file {csFileName}: {ex.Message}");
                    throw;
                }
            }
            
            // Old individual class generation logic has been replaced with file-based generation above

            Console.WriteLine("Conversion completed!");
        }

        private string GenerateCsFileWithMultipleClasses(string fileName, List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, string outputDirectory)
        {
            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.InteropServices;");
            sb.AppendLine();

            // Add namespace
            sb.AppendLine($"namespace Generated_{fileName}");
            sb.AppendLine("{");

            // Generate each class in the file
            for (int i = 0; i < classes.Count; i++)
            {
                var cppClass = classes[i];
                
                if (i > 0)
                    sb.AppendLine(); // Add blank line between classes
                
                if (cppClass.IsInterface)
                {
                    // Generate interface inline
                    var interfaceContent = _interfaceGenerator.GenerateInterface(cppClass);
                    // Remove the outer namespace wrapper and extract just the interface content
                    var lines = interfaceContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    bool insideNamespace = false;
                    foreach (var line in lines)
                    {
                        if (line.Contains("namespace"))
                        {
                            insideNamespace = true;
                            continue;
                        }
                        if (insideNamespace && line.Trim() == "{")
                            continue;
                        if (insideNamespace && line.Trim() == "}")
                        {
                            insideNamespace = false;
                            continue;
                        }
                        if (insideNamespace)
                        {
                            sb.AppendLine("    " + line); // Add extra indentation for namespace
                        }
                    }
                }
                else
                {
                    // Generate class inline with preserved C++ method bodies
                    GenerateClassWithCppBodies(sb, cppClass, parsedSources, fileName);
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private void GenerateClassWithCppBodies(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources, string fileName)
        {
            var accessibility = cppClass.IsPublicExport ? "public" : "internal";
            sb.AppendLine($"    {accessibility} class {cppClass.Name}");
            sb.AppendLine("    {");

            // Add members - preserve original C++ types
            foreach (var member in cppClass.Members)
            {
                var accessModifier = ConvertAccessSpecifier(member.AccessSpecifier);
                var staticModifier = member.IsStatic ? "static " : "";
                sb.AppendLine($"        {accessModifier} {staticModifier}{member.Type} {member.Name};");
            }

            if (cppClass.Members.Any())
                sb.AppendLine();

            // Get method implementations from source files first to avoid duplicates
            var relatedMethods = parsedSources.Values
                .SelectMany(methods => methods)
                .Where(m => m.ClassName == cppClass.Name)
                .ToList();

            // Get names of methods that have implementations (to avoid duplicating declarations)
            var implementedMethodNames = relatedMethods.Select(m => m.Name).ToHashSet();

            // Add methods from header (declarations only) - preserve C++ syntax
            // Skip methods that have implementations in source files to avoid duplicates
            foreach (var method in cppClass.Methods.Where(m => m.AccessSpecifier == AccessSpecifier.Public))
            {
                // Skip if this method has an implementation in source files (avoid duplicates)
                if (!method.HasInlineImplementation && implementedMethodNames.Contains(method.Name))
                    continue;

                var accessModifier = ConvertAccessSpecifier(method.AccessSpecifier);
                var staticModifier = method.IsStatic ? "static " : "";
                var virtualModifier = method.IsVirtual ? "virtual " : "";
                var returnType = method.IsConstructor || method.IsDestructor ? "" : method.ReturnType + " ";
                var parameters = string.Join(", ", method.Parameters.Select(p => FormatCppParameter(p)));
                
                if (method.HasInlineImplementation)
                {
                    // Include inline implementation exactly as-is
                    sb.AppendLine($"        {accessModifier} {staticModifier}{virtualModifier}{returnType}{method.Name}({parameters})");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            {method.InlineImplementation}");
                    sb.AppendLine("        }");
                }
                else
                {
                    // Method declaration without body
                    sb.AppendLine($"        {accessModifier} {staticModifier}{virtualModifier}{returnType}{method.Name}({parameters});");
                }
                sb.AppendLine();
            }

            // Add method implementations from source files - preserve C++ code exactly as-is

            foreach (var method in relatedMethods)
            {
                var accessModifier = "public"; // Assume public for implemented methods
                var staticModifier = method.IsStatic ? "static " : "";
                var returnType = method.IsConstructor || method.IsDestructor ? "" : method.ReturnType + " ";
                var parameters = string.Join(", ", method.Parameters.Select(p => FormatCppParameter(p)));
                
                sb.AppendLine($"        {accessModifier} {staticModifier}{returnType}{method.Name}({parameters})");
                sb.AppendLine("        {");
                
                if (!string.IsNullOrEmpty(method.ImplementationBody))
                {
                    // Include the original C++ implementation body exactly as-is
                    sb.AppendLine(method.ImplementationBody);
                }
                
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
        }

        private string ConvertAccessSpecifier(AccessSpecifier access)
        {
            return access switch
            {
                AccessSpecifier.Public => "public",
                AccessSpecifier.Protected => "protected",
                AccessSpecifier.Private => "private",
                _ => "private"
            };
        }

        private string FormatCppParameter(CppParameter param)
        {
            // Preserve original C++ parameter syntax exactly as-is
            var result = "";
            
            if (param.IsConst)
                result += "const ";
                
            result += param.Type;
            
            if (param.IsReference)
                result += "&";
            else if (param.IsPointer)
                result += "*";
                
            result += " " + param.Name;
            
            if (!string.IsNullOrEmpty(param.DefaultValue))
                result += " = " + param.DefaultValue;
                
            return result;
        }
    }
}