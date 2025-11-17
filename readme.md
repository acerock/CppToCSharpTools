# Introduction
This folder contains files to demostrate how C++ interfaces and classes are defined and how we expect the C# equalent to be produced.

This document describes the recommeded approach to handle the complexity of translating these classes into the desired C# output.

## Test Driven Development
We are building out and maintaining our test suite for all requirements. Every time we do changes we re-run all tests to assure we have not broken existing tests and to assure our new functionality works as expected.

## What this project is about
* This project is about building the C# files based on the content of one or more C++ header (.h) files and implementation (.cpp) files.
* It focus on assembling the type (interfaces and classes) to ensure access modifiers, default values and method bodies is correctly applied.
* This project is to be understood as one step in a pipeline. There will be steps later to follow up on the .cs files produced to reason about type conversions, ref/out parameters etc. This project is about persisting all types and C++ construct for member variables/fields and method arguments so that steps later in the pipeline can succeed doing their tasks.

## What this project is NOT about
* This is not about building 100% valid C# code.
* This is not about converting MFC, Windows API types, C++/CLI or user defined types to the C# world equalent. This is also true for default values for these types. For instance, _T("") should not be translated to string.Empty, or nullptr to null, etc. 

The following files exist to highlight common structure in .h and .cpp files with expected .cs.

# Deveopment life-cycle and tooling
The tool we are building is a .NET CLI app with simple arguments for input source folder, optional comma-separated list of individaul files, and an output folder.

We do TDD to assure we test all features and capabilities. The tests are not using reflection but we assure the code is accessable to the test project.

# Resolving the C# Namespace
For this project we have a pattern based on naming of the input folder name to resolve the namespace for our .cs files.

The namespace to use is "U4.BatchNet.XX.Compatibility" where XX is the last two uppercase characters of the input folder. For instance if input folder is "c:\test\AgrLibHS" the namespace will be "U4.BatchNet.XX.Compatibility" and if the input folder is "c:\test\AgrYX" the namespace should be "U4.BatchNet.XY.Compatibility".

If the input folder name contains '.', '_', or '-' we only consider the trailing characters. This means if the folder is "c:\test\Something.AgrXY" the namespace should be "U4.BatchNet.XY.Compatibility" or if the folder is "c:\test\Something_Sample" the namespace should be "U4.BatchNet.Sample.Compatibility".

We use file scoped namespaces.

# C++ interface defintions
A C++ interface is a class defined with pure virtual methods. 

## Public interfaces
Public interface in C++ is specificed with the __declspec(dllexport) export definition.

#### C++ sample
```
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1,
                           const bool &bParam2,
                           CString *pcParam3) = 0;

	virtual bool MethodTwo() = 0;
};
```
#### C# Equalent
```
public interface ISample
{
    void MethodOne(const CString cParam1,
                   const bool& bParam2,
                   CString* pcParam3);

	bool MethodTwo();
}
```

## Internal interfaces
Public interface in C++ does not have any export definition.
#### C++ sample
```
class ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1,
                           const bool &bParam2,
                           CString *pcParam3) = 0;

	virtual bool MethodTwo() = 0;
};
```
#### C# Equalent
```
internal interface ISample
{
    void MethodOne(const CString cParam1,
                   const bool& bParam2,
                   CString* pcParam3);

	bool MethodTwo();
}
```

### Static methods for interfaces
In C++, pure virtual classes (or interfaces) can contain static methods - typically as a factory pattern, but these are ignored when building the C# equalent and implemented by extension methods on the interface (or object factory by the Create attribute.)
```
	public static class ISampleExtensions
	{
		public static ISample GetInstance(this ISample sample)
		{
			return new CSample();
		}
	}
```

# C++ header files (.h)
A C++ header file can hold one or more type definitions like structs, classes, defines, etc. 
For a class it holds the public, protected, or private access specifiers and the default values of the parameters and these are important to bring to the C# code. If a method is defined without access specifier it is considered private.

