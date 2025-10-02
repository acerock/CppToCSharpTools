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
            var staticMemberInits = new Dictionary<string, List<CppStaticMemberInit>>();

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
                var (methods, staticInits) = _sourceParser.ParseSourceFile(sourceFile);
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                parsedSources[fileName] = methods;
                staticMemberInits[fileName] = staticInits;
            }

            // Generate C# files - one per header file containing all its classes
            foreach (var headerFileKvp in headerFileClasses)
            {
                var fileName = headerFileKvp.Key;
                var classes = headerFileKvp.Value;
                
                if (classes.Count == 0)
                    continue;
                    
                Console.WriteLine($"Generating C# file: {fileName}.cs with {classes.Count} class(es)");
                
                var csFileContent = GenerateCsFileWithMultipleClasses(fileName, classes, parsedSources, staticMemberInits, outputDirectory);
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

        private string GenerateCsFileWithMultipleClasses(string fileName, List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string outputDirectory)
        {
            var sb = new StringBuilder();
            
            // Check if this file contains only interfaces
            bool containsOnlyInterfaces = classes.All(c => c.IsInterface);
            
            // Add appropriate using statements based on content type
            if (containsOnlyInterfaces)
            {
                // Interface-only files get specific using statements
                sb.AppendLine("using Agresso.Types;");
                sb.AppendLine("using BatchNet.Compatibility.Types;");
                sb.AppendLine("using U4.BatchNet.Common.Compatibility;");
            }
            else
            {
                // Files with classes get extended Agresso/BatchNet using statements
                sb.AppendLine("using Agresso.Interface.CoreServices;");
                sb.AppendLine("using Agresso.Types;");
                sb.AppendLine("using BatchNet.Compatibility.Types;");
                sb.AppendLine("using BatchNet.Fundamentals.Compatibility;");
                sb.AppendLine("using U4.BatchNet.Common.Compatibility;");
                sb.AppendLine("using static BatchNet.Compatibility.Level1;");
                sb.AppendLine("using static BatchNet.Compatibility.BatchApi;");
            }
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
                    // Generate interface without extension methods first
                    GenerateInterfaceInline(sb, cppClass);
                    
                    // Generate extension class for static methods if any exist
                    GenerateExtensionClassIfNeeded(sb, cppClass, parsedSources);
                }
                else
                {
                    // Generate class inline with preserved C++ method bodies
                    GenerateClassWithCppBodies(sb, cppClass, parsedSources, staticMemberInits, fileName);
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private void GenerateClassWithCppBodies(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string fileName)
        {
            var accessibility = cppClass.IsPublicExport ? "public" : "internal";
            sb.AppendLine($"    {accessibility} class {cppClass.Name}");
            sb.AppendLine("    {");

            // Add members - preserve original C++ types and add static initializations
            foreach (var member in cppClass.Members)
            {
                var accessModifier = ConvertAccessSpecifier(member.AccessSpecifier);
                var staticModifier = member.IsStatic ? "static " : "";
                
                // Check if this static member has an initialization value from source files
                string initialization = "";
                if (member.IsStatic)
                {
                    var staticInit = staticMemberInits.Values
                        .SelectMany(inits => inits)
                        .FirstOrDefault(init => init.ClassName == cppClass.Name && init.MemberName == member.Name);
                    
                    if (staticInit != null)
                    {
                        initialization = $" = {staticInit.InitializationValue}";
                    }
                }
                
                sb.AppendLine($"        {accessModifier} {staticModifier}{member.Type} {member.Name}{initialization};");
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

                    // Include inline implementation with proper indentation
                    sb.AppendLine($"        {accessModifier} {staticModifier}{virtualModifier}{returnType}{method.Name}({parameters})");
                    sb.AppendLine("        {");
                    
                    // For constructors, add member initializer assignments first
                    if (method.IsConstructor && method.MemberInitializerList.Count > 0)
                    {
                        foreach (var initializer in method.MemberInitializerList)
                        {
                            var convertedValue = ConvertCppToCsValue(initializer.InitializationValue);
                            sb.AppendLine($"            {initializer.MemberName} = {convertedValue};");
                        }
                    }
                    
                    var indentedInlineBody = IndentMethodBody(method.InlineImplementation, "            ");
                    sb.Append(indentedInlineBody);
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
                // Find corresponding header declaration to get access modifier and default values
                var headerMethod = cppClass.Methods.FirstOrDefault(h => h.Name == method.Name);
                var accessModifier = headerMethod != null ? ConvertAccessSpecifier(headerMethod.AccessSpecifier) : "public";
                var staticModifier = method.IsStatic ? "static " : "";
                var returnType = method.IsConstructor || method.IsDestructor ? "" : method.ReturnType + " ";
                
                var parametersWithDefaults = MergeParametersWithDefaults(method.Parameters, headerMethod?.Parameters);
                var parameters = string.Join(", ", parametersWithDefaults.Select(p => FormatCppParameter(p)));
                
                sb.AppendLine($"        {accessModifier} {staticModifier}{returnType}{method.Name}({parameters})");
                sb.AppendLine("        {");
                
                if (!string.IsNullOrEmpty(method.ImplementationBody))
                {
                    // Include the original C++ implementation body with proper indentation
                    var indentedBody = IndentMethodBody(method.ImplementationBody, "            ");
                    sb.Append(indentedBody);
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

        private List<CppParameter> MergeParametersWithDefaults(List<CppParameter> implParameters, List<CppParameter>? headerParameters)
        {
            if (headerParameters == null || headerParameters.Count == 0)
                return implParameters;

            var mergedParameters = new List<CppParameter>();
            
            for (int i = 0; i < implParameters.Count; i++)
            {
                var implParam = implParameters[i];
                var mergedParam = new CppParameter
                {
                    Name = implParam.Name, // Use implementation parameter name
                    Type = implParam.Type, // Use implementation parameter type
                    IsConst = implParam.IsConst,
                    IsReference = implParam.IsReference,
                    IsPointer = implParam.IsPointer,
                    DefaultValue = "" // Will be set below
                };

                // If there's a corresponding header parameter at the same position, use its default value
                if (i < headerParameters.Count && !string.IsNullOrEmpty(headerParameters[i].DefaultValue))
                {
                    mergedParam.DefaultValue = headerParameters[i].DefaultValue;
                }

                mergedParameters.Add(mergedParam);
            }

            return mergedParameters;
        }

        private string IndentMethodBody(string methodBody, string indentation)
        {
            if (string.IsNullOrEmpty(methodBody))
                return "";

            var sb = new StringBuilder();
            var lines = methodBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart();
                
                // Skip completely empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Determine additional indentation for this line
                string additionalIndent = "";
                
                // Check if previous line was a control statement without braces
                if (i > 0)
                {
                    var prevLine = lines[i - 1].TrimStart();
                    if ((prevLine.StartsWith("if ") || prevLine.StartsWith("for ") || 
                         prevLine.StartsWith("while ") || prevLine.StartsWith("else")) &&
                        !prevLine.TrimEnd().EndsWith("{") && !prevLine.TrimEnd().EndsWith(";"))
                    {
                        // The current line should be indented as it's the body of the control statement
                        additionalIndent = "    ";
                    }
                }
                
                // Add proper indentation to all lines
                sb.AppendLine(indentation + additionalIndent + line);
                
                // Add empty line after certain statements for readability
                if ((line.EndsWith(";") && !line.StartsWith("return")) || 
                    (line.StartsWith("return") && i < lines.Length - 1))
                {
                    var nextLine = i + 1 < lines.Length ? lines[i + 1].TrimStart() : "";
                    if (!string.IsNullOrWhiteSpace(nextLine) && !nextLine.StartsWith("}"))
                    {
                        sb.AppendLine();
                    }
                }
            }
            
            return sb.ToString();
        }

        private void GenerateInterfaceInline(StringBuilder sb, CppClass cppInterface)
        {
            // Generate just the interface part (without extension methods)
            var accessibility = cppInterface.IsPublicExport ? "public" : "internal";
            sb.AppendLine($"    {accessibility} interface {cppInterface.Name}");
            sb.AppendLine("    {");

            // Add methods (skip constructors, destructors, and static methods for interfaces)
            var interfaceMethods = cppInterface.Methods
                .Where(m => !m.IsConstructor && !m.IsDestructor && !m.IsStatic)
                .Where(m => m.AccessSpecifier == AccessSpecifier.Public);

            foreach (var method in interfaceMethods)
            {
                var returnType = method.ReturnType ?? "void";
                var parameters = string.Join(", ", method.Parameters.Select(p => GenerateInterfaceParameter(p)));
                sb.AppendLine($"        {returnType} {method.Name}({parameters});");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
        }

        private void GenerateExtensionClassIfNeeded(StringBuilder sb, CppClass cppInterface, Dictionary<string, List<CppMethod>> parsedSources)
        {
            var staticMethods = cppInterface.Methods
                .Where(m => m.IsStatic && m.AccessSpecifier == AccessSpecifier.Public)
                .ToList();

            if (!staticMethods.Any())
                return;

            var allSourceMethods = parsedSources.Values.SelectMany(methods => methods).ToList();

            sb.AppendLine();
            sb.AppendLine($"    public static class {cppInterface.Name}Extensions");
            sb.AppendLine("    {");

            foreach (var staticMethod in staticMethods)
            {
                // Find implementation in source files
                var implementation = allSourceMethods.FirstOrDefault(impl =>
                    impl.ClassName == cppInterface.Name &&
                    impl.Name == staticMethod.Name &&
                    !string.IsNullOrEmpty(impl.ImplementationBody));

                var returnType = staticMethod.ReturnType ?? "void";
                var parameters = $"this {cppInterface.Name} instance";
                
                if (staticMethod.Parameters.Any())
                {
                    var methodParams = string.Join(", ", staticMethod.Parameters.Select(p => GenerateInterfaceParameter(p)));
                    parameters += ", " + methodParams;
                }

                sb.AppendLine($"        public static {returnType} {staticMethod.Name}({parameters})");
                sb.AppendLine("        {");

                if (implementation != null && !string.IsNullOrEmpty(implementation.ImplementationBody))
                {
                    // Use actual implementation body
                    var indentedBody = IndentMethodBody(implementation.ImplementationBody, "            ");
                    sb.Append(indentedBody);
                }
                else
                {
                    sb.AppendLine("            // TODO: Implementation not found");
                    sb.AppendLine("            throw new NotImplementedException();");
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
        }

        private string GenerateInterfaceParameter(CppParameter param)
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

        private string ConvertCppToCsValue(string cppValue)
        {
            if (string.IsNullOrWhiteSpace(cppValue))
                return "default";

            // Basic conversions for common initialization values
            var trimmed = cppValue.Trim();
            
            // Handle numeric literals
            if (int.TryParse(trimmed, out _) || 
                double.TryParse(trimmed, out _) || 
                trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            // Handle string literals
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            {
                return trimmed;
            }

            // Handle character literals
            if (trimmed.StartsWith("'") && trimmed.EndsWith("'"))
            {
                return trimmed;
            }

            // Handle nullptr/NULL
            if (trimmed.Equals("nullptr", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                return "null";
            }

            // For other values, return as-is (might be constants, enums, etc.)
            return trimmed;
        }
    }
}