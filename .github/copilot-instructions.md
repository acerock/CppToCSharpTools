# GitHub Copilot Instructions for CppToCSharpTools

## Project Overview
This is a C++ to C# structural converter that transforms C++ header (.h) and implementation (.cpp) files into C# (.cs) files. The tool focuses on preserving structure, types, and C++ constructs for later pipeline processing.

## Primary Documentation Source
**READ THIS FIRST:** All requirements, patterns, and expected behaviors are documented in [readme.md](../readme.md). This is the authoritative source for:
- Type conversion rules (interfaces, classes, structs)
- Access modifier handling
- Method signature matching and overload resolution
- Comment and region preservation
- Define statement transformation
- Partial class generation
- Static member initialization
- Array member handling
- Namespace resolution patterns

## Development Principles

### Test-Driven Development (TDD)
- **ALWAYS** write tests before implementing features
- **ALWAYS** run the full test suite after changes to ensure no regressions
- Use existing test patterns found in `CppToCsConverter.Tests/`
- Test file naming: `[Feature]Tests.cs` (e.g., `InterfaceGenerationTests.cs`)
- Current test count: 467+ tests - all must pass

### Code Organization
- **Core Library**: `CppToCsConverter.Core/` - API and core conversion logic
- **CLI Tool**: `CppToCsConverter/` - Command-line interface
- **Tests**: `CppToCsConverter.Tests/` - Comprehensive test suite