#### Sample class definition in header file with member access specifiers
From the header file we the most important information to bring to the C# sceleton is the member access specifiers and the defaulting of the parameters. 
Note that there are cases where parameter names are omitted in the header file (legal in C++) or named differently in the .cpp file.
```
#pragma once

#include "ISample.h"

class CSample : public ISample
{
private:
	agrint m_value1;
	CString cValue1;

	CString PrivateMemberWithBodyInHfile();

public:
	CSample();

	void MethodOne();
	bool MethodTwo() { return cValue1 != _T(""); };

private:
	bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
};
```

## C++ class array member variables
C++ classes can have C-style array member variables that we need to transfer to the C# classes and rewrite to an intialized array.

We capture the type, name and the size from the header file declaration:
type name[size];

Where it can be any type, variable name and const value. Our task is to recreate it in the C# class as
type[] name = new type[size];

This is a  follow the rules regarding preceding and consecutive comments and access modifiers.

As mentioned, the goal is to assure array members are correctly persisted just as any other member variable. 

#### Sample fixed sized array member
CSample.h
```
// We move this to the class and another tool will translate it to internal const int ARR_SIZE = 10;
#define ARR_SIZE 10

class CSample : public ISample
{
private:
    // Array of ints
    agrint m_value1[ARR_SIZE]; // Comment about ARR_SIZE 
}
```

Expected CSample.cs
```
internal class CSample : ISample
{
    // We move this to the class and another tool will translate it to internal const int ARR_SIZE = 10;
    internal const int ARR_SIZE = 10;

    // Array of ints
    private agrint[] m_value1 = new agrint[ARR_SIZE]; // Comment about ARR_SIZE
}
```

#### Sample result from header file information
This sample shows how this starts to build up the structure and content of the C# result file.
```
	internal class CSample : ISample
	{
		private agrint m_value1;
		private CString cValue1;

		public CSample();

		public void MethodOne() { return cValue != ""; }
	    public bool MethodTwo();

	    private bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
	}	
}
```

# C++ source files (.cpp)
A method can be inline defined (having method body) in the .h file. If not the body is found in a .cpp file with the class scope resolutor (::).

#### Sample C++ method with class scope resolutor
```
void CSample::MethodTwo()
{
    m_value1 = 1;
}
```

Note that is legal to also use the class scope resolutor in the header file declaration
```
class CMySample 
{
public:
	void CMySample::MethodOne();
	bool CMySample::MethodTwo() { return cValue1 != _T(""); };

private:
	bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
};
```

## Adding method definitions to the C# file
To complete the .cs file with method bodies we have to look at the .cpp file for the implementations.
Also, the .cpp file defines the order of the method implementations in the .cs file, so when the order is different as in the .h file, we will move the method to match the ordering in the .cpp file. This will allow us to do a side-by-side compare of the C++ file and the C# file.

In addition the C++ controls the parameter names for the methods. As we remember, the declaration in the header file defines the parameter defaulting (if any) but not the final parameter names (as this might not be given at all) so we applyes the defaulting to the .cpp method definition.

### Classes with bodies in multiple .cpp files
There is no 1 to 1 relation with .h files and .cpp files. The body of a method can be in .cpp file with same name as the .h file, but in many cases this is not true, and the method bodies can be spread around multiple .cpp files.

In these cases we make a partial C# class implemented in .cs file names as the original .cpp file. 

#### Sample for multiple .cpp files for method bodies
CSample.h
```
class CSample : public ISample
{
private:
    agrint m_value1;

public:
	void MethodOne();
	void MethodTwo();
	void MethodThree();
}
```
CSample.cpp
```
void CSample::MethodOne()
{
    m_value1 = 1;
}
```
SampleMoreImpl.cpp
```
void CSample::MethodTwo()
{
    m_value1 = 2;
}

// Comment for method three
void CSample::MethodThree()
{
    m_value1 = 3;
}
```
CSample.cs
```
internal partial class CSample : ISample
{
    private agrint m_value1;

    public void MethodOne()
    {
        m_value1 = 1;
    }
}
```
SampleMoreImpl.cs
```
internal partial class CSample : ISample
{
    public void MethodTwo()
    {
        m_value1 = 2;
    }

    // Comment for method three
    public void MethodThree()
    {
        m_value1 = 3;
    }
}
```

