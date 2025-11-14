
# Agent setup
You are a C++ and C# expert tasked to make a library with core functionallity and a CLI parser app taking a source file as input argument 1 and output file as argument 2. The library and CLI app will take a source file as input and output the same source file but with adjusted method signatures from C++ syntax to C# based on the samples and rules in this document.

We prefer TTD as a work methology and always run the full test-suite after each change to prove nothing is broken.
We also add and keep comprehensive tests to prove complex cases is handled correctly.

# Translating method parameters from C++ format to C#
The task is to identify methods and their return type, split the method parameters persisting any preceeding or subsequently comments and default values. The parameters will be in C++ format which is described with samples later. Also, to be able to compare side by side the input and output we need to persist possible line breaks and indents in the parameter list.

We should build a model of the method and their parameters so it is later possible to re-write the method and each parameter (with comments, default values, line breaks and indenting) in valid C# format. We are only concerned about the method declaration part and we need to keep the body implementation as it is with no changes.

We need this to be consist and not try to fill out the blanks or invite parameters, comments or default values not in the original source file.

Also, types like CString, agrint, etc., are valid types and should be kept. Our task is not to change these C# native types. We don't need a type converter.

The application should only care about the method signature (described below). The method body will be handled later by another application.

# In the file a method is in the format
<access-specifier> <return-type> <MethodName>(<parameters>)

