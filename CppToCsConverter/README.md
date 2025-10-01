# C++ to C# Structural Converter

This application performs structural conversion from C++ header and source files to C# class files based on the conversion rules defined in the project requirements.

## Features

- **Interface Conversion**: Converts C++ pure virtual classes to C# interfaces
- **Class Conversion**: Converts C++ classes to C# classes with proper member access modifiers
- **Parameter Mapping**: Merges header file parameter defaults with implementation file parameter names  
- **Method Ordering**: Maintains method order from .cpp implementation files
- **Partial Classes**: Generates partial classes when implementation spans multiple .cpp files
- **Static Member Initialization**: Handles static member initialization from .cpp files
- **Type Conversion**: Converts common C++ types to C# equivalents (MFC types, basic types, etc.)

## Conversion Rules

### Interfaces
- C++ classes with `__declspec(dllexport)` become `public interface`
- C++ classes without export declaration become `internal interface`
- Pure virtual methods (`= 0`) become interface methods
- Static methods become extension methods
- Pointer parameters become `out` parameters
- Reference parameters become `ref` parameters

### Classes
- Public/private/protected access specifiers are preserved
- Method parameter names from .cpp files are used
- Default parameter values from .h files are preserved  
- Method ordering follows .cpp file implementation order
- Inline methods in headers are converted with basic C++ to C# syntax conversion

### Multiple Implementation Files
- Creates partial classes when methods are implemented across multiple .cpp files
- Each .cpp file becomes a separate .cs file with `partial class`
- Maintains original file naming convention

## Usage

```bash
CppToCsConverter <source_directory> [output_directory]
```

**Parameters:**
- `source_directory`: Directory containing C++ .h and .cpp files
- `output_directory`: (Optional) Directory for generated C# files. Defaults to `<source_directory>/Generated_CS`

**Example:**
```bash
CppToCsConverter C:\Source\CppProject C:\Output\CsProject
```

## Generated Output

For the sample files in this project:

### ISample.h → ISample.cs
```csharp
public interface ISample
{
    void MethodOne(string cParam1, bool bParam2, out string pcParam3);
    bool MethodTwo();
}

public static class ISampleExtensions
{
    public static ISample GetInstance(this ISample instance)
    {
        return new CSample();
    }
}
```

### CSample.h + CSample.cpp → CSample.cs  
```csharp
public class CSample : ISample
{
    #region Private Members
    private int m_value1;
    private string cValue1;
    private string cValue2;
    private string cValue3;
    private static int m_iIndex = -1;
    #endregion

    public CSample()
    {
        m_value1 = 0;
        cValue1 = "ABC";
        cValue2 = "DEF"; 
        cValue3 = "GHI";
    }

    public void MethodOne(string cParam1, bool bParam2, out string pcParam3)
    {
        // Implementation from CSample.cpp
    }

    public bool MethodTwo()
    {
        return cValue1 == cValue2;
    }

    // ... other methods
}
```

## Architecture

- **Core**: `CppToCsStructuralConverter` - Main conversion orchestrator
- **Parsers**: 
  - `CppHeaderParser` - Parses .h files for class definitions  
  - `CppSourceParser` - Parses .cpp files for method implementations
- **Generators**:
  - `CsInterfaceGenerator` - Generates C# interfaces
  - `CsClassGenerator` - Generates C# classes (normal and partial)
  - `TypeConverter` - Handles C++ to C# type conversions
- **Models**: Data structures representing parsed C++ constructs

## Limitations

- Basic C++ to C# syntax conversion (manual review recommended)
- Template types have limited conversion support
- Complex macro expansions are not handled
- Assumes MFC types are available in target environment
- Does not handle preprocessor directives beyond basic #include

## Extensibility

The `TypeConverter` class can be extended with additional type mappings:

```csharp
typeConverter.AddCustomTypeMapping("MyCustomType", "MyCSType");
```

New parsing rules can be added to the respective parser classes for handling additional C++ constructs.