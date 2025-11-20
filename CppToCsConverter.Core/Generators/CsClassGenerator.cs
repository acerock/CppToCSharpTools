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

            // Add file-scoped namespace
            sb.AppendLine("namespace GeneratedClasses;");
            sb.AppendLine();

            // Add comments before class declaration
            if (cppClass.PrecedingComments.Any())
            {
                foreach (var comment in cppClass.PrecedingComments)
                {
                    sb.AppendLine(comment);
                }
            }

            // Class declaration
            var partialKeyword = isPartial ? "partial " : "";
            var inheritance = cppClass.BaseClasses.Any() ? $" : {string.Join(", ", cppClass.BaseClasses)}" : "";
            sb.AppendLine($"public {partialKeyword}class {cppClass.Name}{inheritance}");
            sb.AppendLine("{");

            // Add define statements (only in non-partial classes or main class file)
            if (!isPartial || fileName == cppClass.Name)
            {
                WriteDefineStatements(sb, cppClass);
            }

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

            sb.AppendLine("}");

            return sb.ToString();
        }

        private void WriteDefineStatements(StringBuilder sb, CppClass cppClass)
        {
            // Write header defines first
            foreach (var define in cppClass.HeaderDefines)
            {
                WriteCommentsAndDefine(sb, define);
            }
            
            // Then source defines (ordered by source file)
            foreach (var define in cppClass.SourceDefines.OrderBy(d => d.SourceFileName))
            {
                WriteCommentsAndDefine(sb, define);
            }
            
            // Add a blank line after defines if any were written
            if (cppClass.HeaderDefines.Any() || cppClass.SourceDefines.Any())
            {
                sb.AppendLine();
            }
        }

        private void WriteCommentsAndDefine(StringBuilder sb, CppDefine define)
        {
            // Write preceding comments
            foreach (var comment in define.PrecedingComments)
            {
                sb.AppendLine(comment);
            }
            
            // Write the define statement itself
            sb.AppendLine(define.FullDefinition);
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
            // Use the shared utility method for consistent member generation
            CppToCsConverter.Core.Utils.MemberGenerationHelper.GenerateMember(
                sb, 
                member, 
                accessSpecifier => GetAccessSpecifierName(accessSpecifier).ToLower());
        }

        private void GenerateMethod(StringBuilder sb, CppMethod method, List<CppMethod> implementationMethods, CppClass cppClass)
        {
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
            if (method.IsConstructor || method.IsDestructor)
            {
                returnType = ""; // No return type for constructors or destructors
            }
            else
            {
                // Preserve original C++ return type for downstream processing
                returnType = method.ReturnType + " ";
            }
            
            // Merge header declaration with implementation parameters
            var mergedMethod = MergeMethodWithImplementation(method, implementationMethods);
            
            // Generate method signature with parameter comments
            var methodName = method.IsConstructor ? cppClass.Name : method.Name;
            GenerateMethodSignatureWithComments(sb, accessibility, staticKeyword, virtualKeyword, returnType, methodName, mergedMethod.Parameters);
            sb.AppendLine("    {");

            // Generate method body

            if (method.HasInlineImplementation)
            {
                // For constructors with member initializer lists, add them first
                if (method.IsConstructor && method.MemberInitializerList.Count > 0)
                {

                    foreach (var initializer in method.MemberInitializerList)
                    {
                        var convertedValue = ConvertCppToCsValue(initializer.InitializationValue);
                        sb.AppendLine($"        {initializer.MemberName} = {convertedValue};");
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
                else if (!string.IsNullOrEmpty(method.ImplementationBody))
                {
                    // Use implementation from method itself (e.g., struct constructors)
                    var convertedBody = ConvertCppToCsBody(method.ImplementationBody);
                    var indentedBody = CppToCsConverter.Core.Utils.IndentationManager.ReindentMethodBody(
                        convertedBody, method.ImplementationIndentation);
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
                            sb.AppendLine($"        {initializer.MemberName} = {convertedValue};");

                        }
                        
                        // Add existing inline implementation if available (constructor body)
                        if (!string.IsNullOrEmpty(method.InlineImplementation))
                        {
                            var convertedBody = ConvertCppToCsBody(method.InlineImplementation);
                            sb.AppendLine(AddIndentation(convertedBody, "        "));

                        }
                        
                        if (method.MemberInitializerList.Count == 0 && string.IsNullOrEmpty(method.InlineImplementation))
                        {
                            sb.AppendLine("        // TODO: Initialize members");
                        }
                    }
                    else if (method.ReturnType != "void" && !method.IsDestructor && !string.IsNullOrEmpty(method.ReturnType))
                    {
                        var defaultReturn = GetDefaultReturnValue(method.ReturnType);
                        sb.AppendLine($"        return {defaultReturn};");
                    }
                    else
                    {
                        sb.AppendLine("        // TODO: Implement method");
                    }
                }
            }

            sb.AppendLine("    }");

            // Add header region end (from .h file - converted to comment)
            if (!string.IsNullOrEmpty(method.HeaderRegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"    {method.HeaderRegionEnd}");
            }

            // Add source region end (from .cpp file - preserved as region)
            if (!string.IsNullOrEmpty(method.SourceRegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"    {method.SourceRegionEnd}");
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

        private string GenerateParameter(CppParameter param)
        {
            // If original text contains comments and no positioned comments are available, preserve original formatting
            if (!string.IsNullOrEmpty(param.OriginalText) && 
                (param.OriginalText.Contains("/*") || param.OriginalText.Contains("//")) &&
                (param.PositionedComments == null || !param.PositionedComments.Any()))
            {
                return param.OriginalText.Trim();
            }

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

        private string GenerateParameterWithPositionedComments(CppParameter param)
        {
            // Generate base parameter
            var baseParam = GenerateParameter(param);
            
            // If no positioned comments, fall back to legacy behavior
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
            
            // Add suffix comments
            if (suffixComments.Any())
            {
                foreach (var comment in suffixComments)
                {
                    result.Append(" " + comment.CommentText);
                }
            }
            
            return result.ToString();
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
        
        private void GenerateMethodSignatureWithComments(StringBuilder sb, string accessibility, string staticKeyword, 
            string virtualKeyword, string returnType, string methodName, List<CppParameter> parameters)
        {
            // Check if any parameter has comments - if not, use simple single-line format
            bool hasParameterComments = parameters.Any(p => 
                (p.PositionedComments?.Any() ?? false) || 
                p.InlineComments.Any() ||
                (!string.IsNullOrEmpty(p.OriginalText) && (p.OriginalText.Contains("/*") || p.OriginalText.Contains("//"))));
            
            // Debug GetRate method specifically
            if (methodName == "GetRate")
            {
                Console.WriteLine($"DEBUG: CsClassGenerator.GenerateMethodSignatureWithComments for {methodName}");
                Console.WriteLine($"DEBUG: hasParameterComments = {hasParameterComments}");
                Console.WriteLine($"DEBUG: parameters.Count = {parameters.Count}");
                for (int i = 0; i < parameters.Count; i++)
                {
                    var p = parameters[i];
                    Console.WriteLine($"  Param[{i}]: {p.Name}");
                    Console.WriteLine($"    PositionedComments: {p.PositionedComments?.Count ?? 0}");
                    Console.WriteLine($"    InlineComments: {p.InlineComments?.Count ?? 0}");
                    Console.WriteLine($"    OriginalText: '{p.OriginalText}'");
                    Console.WriteLine($"    Contains /*: {(!string.IsNullOrEmpty(p.OriginalText) && p.OriginalText.Contains("/*"))}");
                }
            }
            

            
            if (!hasParameterComments || parameters.Count == 0)
            {
                // Simple single-line format
                var parameterStrings = parameters.Select(GenerateParameter);
                var parametersString = string.Join(", ", parameterStrings);
                sb.AppendLine($"    {accessibility} {staticKeyword}{virtualKeyword}{returnType}{methodName}({parametersString})");
            }
            else
            {
                // Multi-line format with positioned comments
                sb.AppendLine($"    {accessibility} {staticKeyword}{virtualKeyword}{returnType}{methodName}(");
                
                for (int i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    var isLast = i == parameters.Count - 1;
                    
                    // Generate parameter with positioned comments
                    var paramString = GenerateParameterWithPositionedComments(param);
                    
                    // Add the parameter with proper comma
                    if (isLast)
                    {
                        sb.AppendLine($"            {paramString})");
                    }
                    else
                    {
                        sb.AppendLine($"            {paramString},");
                    }
                }
                
                // If we didn't add the closing paren (no parameters), add it
                if (parameters.Count == 0)
                {
                    sb.AppendLine("    )");
                }
            }
        }
    }
}