## Classes with static member and intialization
A C++ class can have a static member that is initialized outside the constructor. In this cases we need to apply the same initialization to the C# equalent.

#### Sample for multiple .cpp files with method bodies
CSample.h
```
class CSample : public ISample
{
private:
	static agrint s_value;
}
```

CSample.cpp
```
CSample::m_value = 42;
```

CSample.cs
```
internal class CSample : ISample
{
    private static agrint s_value = 42;
}
```

## Classes with static array members and intialization
A C++ class can have a static array member that is initialized outside the constructor. In this cases we need to apply the same initialization to the C# equalent.

#### Sample static array initialization
CStaticClass.h
```
#pragma once

class CStaticClass
{
public:
    static const CString ColFrom[4];
    static const CString ColTo[4];
};
```
CStaticClass.cpp
```
#include "StdAfx.h"

const CString CStaticClass::ColFrom[] = { _T("from_1"), _T("from_2"), _T("from_3"), _T("from_4") };
const CString CStaticClass::ColTo[] = { _T("to_1"), _T("to_2"), _T("to_3"), _T("to_4") };
```
Expected CStaticClass.cs
```
    internal static class CStaticClass
    {
        public static CString[] ColFrom = { _T("from_1"), _T("from_2"), _T("from_3"), _T("from_4") };
        public static CString[] ColTo = { _T("to_1"), _T("to_2"), _T("to_3"), _T("to_4") };
    }
```

### Local methods in C++ source files without class scope regulator
Any .cpp file can have methods not part of a class. Local methods are identified by not having a class scope resolutor (::). 

A local method might have a forward declaration in the .cpp file before it is used. A forward declaration is equal to method declarations in header files but for .cpp forward declaration we can saftly ignore these. 

A local method with body should be persisted in the .cs file as a private static class member in the order they exists in the original .cpp file. If a local method is found in a partial class .cpp, we write it to the partial class C# file in the order it was found in the .cpp file. 

As a local method does not have a class scope resolutor we need to know when to look for them an how to identify the class where it belongs. For this we can use the following rule:
1. We only need to look for local methods in .cpp files when looking for non-inline class member methods using the class scope regulators.
2. To not have to look for local methods each and every time we look for a non-inline class member we can cache the .cpp file name to not having to do another search for the next method in this .cpp file.
3. When one or more local methods are found we
   a. Capture it as it as it was a normal class member and add it to the class member collection as a private and static method.
   b. Sicne we do this for every class non-inline method, we assure we don't get duplicates persited using its parameter signature. Note that local methods can have overloads so to not loose local methods we need to match the signature.
   c. The method is considered a private and static method
   d. In the case of partial class implementations, the target .cs file will be the name where the method was found.
   e. We can read and persist the method using our existing parsing and model (with adjustments).
   f. We capture the method order in the .cpp file so we can restore the methods to the class (or partial class) in the same order.
   
#### Sample .cpp files with local methods
CSample.h
```
class CSample : public ISample
{
private:
    agrint m_value1;

public:
	void MethodOne();
	void MethodTwo();
	void MethodThree();
}
```
CSample.cpp
```
#include "CSample.h"

// A comment for the local method
bool CheckSomeValue(/* IN */ const agrint& value)
{
    // Method body
    return value > 0 && value < 1000;
}

void CSample::MethodOne()
{
    if (CheckSomeValue(3))
        m_value1 = 1;
}
```
SampleMoreImpl.cpp
```
void CSample::MethodTwo()
{
    m_value1 = 2;
}

// A comment for the local method 2
bool CheckSomeValue2(/* IN */ const agrint& value)
{
    // Method body
    return value > 0 && value < 1000;
}

// Comment for method three
void CSample::MethodThree()
{
    m_value1 = 3;
}
```
CSample.cs
```
internal partial class CSample : ISample
{
    private agrint m_value1;

    // A comment for the local method
    internal static bool CheckSomeValue(/* IN */ const agrint& value)
    {
        // Method body
        return value > 0 && value < 1000;
    }

    public void MethodOne()
    {
        if (CheckSomeValue(3))
            m_value1 = 1;
    }
}
```
SampleMoreImpl.cs
```
internal partial class CSample : ISample
{
    public void MethodTwo()
    {
        m_value1 = 2;
    }

    // A comment for the local method 2
    private static bool CheckSomeValue2(/* IN */ const agrint& value)
    {
        // Method body
        return value != 0;
    }

    // Comment for method three
    public void MethodThree()
    {
        m_value1 = 3;
    }
}
```

