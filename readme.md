# Introduction
This folder contains files to demostrate how C++ interfaces and classes are defined and how we expect the C# equalent to be produced.

This document describes the recommeded approach to handle the complexity of translating these classes into the desired C# output.

The following files exist to highlight common structure in .h and .cpp files with expected .cs.

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
    void MethodOne(CString cParam1,
                   bool bParam2,
                   out CString pcParam3);

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
    void MethodOne(CString cParam1,
                   bool bParam2,
                   out CString pcParam3);

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

## C++ header files (.h)
A C++ header file can hold one or more type definitions like structs, classes, defines, etc. 
For a class it defines the public, protected, or private access specifiers and the default values of the parameters and these are important to bring to the C# code. If a method is defined without access specifier it is considered private.

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

#### Sample result from header file information
This sample shows how this starts to build up the structure and content of the C# result file.
```
	public class CSample : ISample
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

## C++ source files (.cpp)
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

### Adding method definitions to the C# file
To complete the .cs file with method bodies we have to look at the .cpp file for the implementations.
Also, the .cpp file defines the order of the method implementations in the .cs file, so when the order is different as in the .h file, we will move the method to match the ordering in the .cpp file. This will allow us to do a side-by-side compare of the C++ file and the C# file.

In addition the C++ defines the parameter names. As we remember, the declaration in the header file defines the parameter defaulting (if any) but not the final parameter names (as this might not be given at all) so we applyes the defaulting to the .cpp method definition.

## Classes with bodies in multiple .cpp files
There is no 1 to 1 relation with .h files and .cpp files. The body of a method can be in .cpp file with same name as the .h file, but in many cases this is not true, and the method bodies can be spread around multiple .cpp files.

In these cases we make a partial C# class implemented in .cs file names as the original .cpp file. 

#### Sample for multiple .cpp files with method bodies
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
```
CSample.cs
```
public partial class CSample : ISample
{
    public void MethodOne()
    {
        m_value1 = 1;
    }
}
```
SampleMoreImpl.cs
```
public partial class CSample : ISample
{
    public void MethodTwo()
    {
        m_value1 = 2;
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
public class CSample : ISample
{
    private static agrint s_value = 42;
}
```

## Classes with static members and intialization
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
public class CSample : ISample
{
    private static agrint s_value = 42;
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
