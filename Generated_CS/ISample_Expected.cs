using System;

namespace GeneratedInterfaces
{
    /// <summary>
    /// Generated from ISample.h
    /// C++ interface with __declspec(dllexport) becomes public interface
    /// </summary>
    public interface ISample
    {
        void MethodOne(string cParam1, bool bParam2, out string pcParam3);
        bool MethodTwo();
    }

    /// <summary>
    /// Extension methods for static methods from the interface
    /// </summary>
    public static class ISampleExtensions
    {
        public static ISample GetInstance(this ISample instance)
        {
            return new CSample();
        }
    }
}