<access-specifier> (Optional): This keyword defines the accessibility level of the method. Common access specifiers include:
- public: Accessible from anywhere.
- private: Accessible only within the defining class or struct.
- protected: Accessible within the defining class and by derived classes.
- internal: Accessible only within the current assembly.
- protected internal: Accessible within the current assembly and by derived classes in other assemblies.
- private protected: Accessible within the defining class and by derived classes in the same assembly.
- <return-type>: This specifies the data type of the value that the method will return. If the method does not return any value, the void keyword is used.
- <MethodName>: This is the unique identifier for the method. It should follow C# naming conventions (e.g., PascalCase for method names).
- <parameters> (Optional): This is a comma-separated list of parameters that the method accepts. Each parameter consists of a data type and a parameter name (e.g., int number, string message). Parameters allow data to be passed into the method for processing.
{ // Method body }: This block contains the statements that define the method's logic and actions.
return <value>; (Optional): If the method has a non-void return type, a return statement is used to send a value back to the calling code. The data type of the returned value must match the specified <return-type>.

# Parser
A method signature parser should be able to identity the various allowed C++ variants and persist the parameters by order in memory.
The model is then later used by a independent Rewrite logic to output the C# valid version of each method signature.

Please visit all samples in this file and to TDD development for the use-cases.

# Rewrite Logic
Once we have a proven parser and model able to understand all allowed variations we will create a component that based on the model output a file with updated method signature with respect to types, comments, line breaks, and indent.

## C++ Pointer Return type rewrites
A method returning a pointers is changed to return the type only.

### C++ styled Input
```
public CString* GetGreet()
{
    return new CString("Hello!");
}
```
### Expected C# output
```
public CString GetGreet()
{
    return new CString("Hello!");
}
```

## Method Argument ref and out handling
For each argument we must descide if it is a ref parameter or not. We do not use the out keyword (for now).

The rules are as follows:
1. **Parameters with `const` modifier**: Never add `ref` or `out` (regardless of `*` or `&`)
2. **Parameters without `const` and without `*` or `&`**: No `ref` or `out` modifier
3. **Parameters with `*` but no `const`**: Add `ref` modifier only if the type is a value type:
   - agrint, CString, TAttId, TDate, int, long, double, float
4. **All other cases**: No `ref` or `out` modifier

### Samples - no ref or out
#### C++ styled Input
```
public void Greet1(const agrint int1)
{
    /* body */
}

public void Greet2(const agrint& int1)
{
    /* body */
}

public void Greet3(agrint int1, const agrint &int2)
{
    /* body */
}
```
#### Expected C# output
```
public void Greet1(agrint int1)
{
    /* body */
}

public void Greet2(agrint int1)
{
    /* body */
}

public void Greet3(agrint int1, agrint int2)
{
    /* body */
}
```

### Samples - ref
#### C++ styled Input
```
public void Greet1(agrint& int1)
{
    /* body */
}

public void Greet2(agrint &int1)
{
    /* body */
}

public void Greet3(agrint* int1, agrint &int2)
{
    /* body */
}
```
#### Expected C# output
```
public void Greet1(ref agrint int1)
{
    /* body */
}

public void Greet2(ref agrint int1)
{
    /* body */
}

public void Greet3(ref agrint int1, ref agrint int2)
{
    /* body */
}
```

### Samples - Pointer and no ref
#### C++ styled Input
```
public void Greet1(SomeUnknownType* pSomething)
{
    /* body */
}

public void Greet2(const SomeUknownType* pSomething)
{
    /* body */
}

public void Greet3(TDimValue* pDimValue)
{
    /* body */
}
```
#### Expected C# output
```
public void Greet1(SomeUnknownType pSomething)
{
    /* body */
}

public void Greet2(SomeUnknownType pSomething)
{
    /* body */
}

public void Greet3(TDimValue pDimValue)
{
    /* body */
}
```
# Samples input and output

## Methods with no parameters and no return value (void):
### C++ styled Input
```
public void Greet()
{
    Console.WriteLine("Hello!");
}

public void Greet2(void)
{
    Console.WriteLine("Hello 2!");
}

void Greet3 ( ) // Comment on method
{
    Console.WriteLine("Hello 3!");
}

void Greet4() { Console.WriteLine("Hello 4!"); }
```
### Expected C# output
```
public void Greet()
{
    Console.WriteLine("Hello!");
}

public void Greet2()
{
    Console.WriteLine("Hello 2!");
}

void Greet3() // Comment on method
{
    Console.WriteLine("Hello 2!");
}

void Greet4() { Console.WriteLine("Hello 4!"); }
```

## Valid methods with single parameters and no return value (void):
### C++ styled Input
```
public void Greet(const CString& cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet2(CString cParam1)
{
    Console.WriteLine(cParam1);
}

void Greet3( ) // Comment on method
{
    Console.WriteLine("Hello 3!");
}

void Greet4() { Console.WriteLine("Hello 4!"); }
```
### Expected C# output
```
public void Greet(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet2()
{
    Console.WriteLine("Hello 2!");
}

void Greet3() // Comment on method
{
    Console.WriteLine("Hello 3!");
}

void Greet4() { Console.WriteLine("Hello 4!"); }
```

## C++ Parameters variations:
### C++ styled Input
```
public void Greet1(const CString& cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet2(CString& const cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet3(CString & const cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet4(CString &const cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet5(CString const &cParam1)
{
    Console.WriteLine(cParam1);
}
```
### Expected C# output
```
public void Greet1(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet2(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet3(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet4(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet5(CString cParam1)
{
    Console.WriteLine(cParam1);
}
```

## C++ Parameters variations with spaces:
### C++ styled Input
```
public void Greet1(const CString& cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet2(CString& const cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet3(CString & const cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet4(CString &const cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet5(CString const &cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet6(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public   void   Greet7  (  CString cParam1  )   
{
    Console.WriteLine(cParam1);
}
```
### Expected C# output
```
public void Greet1(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet2(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet3(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet4(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet5(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet6(CString cParam1)
{
    Console.WriteLine(cParam1);
}

public void Greet7(CString cParam1)
{
    Console.WriteLine(cParam1);
}
```

## C++ Multiple parameters with comments:
### C++ styled Input
```
public void Greet1(/* This is IN */ const CString& cParam1, /* This is out */ CString& cOutParam)
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}

public void Greet2(const CString& cParam1 /* This is IN */, CString& cOutParam /* This is out */)
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}

public void Greet3(
    /* This is IN */ const CString& cParam1, 
    /* This is out */ CString& cOutParam)
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}

public void Greet4(
    const CString& cParam1, /* This is IN */
    CString& cOutParam) /* This is out */
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}
```
### Expected C# output
```
public void Greet1(/* This is IN */ CString cParam1, /* This is out */ ref CString cOutParam)
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}

public void Greet2(CString cParam1 /* This is IN */, ref CString cOutParam /* This is out */)
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}

public void Greet3(
    /* This is IN */ CString cParam1, 
    /* This is out */ ref CString cOutParam)
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}

public void Greet4(
    CString cParam1, /* This is IN */
    ref CString cOutParam) /* This is out */
{
    Console.WriteLine(cParam1);
    cOutParam = "Hi to you too!";
}
```
