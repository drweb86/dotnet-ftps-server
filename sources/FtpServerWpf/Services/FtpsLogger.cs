using System;
using FtpsServerLibrary;

namespace FtpsServerApp.Services
{
    public class FtpsLogger : IFtpsServerLog
    {
        public event Action<string>? LogMessageReceived;

        public void Debug(string message)
        {
            Log("DEBUG", message);
        }

        public void Error(Exception ex, string message)
        {
            Log("ERROR", $"{message}: {ex.Message}");
        }

        public void Fatal(Exception ex, string message)
        {
            Log("FATAL", $"{message}: {ex.Message}");
        }

        public void Info(string message)
        {
            Log("INFO", message);
        }

        public void Warn(string message)
        {
            Log("WARN", message);
        }

        private void Log(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] [{level}] {message}";
            LogMessageReceived?.Invoke(logMessage);
        }
    }
}
