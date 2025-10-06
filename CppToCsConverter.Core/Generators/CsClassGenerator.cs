using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Core.Generators
{
    public class CsClassGenerator
    {
        private readonly TypeConverter _typeConverter;

        public CsClassGenerator()
        {
            _typeConverter = new TypeConverter();
        }

        public string GenerateClass(CppClass cppClass, List<CppMethod> implementationMethods, string fileName)
        {
            return GenerateClassInternal(cppClass, implementationMethods, fileName, false);
        }

        public string GeneratePartialClass(CppClass cppClass, List<CppMethod> implementationMethods, string fileName)
        {
            return GenerateClassInternal(cppClass, implementationMethods, fileName, true);
        }

        private string GenerateClassInternal(CppClass cppClass, List<CppMethod> implementationMethods, string fileName, bool isPartial)
        {
            // Check if this is truly header-only generation (no implementations AND no inline methods)
            var methodsNeedingImplementation = cppClass.Methods.Where(m => 
                !m.HasInlineImplementation && 
                !string.IsNullOrEmpty(m.Name) && 
                !m.IsConstructor).Count();
            
            var availableImplementations = implementationMethods?.Count ?? 0;
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

            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine();

            // Add namespace
            sb.AppendLine("namespace GeneratedClasses");
            sb.AppendLine("{");

            // Add comments before class declaration
            if (cppClass.PrecedingComments.Any())
            {
                foreach (var comment in cppClass.PrecedingComments)
                {
                    sb.AppendLine($"    {comment}");
                }
            }

            // Class declaration
            var partialKeyword = isPartial ? "partial " : "";
            var inheritance = cppClass.BaseClasses.Any() ? $" : {string.Join(", ", cppClass.BaseClasses)}" : "";
            sb.AppendLine($"    public {partialKeyword}class {cppClass.Name}{inheritance}");
            sb.AppendLine("    {");

            // Add members (only in non-partial classes or main class file)
            if (!isPartial || fileName == cppClass.Name)
            {
                GenerateMembers(sb, cppClass);
            }

            // Add methods - order by implementation order if available
            var orderedMethods = GetOrderedMethods(cppClass, implementationMethods ?? new List<CppMethod>());
            
            foreach (var method in orderedMethods)
            {
                GenerateMethod(sb, method, implementationMethods ?? new List<CppMethod>(), cppClass);
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateMembers(StringBuilder sb, CppClass cppClass)
        {
            // Generate members in the order they appear, preserving comments and regions
            foreach (var member in cppClass.Members)
            {
                GenerateMember(sb, member);
            }
        }

        private void GenerateMember(StringBuilder sb, CppMember member)
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

            var accessibility = GetAccessSpecifierName(member.AccessSpecifier).ToLower();
            var staticKeyword = member.IsStatic ? "static " : "";
            // Preserve original C++ type for downstream processing
            var csType = member.Type;
            
            sb.AppendLine($"        {accessibility} {staticKeyword}{csType} {member.Name};");

            // Add region end marker (from .h file, converted to comment)  
            if (!string.IsNullOrEmpty(member.RegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"        {member.RegionEnd}");
            }
        }

        private void GenerateMethod(StringBuilder sb, CppMethod method, List<CppMethod> implementationMethods, CppClass cppClass)
        {
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

            // Add comments from .h file with proper indentation
            if (method.HeaderComments.Any())
            {
                var indentedComments = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodComments(
                    method.HeaderComments, method.HeaderCommentIndentation);
                if (!string.IsNullOrEmpty(indentedComments))
                {
                    sb.AppendLine(indentedComments);
                }
            }

            // Add comments from .cpp file with proper indentation
            if (method.SourceComments.Any())
            {
                var indentedComments = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodComments(
                    method.SourceComments, method.SourceCommentIndentation);
                if (!string.IsNullOrEmpty(indentedComments))
                {
                    sb.AppendLine(indentedComments);
                }
            }

            var accessibility = GetAccessSpecifierName(method.AccessSpecifier).ToLower();
            var staticKeyword = method.IsStatic ? "static " : "";
            var virtualKeyword = method.IsVirtual && !method.IsStatic ? "virtual " : "";
            
            // Handle constructors and destructors properly
            string returnType = "";
            if (method.IsConstructor)
            {
                returnType = ""; // No return type for constructors
            }
            else if (method.IsDestructor)
            {
                return; // Skip destructors in C# - handled by finalizer/dispose pattern
            }
            else
            {
                // Preserve original C++ return type for downstream processing
                returnType = method.ReturnType + " ";
            }
            
            // Merge header declaration with implementation parameters
            var mergedMethod = MergeMethodWithImplementation(method, implementationMethods);
            var parameters = string.Join(", ", mergedMethod.Parameters.Select(GenerateParameter));

            // Generate method signature
            var methodName = method.IsConstructor ? cppClass.Name : method.Name;
            sb.AppendLine($"        {accessibility} {staticKeyword}{virtualKeyword}{returnType}{methodName}({parameters})");
            sb.AppendLine("        {");

            // Generate method body

            if (method.HasInlineImplementation)
            {
                // For constructors with member initializer lists, add them first
                if (method.IsConstructor && method.MemberInitializerList.Count > 0)
                {

                    foreach (var initializer in method.MemberInitializerList)
                    {
                        var convertedValue = ConvertCppToCsValue(initializer.InitializationValue);
                        sb.AppendLine($"            {initializer.MemberName} = {convertedValue};");
                    }
                }
                
                // Use inline implementation from header
                var convertedBody = ConvertCppToCsBody(method.InlineImplementation);
                // For inline implementations, detect indentation from the inline body
                var originalIndentation = CppToCsConverter.Core.Utils.IndentationManager.DetectOriginalIndentation(method.InlineImplementation);
                var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                    convertedBody, originalIndentation);
                sb.AppendLine(indentedBody);
            }
            else
            {
                // Use implementation from .cpp file if available
                var implMethod = implementationMethods.FirstOrDefault(m => 
                    m.Name == method.Name && m.ClassName == method.ClassName);
                
                if (implMethod != null && !string.IsNullOrEmpty(implMethod.ImplementationBody))
                {
                    var convertedBody = ConvertCppToCsBody(implMethod.ImplementationBody);
                    var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                        convertedBody, implMethod.ImplementationIndentation);
                    sb.AppendLine(indentedBody);
                }
                else
                {
                    // Generate placeholder
                    if (method.IsConstructor)
                    {

                        
                        // Generate member initializer assignments
                        foreach (var initializer in method.MemberInitializerList)
                        {
                            var convertedValue = ConvertCppToCsValue(initializer.InitializationValue);
                            sb.AppendLine($"            {initializer.MemberName} = {convertedValue};");

                        }
                        
                        // Add existing inline implementation if available (constructor body)
                        if (!string.IsNullOrEmpty(method.InlineImplementation))
                        {
                            var convertedBody = ConvertCppToCsBody(method.InlineImplementation);
                            sb.AppendLine(AddIndentation(convertedBody, "            "));

                        }
                        
                        if (method.MemberInitializerList.Count == 0 && string.IsNullOrEmpty(method.InlineImplementation))
                        {
                            sb.AppendLine("            // TODO: Initialize members");
                        }
                    }
                    else if (method.ReturnType != "void" && !method.IsDestructor && !string.IsNullOrEmpty(method.ReturnType))
                    {
                        var defaultReturn = GetDefaultReturnValue(method.ReturnType);
                        sb.AppendLine($"            return {defaultReturn};");
                    }
                    else
                    {
                        sb.AppendLine("            // TODO: Implement method");
                    }
                }
            }

            sb.AppendLine("        }");

            // Add header region end (from .h file - converted to comment)
            if (!string.IsNullOrEmpty(method.HeaderRegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"        {method.HeaderRegionEnd}");
            }

            // Add source region end (from .cpp file - preserved as region)
            if (!string.IsNullOrEmpty(method.SourceRegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"        {method.SourceRegionEnd}");
            }
        }

        private CppMethod MergeMethodWithImplementation(CppMethod headerMethod, List<CppMethod> implementationMethods)
        {
            var implMethod = implementationMethods.FirstOrDefault(m => 
                m.Name == headerMethod.Name && m.ClassName == headerMethod.ClassName);

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
                Parameters = new List<CppParameter>()
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

        private string GenerateParameter(CppParameter param)
        {
            // Preserve original C++ parameter type for downstream processing
            var csType = param.Type;
            var modifier = "";

            // Handle pointer parameters as out/ref
            if (param.IsPointer && !param.IsConst)
            {
                modifier = "out ";
            }
            else if (param.IsReference && !param.IsConst)
            {
                modifier = "ref ";
            }

            var paramDecl = $"{modifier}{csType} {param.Name}";

            // Add default value if present and fix null conversion
            if (!string.IsNullOrEmpty(param.DefaultValue))
            {
                var defaultValue = _typeConverter.ConvertDefaultValue(param.DefaultValue);
                // Fix "null" default values for value types
                if (defaultValue == "null" && (csType == "int" || csType == "bool" || csType == "double" || csType == "float"))
                {
                    defaultValue = GetDefaultReturnValue(csType);
                }
                paramDecl += $" = {defaultValue}";
            }

            return paramDecl;
        }

        private List<CppMethod> GetOrderedMethods(CppClass cppClass, List<CppMethod> implementationMethods)
        {
            // Order methods by implementation order first, then by header order
            var result = new List<CppMethod>();
            var processedMethods = new HashSet<string>();

            // First, add methods in implementation order
            var orderedImplMethods = implementationMethods
                .Where(m => m.ClassName == cppClass.Name)
                .OrderBy(m => m.OrderIndex);

            foreach (var implMethod in orderedImplMethods)
            {
                var headerMethod = cppClass.Methods
                    .FirstOrDefault(m => m.Name == implMethod.Name);
                
                if (headerMethod != null)
                {
                    result.Add(headerMethod);
                    processedMethods.Add(headerMethod.Name);
                }
            }

            // Then add remaining methods from header
            foreach (var headerMethod in cppClass.Methods)
            {
                if (!processedMethods.Contains(headerMethod.Name))
                {
                    result.Add(headerMethod);
                }
            }

            return result;
        }

        private string ConvertCppToCsBody(string cppBody)
        {
            if (string.IsNullOrWhiteSpace(cppBody))
                return "// Empty method body";

            // Basic C++ to C# conversion
            var csBody = cppBody
                .Replace("_T(\"", "\"")  // Remove _T macro
                .Replace("_T('", "'")    // Remove _T macro for chars
                .Replace("->", ".")      // Convert pointer access to dot notation
                .Replace("::", ".")      // Convert scope resolution to dot notation
                .Replace("NULL", "null") // Convert NULL to null
                .Replace("TRUE", "true") // Convert TRUE to true
                .Replace("FALSE", "false"); // Convert FALSE to false

            return csBody.Trim();
        }

        private string AddIndentation(string text, string indentation)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(Environment.NewLine, lines.Select(line => indentation + line.Trim()));
        }

        private string GetDefaultReturnValue(string returnType)
        {
            // Preserve original C++ type for downstream processing
            var csType = returnType;
            
            switch (csType.ToLower())
            {
                case "bool": return "false";
                case "int":
                case "long":
                case "short":
                case "byte":
                case "double":
                case "float":
                case "decimal": return "0";
                case "string": return "string.Empty";
                default: return "default";
            }
        }

        private int GetAccessSpecifierOrder(AccessSpecifier accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case AccessSpecifier.Public: return 1;
                case AccessSpecifier.Protected: return 2;
                case AccessSpecifier.Private: return 3;
                default: return 4;
            }
        }

        private string GetAccessSpecifierName(AccessSpecifier accessSpecifier)
        {
            return accessSpecifier.ToString().ToLowerInvariant();
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