## Struct type
For structs the goal is to change them into valid C# classes.

### Identifying structs
There are three differnt ways to define a struct in C++ and we need to identity all.

#### struct
```
struct MyStruct
{
    bool MyBoolField;
    agrint MyIntField;
};
```
### typedef struct
```
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;
```
### typedef struct tag
```
typedef struct MyTag
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;
```

### Persisting structs in .cs files
The app should identify correct .cs file home for the struct and keep it's order from the .h files.

Structs should be transformed to internal classes and same rules for classes should be applied when it comes to access modifiers except default access modifier is internal.

#### Struct type defined in a header file with pure virtual classes (interface)
Header file containing pure virtual classes will not have a matching .cpp file. Consider a pure virtual class declaration ISample in ISample.h where there is no ISample.cpp. 
In this case, the struct is copied to the ISample.cs file in the order they appear in the .h file

##### Sample
ISample.h
```
#pragma once

/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;

/* The Interface */
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1,
                           const bool &bParam2,
                           CString *pcParam3) = 0;

    virtual bool MethodTwo() = 0;
};

// This comment is for the other struct

typedef struct MyTag
{
    // This struct has a comment copied as is
    bool someBool;
    agrint intValue;
} MyOtherStruct;

```
ISample.cs
```
/* My struct */
internal class MyStruct
{
    internal bool MyBoolField;
    internal agrint MyIntField;
};

/* The Interface */
public interface ISample
{
    void MethodOne(CString cParam1,
                   bool bParam2,
                   out CString pcParam3);

    bool MethodTwo();
}

// This comment is for the other struct

internal class MyOtherStruct
{
    // This struct has a comment copied as is
    internal bool someBool;
    internal agrint intValue;
};

internal static class ISampleExtensions
{
    public static ISample GetInstance(this ISample sample)
    {
        CSample* pSample = new CSample();
        return pSample;
    }
}
```

### Struct type defined in a header file together with a class with .cpp implementation
In this case we copy the structs as is in the same order they appear in the header/source file.
Struct types can be defined both in .h files and .cpp files. We follow the same rules for the order they are writtent to the source .cs file as for classes.

### Struct type defined in a header file with no class
In this case we create a .cs file with same name as the .h file where all structs written as a class to this file.
##### Sample
MyStructs.h
```
#pragma once

/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;

public:
    // Constructor
    MyStruct(const bool& bField, const agrint& intField)
    {
        MyBoolField = bField;
        MyIntField = intField;
    };
} MyStruct;

// This comment is for the other struct

typedef struct MyTag
{
    // This struct has a comment copied as is
    bool someBool;
    agrint intValue;
} MyOtherStruct;

```
MyStructs.cs
```
/* My struct */
internal class MyStruct
{
    internal bool MyBoolField;
    internal agrint MyIntField;

    // Constructor
    public MyStruct(const bool& bField, const agrint& intField)
    {
        MyBoolField = bField;
        MyIntField = intField;
    };
};

// This comment is for the other struct

internal class MyOtherStruct
{
    // This struct has a comment copied as is
    internal bool someBool;
    internal agrint intValue;
};
```

## Define statements
Both C++ header (.h) and source files (.cpp) can hold #define statements and we need to assure these are collected and persisted in the model, and then written to correct location when we assemble the .cs files. 
In .h files, #define statements might appear outside class and struct definitions, and in .cpp files they appear outside the method bodies.

For this application the task is to assure we collect them from the header and source files as they are with preceding comments, persist them in the model as CppConst objects, and write them to the the target location in the .cs file with correct member variable indention and comments.

We follow a simple rule to transform the #define statement to `internal const <type> <name> = <value>;` statements.

