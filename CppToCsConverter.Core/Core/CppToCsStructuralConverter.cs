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

            ConvertFiles(headerFiles, sourceFiles, outputDirectory, sourceDirectory);
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

            ConvertFiles(headerFiles.ToArray(), sourceFiles.ToArray(), outputDirectory, sourceDirectory);
        }

        public void ConvertFiles(string[] headerFiles, string[] sourceFiles, string outputDirectory, string sourceDirectory = "")
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
                
                // Log what we found (classes and structs are now unified)
                foreach (var cppClass in classes)
                {
                    parsedHeaders[cppClass.Name] = cppClass;
                    var type = cppClass.IsInterface ? "interface" : (cppClass.IsStruct ? "struct" : "class");
                    Console.WriteLine($"Found {type}: {cppClass.Name} in {Path.GetFileName(headerFile)}");
                }
            }

            // Parse source files with complete file data including top comments
            var sourceDefines = new Dictionary<string, List<CppDefine>>();
            var sourceFileTopComments = new Dictionary<string, List<string>>();
            var sourceStructs = new Dictionary<string, List<CppStruct>>();
            foreach (var sourceFile in sourceFiles)
            {
                Console.WriteLine($"Parsing source: {Path.GetFileName(sourceFile)}");
                var sourceFileData = _sourceParser.ParseSourceFileComplete(sourceFile);
                
                // Skip source files that have no class methods (methods with ::)
                // These are files with only local functions, structs, or MAIN macros
                var hasClassMethods = sourceFileData.Methods.Any(m => !m.IsLocalMethod);
                if (!hasClassMethods)
                {
                    Console.WriteLine($"Skipping {Path.GetFileName(sourceFile)} - no class methods found (only local functions/structs)");
                    continue;
                }
                
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                parsedSources[fileName] = sourceFileData.Methods;
                staticMemberInits[fileName] = sourceFileData.StaticMemberInits;
                sourceDefines[fileName] = sourceFileData.Defines;
                sourceFileTopComments[fileName] = sourceFileData.FileTopComments;
                sourceStructs[fileName] = sourceFileData.Structs;
                
                // Log found structs
                foreach (var structDef in sourceFileData.Structs)
                {
                    Console.WriteLine($"Found struct: {structDef.Name} in {Path.GetFileName(sourceFile)}");
                }
                
                // Debug parsed methods
                Console.WriteLine($"DEBUG: Parsed {sourceFileData.Methods.Count} methods from {fileName}");
                foreach (var method in sourceFileData.Methods)
                {
                    if (method.Name == "GetRate")
                    {
                        Console.WriteLine($"  Found GetRate in {fileName} with {method.Parameters.Count} parameters");
                        foreach (var param in method.Parameters)
                        {
                            Console.WriteLine($"    Param: {param.Name}, PositionedComments: {param.PositionedComments?.Count ?? 0}");
                        }
                    }
                }
                

            }

            // Add structs from source files to the header file classes (so they get generated)
            // Insert them after header structs but before the main class
            foreach (var sourceStructKvp in sourceStructs)
            {
                var fileName = sourceStructKvp.Key;
                var structs = sourceStructKvp.Value;
                
                // Convert CppStruct to CppClass
                foreach (var cppStruct in structs)
                {
                    var cppClass = new CppClass
                    {
                        Name = cppStruct.Name,
                        IsStruct = true,
                        IsPublicExport = false, // Structs from source files are internal
                        Members = cppStruct.Members,
                        Methods = cppStruct.Methods, // Copy methods (including constructors)
                        PrecedingComments = cppStruct.PrecedingComments ?? new List<string>()
                    };
                    
                    // Add to the same file's class list
                    if (!headerFileClasses.ContainsKey(fileName))
                    {
                        headerFileClasses[fileName] = new List<CppClass>();
                    }
                    
                    // Insert struct before the first non-struct class (the main class)
                    var insertIndex = headerFileClasses[fileName].FindIndex(c => !c.IsStruct);
                    if (insertIndex >= 0)
                    {
                        headerFileClasses[fileName].Insert(insertIndex, cppClass);
                    }
                    else
                    {
                        // No main class yet, just add at end
                        headerFileClasses[fileName].Add(cppClass);
                    }
                }
            }

            // Generate C# files - one per header file containing all its classes (including structs as classes)
            foreach (var headerFileKvp in headerFileClasses)
            {
                var fileName = headerFileKvp.Key;
                var classes = headerFileKvp.Value;
                
                if (classes.Count == 0)
                    continue;
                    
                Console.WriteLine($"Generating C# file: {fileName}.cs with {classes.Count} type(s)");
                
                // Generate main C# file
                GenerateAndWriteFile(fileName, outputDirectory, classes, parsedSources, staticMemberInits, sourceDirectory, sourceDefines, sourceFileTopComments);
                
                // Generate additional partial class files for classes that need them
                GenerateAdditionalPartialFiles(fileName, classes, parsedSources, staticMemberInits, sourceFileTopComments, outputDirectory, sourceDirectory);
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
                sb.AppendLine("using BatchNet;");
                sb.AppendLine("using BatchNet.Compatibility;");
                sb.AppendLine("using U4.BatchNet.ServerLib.Compatibility;");
            }
            else
            {
                // Files with classes get extended Agresso/BatchNet using statements
                sb.AppendLine("using Agresso.Types;");
                sb.AppendLine("using Agresso.Interface.CoreServices;");
                sb.AppendLine("using BatchNet;");
                sb.AppendLine("using BatchNet.Compatibility;");
                sb.AppendLine("using BatchNet.Fundamentals.Compatibility;");
                sb.AppendLine("using U4.BatchNet.ServerLib.Compatibility;");
                sb.AppendLine("using static BatchNet.Compatibility.Level1;");
                sb.AppendLine("using static BatchNet.Compatibility.Level2;");
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

        private void AddNamespace(StringBuilder sb, string fileName, string sourceDirectory)
        {
            string namespaceName = ResolveNamespace(sourceDirectory);
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        /// <summary>
        /// Resolves the namespace based on the source directory path according to README requirements.
        /// Pattern: "U4.BatchNet.XX.Compatibility" where XX is the last two uppercase characters of the input folder,
        /// or the full folder name if less than two uppercase characters are found.
        /// If the folder name contains '.', '_', or '-', only trailing characters after the last occurrence are considered.
        /// </summary>
        /// <param name="sourceDirectory">The source directory path</param>
        /// <returns>The resolved namespace</returns>
        public string ResolveNamespace(string sourceDirectory)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
            {
                return "Generated_Unknown";
            }

            // Get the directory name from the full path
            string folderName = Path.GetFileName(sourceDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            
            if (string.IsNullOrEmpty(folderName))
            {
                return "Generated_Unknown";
            }

            // If folder name contains '.', '_', or '-', only consider trailing characters after the last occurrence
            char[] separators = { '.', '_', '-' };
            int lastSeparatorIndex = -1;
            
            foreach (char separator in separators)
            {
                int index = folderName.LastIndexOf(separator);
                if (index > lastSeparatorIndex)
                {
                    lastSeparatorIndex = index;
                }
            }
            
            // Extract the trailing part after the last separator, or use the full name if no separators found
            string relevantPart = lastSeparatorIndex >= 0 ? folderName.Substring(lastSeparatorIndex + 1) : folderName;

            // Extract the last two uppercase characters from the relevant part
            var upperCaseChars = relevantPart.Where(char.IsUpper).ToArray();
            
            if (upperCaseChars.Length >= 2)
            {
                // Take the last two uppercase characters
                string suffix = new string(upperCaseChars.Skip(upperCaseChars.Length - 2).ToArray());
                return $"U4.BatchNet.{suffix}.Compatibility";
            }
            else
            {
                // Less than two uppercase characters found, use the relevant part
                return $"U4.BatchNet.{relevantPart}.Compatibility";
            }
        }

        private void GenerateFileContent(StringBuilder sb, List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, Dictionary<string, List<CppDefine>>? sourceDefines, string fileName, bool isPartialFile, List<CppMethod>? partialMethods = null)
        {
            if (isPartialFile)
            {
                // For partial files, generate only the specified class with specific methods
                if (classes.Count > 0)
                {
                    var cppClass = classes[0];
                    string classAccessModifier = cppClass.IsStruct ? "internal" : 
                                                (cppClass.IsPublicExport ? "public" : "internal");
                    sb.AppendLine($"{classAccessModifier} partial class {cppClass.Name}");
                    sb.AppendLine("{");

                    // Generate methods for this partial file
                    if (partialMethods != null)
                    {
                        foreach (var method in partialMethods)
                        {
                            GenerateMethodForPartialClass(sb, method, cppClass.Name, parsedSources, "    "); // 4 spaces for partial files
                        }
                    }

                    sb.AppendLine("}");
                }
            }
            else
            {
                // Associate source defines with classes before generation
                AssociateSourceDefinesWithClasses(classes, parsedSources, sourceDefines);

                // Generate each class in the file (structs are now classes with IsStruct=true)
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
                        // Generate class inline with preserved C++ method bodies (structs go through same path)
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

        private void GenerateAndWriteFile(string fileName, string outputDirectory, List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string sourceDirectory, Dictionary<string, List<CppDefine>>? sourceDefines = null, Dictionary<string, List<string>>? sourceFileTopComments = null, bool isPartialFile = false, List<CppMethod>? partialMethods = null)
        {
            var sb = new StringBuilder();
            
            // Determine if interface-only for using statements
            bool containsOnlyInterfaces = classes.All(c => c.IsInterface);
            
            AddFileTopComments(sb, fileName, sourceFileTopComments);
            AddUsingStatements(sb, containsOnlyInterfaces);
            AddNamespace(sb, fileName, sourceDirectory);
            GenerateFileContent(sb, classes, parsedSources, staticMemberInits, sourceDefines, fileName, isPartialFile, partialMethods);
            
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
                sb.AppendLine($"    {comment}");
            }
            
            // Transform the define to a C# const declaration
            sb.AppendLine($"    {define.ToCSharpConst()}");
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
                    sb.AppendLine(comment);
                }
            }

            // Structs are always internal classes in C#
            var accessibility = cppClass.IsStruct ? "internal" : 
                               (cppClass.IsPublicExport ? "public" : "internal");
            var classStaticModifier = ShouldBeStaticClass(cppClass, parsedSources) ? "static " : "";
            sb.AppendLine($"{accessibility} {classStaticModifier}class {cppClass.Name}");
            sb.AppendLine("{");

            // Add define statements first
            WriteDefineStatementsInline(sb, cppClass);

            // Add members - preserve original C++ types and add static initializations
            foreach (var member in cppClass.Members)
            {
                // Add region start marker (from .h file, converted to comment)
                if (!string.IsNullOrEmpty(member.RegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"    {member.RegionStart}");
                    sb.AppendLine();
                }

                // Add comments before member
                if (member.PrecedingComments.Any())
                {
                    foreach (var comment in member.PrecedingComments)
                    {
                        sb.AppendLine($"    {comment}");
                    }
                }

                var accessModifier = ConvertAccessSpecifier(member.AccessSpecifier);
                var staticModifier = member.IsStatic ? "static " : "";
                
                // Check if this static member has an initialization value from source files
                string initialization = "";
                string memberType = (member.IsConst ? "const " : "") + member.Type;
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
                        // Array declaration without initialization - provide proper C# initialization
                        memberType = $"{member.Type}[]";
                        initialization = $" = new {member.Type}[{member.ArraySize}]";
                    }
                }
                else if (member.IsArray)
                {
                    // Non-static array member - provide proper C# initialization
                    memberType = $"{member.Type}[]";
                    initialization = $" = new {member.Type}[{member.ArraySize}]";
                }
                
                // Include postfix comment if present
                var postfixComment = string.IsNullOrEmpty(member.PostfixComment) ? "" : $" {member.PostfixComment}";
                sb.AppendLine($"    {accessModifier} {staticModifier}{memberType} {memberName}{initialization};{postfixComment}");

                // Add region end marker (from .h file, converted to comment)  
                if (!string.IsNullOrEmpty(member.RegionEnd))
                {
                    sb.AppendLine();
                    sb.AppendLine($"    {member.RegionEnd}");
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
                    sb.AppendLine($"    {method.SourceRegionStart}");
                    sb.AppendLine();
                }

                // Add header region start (from .h file - converted to comment)
                if (!string.IsNullOrEmpty(method.HeaderRegionStart))
                {
                    sb.AppendLine();
                    sb.AppendLine($"    {method.HeaderRegionStart}");
                    sb.AppendLine();
                }

                // Add comments from .h file
                if (method.HeaderComments.Any())
                {
                    foreach (var comment in method.HeaderComments)
                    {
                        sb.AppendLine($"    {comment}");
                    }
                }

                // Add comments from .cpp file
                if (method.SourceComments.Any())
                {
                    foreach (var comment in method.SourceComments)
                    {
                        sb.AppendLine($"    {comment}");
                    }
                }

                var accessModifier = ConvertAccessSpecifier(method.AccessSpecifier);
                var staticModifier = method.IsStatic ? "static " : "";
                var virtualModifier = method.IsVirtual ? "virtual " : "";
                var returnType = method.IsConstructor || method.IsDestructor ? "" : method.ReturnType + " ";
                
                if (method.HasInlineImplementation)
                {
                    // Include inline implementation with proper indentation

                    GenerateMethodSignatureWithComments(sb, accessModifier, staticModifier, virtualModifier, returnType, method.Name, method.Parameters);
                    sb.AppendLine("    {");
                    
                    // For constructors, add member initializer assignments first
                    if (method.IsConstructor && method.MemberInitializerList.Count > 0)
                    {
                        foreach (var initializer in method.MemberInitializerList)
                        {
                            var convertedValue = ConvertCppToCsValue(initializer.InitializationValue);
                            sb.AppendLine($"        {initializer.MemberName} = {convertedValue};");
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
                    sb.AppendLine("    }");
                }
                else
                {
                    // Method declaration without body
                    GenerateMethodDeclarationWithComments(sb, accessModifier, staticModifier, virtualModifier, returnType, method.Name, method.Parameters);
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

                var accessModifier = headerMethod != null ? ConvertAccessSpecifier(headerMethod.AccessSpecifier) : 
                                    (method.IsLocalMethod ? ConvertAccessSpecifier(method.AccessSpecifier) : "public");
                var staticModifier = method.IsStatic ? "static " : "";
                var virtualModifier = method.IsVirtual ? "virtual " : "";
                var returnType = method.IsConstructor || method.IsDestructor ? "" : method.ReturnType + " ";
                
                var parametersWithDefaults = MergeParametersWithDefaults(method.Parameters, headerMethod?.Parameters);
                
                // Use comment-aware method signature generation (same as other paths)
                GenerateMethodSignatureWithComments(sb, accessModifier, staticModifier, virtualModifier, returnType, method.Name, parametersWithDefaults, "    ");
                sb.AppendLine("    {");
                
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
                
                sb.AppendLine("    }");
                sb.AppendLine();
                

            }

            sb.AppendLine("}");
        }

        private string ConvertAccessSpecifier(AccessSpecifier access)
        {
            return access switch
            {
                AccessSpecifier.Public => "public",
                AccessSpecifier.Protected => "protected",
                AccessSpecifier.Private => "private",
                AccessSpecifier.Internal => "internal",
                _ => "private"
            };
        }

        private void GenerateMethodSignatureWithComments(StringBuilder sb, string accessModifier, string staticModifier, 
            string virtualModifier, string returnType, string methodName, List<CppParameter> parameters, string baseIndent = "    ")
        {
            // Check if any parameter has comments - if not, use simple single-line format
            bool hasParameterComments = parameters.Any(p => 
                (p.PositionedComments?.Any() ?? false) || 
                p.InlineComments.Any() ||
                (!string.IsNullOrEmpty(p.OriginalText) && (p.OriginalText.Contains("/*") || p.OriginalText.Contains("//"))));
            
            // Debug TrickyToMatch signature generation

            
            if (!hasParameterComments || parameters.Count == 0)
            {
                // Simple single-line format (existing behavior)
                var parametersString = string.Join(", ", parameters.Select(p => FormatCppParameter(p)));
                sb.AppendLine($"{baseIndent}{accessModifier} {staticModifier}{virtualModifier}{returnType}{methodName}({parametersString})");
            }
            else
            {
                // Multi-line format with positioned comments (like CsClassGenerator)
                sb.AppendLine($"{baseIndent}{accessModifier} {staticModifier}{virtualModifier}{returnType}{methodName}(");
                
                for (int i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    var isLast = i == parameters.Count - 1;
                    
                    // Generate parameter with positioned comments, including comma if not last
                    var paramString = FormatCppParameterWithPositionedComments(param, includeComma: !isLast);
                    
                    // Add the parameter
                    if (isLast)
                    {
                        sb.AppendLine($"{baseIndent}    {paramString})");
                    }
                    else
                    {
                        sb.AppendLine($"{baseIndent}    {paramString}");
                    }
                }
            }
        }
        
        private void GenerateMethodDeclarationWithComments(StringBuilder sb, string accessModifier, string staticModifier, 
            string virtualModifier, string returnType, string methodName, List<CppParameter> parameters, string baseIndent = "    ")
        {
            // Check if any parameter has comments - if not, use simple single-line format
            bool hasParameterComments = parameters.Any(p => (p.PositionedComments?.Any() ?? false) || p.InlineComments.Any());
            
            if (!hasParameterComments || parameters.Count == 0)
            {
                // Simple single-line format (existing behavior)
                var parametersString = string.Join(", ", parameters.Select(p => FormatCppParameter(p)));
                sb.AppendLine($"{baseIndent}    {accessModifier} {staticModifier}{virtualModifier}{returnType}{methodName}({parametersString});");
            }
            else
            {
                // Multi-line format with positioned comments (like CsClassGenerator)
                sb.AppendLine($"{baseIndent}    {accessModifier} {staticModifier}{virtualModifier}{returnType}{methodName}(");
                
                for (int i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    var isLast = i == parameters.Count - 1;
                    
                    // Generate parameter with positioned comments, including comma if not last
                    var paramString = FormatCppParameterWithPositionedComments(param, includeComma: !isLast);
                    
                    // Add the parameter
                    if (isLast)
                    {
                        sb.AppendLine($"{baseIndent}        {paramString});");
                    }
                    else
                    {
                        sb.AppendLine($"{baseIndent}        {paramString}");
                    }
                }
            }
        }
        
        public string FormatCppParameterWithPositionedComments(CppParameter param, bool includeComma = false)
        {
            // Generate base parameter WITHOUT positioned comments (to avoid duplication)
            var baseParam = FormatCppParameterClean(param);
            
            // If no positioned comments, return base parameter
            if (param.PositionedComments == null || !param.PositionedComments.Any())
            {
                return baseParam;
            }
            
            var prefixComments = param.PositionedComments.Where(pc => pc.Position == CommentPosition.Prefix).ToList();
            var suffixComments = param.PositionedComments.Where(pc => pc.Position == CommentPosition.Suffix).ToList();
            
            var result = new StringBuilder();
            
            // Add prefix comments
            if (prefixComments.Any())
            {
                foreach (var comment in prefixComments)
                {
                    result.Append(comment.CommentText + " ");
                }
            }
            
            // Add the parameter
            result.Append(baseParam);
            
            // Check if any suffix comment is a C++ style comment (//), which should have comma before it
            var hasCppStyleSuffixComment = suffixComments.Any(c => c.CommentText.TrimStart().StartsWith("//"));
            
            // If this parameter should have a comma separator (not last param)
            if (includeComma)
            {
                if (hasCppStyleSuffixComment)
                {
                    // For // style comments, comma goes BEFORE the comment
                    result.Append(",");
                }
            }
            
            // Add suffix comments
            if (suffixComments.Any())
            {
                foreach (var comment in suffixComments)
                {
                    result.Append(" " + comment.CommentText);
                }
            }
            
            // If comma should be added and we didn't add it before (no C++ style comment), add it after comments
            if (includeComma && !hasCppStyleSuffixComment)
            {
                result.Append(",");
            }
            
            return result.ToString();
        }

        public string FormatCppParameterClean(CppParameter param)
        {
            // Format parameter without any comments (to avoid duplication)
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
            
            // Add positioned comments if present
            if (param.PositionedComments != null && param.PositionedComments.Any())
            {
                var prefixComments = param.PositionedComments.Where(pc => pc.Position == CommentPosition.Prefix).ToList();
                var suffixComments = param.PositionedComments.Where(pc => pc.Position == CommentPosition.Suffix).ToList();
                
                // Build result with positioned comments
                var finalResult = "";
                
                // Add prefix comments
                if (prefixComments.Any())
                {
                    foreach (var comment in prefixComments)
                    {
                        finalResult += comment.CommentText + " ";
                    }
                }
                
                // Add the parameter
                finalResult += result;
                
                // Add suffix comments
                if (suffixComments.Any())
                {
                    foreach (var comment in suffixComments)
                    {
                        finalResult += " " + comment.CommentText;
                    }
                }
                
                return finalResult;
            }
            // Fallback to legacy inline comments if positioned comments not available
            else if (param.InlineComments != null && param.InlineComments.Any())
            {
                foreach (var comment in param.InlineComments)
                {
                    result += " " + comment;
                }
            }
            // Final fallback: use original text if it contains comments
            else if (!string.IsNullOrEmpty(param.OriginalText) && 
                     (param.OriginalText.Contains("/*") || param.OriginalText.Contains("//")))
            {
                return param.OriginalText.Trim();
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
                    InlineComments = implParam.InlineComments, // Use source comments for implemented methods (legacy)
                    PositionedComments = implParam.PositionedComments, // Use source positioned comments for implemented methods
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
            sb.AppendLine($"{accessibility} interface {cppInterface.Name}");
            sb.AppendLine("{");

            // Add methods (skip constructors, destructors, and static methods for interfaces)
            var interfaceMethods = cppInterface.Methods
                .Where(m => !m.IsConstructor && !m.IsDestructor && !m.IsStatic)
                .Where(m => m.AccessSpecifier == AccessSpecifier.Public);

            foreach (var method in interfaceMethods)
            {
                var returnType = method.ReturnType ?? "void";
                var parameters = string.Join(", ", method.Parameters.Select(p => GenerateInterfaceParameter(p)));
                sb.AppendLine($"    {returnType} {method.Name}({parameters});");
                sb.AppendLine();
            }

            sb.AppendLine("}");
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
            sb.AppendLine($"public static class {cppInterface.Name}Extensions");
            sb.AppendLine("{");

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

                sb.AppendLine($"    public static {returnType} {staticMethod.Name}({parameters})");
                sb.AppendLine("    {");

                // For interface extension methods, provide clean C# factory implementations
                if (IsFactoryMethod(staticMethod, cppInterface.Name))
                {
                    var implementationClassName = GetImplementationClassName(cppInterface.Name);
                    sb.AppendLine($"        return new {implementationClassName}();");
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
                    sb.AppendLine("        // TODO: Implementation not found");
                    sb.AppendLine("        throw new NotImplementedException();");
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
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

        private void GeneratePartialClass(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string fileName)
        {

            // Generate the main partial class file content (header-based content)
            GenerateMainPartialClass(sb, cppClass, parsedSources, staticMemberInits, fileName);
        }

        private void GenerateMainPartialClass(StringBuilder sb, CppClass cppClass, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string fileName)
        {
            // Add comments before class declaration
            if (cppClass.PrecedingComments.Any())
            {
                foreach (var comment in cppClass.PrecedingComments)
                {
                    sb.AppendLine($"    {comment}");
                }
            }

            // Determine access modifier for the class (structs are always internal)
            string classAccessModifier = cppClass.IsStruct ? "internal" : 
                                        (cppClass.IsPublicExport ? "public" : "internal");
            
            sb.AppendLine($"{classAccessModifier} partial class {cppClass.Name}");
            sb.AppendLine("{");

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
                sb.AppendLine("    // Methods for main file (inline + same-named source)");
                
                // First: Generate inline methods in header declaration order (methods without implementation or inline methods)
                var inlineMethods = methodsForMainFile.Where(m => 
                    string.IsNullOrEmpty(m.TargetFileName) || 
                    (m.TargetFileName == fileName && m.HasInlineImplementation))
                    .OrderBy(m => cppClass.Methods.IndexOf(m)) // Maintain header order for inline methods
                    .ToList();
                
                foreach (var method in inlineMethods)
                {
                    GenerateMethodForPartialClass(sb, method, cppClass.Name, parsedSources, "    "); // 4 spaces for file-scoped namespaces
                }
                
                // Second: Generate methods with source implementation in source file order
                var sourceMethods = methodsForMainFile.Where(m => 
                    m.TargetFileName == fileName && !m.HasInlineImplementation)
                    .OrderBy(m => m.OrderIndex) // Order by source file order
                    .ToList();
                
                foreach (var method in sourceMethods)
                {
                    GenerateMethodForPartialClass(sb, method, cppClass.Name, parsedSources, "    "); // 4 spaces for file-scoped namespaces
                }
            }

            sb.AppendLine("}");
        }

        private void GenerateMemberForPartialClass(StringBuilder sb, CppMember member, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, string className)
        {
            // Use the shared utility method for consistent member generation
            // Partial classes use 4 spaces for file-scoped namespaces
            CppToCsConverter.Core.Utils.MemberGenerationHelper.GenerateMember(
                sb, 
                member, 
                ConvertAccessSpecifier,
                staticMemberInits,
                className,
                "    "); // 4 spaces for file-scoped namespaces
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
            
            sb.AppendLine($"    public static {memberType} {staticMember.Name}{initialization};");
        }

        private void GenerateMethodForPartialClass(StringBuilder sb, CppMethod method, string className, Dictionary<string, List<CppMethod>> parsedSources, string baseIndent = "    ")
        {
            // Use existing method generation logic
            string accessModifier = ConvertAccessSpecifier(method.AccessSpecifier);
            string staticModifier = method.IsStatic ? "static " : "";
            string virtualModifier = method.IsVirtual ? "virtual " : "";
            string returnType = method.IsConstructor || method.IsDestructor ? "" : 
                               (string.IsNullOrWhiteSpace(method.ReturnType) ? "void" : method.ReturnType);
            

            // Merge header method with implementation method to preserve positioned comments
            var mergedMethod = MergeHeaderMethodWithImplementation(method, parsedSources, className);
            

            // Use comment-aware method signature generation (same as non-partial)
            string returnTypeWithSpace = string.IsNullOrEmpty(returnType) ? "" : returnType + " ";
            GenerateMethodSignatureWithComments(sb, accessModifier, staticModifier, virtualModifier, returnTypeWithSpace, method.Name, mergedMethod.Parameters, baseIndent);
            sb.AppendLine($"{baseIndent}{{");
            
            // Use implementation body if available
            if (!string.IsNullOrEmpty(mergedMethod.ImplementationBody))
            {
                // Reindent to standard 8 spaces first
                var standardIndentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                    mergedMethod.ImplementationBody, 
                    mergedMethod.ImplementationIndentation
                );
                // Adjust to target indent level (baseIndent + 4)
                var targetIndent = baseIndent + "    ";
                var adjustedBody = AdjustIndentation(standardIndentedBody, "        ", targetIndent);
                sb.Append(adjustedBody);
                sb.AppendLine();
            }
            else if (!string.IsNullOrEmpty(mergedMethod.InlineImplementation))
            {
                // Reindent to standard 8 spaces first
                var standardIndentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                    mergedMethod.InlineImplementation, 
                    0
                );
                // Adjust to target indent level (baseIndent + 4)
                var targetIndent = baseIndent + "    ";
                var adjustedBody = AdjustIndentation(standardIndentedBody, "        ", targetIndent);
                sb.Append(adjustedBody);
                sb.AppendLine();
            }
            else if (mergedMethod.HasResolvedImplementation)
            {
                // Method has resolved implementation but body is empty - don't add TODO
            }
            else
            {
                // Log unresolved method for investigation
                var signatureInfo = $"{mergedMethod.ReturnType} {mergedMethod.ClassName}::{mergedMethod.Name}({string.Join(", ", mergedMethod.Parameters.Select(p => p.Type))})";
                Console.WriteLine($"??  WARNING: Method implementation not found: {signatureInfo}");
                Console.WriteLine($"    Class: {mergedMethod.ClassName}, Target file: {mergedMethod.TargetFileName}.cs");
                sb.AppendLine($"{baseIndent}    // TODO: Implement method body");
            }
            
            sb.AppendLine($"{baseIndent}}}");
            sb.AppendLine();
        }

        /// <summary>
        /// Adjusts indentation of a text block from one level to another
        /// </summary>
        private string AdjustIndentation(string text, string fromIndent, string toIndent)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var result = new System.Text.StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (i > 0)
                    result.Append("\n");
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line - preserve as empty
                    continue;
                }
                else if (line.StartsWith(fromIndent))
                {
                    // Replace the from indent with to indent
                    result.Append(toIndent + line.Substring(fromIndent.Length));
                }
                else
                {
                    // Line doesn't start with expected indent - preserve as is
                    result.Append(line);
                }
            }
            
            return result.ToString();
        }

        private CppMethod MergeHeaderMethodWithImplementation(CppMethod headerMethod, Dictionary<string, List<CppMethod>> parsedSources, string className)
        {
            // Find implementation methods for this class
            List<CppMethod> implementationMethods = new List<CppMethod>();

            foreach (var methodsList in parsedSources.Values)
            {
                implementationMethods.AddRange(methodsList.Where(m => m.ClassName == className));
            }

            // Find implementation method by name AND signature (parameter types)
            var implMethod = implementationMethods.FirstOrDefault(m => 
                m.Name == headerMethod.Name && 
                m.ClassName == headerMethod.ClassName &&
                ParametersMatch(headerMethod.Parameters, m.Parameters));

            if (implMethod == null)
                return headerMethod;

            // Create merged method with header's default values and implementation's parameter names
            var merged = new CppMethod
            {
                Name = headerMethod.Name,
                ReturnType = headerMethod.ReturnType,
                AccessSpecifier = headerMethod.AccessSpecifier,
                IsStatic = headerMethod.IsStatic,
                IsVirtual = headerMethod.IsVirtual,
                IsConstructor = headerMethod.IsConstructor,
                IsDestructor = headerMethod.IsDestructor,
                IsConst = headerMethod.IsConst,
                ClassName = headerMethod.ClassName,
                Parameters = new List<CppParameter>(),
                
                // Copy implementation body and resolved flag from implementation
                ImplementationBody = implMethod.ImplementationBody,
                HasResolvedImplementation = implMethod.HasResolvedImplementation,
                ImplementationIndentation = implMethod.ImplementationIndentation
            };

            // Merge parameters
            for (int i = 0; i < Math.Max(headerMethod.Parameters.Count, implMethod.Parameters.Count); i++)
            {
                var headerParam = i < headerMethod.Parameters.Count ? headerMethod.Parameters[i] : null;
                var implParam = i < implMethod.Parameters.Count ? implMethod.Parameters[i] : null;

                var mergedParam = new CppParameter();

                if (headerParam != null && implParam != null)
                {
                    // Use implementation name but header's default value and type info
                    mergedParam.Name = implParam.Name;
                    mergedParam.Type = headerParam.Type;
                    mergedParam.DefaultValue = headerParam.DefaultValue;
                    mergedParam.IsConst = headerParam.IsConst;
                    mergedParam.IsPointer = headerParam.IsPointer;
                    mergedParam.IsReference = headerParam.IsReference;
                    
                    // For parameter comments: use implementation comments since this method has an implementation
                    // (per readme.md: "For methods with implementation we need ignore any comments from the header 
                    // and persist the source (.cpp) method argument list comments")
                    mergedParam.InlineComments = implParam.InlineComments;
                    mergedParam.PositionedComments = implParam.PositionedComments;
                    mergedParam.OriginalText = implParam.OriginalText;
                }
                else if (headerParam != null)
                {
                    mergedParam = headerParam;
                }
                else if (implParam != null)
                {
                    mergedParam = implParam;
                }

                merged.Parameters.Add(mergedParam);
            }

            return merged;
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
                    // Copy TargetFileName, ImplementationBody, and OrderIndex from source method
                    headerMethod.TargetFileName = sourceMethod.TargetFileName;
                    headerMethod.OrderIndex = sourceMethod.OrderIndex; // Copy order index for proper method ordering
                    if (string.IsNullOrEmpty(headerMethod.ImplementationBody))
                    {
                        headerMethod.ImplementationBody = sourceMethod.ImplementationBody;
                        headerMethod.ImplementationIndentation = sourceMethod.ImplementationIndentation;
                    }
                }
            }
            
            // Add local methods from source files that don't exist in the header
            var localMethods = allSourceMethods.Where(sm => 
                sm.ClassName == cppClass.Name && 
                sm.IsLocalMethod &&
                !cppClass.Methods.Any(hm => hm.Name == sm.Name)).ToList();
                
            foreach (var localMethod in localMethods)
            {
                cppClass.Methods.Add(localMethod);
            }
        }

        private bool ParametersMatch(List<CppParameter> headerParams, List<CppParameter> sourceParams)
        {
            // Parameter counts must match
            if (headerParams.Count != sourceParams.Count)
                return false;
            
            // Compare each parameter type
            for (int i = 0; i < headerParams.Count; i++)
            {
                var headerType = NormalizeParameterType(headerParams[i].Type);
                var sourceType = NormalizeParameterType(sourceParams[i].Type);
                
                if (headerType != sourceType)
                    return false;
            }
            
            return true;
        }

        private void GenerateAdditionalPartialFiles(string fileName, List<CppClass> classes, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<CppStaticMemberInit>> staticMemberInits, Dictionary<string, List<string>>? sourceFileTopComments, string outputDirectory, string sourceDirectory)
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
                            GeneratePartialClassFile(cppClass, targetFile, methodsForTarget, parsedSources, sourceFileTopComments, outputDirectory, sourceDirectory);
                        }
                    }
                }
            }
        }

        private void GeneratePartialClassFile(CppClass cppClass, string targetFileName, List<CppMethod> methods, Dictionary<string, List<CppMethod>> parsedSources, Dictionary<string, List<string>>? sourceFileTopComments, string outputDirectory, string sourceDirectory)
        {
            // Use the refactored method to generate and write the partial file
            var classes = new List<CppClass> { cppClass };
            var staticMemberInits = new Dictionary<string, List<CppStaticMemberInit>>();
            
            GenerateAndWriteFile(targetFileName, outputDirectory, classes, parsedSources, staticMemberInits, sourceDirectory, sourceDefines: null, sourceFileTopComments: sourceFileTopComments, isPartialFile: true, partialMethods: methods);
        }
    }
}