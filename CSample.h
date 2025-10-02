#pragma once

#include "ISample.h"

struct StructOne
{
public:
	agrint lTestType;

	TAttId attId;
	TDimValue dimVal;
};

class CSomeClass
{
	StructOne memberOne;
	int memberTwo;

	public:
		CSomeClass() : memberTwo(33) {
			memberOne.lTestType = 0;
		}

		int GetMemberTwo() const;
};

class CSample : public ISample
{
private:
	agrint m_value1;

	CString cValue1;
	CString cValue2;
	CString cValue3;

	static agrint m_iIndex;

	CString PrivateMemberWithBodyInHfile(const TAttId &att_id)
	{
		if (cValue1.IsEmpty()) return _T("");

		return cValue1;
	}

public:
	CSample();
	~CSample();

	void MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);

	// Method with body in header file
	bool MethodTwo() { return cValue1 == cValue2; }

private:

	bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
	bool MethodP5(const TDimValue& dim1, const agrint& int1, const agrint& int2=0);
	bool MethodP4() const;
	bool MethodP2(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
	bool MethodP3(const TDimValue& dim1, const agrint& int1, const agrint& int2);

	int MethodPrivInl1(const TDimValue& dim1)
	{
		if (dim1.IsEmpty()) 
			return 0;
		
		return 42;
	}

	bool CSample::MethodPrivInl2(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue=0, bool bError=false)
	{
		if (dimPd.IsEmpty()) 
			return bError;

		return lLimitHorizon >= iValue;
	}

	int m_iAnotherPrivateInteger;
};
