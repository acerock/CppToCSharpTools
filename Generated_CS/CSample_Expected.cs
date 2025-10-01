using System;

namespace GeneratedClasses
{
    /// <summary>
    /// Generated from CSample.h and CSample.cpp
    /// Demonstrates the expected structural conversion from C++ to C#
    /// </summary>
    public class CSample : ISample
    {
        #region Private Members
        
        private int m_value1;
        private string cValue1;
        private string cValue2;
        private string cValue3;
        private static int m_iIndex = -1; // Static member initialization from .cpp
        private int m_iAnotherPrivateInteger;
        
        #endregion

        #region Public Methods

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

        public void MethodOne(string cParam1, bool bParam2, out string pcParam3)
        {
            // Implementation from .cpp - parameter names from implementation, 
            // but would use defaults from header if present
            pcParam3 = string.Empty; // Initialize out parameter
            // Implementation of MethodOne
        }

        /// <summary>
        /// Method with inline implementation in header - converted directly
        /// </summary>
        public bool MethodTwo() 
        { 
            return cValue1 == cValue2; 
        }

        #endregion

        #region Private Methods

        // Parameter names from .cpp implementation: (dimPd, lLimitHorizon, iValue, bError)
        // Default values from .h declaration: (dim1, int1, int2=0, bool1=false)
        // Result: use impl names with header defaults
        private bool MethodP1(TDimValue dimPd, int lLimitHorizon, int iValue = 0, bool bError = false)
        {
            if (dimPd.IsEmpty()) 
                return bError;

            return lLimitHorizon >= iValue;
        }

        private bool MethodP2(TDimValue dim1, int int1, int int2 = 0, bool bool1 = false)
        {
            // Implementation of MethodP2
            return true;
        }

        // Parameter name mismatch resolved - using implementation names
        private bool MethodP3(TDimValue dimVal, int intVal, int int2)
        {
            // Implementation of MethodP3
            return false;
        }

        private bool MethodP4()
        {
            // Implementation of MethodP4
            return cValue1 == cValue3;
        }

        private bool MethodP5(TDimValue dim1, int int1, int int2 = 0)
        {
            // Implementation of MethodP5
            return !dim1.IsEmpty() && int1 > int2;
        }

        /// <summary>
        /// Private method with inline implementation in header
        /// </summary>
        private string PrivateMemberWithBodyInHfile(TAttId att_id)
        {
            if (cValue1 == string.Empty) return string.Empty;
            return cValue1;
        }

        /// <summary>
        /// Another inline method from header
        /// </summary>
        private int MethodPrivInl1(TDimValue dim1)
        {
            if (dim1.IsEmpty()) 
                return 0;
            
            return 42;
        }

        /// <summary>
        /// Inline method with class qualifier (fixed)
        /// Original: bool CSample::MethodPrivInl2(...) - invalid in header
        /// Corrected: bool MethodPrivInl2(...)
        /// </summary>
        private bool MethodPrivInl2(TDimValue dimPd, int lLimitHorizon, int iValue = 0, bool bError = false)
        {
            if (dimPd.IsEmpty()) 
                return bError;

            return lLimitHorizon >= iValue;
        }

        #endregion
    }
}