Collecting defines from C++ files:
1. Defines can be found in both the header (.h) and source files (.cpp) for a class.
2. There might be more defines found in multi-file C++ class source files.

Constructing C# class with defines
1. Defines collected from header file are reconstructed as C# internal const members at the very start of the class after the class opening bracket. In case of partial classes, these defines will go to the main .cs file.
2. Defines collected from source files are reconstructed as C# private const members after the ones from the header file.
3. In case of source file defines in a partial class scenario, the partial source file defines are written to the matching partial class .cs file.

### Transforming to const members in the .cs file
The define statement is translated to an internal const in the format 
```
<access-modifier> <type> <name> = <value>;
```
If the define was found in header file, the <access-modifier> is internal.
If the define was found in source file, the <access-modifier> is private.

To descide the type we need to look at the value. 
#### Samples
```
#define mychar1define _T('')
#define mychar2define _T( '\t' )
#define mychar3define 't'

#define mystr1define _("")
#define mystr2define _("A")
#define mystr3define _("ABC")

#define myint1define    0
#define myint2define    123

#define mylong1define   0L
#define mylong2define   -123456789L

#define mydouble1define 0.0005
#define mydobule2define -0.1
#define mydobule3define 1D

#define mybool1define   TRUE
#define mybool3define   YES
#define mybool3define   OK
#define mybool4define   true
#define mybool5define   FALSE
#define mybool6define   NO
#define mybool7define   NOTOK
#define mybool8define   false
```

Are written to .cs as follows:
```
internal const char mychar1define = '';
internal const char mychar2define = '\t';
internal const char mychar3define 't';
internal const string mystr1define = "";
internal const string mystr2define = "A";
internal const string mystr3define = "ABC";

internal const int myint1define = 0;
internal const int myint2define = 123;

internal const long mylong1define = 0L;
internal const long mylong2define = -123456789L;

internal const double mydouble1define = 0.0005;
internal const double mydobule2define = -0.1;
internal const double mydobule3define = 1D;

internal const bool mybool1define = true;
internal const bool mybool3define = true;
internal const bool mybool3define = true;
internal const bool mybool4define = true;
internal const bool mybool5define = false;
internal const bool mybool6define = false;
internal const bool mybool7define = false;
internal const bool mybool8define = false;
```

### Defines with no value
We only collect define statements that followed with a value or expression and ignore those without.
For instance, we ignore defines like:
```
#define ARCHIVER_H_
```

### Sample with defines in both header and source files
CSample.h
```
#pragma once

// Here are some defines
// Comment for warning
#define WARNING 1
// Comment for stop
#define STOP 2
#define STOP_ALL 4

class CSample : public ISample
{
private:
    agrint m_value1;

public:
    void MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);    
}

// Some more defines
#define MY_DEFINE4 4
#define MY_DEFINE5 5
```

CSample.cpp
```
/* DEFINES IN CPP*/
// Also cpp files can have defines
#define CPP_DEFINE 10
#define CPP_DEFINE2 20 
// Comment for cpp define 3
#define CPP_DEFINE3 30 

void CSample::MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    // Implementation of MethodOne
}

#define CPP_DEFINE4 40
```

Expected generated CSample.cs
```
namespace Generated_CSample;

// Comment for class
internal class CSample : ISample
{
    // Here are some defines
    // Comment for warning
    internal const int WARNING = 1;
    // Comment for stop
    internal const int STOP = 2;
    internal const int STOP_ALL = 4;
    // Some more defines
    internal const int MY_DEFINE4 = 4;
    internal const int MY_DEFINE5 = 5;

    /* DEFINES IN CPP*/
    // Also cpp files can have defines
    private const int CPP_DEFINE = 10;
    private const int CPP_DEFINE2 = 20;
    // Comment for cpp define 3
    private const int CPP_DEFINE3 = 30;
    private const int CPP_DEFINE4 = 40;

    private agrint m_value1;

    public void MethodOne(CString cParam1,
            bool bParam2,
            out CString pcParam3)
    {
        // Implementation of MethodOne
    }
}
```

