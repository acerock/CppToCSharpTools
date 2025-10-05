using System;

namespace CppToCsConverter.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void LogError(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public void LogWarning(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public void LogInfo(string message)
        {
            Console.WriteLine(message);
        }
    }
}