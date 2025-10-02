using Agresso.Types;
using BatchNet.Compatibility.Types;
using U4.BatchNet.Common.Compatibility;


namespace U4.BatchNet.LibSample.Compatibility
{
	public static class ISampleExtensions
	{
		public static ISample GetInstance(this ISample sample)
		{
			CSample* pSample = new CSample();
			return pSample;
		}
	}
	
	public class CSample : ISample
	{
		private agrint m_value1;

		private CString cValue1;
		private CString cValue2;
		private CString cValue3;

		private static agrint m_iIndex = -1;

		public CSample()
		{
			m_value1 = 0;

			cValue1 = "ABC";
			cValue2 = "DEF";
			cValue3 = "GHI";
		}

		~CSample()
		{
			// Cleanup code here
		}

		public void MethodOne(CString cParam1,
					bool bParam2,
					out CString pcParam3)
		{
			// Implementation of MethodOne
		}

		private bool MethodP1(TDimValue dimPd, agrint lLimitHorizon, agrint iValue, bool bError)
		{
			if (dimPd.IsEmpty())
				return bError;

			return lLimitHorizon >= iValue;
		}

		private bool MethodP2(TDimValue dim1, agrint int1, agrint int2, bool bool1)
		{
			// Implementation of MethodP2
			return true;
		}

		private bool MethodP3(TDimValue dimVal, agrint intVal, agrint int2)
		{
			// Implementation of MethodP3
			return false;
		}

		private bool MethodP4()
		{
			// Implementation of MethodP4
			return cValue1 == cValue3;
		}

		// Method with body in header file
		public bool MethodTwo() { return cValue1 == cValue2; }

		private CString PrivateMemberWithBodyInHfile(TAttId att_id)
		{
			if (cValue1.IsEmpty()) return "";

			return cValue1;
		}

		private int MethodPrivInl1(TDimValue dim1)
		{
			if (dim1.IsEmpty())
				return 0;

			return 42;
		}

		private bool MethodPrivInl2(TDimValue dimPd, agrint lLimitHorizon, agrint iValue = 0, bool bError = false)
		{
			if (dimPd.IsEmpty())
				return bError;

			return lLimitHorizon >= iValue;
		}

		private int m_iAnotherPrivateInteger;
	}	
}