## Comments and regions
We want to assure we don't loose comments or C++ regions when building the .cs equalent code. 

### Comments
A comment is a block of lines outside method bodies that we want to persist in the resulting .cs file. 
With block of lines we mean:
* One or multiple lines where first two printable characters are "//" 
* One or multiple lines where first two printable characters are "/*" until end of comment "*/"
* If a comment is followed with one or multiple empty lines and then another block of comments, we persist the empty lines and extend the comment with the new lines with comments.

We have already defined that comments inside method bodies are handled by persisting method bodies as they are including comments. Here we care about comments outside method bodies describing a following type declarations (like interfaces, structs, classes) or class members like variables, methods, constructors, and destructors.

#### Comments in method parameter list
Method parameter lists can have comments in any order and any position both in the header declaration and source file implementation. The comments should be ignored when matching declaration with implementation.
For inline methods we persist the comments from the parameter list and write them to the .cs method parameter list.
For methods with implementation we need ignore any comments from the header and persist the source (.cpp) method parameter list comments this is the same for single class implementation as for partial class implementations.

Parameter comments are posional as they can be considered prefixing a parameter or postfixing a parameter.

### Samples for comments in method parameter list
The following samples show that comments in parameter list is valid at any point both in header and source files.

We need to assure method signatures still match by ignoring the comments but persist the comments for the inline method parameter list as well as the source (.cpp) parameter list. For methods with implementation in the source file, we persit the comments from the source file.

Note, the sample also shows that a valid C++ the header do not have to specify names for the parameters. We expect method to body matching to succeed as this is the same case as if the parameter names differs between the header declaration and the source implementation.

CSample.h
```
#pragma once

class CSample : public ISample
{
private:
    agrint m_value1;

public:
    void MethodOne(const CString&, // comment
                   const bool &, CString *);

    bool InlineMethod(/*void*/)
    { 
        return m_value1 > 3 && m_value1 < 33; 
    }    
}
```

CSample.cpp
```
void CSample::MethodOne(
    /* IN */ const CString& cParam1,
    /* IN */ const bool &bParam2,
    // somebody comment something here
    /* OUT */ CString *pcParam3)
{
    // Implementation of MethodOne
}
```

Expected CSample.cs
```
namespace Generated_CSample;

// Comment for class
internal class CSample : ISample
{
    private agrint m_value1;

    bool InlineMethod(/*void*/)
    { 
        return m_value1 > 3 && m_value1 < 33; 
    }    

    public void MethodOne(
            /* IN */ const CString& cParam1,
            /* IN */ const bool &bParam2,
            // somebody comment something here
            /* OUT */ CString *pcParam3)
    {
        // Implementation of MethodOne
    }
}

```

### File top comments
Source files (.cpp) can start with a block of comments at the very top that is not mapped to a consecutive element (class, struct, or define). This block of comment typically appear before any #include statement and we consider this a file top comment. Top file comments should be persisted and written to the top of the .cs file before any using statement.

This is general for the source file (.cpp) independent of the content otherwise (multiple classes, partial classes, or single classes). Top file comments sticks to the file.