### Test Data and Working Folders
- **SamplesAndExpectations/**: Small, well-defined test cases for development and testing
  - Contains example .h and .cpp files with known expected outputs
  - Use this folder for testing during development
  - Example: `SamplesAndExpectations/ISample.h`, `CSample.h`, etc.
  
- **Work/**: Working directory for ad-hoc testing and experiments
  - Place temporary test files here
  - Output subdirectories for different test scenarios
  - Example: `Work/SampleNet/` for output files
  
- **d:\Tests\CppToCs/**: **REAL PRODUCTION CODE - DO NOT USE FOR TESTING**
  - Contains actual production C++ codebase (e.g., AgrLibHS/)
  - Full conversion pipeline with multiple post-processing steps
  - Only use for final validation, never for development testing

### CLI Tool Usage

The `CppToCsConverter` CLI tool accepts the following arguments:

#### Basic Syntax
```bash
CppToCsConverter <source_directory> [output_directory]
CppToCsConverter <source_directory> <file1,file2,...> [output_directory]
```

#### Arguments
- `source_directory`: Path to folder containing .h and .cpp files (required)
- `output_directory`: Path where .cs files will be generated (optional, defaults to `<source_directory>/Generated_CS`)
- `file list`: Comma-separated list of specific files to convert (optional)

#### Usage Examples

**Example 1: Convert all files from SamplesAndExpectations**
```bash
dotnet run --project CppToCsConverter -- "D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations" "D:\BatchNetTools\CppToCSharpTools\Work\Output"
```

**Example 2: Convert all files with default output location**
```bash
dotnet run --project CppToCsConverter -- "D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations"
# Output goes to: D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations\Generated_CS
```

**Example 3: Convert specific files only**
```bash
dotnet run --project CppToCsConverter -- "D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations" "ISample.h,CSample.h,CSample.cpp"
# Output goes to: D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations\Generated_CS
```

**Example 4: Convert specific files to custom output**
```bash
dotnet run --project CppToCsConverter -- "D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations" "ISample.h,CSample.h,CSample.cpp" "D:\BatchNetTools\CppToCSharpTools\Work\CustomOutput"
```

**Example 5: Using from tests (common pattern)**
```csharp
var api = new CppToCsConverterApi();
string outputDir = Path.Combine(Path.GetTempPath(), "TestOutput");
api.ConvertDirectory(
    sourceDirectory: @"D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations",
    outputDirectory: outputDir,
    specificFiles: null  // or new[] { "ISample.h", "CSample.cpp" }
);
```

#### Quick Test Commands
```bash
# From solution root - convert samples to Work folder
dotnet run --project CppToCsConverter -- ".\SamplesAndExpectations" ".\Work\TestOutput"

# Check generated output
Get-Content ".\Work\TestOutput\ISample.cs"
Get-Content ".\Work\TestOutput\SampleDefines.cs"
Get-Content ".\Work\TestOutput\CSample.cs"
```

### Key Architectural Patterns

#### Model Classes (CppToCsConverter.Core/Models/)
- `CppClass` - Represents C++ classes/structs/interfaces
- `CppMethod` - Method declarations and implementations
- `CppParameter` - Method parameters with positioned comments
- `CppMember` - Class member variables
- `CppDefine` - #define statements
- `CppStaticMemberInit` - Static member initializations

#### Parsers (CppToCsConverter.Core/Parsers/)
- `HeaderParser` - Parses .h files
- `SourceParser` - Parses .cpp files  
- `ParameterParser` - Handles parameter lists with comments
- `MethodBodyParser` - Extracts method implementations

#### Generators (CppToCsConverter.Core/Generators/)
- `CsClassGenerator` - Generates C# classes (API path)
- `CsInterfaceGenerator` - Generates C# interfaces
- `CppToCsStructuralConverter` - Main converter (CLI path)

## Critical Implementation Rules

### What This Tool DOES
✅ Preserve C++ types exactly as-is (e.g., `const CString&`, `agrint*`)
✅ Copy method bodies verbatim including all C++ syntax
✅ Maintain comment positions and formatting
✅ Handle partial classes across multiple .cpp files
✅ Convert access modifiers (private/protected/public)
✅ Transform #define to const members
✅ Initialize static members and arrays
✅ Preserve parameter default values from .h files
✅ Use parameter names from .cpp implementations
✅ Generate file-scoped namespaces

### What This Tool DOES NOT Do
❌ Convert C++ types to C# types (e.g., CString → string)
❌ Modify method body syntax or logic
❌ Add TODO comments or throw NotImplementedException
❌ Change _T("") to string.Empty or nullptr to null
❌ Rewrite C++ syntax to C# syntax
❌ Add new code not present in source files

### Blank Line Rules (IMPORTANT - Recently Fixed)
When generating C# code, follow these spacing rules:
- ✅ Single blank line BETWEEN methods
- ✅ Single blank line BETWEEN members and methods
- ❌ NO blank line before class closing brace
- ❌ NO blank line after last method
- ❌ NO consecutive blank lines

**Implementation Pattern:**
```csharp
for (int i = 0; i < items.Count; i++)
{
    GenerateItem(sb, items[i]);
    
    // Only add blank line between items, not after last
    if (i < items.Count - 1)
    {
        sb.AppendLine();
    }
}
```

### Interface Generation
Public interfaces (with `__declspec(dllexport)`):
- Add `[Create(typeof(ImplementingClass))]` attribute
- The implementing class is resolved from static factory method in .cpp
- Access modifier: `public interface`

Internal interfaces (no export):
- No Create attribute
- Access modifier: `internal interface`

### Define Statement Handling

#### From Interface Header Files (.h with public interface)
- Defines become public const members in a public static class
- Class name: `[InterfaceName]Defines` (e.g., `ISample.h` → `SampleDefines`)
- Separate .cs file: `[InterfaceName]Defines.cs`

#### From Class Header Files (.h)
- Defines become internal const members at start of class
- Appear after class opening brace, before members
- For partial classes: goes in main .cs file

#### From Source Files (.cpp)  
- Defines become private const members
- For partial classes: goes in corresponding partial .cs file

### Namespace Resolution Pattern
Format: `U4.BatchNet.XX.Compatibility` where XX is:
- Last two uppercase characters from input folder name
- If folder contains '.', '_', or '-': only trailing portion after last separator
- Examples:
  - `AgrLibHS` → `U4.BatchNet.HS.Compatibility`
  - `Something.AgrXY` → `U4.BatchNet.XY.Compatibility`
  - `Something_Sample` → `U4.BatchNet.Sample.Compatibility`

### Partial Class Generation
When a class has methods in multiple .cpp files:
- Main file: `ClassName.cs` - contains members, static inits, inline methods, and methods from `ClassName.cpp`
- Partial files: `[SourceFileName].cs` - contains methods from that source file
- All marked with `partial` keyword
- Each .cs file gets top comments from corresponding .cpp file

### Comment Preservation
- **Preceding comments**: Block of lines before type/member
- **Postfix comments**: Comment on same line after declaration
- **Parameter comments**: Positioned comments in parameter lists
- **File top comments**: Comments before #include statements
- **Header regions**: Convert `#pragma region` to `// #region` comment
- **Source regions**: Keep as `#region` / `#endregion`

### Common Pitfalls to Avoid
1. **DO NOT** add blank lines after last method in a class
2. **DO NOT** use foreach loops for code generation - use indexed for loops for blank line control
3. **DO NOT** assume parameter names match between .h and .cpp
4. **DO NOT** modify method bodies or C++ syntax
5. **DO NOT** skip tests - TDD is mandatory
6. **DO NOT** break existing tests when adding new features

### Testing Patterns
```csharp
[Fact]
public void Feature_Scenario_ExpectedBehavior()
{
    // Arrange - Setup test data
    string headerContent = @"...";
    string sourceContent = @"...";
    
    // Act - Execute conversion
    var converter = new CppToCsStructuralConverter();
    // ... perform conversion
    
    // Assert - Verify results
    Assert.Contains("expected text", result);
    Assert.DoesNotContain("unwanted text", result);
}
```

### When Adding New Features
1. Read relevant section in readme.md
2. Write failing tests first
3. Implement minimum code to pass tests
4. Run full test suite (must have 100% pass rate)
5. Refactor if needed
6. Update this file if patterns change

### Recent Major Fixes
- Postfix comment preservation for #define statements (added `PostfixComment` property to `CppDefine` model)
- Empty method body handling with `HasResolvedImplementation` flag
- Blank line generation using indexed for loops
- Partial class blank line handling (main file + partial files)
- Constructor/destructor void return type fix
- Parameter comment positioning and preservation

## Questions or Uncertainties?
1. Check readme.md first - it's the source of truth
2. Look for similar tests in `CppToCsConverter.Tests/`
3. Examine existing parser/generator code for patterns
4. Ask the user before making assumptions

## Success Criteria
- All 467+ tests passing
- No regression in existing functionality  
- Generated C# files match patterns in readme.md
- Code follows established architectural patterns
- TDD approach maintained throughout
