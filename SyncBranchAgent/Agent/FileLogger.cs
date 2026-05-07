using System;
using System.IO;

namespace SyncBranchAgent.Agent
{
    public class FileLogger
    {
        private readonly string logPath;
        private readonly object syncRoot = new object();

        public FileLogger(string logPath)
        {
            this.logPath = logPath;
            Directory.CreateDirectory(logPath);
        }

        public void Info(string message)
        {
            Write("INFO", message, null);
        }

        public void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private void Write(string level, string message, Exception ex)
        {
            var line = DateTime.UtcNow.ToString("o") + " [" + level + "] " + message;
            if (ex != null)
            {
                line += Environment.NewLine + ex;
            }

            lock (syncRoot)
            {
                var file = Path.Combine(logPath, "branch-sync-" + DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
                File.AppendAllText(file, line + Environment.NewLine);
            }
        }
    }
}
