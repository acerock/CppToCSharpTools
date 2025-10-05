# CppToCsConverter.Core

Core library for converting C++ code structures to C# equivalents.

## Overview

This library provides the core functionality for parsing C++ header (.h) and source (.cpp) files and converting them to equivalent C# class structures. It preserves comments, method implementations, default parameter values, and access specifiers.

## Main Components

### Core Classes

- **CppToCsConverterApi**: Main entry point providing a simplified API for conversion operations
- **CppToCsStructuralConverter**: Core converter logic that orchestrates the conversion process

### Parsers

- **CppHeaderParser**: Parses C++ header files (.h) to extract class declarations, methods, and comments
- **CppSourceParser**: Parses C++ source files (.cpp) to extract method implementations and comments

### Generators  

- **CsClassGenerator**: Generates C# class code from parsed C++ structures
- **CsInterfaceGenerator**: Generates C# interface code from parsed C++ structures
- **TypeConverter**: Handles conversion of C++ types to C# equivalents

### Models

- **CppClass**: Represents a parsed C++ class/struct with all its members, methods, and metadata
- **CppStaticMemberInit**: Represents static member initializations found in source files

## Usage

```csharp
using CppToCsConverter.Core;

// Create an instance of the converter API
var converter = new CppToCsConverterApi();

// Convert all files in a directory
converter.ConvertDirectory("C:\\SourceCode\\CppProject", "C:\\Output\\CsProject");

// Convert specific files
converter.ConvertSpecificFiles("C:\\SourceCode", 
    new[] { "MyClass.h", "MyClass.cpp" }, 
    "C:\\Output");

// Convert files directly
converter.ConvertFiles(
    new[] { "C:\\Source\\MyClass.h" }, 
    new[] { "C:\\Source\\MyClass.cpp" }, 
    "C:\\Output");
```

## Features

- ✅ Preserves C++ comments from both header and source files
- ✅ Maintains default parameter values from header declarations  
- ✅ Includes method implementations from source files
- ✅ Respects access specifiers (private, protected, public)
- ✅ Handles method overloads correctly
- ✅ Supports inline method definitions
- ✅ Processes region markers and pragma directives
- ✅ Handles static member initializations

## Target Framework

- .NET 8.0

## Testing

The library exposes internal methods to the test assembly using `InternalsVisibleTo`, making tests more readable and maintainable by avoiding reflection:

```csharp
// Before (using reflection)
var methodInfo = typeof(CppToCsStructuralConverter).GetMethod("GetMethodSignature", 
    BindingFlags.NonPublic | BindingFlags.Instance);
var result = (string)methodInfo.Invoke(converter, new object[] { method });

// After (using friend assembly)
var result = converter.GetMethodSignature(method);
```

### Internal Methods Available for Testing

- `GetMethodSignature(CppMethod method)` - Generates unique signatures for method overload detection
- `NormalizeParameterType(string type)` - Normalizes C++ parameter types for comparison
- `IndentMethodBody(string methodBody, string indentation)` - Properly indents method body code