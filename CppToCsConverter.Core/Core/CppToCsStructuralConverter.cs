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
            var headerFileStructs = new Dictionary<string, List<CppStruct>>();
            
            foreach (var headerFile in headerFiles)
            {
                Console.WriteLine($"Parsing header: {Path.GetFileName(headerFile)}");
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var structs = _headerParser.ParseStructsFromHeaderFile(headerFile);
                var fileName = Path.GetFileNameWithoutExtension(headerFile);
                
                headerFileClasses[fileName] = classes;
                headerFileStructs[fileName] = structs;
                
                // Also add to the main dictionary for backward compatibility
                foreach (var cppClass in classes)
                {
                    parsedHeaders[cppClass.Name] = cppClass;
                    Console.WriteLine($"Found class: {cppClass.Name} in {Path.GetFileName(headerFile)}");
                }
                
                foreach (var cppStruct in structs)
                {
                    Console.WriteLine($"Found struct: {cppStruct.Name} in {Path.GetFileName(headerFile)}");
                }
            }

            // Parse source files with complete file data including top comments
            var sourceDefines = new Dictionary<string, List<CppDefine>>();
            var sourceFileTopComments = new Dictionary<string, List<string>>();
            foreach (var sourceFile in sourceFiles)
            {
                Console.WriteLine($"Parsing source: {Path.GetFileName(sourceFile)}");
                var sourceFileData = _sourceParser.ParseSourceFileComplete(sourceFile);
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                parsedSources[fileName] = sourceFileData.Methods;
                staticMemberInits[fileName] = sourceFileData.StaticMemberInits;
                sourceDefines[fileName] = sourceFileData.Defines;
                sourceFileTopComments[fileName] = sourceFileData.FileTopComments;
            }

            // Generate C# files - one per header file containing all its classes and structs
            foreach (var headerFileKvp in headerFileClasses)
            {
                var fileName = headerFileKvp.Key;
                var classes = headerFileKvp.Value;
                var structs = headerFileStructs.ContainsKey(fileName) ? headerFileStructs[fileName] : new List<CppStruct>();
                
                if (classes.Count == 0 && structs.Count == 0)
                    continue;
                    
                Console.WriteLine($"Generating C# file: {fileName}.cs with {classes.Count} class(es) and {structs.Count} struct(s)");
                
                // Generate main C# file
                GenerateAndWriteFile(fileName, outputDirectory, classes, structs, parsedSources, staticMemberInits, sourceDefines, sourceFileTopComments);
                
                // Generate additional partial class files for classes that need them
                GenerateAdditionalPartialFiles(fileName, classes, parsedSources, staticMemberInits, sourceFileTopComments, outputDirectory);
            }
            
            // Old individual class generation logic has been replaced with file-based generation above

            Console.WriteLine("Conversion completed!");
        }



        private void AddUsingStatements(StringBuilder sb, bool interfaceOnly)
        {
            if (interfaceOnly)
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
        }

        private void AddFileTopComments(StringBuilder sb, string fileName, Dictionary<string, List<string>>? sourceFileTopComments)
        {
            if (sourceFileTopComments == null)
                return;
                
            // Look for file top comments from the specific source file that matches this output file
            var relevantComments = new List<string>();
            
            // Check for exact filename match
            if (sourceFileTopComments.ContainsKey(fileName))
            {
                relevantComments.AddRange(sourceFileTopComments[fileName]);
            }
            
            // Write comments if any found
            if (relevantComments.Any())
            {
                foreach (var comment in relevantComments)
                {
                    sb.AppendLine(comment);
                }
                sb.AppendLine(); // Add blank line after file top comments
            }
        }

        private void AddNamespace(StringBuilder sb, string fileName)
        {
            sb.AppendLine($"namespace Generated_{fileName}");
            sb.AppendLine("{");
        }

        private void GenerateFileContent(StringBuilder sb, List<CppClass> classes, List<CppStruct> structs, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, Dictionary<string, List<CppDefine>>? sourceDefines, string fileName, bool isPartialFile, List<CppMethod>? partialMethods = null)
        {
            if (isPartialFile)
            {
                // For partial files, generate only the specified class with specific methods
                if (classes.Count > 0)
                {
                    var cppClass = classes[0];
                    string classAccessModifier = cppClass.IsPublicExport ? "public" : "internal";
                    sb.AppendLine($"    {classAccessModifier} partial class {cppClass.Name}");
                    sb.AppendLine("    {");

                    // Generate methods for this partial file
                    if (partialMethods != null)
                    {
                        foreach (var method in partialMethods)
                        {
                            GenerateMethodForPartialClass(sb, method, cppClass.Name);
                        }
                    }

                    sb.AppendLine("    }");
                }
            }
            else
            {
                // Generate structs first (maintain order from .h file)
                for (int i = 0; i < structs.Count; i++)
                {
                    var cppStruct = structs[i];
                    
                    if (i > 0 || classes.Count > 0)
                        sb.AppendLine(); // Add blank line between items
                    
                    GenerateStructInline(sb, cppStruct);
                }

                // Associate source defines with classes before generation
                AssociateSourceDefinesWithClasses(classes, parsedSources, sourceDefines);

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
            }
        }

        private void AssociateSourceDefinesWithClasses(List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppDefine>>? sourceDefines)
        {
            if (sourceDefines == null || !sourceDefines.Any())
                return;

            // Process each source file and determine which class should get its defines
            foreach (var sourceEntry in parsedSources)
            {
                var sourceFileName = sourceEntry.Key;
                var methods = sourceEntry.Value;
                
                if (!sourceDefines.ContainsKey(sourceFileName))
                    continue;
                    
                var definesForThisFile = sourceDefines[sourceFileName];
                if (!definesForThisFile.Any())
                    continue;
                
                // Find which class should get the defines from this source file
                var targetClass = DetermineTargetClassForSourceDefines(classes, methods, sourceFileName);
                
                if (targetClass != null)
                {
                    targetClass.SourceDefines.AddRange(definesForThisFile);
                }
            }
        }

        private CppClass? DetermineTargetClassForSourceDefines(List<CppClass> classes, List<CppMethod> methods, string sourceFileName)
        {
            // Get classes that have methods in this source file
            var classesWithMethods = classes
                .Where(c => methods.Any(m => m.ClassName == c.Name))
                .ToList();
                
            if (!classesWithMethods.Any())
                return null;
                
            // Strategy 1: If there's a class whose name matches the source filename, use that
            var matchingClass = classesWithMethods
                .FirstOrDefault(c => c.Name.Equals(sourceFileName, StringComparison.OrdinalIgnoreCase));
            if (matchingClass != null)
                return matchingClass;
            
            // Strategy 2: If there's only one class with methods in this file, use that
            if (classesWithMethods.Count == 1)
                return classesWithMethods[0];
            
            // Strategy 3: Use the class with the most methods in this source file
            var classMethodCounts = classesWithMethods
                .Select(c => new { Class = c, MethodCount = methods.Count(m => m.ClassName == c.Name) })
                .OrderByDescending(x => x.MethodCount)
                .ToList();
                
            return classMethodCounts.First().Class;
        }

        private void GenerateAndWriteFile(string fileName, string outputDirectory, List<CppClass> classes, List<CppStruct> structs, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, Dictionary<string, List<CppDefine>>? sourceDefines = null, Dictionary<string, List<string>>? sourceFileTopComments = null, bool isPartialFile = false, List<CppMethod>? partialMethods = null)
        {
            var sb = new StringBuilder();
            
            // Determine if interface-only for using statements
            bool containsOnlyInterfaces = classes.All(c => c.IsInterface);
            
            AddFileTopComments(sb, fileName, sourceFileTopComments);
            AddUsingStatements(sb, containsOnlyInterfaces);
            AddNamespace(sb, fileName);
            GenerateFileContent(sb, classes, structs, parsedSources, staticMemberInits, sourceDefines, fileName, isPartialFile, partialMethods);
            
            sb.AppendLine("}");
            
            // Write the file
            var csFileName = Path.Combine(outputDirectory, $"{fileName}.cs");
            WriteFileToDirectory(csFileName, sb.ToString(), fileName);
        }

        private void WriteDefineStatementsInline(StringBuilder sb, CppClass cppClass)
        {
            // Write header defines first
            foreach (var define in cppClass.HeaderDefines)
            {
                WriteCommentsAndDefineInline(sb, define);
            }
            
            // Then source defines (ordered by source file)
            foreach (var define in cppClass.SourceDefines.OrderBy(d => d.SourceFileName))
            {
                WriteCommentsAndDefineInline(sb, define);
            }
            
            // Add a blank line after defines if any were written
            if (cppClass.HeaderDefines.Any() || cppClass.SourceDefines.Any())
            {
                sb.AppendLine();
            }
        }

        private void WriteCommentsAndDefineInline(StringBuilder sb, CppDefine define)
        {
            // Write preceding comments with proper indentation
            foreach (var comment in define.PrecedingComments)
            {
                sb.AppendLine($"        {comment}");
            }
            
            // Write the define statement itself with proper indentation
            sb.AppendLine($"        {define.FullDefinition}");
        }

        private void WriteFileToDirectory(string filePath, string content, string fileName)
        {
            try
            {
                Console.WriteLine($"Writing C# file: {filePath}");
                var normalizedContent = content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                File.WriteAllText(filePath, normalizedContent);
                Console.WriteLine($"Generated C# file: {fileName}.cs (Size: {content.Length} chars)");
                
                // Verify file was written
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    Console.WriteLine($"File verified: {filePath} (Size: {fileInfo.Length} bytes)");
                }
                else
                {
                    Console.WriteLine($"ERROR: File was not created: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR writing C# file {filePath}: {ex.Message}");
                throw;
            }
        }

        private void GenerateClassWithCppBodies(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string fileName)
        {
            // First, enrich header methods with TargetFileName from source implementations
            EnrichMethodsWithTargetFileNames(cppClass, parsedSources);
            
            // Check if this class needs partial generation based on TargetFileName distribution
            if (cppClass.IsPartialClass())
            {
                GeneratePartialClass(sb, cppClass, parsedSources, staticMemberInits, fileName);
                return;
            }
            
            // Check if this is header-only generation and show warning
            var implementationMethods = parsedSources.ContainsKey(cppClass.Name) ? parsedSources[cppClass.Name] : new List<CppMethod>();
            
            // Check if this is truly header-only generation (no implementations AND no inline methods)
            var methodsNeedingImplementation = cppClass.Methods.Where(m => 
                !m.HasInlineImplementation && 
                !string.IsNullOrEmpty(m.Name) && 
                !m.IsConstructor).Count();
            
            var availableImplementations = implementationMethods.Count;
            bool hasAnyMethodBodies = cppClass.Methods.Any(m => m.HasInlineImplementation) || availableImplementations > 0;
            
            // Only warn if there are methods needing implementation but no bodies available anywhere
            bool isHeaderOnlyGeneration = methodsNeedingImplementation > 0 && !hasAnyMethodBodies;

            if (isHeaderOnlyGeneration)
            {
                // Write warning to console with yellow color
                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  WARNING: Generating '{fileName}' from header-only content. Methods will contain TODO implementations.");
                Console.WriteLine($"    Class: {cppClass.Name} | Header methods: {cppClass.Methods.Count} | Implementation methods: {availableImplementations}");
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

            // Add define statements first
            WriteDefineStatementsInline(sb, cppClass);

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
                // Debug for TrickyToMatch
                if (method.Name == "TrickyToMatch")
                {

                }

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
                
                // Debug for TrickyToMatch
                if (method.Name == "TrickyToMatch")
                {

                }
                
                sb.AppendLine($"        {accessModifier} {staticModifier}{returnType}{method.Name}({parameters})");
                sb.AppendLine("        {");
                
                // Debug for TrickyToMatch
                if (method.Name == "TrickyToMatch")
                {

                }
                
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
            
            // Add inline comments if present
            if (param.InlineComments != null && param.InlineComments.Any())
            {
                foreach (var comment in param.InlineComments)
                {
                    result += " " + comment;
                }
            }
                
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
                    DefaultValue = "", // Will be set below
                    InlineComments = implParam.InlineComments, // Use source comments for implemented methods
                    OriginalText = implParam.OriginalText
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
            // Debug output for TrickyToMatch
            if (sourceMethod.Name == "TrickyToMatch")
            {

            }

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

        /// <summary>
        /// Generates a struct as-is from C++ without transformation
        /// </summary>
        private void GenerateStructInline(StringBuilder sb, CppStruct cppStruct)
        {
            // Add comments before struct declaration
            if (cppStruct.PrecedingComments.Any())
            {
                foreach (var comment in cppStruct.PrecedingComments)
                {
                    sb.AppendLine($"    {comment}");
                }
            }

            // Normalize line endings in the original definition first
            var normalizedDefinition = cppStruct.OriginalDefinition.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            var lines = normalizedDefinition.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    // Preserve original indentation and add base indentation for C# file
                    sb.AppendLine($"    {line}");
                }
            }
            
            // Add blank line after struct
            sb.AppendLine();
        }

        private void GeneratePartialClass(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string fileName)
        {
            // Generate the main partial class file content (header-based content)
            GenerateMainPartialClass(sb, cppClass, staticMemberInits, fileName);
        }

        private void GenerateMainPartialClass(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string fileName)
        {
            // Add comments before class declaration
            if (cppClass.PrecedingComments.Any())
            {
                foreach (var comment in cppClass.PrecedingComments)
                {
                    sb.AppendLine($"    {comment}");
                }
            }

            // Determine access modifier for the class
            string classAccessModifier = cppClass.IsPublicExport ? "public" : "internal";
            
            sb.AppendLine($"    {classAccessModifier} partial class {cppClass.Name}");
            sb.AppendLine("    {");

            // Generate members (fields) - these go in the main partial class
            foreach (var member in cppClass.Members)
            {
                GenerateMemberForPartialClass(sb, member, staticMemberInits, cppClass.Name);
            }

            // Generate static members 
            foreach (var staticMember in cppClass.StaticMembers)
            {
                GenerateStaticMemberForPartialClass(sb, staticMember, staticMemberInits, cppClass.Name);
            }

            // Generate methods for main file (inline methods from header + methods from same-named source file)
            var methodsForMainFile = cppClass.Methods.Where(m => 
                string.IsNullOrEmpty(m.TargetFileName) || m.TargetFileName == fileName).ToList();
            if (methodsForMainFile.Any())
            {
                sb.AppendLine();
                sb.AppendLine("        // Methods for main file (inline + same-named source)");
                foreach (var method in methodsForMainFile)
                {
                    GenerateMethodForPartialClass(sb, method, cppClass.Name);
                }
            }

            sb.AppendLine("    }");
        }

        private void GenerateMemberForPartialClass(StringBuilder sb, CppMember member, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string className)
        {
            // Add region start marker if present
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
            
            // Generate member with same logic as existing code
            string initialization = "";
            string memberType = member.Type;
            string memberName = member.Name;
            
            if (member.IsStatic)
            {
                var staticInit = staticMemberInits.Values
                    .SelectMany(inits => inits)
                    .FirstOrDefault(init => init.ClassName == className && init.MemberName == member.Name);
                
                if (staticInit != null)
                {
                    initialization = $" = {staticInit.InitializationValue}";
                    
                    if (member.IsArray || staticInit.IsArray)
                    {
                        memberType = staticInit.Type + "[]";
                    }
                }
            }

            sb.AppendLine($"        {accessModifier} {staticModifier}{memberType} {memberName}{initialization};");

            // Add region end marker if present
            if (!string.IsNullOrEmpty(member.RegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"        {member.RegionEnd}");
                sb.AppendLine();
            }
        }

        private void GenerateStaticMemberForPartialClass(StringBuilder sb, CppStaticMember staticMember, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string className)
        {
            // Use same logic as existing static member generation
            var staticInit = staticMemberInits.Values
                .SelectMany(inits => inits)
                .FirstOrDefault(init => init.ClassName == className && init.MemberName == staticMember.Name);
            
            string initialization = "";
            string memberType = staticMember.Type;
            
            if (staticInit != null)
            {
                initialization = $" = {staticInit.InitializationValue}";
                if (staticInit.IsArray)
                {
                    memberType = staticInit.Type + "[]";
                }
            }
            
            sb.AppendLine($"        public static {memberType} {staticMember.Name}{initialization};");
        }

        private void GenerateMethodForPartialClass(StringBuilder sb, CppMethod method, string className)
        {
            // Use existing method generation logic
            string accessModifier = ConvertAccessSpecifier(method.AccessSpecifier);
            string staticModifier = method.IsStatic ? "static " : "";
            string virtualModifier = method.IsVirtual ? "virtual " : "";
            string returnType = string.IsNullOrWhiteSpace(method.ReturnType) ? "void" : method.ReturnType;
            
            var parameters = string.Join(", ", method.Parameters.Select(p => FormatCppParameter(p)));
            
            sb.AppendLine($"        {accessModifier} {staticModifier}{virtualModifier}{returnType} {method.Name}({parameters})");
            sb.AppendLine("        {");
            
            // Use implementation body if available
            if (!string.IsNullOrEmpty(method.ImplementationBody))
            {
                var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                    method.ImplementationBody, 
                    method.ImplementationIndentation
                );
                sb.Append(indentedBody);
                sb.AppendLine();
            }
            else if (!string.IsNullOrEmpty(method.InlineImplementation))
            {
                var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                    method.InlineImplementation, 
                    0
                );
                sb.Append(indentedBody);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("            // TODO: Implement method body");
            }
            
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void EnrichMethodsWithTargetFileNames(CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources)
        {
            // Get all source methods from all parsed source files
            var allSourceMethods = parsedSources.Values.SelectMany(methods => methods).ToList();
            
            foreach (var headerMethod in cppClass.Methods)
            {
                // Skip methods that already have a TargetFileName (e.g., inline methods)
                if (!string.IsNullOrEmpty(headerMethod.TargetFileName))
                    continue;
                
                // Find matching source method
                var sourceMethod = allSourceMethods.FirstOrDefault(sm => 
                    sm.ClassName == cppClass.Name && 
                    sm.Name == headerMethod.Name &&
                    ParametersMatch(headerMethod.Parameters, sm.Parameters));
                
                if (sourceMethod != null)
                {
                    // Copy TargetFileName and ImplementationBody from source method
                    headerMethod.TargetFileName = sourceMethod.TargetFileName;
                    if (string.IsNullOrEmpty(headerMethod.ImplementationBody))
                    {
                        headerMethod.ImplementationBody = sourceMethod.ImplementationBody;
                        headerMethod.ImplementationIndentation = sourceMethod.ImplementationIndentation;
                    }
                }
            }
        }

        private bool ParametersMatch(List<CppParameter> headerParams, List<CppParameter> sourceParams)
        {
            if (headerParams.Count != sourceParams.Count)
                return false;
            
            for (int i = 0; i < headerParams.Count; i++)
            {
                // For a simple match, just compare parameter count
                // More sophisticated matching could compare types, but parameter count is usually sufficient
                // since method names + parameter count typically uniquely identify methods in C++
            }
            
            return true;
        }

        private void GenerateAdditionalPartialFiles(string fileName, List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, Dictionary<string, List<string>>? sourceFileTopComments, string outputDirectory)
        {
            foreach (var cppClass in classes)
            {
                // Skip interfaces - they should not be processed as partial classes
                if (cppClass.IsInterface)
                    continue;
                    
                // First, enrich header methods with TargetFileName from source implementations
                EnrichMethodsWithTargetFileNames(cppClass, parsedSources);
                
                // Check if this class needs partial generation
                if (cppClass.IsPartialClass())
                {
                    // Get methods grouped by target file
                    var methodsByTargetFile = cppClass.GetMethodsByTargetFile();
                    var targetFileNames = cppClass.GetTargetFileNames();
                    
                    // Generate a separate partial file for each target file that has methods
                    foreach (var targetFile in targetFileNames)
                    {
                        // Skip generating partial file if it has the same name as the main file (to avoid overwriting)
                        if (targetFile == fileName)
                            continue;
                            
                        var methodsForTarget = methodsByTargetFile[targetFile];
                        if (methodsForTarget.Any())
                        {
                            GeneratePartialClassFile(cppClass, targetFile, methodsForTarget, sourceFileTopComments, outputDirectory);
                        }
                    }
                }
            }
        }

        private void GeneratePartialClassFile(CppClass cppClass, string targetFileName, List<CppMethod> methods, Dictionary<string, List<string>>? sourceFileTopComments, string outputDirectory)
        {
            // Use the refactored method to generate and write the partial file
            var classes = new List<CppClass> { cppClass };
            var structs = new List<CppStruct>(); // Partial files don't include structs
            var parsedSources = new Dictionary<string, List<CppMethod>>();
            var staticMemberInits = new Dictionary<string, List<CppStaticMemberInit>>();
            
            GenerateAndWriteFile(targetFileName, outputDirectory, classes, structs, parsedSources, staticMemberInits, sourceDefines: null, sourceFileTopComments: sourceFileTopComments, isPartialFile: true, partialMethods: methods);
        }
    }
}