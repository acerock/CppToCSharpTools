using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Generators;

namespace CppToCsConverter.Core.Core
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

        public void ConvertFiles(string[] headerFiles, string[] sourceFiles, string outputDirectory)
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
            // Check if this is header-only generation and show warning
            var implementationMethods = parsedSources.ContainsKey(cppClass.Name) ? parsedSources[cppClass.Name] : new List<CppMethod>();
            bool isHeaderOnlyGeneration = implementationMethods.Count == 0 || 
                                        (cppClass.Methods.Count > 0 && implementationMethods.Count < cppClass.Methods.Count / 2);

            if (isHeaderOnlyGeneration && cppClass.Methods.Any())
            {
                // Write warning to console with yellow color
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  WARNING: Generating '{fileName}' from header-only content. Methods will contain TODO implementations.");
                Console.WriteLine($"    Class: {cppClass.Name} | Header methods: {cppClass.Methods.Count} | Implementation methods: {implementationMethods.Count}");
                Console.ForegroundColor = currentColor;
            }

            // Add comments before class declaration
            if (cppClass.PrecedingComments.Any())
            {
                foreach (var comment in cppClass.PrecedingComments)
                {
                    sb.AppendLine($"    {comment}");
                }
            }

            var accessibility = cppClass.IsPublicExport ? "public" : "internal";
            var classStaticModifier = ShouldBeStaticClass(cppClass, parsedSources) ? "static " : "";
            sb.AppendLine($"    {accessibility} {classStaticModifier}class {cppClass.Name}");
            sb.AppendLine("    {");

            // Add members - preserve original C++ types and add static initializations
            foreach (var member in cppClass.Members)
            {
                // Add region start marker (from .h file, converted to comment)
                if (!string.IsNullOrEmpty(member.RegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        {member.RegionStart}");
                    sb.AppendLine();
                }

                // Add comments before member
                if (member.PrecedingComments.Any())
                {
                    foreach (var comment in member.PrecedingComments)
                    {
                        sb.AppendLine($"        {comment}");
                    }
                }

                var accessModifier = ConvertAccessSpecifier(member.AccessSpecifier);
                var staticModifier = member.IsStatic ? "static " : "";
                
                // Check if this static member has an initialization value from source files
                string initialization = "";
                string memberType = member.Type;
                string memberName = member.Name;
                
                if (member.IsStatic)
                {
                    var staticInit = staticMemberInits.Values
                        .SelectMany(inits => inits)
                        .FirstOrDefault(init => init.ClassName == cppClass.Name && init.MemberName == member.Name);
                    
                    if (staticInit != null)
                    {
                        initialization = $" = {staticInit.InitializationValue}";
                        
                        // Handle array syntax conversion: static const CString ColFrom[4] with initialization becomes 
                        // public static CString[] ColFrom = { ... };
                        if (member.IsArray || staticInit.IsArray)
                        {
                            memberType = $"{member.Type}[]";
                        }
                    }
                    else if (member.IsArray)
                    {
                        // Array declaration without initialization
                        memberType = $"{member.Type}[]";
                    }
                }
                else if (member.IsArray)
                {
                    // Non-static array member
                    memberType = $"{member.Type}[]";
                }
                
                sb.AppendLine($"        {accessModifier} {staticModifier}{memberType} {memberName}{initialization};");

                // Add region end marker (from .h file, converted to comment)  
                if (!string.IsNullOrEmpty(member.RegionEnd))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        {member.RegionEnd}");
                }
            }

            if (cppClass.Members.Any())
                sb.AppendLine();

            // Get method implementations from source files first to avoid duplicates
            var relatedMethods = parsedSources.Values
                .SelectMany(methods => methods)
                .Where(m => m.ClassName == cppClass.Name)
                .ToList();

            // Create signature-based matching for overloaded methods
            var implementedMethodSignatures = relatedMethods.Select(m => GetMethodSignature(m)).ToHashSet();

            // Add methods from header (declarations only) - preserve C++ syntax
            // Skip methods that have implementations in source files to avoid duplicates
            foreach (var method in cppClass.Methods)
            {
                // Skip if this exact method signature has an implementation in source files (avoid duplicates)
                if (!method.HasInlineImplementation && implementedMethodSignatures.Contains(GetMethodSignature(method)))
                    continue;

                // Add source region start (from .cpp file - preserved as region)
                if (!string.IsNullOrEmpty(method.SourceRegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        {method.SourceRegionStart}");
                    sb.AppendLine();
                }

                // Add header region start (from .h file - converted to comment)
                if (!string.IsNullOrEmpty(method.HeaderRegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        {method.HeaderRegionStart}");
                    sb.AppendLine();
                }

                // Add comments from .h file
                if (method.HeaderComments.Any())
                {
                    foreach (var comment in method.HeaderComments)
                    {
                        sb.AppendLine($"        {comment}");
                    }
                }

                // Add comments from .cpp file
                if (method.SourceComments.Any())
                {
                    foreach (var comment in method.SourceComments)
                    {
                        sb.AppendLine($"        {comment}");
                    }
                }

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
                    
                    // Use IndentationManager for proper context-aware indentation
                    var originalIndentation = CppToCsConverter.Core.Utils.IndentationManager.DetectOriginalIndentation(method.InlineImplementation);
                    var indentedInlineBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                        method.InlineImplementation, 
                        originalIndentation
                    );
                    sb.Append(indentedInlineBody);
                    sb.AppendLine(); // Ensure line break before closing brace
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
                // Add source region start (from .cpp file - preserved as region)
                if (!string.IsNullOrEmpty(method.SourceRegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        {method.SourceRegionStart}");
                    sb.AppendLine();
                }

                // Find corresponding header declaration to get access modifier and default values
                var headerMethod = FindMatchingHeaderMethod(cppClass.Methods, method);
                
                // Add header region start (from .h file - converted to comment)
                if (headerMethod != null && !string.IsNullOrEmpty(headerMethod.HeaderRegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"        {headerMethod.HeaderRegionStart}");
                    sb.AppendLine();
                }

                // Add comments from .h file
                if (headerMethod != null && headerMethod.HeaderComments.Any())
                {
                    foreach (var comment in headerMethod.HeaderComments)
                    {
                        sb.AppendLine($"        {comment}");
                    }
                }

                // Add comments from .cpp file
                if (method.SourceComments.Any())
                {
                    foreach (var comment in method.SourceComments)
                    {
                        sb.AppendLine($"        {comment}");
                    }
                }

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
                    // Use 8 spaces since .cpp method bodies already have indentation
                    // Use IndentationManager for proper context-aware indentation
                    var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                        method.ImplementationBody, 
                        method.ImplementationIndentation
                    );
                    sb.Append(indentedBody);
                    sb.AppendLine(); // Ensure line break before closing brace
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

        internal string IndentMethodBody(string methodBody, string indentation)
        {
            if (string.IsNullOrEmpty(methodBody))
                return "";

            // Simply add indentation to each line - preserve everything else exactly as captured
            var lines = methodBody.Split(new[] { '\n' }, StringSplitOptions.None);
            var result = new StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r'); // Remove any trailing \r from Windows line endings
                
                if (i > 0)
                {
                    result.Append('\n'); // Add line break before each line except the first
                }
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line or line with only whitespace - replace with just indentation
                    result.Append(indentation);
                }
                else
                {
                    // Non-empty line - add indentation + preserve original content exactly
                    result.Append(indentation + line);
                }
            }
            
            return result.ToString();
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

                var returnType = ConvertTypeForExtensionMethod(staticMethod.ReturnType ?? "void");
                var parameters = $"this {cppInterface.Name} instance";
                
                if (staticMethod.Parameters.Any())
                {
                    var methodParams = string.Join(", ", staticMethod.Parameters.Select(p => GenerateInterfaceParameter(p)));
                    parameters += ", " + methodParams;
                }

                sb.AppendLine($"        public static {returnType} {staticMethod.Name}({parameters})");
                sb.AppendLine("        {");

                // For interface extension methods, provide clean C# factory implementations
                if (IsFactoryMethod(staticMethod, cppInterface.Name))
                {
                    var implementationClassName = GetImplementationClassName(cppInterface.Name);
                    sb.AppendLine($"            return new {implementationClassName}();");
                }
                else if (implementation != null && !string.IsNullOrEmpty(implementation.ImplementationBody))
                {
                    // Use actual implementation body for non-factory methods
                    // Use IndentationManager for proper context-aware indentation
                    var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                        implementation.ImplementationBody, 
                        implementation.ImplementationIndentation
                    );
                    sb.Append(indentedBody);
                    sb.AppendLine(); // Ensure line break before closing brace
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

        private bool ShouldBeStaticClass(CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources)
        {
            // A class should be static if:
            // 1. All members are static 
            // 2. All methods are static (except constructors/destructors which aren't allowed in static classes)
            // 3. Has no instance constructors
            
            // Check if all members are static
            var hasNonStaticMembers = cppClass.Members.Any(m => !m.IsStatic);
            if (hasNonStaticMembers)
                return false;
                
            // Check if all methods from header are static (except constructors/destructors)
            var hasNonStaticHeaderMethods = cppClass.Methods
                .Where(m => !m.IsConstructor && !m.IsDestructor)
                .Any(m => !m.IsStatic);
            if (hasNonStaticHeaderMethods)
                return false;
                
            // Check if any methods from source files are non-static
            var relatedSourceMethods = parsedSources.Values
                .SelectMany(methods => methods)
                .Where(m => m.ClassName == cppClass.Name && !m.IsConstructor && !m.IsDestructor);
            var hasNonStaticSourceMethods = relatedSourceMethods.Any(m => !m.IsStatic);
            if (hasNonStaticSourceMethods)
                return false;
                
            // If class has only static members and methods, it should be static
            return cppClass.Members.Any() || cppClass.Methods.Any() || relatedSourceMethods.Any();
        }

        internal string GetMethodSignature(CppMethod method)
        {
            // Create a unique signature that includes method name and parameter types
            var parameterTypes = method.Parameters.Select(p => NormalizeParameterType(p.Type));
            return $"{method.Name}({string.Join(",", parameterTypes)})";
        }

        internal string NormalizeParameterType(string type)
        {
            // Normalize parameter type to handle variations in const, reference, pointer syntax
            return type.Trim()
                .Replace(" ", "")           // Remove spaces
                .Replace("const", "")       // Remove const keyword
                .Replace("&", "")           // Remove reference
                .Replace("*", "")           // Remove pointer
                .ToLowerInvariant();        // Case insensitive comparison
        }

        private CppMethod? FindMatchingHeaderMethod(List<CppMethod> headerMethods, CppMethod sourceMethod)
        {
            // First try to find exact match by name and parameter count
            var candidates = headerMethods.Where(h => h.Name == sourceMethod.Name && h.Parameters.Count == sourceMethod.Parameters.Count).ToList();
            
            if (candidates.Count == 1)
            {
                return candidates[0];
            }
            
            if (candidates.Count > 1)
            {
                // Multiple candidates, try to match by parameter types
                foreach (var candidate in candidates)
                {
                    bool typesMatch = true;
                    for (int i = 0; i < candidate.Parameters.Count; i++)
                    {
                        var headerType = NormalizeParameterType(candidate.Parameters[i].Type);
                        var sourceType = NormalizeParameterType(sourceMethod.Parameters[i].Type);
                        
                        if (headerType != sourceType)
                        {
                            typesMatch = false;
                            break;
                        }
                    }
                    
                    if (typesMatch)
                    {
                        return candidate;
                    }
                }
            }
            
            // Fall back to just name match if no perfect match found
            return headerMethods.FirstOrDefault(h => h.Name == sourceMethod.Name);
        }

        private string ConvertTypeForExtensionMethod(string cppType)
        {
            // For extension methods, convert C++ pointer types to C# reference types
            // Remove trailing pointer indicator if present
            if (cppType.EndsWith("*"))
            {
                return cppType.Substring(0, cppType.Length - 1).Trim();
            }
            
            // Return the type as-is if no conversion needed
            return cppType;
        }

        private bool IsFactoryMethod(CppMethod method, string interfaceName)
        {
            // Check if this is a factory method that returns the interface type
            // Common patterns: GetInstance, Create, etc.
            var returnType = ConvertTypeForExtensionMethod(method.ReturnType ?? "");
            return returnType == interfaceName && 
                   (method.Name.Contains("Instance") || 
                    method.Name.Contains("Create") ||
                    method.Name == "Get" ||
                    method.Name.StartsWith("Get"));
        }

        private string GetImplementationClassName(string interfaceName)
        {
            // Convert interface name to implementation class name
            // ISample -> CSample, IMyInterface -> CMyInterface, etc.
            if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
            {
                return "C" + interfaceName.Substring(1);
            }
            
            // Fallback: just prepend C
            return "C" + interfaceName;
        }
    }
}