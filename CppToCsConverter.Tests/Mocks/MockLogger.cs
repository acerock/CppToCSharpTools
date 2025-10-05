using System.Collections.Generic;
using CppToCsConverter.Core.Logging;

namespace CppToCsConverter.Tests.Mocks
{
    public class MockLogger : ILogger
    {
        public List<string> ErrorMessages { get; } = new List<string>();
        public List<string> WarningMessages { get; } = new List<string>();
        public List<string> InfoMessages { get; } = new List<string>();

        public void LogError(string message)
        {
            ErrorMessages.Add(message);
        }

        public void LogWarning(string message)
        {
            WarningMessages.Add(message);
        }

        public void LogInfo(string message)
        {
            InfoMessages.Add(message);
        }
    }
}