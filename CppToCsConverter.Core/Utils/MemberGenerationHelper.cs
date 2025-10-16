using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Core.Utils
{
    /// <summary>
    /// Shared utility for generating C# member declarations from C++ members.
    /// Used by both single-file class generation and partial class generation to ensure consistency.
    /// </summary>
    public static class MemberGenerationHelper
    {
        /// <summary>
        /// Generates a C# member declaration from a C++ member with all associated comments, regions, and formatting.
        /// </summary>
        /// <param name="sb">StringBuilder to append the generated content to</param>
        /// <param name="member">The C++ member to generate</param>
        /// <param name="accessSpecifierConverter">Function to convert AccessSpecifier to string (allows different formatting)</param>
        /// <param name="staticMemberInits">Optional static member initializations for static members</param>
        /// <param name="className">Optional class name for static member initialization lookup</param>
        public static void GenerateMember(
            StringBuilder sb, 
            CppMember member, 
            Func<AccessSpecifier, string> accessSpecifierConverter,
            Dictionary<string, List<CppStaticMemberInit>>? staticMemberInits = null,
            string? className = null)
        {
            // Handle region start marker
            if (!string.IsNullOrEmpty(member.RegionStart))
            {
                sb.AppendLine();
                sb.AppendLine($"        {member.RegionStart}");
                sb.AppendLine();
            }

            // Handle preceding comments
            if (member.PrecedingComments.Any())
            {
                foreach (var comment in member.PrecedingComments)
                {
                    sb.AppendLine($"        {comment}");
                }
            }

            // Generate access modifier and static modifier
            var accessModifier = accessSpecifierConverter(member.AccessSpecifier);
            var staticModifier = member.IsStatic ? "static " : "";
            
            // Handle type and initialization (with static member initialization support)
            var (memberType, initialization) = ProcessMemberTypeAndInitialization(
                member, staticMemberInits, className);
            
            // Handle postfix comment
            var postfixComment = string.IsNullOrEmpty(member.PostfixComment) ? "" : $" {member.PostfixComment}";
            
            // Generate the final member declaration line
            sb.AppendLine($"        {accessModifier} {staticModifier}{memberType} {member.Name}{initialization};{postfixComment}");

            // Handle region end marker
            if (!string.IsNullOrEmpty(member.RegionEnd))
            {
                sb.AppendLine();
                sb.AppendLine($"        {member.RegionEnd}");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Processes member type and initialization, handling static member initialization if applicable.
        /// </summary>
        /// <param name="member">The C++ member</param>
        /// <param name="staticMemberInits">Static member initializations</param>
        /// <param name="className">Class name for static member lookup</param>
        /// <returns>Tuple of (memberType, initialization)</returns>
        private static (string memberType, string initialization) ProcessMemberTypeAndInitialization(
            CppMember member,
            Dictionary<string, List<CppStaticMemberInit>>? staticMemberInits,
            string? className)
        {
            string memberType = member.Type;
            string initialization = "";

            // Handle static member initialization if this is a static member and we have initialization data
            if (member.IsStatic && staticMemberInits != null && !string.IsNullOrEmpty(className))
            {
                var staticInit = staticMemberInits.Values
                    .SelectMany(inits => inits)
                    .FirstOrDefault(init => init.ClassName == className && init.MemberName == member.Name);
                
                if (staticInit != null)
                {
                    initialization = $" = {staticInit.InitializationValue}";
                    
                    // Handle array type modification
                    if (member.IsArray || staticInit.IsArray)
                    {
                        memberType = staticInit.Type + "[]";
                    }
                }
            }
            
            // Handle array members without static initialization
            if (member.IsArray && string.IsNullOrEmpty(initialization))
            {
                memberType = $"{member.Type}[]";
                initialization = $" = new {member.Type}[{member.ArraySize}]";
            }

            return (memberType, initialization);
        }
    }
}