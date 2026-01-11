using System;
using System.IO;

namespace AgentConnect.Services
{
    public static class UpdateLogger
    {
        private static readonly object _lock = new object();
        private static readonly string _logPath;

        static UpdateLogger()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(appData, "AgentConnect", "Logs");
            Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, $"update-{DateTime.Now:yyyy-MM-dd}.log");
        }

        public static string LogPath => _logPath;

        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] {message}";

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
                catch
                {
                    // Ignore logging failures
                }
            }

            System.Diagnostics.Debug.WriteLine(line);
        }

        public static void Log(string category, string message)
        {
            Log($"[{category}] {message}");
        }

        public static void LogException(string category, Exception ex)
        {
            Log(category, $"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Log(category, $"STACK: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Log(category, $"INNER: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
    }
}