#### Sample for multiple .cpp files for method bodies
CSample.h
```
#pragma once
// This is a class comment and maps to the class
class CSample : public ISample
{
private:
    agrint m_value1;

public:
	void MethodOne();
	void MethodTwo();
	void MethodThree();
}
```
CSample.cpp
```
// This is a file comment as it starts before the #include statement
/* This is a multi-line comments that is consider a continuation of the single-line comment
 * above. All comments, single lines, and multi-lines are together considered a block and handled by
 * our general rules for comments but sticks to the file
 */ 
#include "stdafx.h"
#include "CSample.h"

// This is a method comment and maps to the method
void CSample::MethodOne()
{
    m_value1 = 1;
}
```
SampleMoreImpl.cpp
```
/* 
 * SampleMoreImpl.cpp contains MethodTwo() and this block of comments should be
 * persisted to the top of SampleMoreImpl.cs - before the using statements 
 */ 
#include "stdafx.h"
#include "CSample.h"

// Method comments
void CSample::MethodTwo()
{
    m_value1 = 2;
}

// Comment for method three
void CSample::MethodThree()
{
    m_value1 = 3;
}
```
CSample.cs
```
// This is a file comment as it starts before the #include statement
/* This is a multi-line comments that is consider a continuation of the single-line comment
 * above. All comments, single lines, and multi-lines are together considered a block and handled by
 * our general rules for comments but sticks to the file
 */ 
using Agresso.Types;
using Agresso.Interface.CoreServices;
using BatchNet;
using BatchNet.Compatibility;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.ServerLib.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.Level2;
using static BatchNet.Compatibility.BatchApi;

internal partial class CSample : ISample
{
    private agrint m_value1;

    // This is a method comment and maps to the method
    public void MethodOne()
    {
        m_value1 = 1;
    }
}
```
SampleMoreImpl.cs
```
/* 
 * SampleMoreImpl.cpp contains MethodTwo() and this block of comments should be
 * persisted to the top of SampleMoreImpl.cs - before the using statements 
 */ 

using Agresso.Types;
using Agresso.Interface.CoreServices;
using BatchNet;
using BatchNet.Compatibility;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.ServerLib.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.Level2;
using static BatchNet.Compatibility.BatchApi;

internal partial class CSample : ISample
{
    // Method comments
    public void MethodTwo()
    {
        m_value1 = 2;
    }

    // Comment for method three
    public void MethodThree()
    {
        m_value1 = 3;
    }
}
```

### Rules
1) Comments are associated to the consecutive type or member and restored before their equivalent in the .cs file.
2) If a method has a block of comments associated in both the .h file and the .cpp file, we persist boths and write the comment from the .h file first.
3) There are cases where a comment follows a construct. For instance a member variable can have a comment on the same line following its declaration. Also, see the rules for pragma region in .h files.
4) We never add new comments or change the content of a existing comment, the goal is to persist the comments and write them to right location in the .cs file.

### Samples:
#### Comment block before class declaration
```
// This is a sample class
class CSample : public ISample
{
private:
	agrint m_value;
}
```

C# result 
```
// This is a sample class
internal class CSample : ISample
{
    private agrint m_value;
}
```

### Advanced comment block before class declaration
```
/* 
 *  Some description goes here
    And it ends like this */

// We actually have more to comment

/* And more */
class CSample : public ISample
{
private:
	agrint m_value;
}
```

C# result 
```
/* 
 *  Some description goes here
    And it ends like this */

// We actually have more to comment

/* And more */
internal class CSample : ISample
{
    private agrint m_value;
}
```

### Comment block before member variable
CSample.h
```
class CSample : public ISample
{
private:

    // My value holder

	agrint m_value;
}
```

Expected C# result 
```
internal class CSample : ISample
{
    // My value holder

    private agrint m_value;
}
```

### Optional comment block after member variable
CSample.h
```
class CSample : public ISample
{
private:

    // My value holder

	agrint m_value; /* This is a comment for the m_value member and
                     * it might span multiple lines 
                     */
}
```

Expected C# result 
```
internal class CSample : ISample
{
    // My value holder

    private agrint m_value; /* This is a comment for the m_value member and
                             * it might span multiple lines 
                             */
}
```

### Comment block before member definition in .h file
CSample.h
```
class CSample : public ISample
{
private:

	agrint m_value;

    /* Here is a test method
     * We describe stuff here
       still inside comment
    */ 
    void MethodSample(const CString & cStr);
}
```

CSample.cpp
```
void CSample::MethodSample(const CString & cStr)
{
    AGRWriteLog(cStr);
}
```

C# result 
```
internal class CSample : ISample
{
    private agrint m_value;

    /* Here is a test method
     * We describe stuff here
       still inside comment
    */ 
    private void MethodSample(const CString & cStr)
    {
        AGRWriteLog(cStr);
    }
}
```

### Comment block before member definition in .h file and .cpp implementation
CSample.h
```
class CSample : public ISample
{
private:

	agrint m_value;

    /* Here is a test method
     * We describe stuff here
       still inside comment
    */ 
    void MethodSample(const CString & cStr);
}
```

