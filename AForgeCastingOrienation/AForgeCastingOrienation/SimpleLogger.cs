using System;
using System.IO;

namespace AForgeCastingOrienation
{
    /// <summary>
    /// Simple file-based logger for application errors and events
    /// </summary>
    internal class SimpleLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AForgeCastingOrientation",
            "app.log"
        );

        private static readonly object LockObject = new object();

        /// <summary>
        /// Logs an error message to the log file
        /// </summary>
        /// <param name="message">Error message to log</param>
        /// <param name="exception">Optional exception to log</param>
        public static void LogError(string message, Exception exception = null)
        {
            Log("ERROR", message, exception);
        }

        /// <summary>
        /// Logs an informational message to the log file
        /// </summary>
        /// <param name="message">Information message to log</param>
        public static void LogInfo(string message)
        {
            Log("INFO", message, null);
        }

        /// <summary>
        /// Core logging method
        /// </summary>
        private static void Log(string level, string message, Exception exception)
        {
            try
            {
                lock (LockObject)
                {
                    // Ensure directory exists
                    string directory = Path.GetDirectoryName(LogFilePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Format log entry
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{level}] {message}";
                    
                    if (exception != null)
                    {
                        logEntry += $"{Environment.NewLine}Exception: {exception.GetType().Name}: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                    }

                    // Write to file
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if logging fails to prevent cascading errors
            }
        }
    }
}