CSample.cpp
```
// For now we just log
void CSample::MethodSample(const CString & cStr)
{
    AGRWriteLog(cStr);
}
```

C# result 
```
internal class CSample : ISample
{
    private agrint m_value;

    /* Here is a test method
     * We describe stuff here
       still inside comment
    */ 
    // For now we just log
    private void MethodSample(const CString & cStr)
    {
        AGRWriteLog(cStr);
    }
}
```

## Regions
In C++ regions are defined by using #pragma region and #pragma endregion in both .h or .cpp file.
* We only want to recreate regions from the .cpp files since the .cpp files defines the order the member methods appear in the .cs file. By trying to also recreate the regions defined in .h files we might end up with conflicting or incorrect regions in the .cs file.
* Instead, we want to turn regions in the .h file into comments and handle them as ordenary comments where a) the region start comment is written before teh comments of the consecutive member variable or method and b) the region end comment is written after the preceding member variable or method. 
* For .cpp files, the region start is associated with the consecutive member and is written before any comments for that member, and the region end is assosicated with the preceding member and is written after the member body with an empty line between.
* Note that region start can be followed by description and we want to persist this. For intance ```#pragma region My Nice Region``` which is written as ```#region My Nice Region```. Region end can be followed by a comment on same line and we want to persist this as it is. For intance ```#pragma regionend // My Nice Region``` is persisted as ```#endregion // My Nice Regsion```.

#### Samples
CSample.h
```
class CSample : public ISample
{
private:
#pragma region My Variables

    // My comment
	agrint m_value;

#pragma endregion // My Variables

public:
    /* Some method */ 
    void MethodSample(const CString & cStr);
    /* Some method II */ 
    void MethodSample2(const CString & cStr);
    /* Some method III */ 
    void MethodSample3(const CString & cStr);
}
```

CSample.cpp
```
void CSample::MethodSample(const CString & cStr)
{
    AGRWriteLog(cStr);
}

#pragma region More Samples

void CSample::MethodSample2(const CString & cStr)
{
    AGRWriteLog(cStr);
}

void CSample::MethodSample3(const CString & cStr)
{
    AGRWriteLog(cStr);
}

#pragma endregion
```

C# result 
```
internal class CSample : ISample
{
    //#region My Variables

    // My comment
    private agrint m_value;

    //#endregion // My Variables

    /* Some method */ 
    public void MethodSample(const CString & cStr)
    {
        AGRWriteLog(cStr);
    }

    #region More Samples

    /* Some method II */ 
    public void MethodSample2(const CString & cStr)
    {
        AGRWriteLog(cStr);
    }

    /* Some method III */ 
    public void MethodSample3(const CString & cStr)
    {
        AGRWriteLog(cStr);
    }

    #endregion
}
```

# Summary
Typical unit of code consist of:
1. .h file with a pure virtual class definition (interface)
2. .h file with class definition in one of the following shapes:
    a) Pure definition with member variables and member function declaration
    b) Definition with member variables and member function declaration where one or more member function has an inline implementation.
    c) .h file with full inline implementation (no implementation in .cpp file)
3. One or more .cpp files containing the member function implementations (prefixed with ClassName::)
4. Classes where all members are static should be declared as static in .cs equalent.
6. Comments should be persisted as is for their consecutive type of member definition.
7. Pragma region and endregion should be persisted from .cpp file to the .cs file. Pragma region in .h files should be turned into comments and persisted as such. 

When we construct the C# files we have the following rules:
1. .h file defines the member variables and their access modifiers (private, protected, public)
2. .h file defines the parameter value defaulting (if any)
3. .cpp file defines the order of the implementations and the method bodies. The metod body should be copied just as it is.
4. Method bodies are persited as they are
5. .cpp file defines the real parameter names
6. When more than one .cpp file holds the class member implementations, we make the class partial and uses the same file names as the .cpp.
7. This is all a mechical operation consentrating on the structure of the classes and parameter defaulting. 
8. During these steps we do not about 
   a) change data types to their C# equalent 
   b) rewrite c++ syntax to c# syntax in parameter declarations or method bodies
   c) Add new code, comments or throw NoImplementedException.
