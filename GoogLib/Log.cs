namespace GoogLib
{
    public class Log
    {
        public const string LogFileName = "trebuchet_{0}.log";
        public const string LogFolderName = "Logs";
        public const int MaxLogFiles = 10;
        public const long MaxLogSize = 1024 * 1024 * 10;
        
        #if DEBUG
        public const LogSeverity MinimumSeverity = LogSeverity.Debug;
        #else
        public const ApplicationLogSeverity MinimumSeverity = ApplicationLogSeverity.Info;
        #endif

        private static Log? _instance;

        private Log()
        { }

        public static Log Instance => _instance ??= new Log();

        /// <summary>
        /// Get the most recent log file that is under the max size or get a new one if none is found
        /// </summary>
        /// <returns>Path toward the most recent log file</returns>
        public string GetLogFilePath()
        {
            var logFolder = Path.Combine(Environment.CurrentDirectory, LogFolderName);
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            var logFiles = Directory.GetFiles(logFolder, "*.log");
            if (logFiles.Length == 0)
                return Path.Combine(logFolder, string.Format(LogFileName, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));

            var mostRecent = logFiles[0];
            var mostRecentDate = File.GetLastWriteTime(mostRecent);
            for (var i = 1; i < logFiles.Length; i++)
            {
                var logFile = logFiles[i];
                var logFileDate = File.GetLastWriteTime(logFile);
                if (logFileDate > mostRecentDate)
                {
                    mostRecent = logFile;
                    mostRecentDate = logFileDate;
                }
            }

            if (new FileInfo(mostRecent).Length > MaxLogSize)
                return Path.Combine(logFolder, string.Format(LogFileName, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));

            return mostRecent;
        }

        /// <summary>
        /// Log a message to the most recent log file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public static void Write(string message, LogSeverity level)
        {
            if (level < MinimumSeverity)
                return;
            var logFile = Instance.GetLogFilePath();
            File.AppendAllText(logFile, $"[{DateTime.Now}] {level}: {message}" + Environment.NewLine);
            Instance.PurgeOldLogFiles();
        }

        /// <summary>
        /// Log an exception to the most recent log file as error
        /// </summary>
        /// <param name="ex"></param>
        public static void Write(Exception ex)
        {
            var logFile = Instance.GetLogFilePath();
            File.AppendAllText(logFile, $"[{DateTime.Now}] {LogSeverity.Critical}: {ex.Message}" + Environment.NewLine);
            File.AppendAllText(logFile, ex.StackTrace + Environment.NewLine);
            if (ex.InnerException != null)
                Write(ex.InnerException);
            Instance.PurgeOldLogFiles();
        }

        private void PurgeOldLogFiles()
        {
            var logFolder = Path.Combine(Environment.CurrentDirectory, LogFolderName);
            var logFiles = Directory.GetFiles(logFolder, "*.log");
            if (logFiles.Length <= MaxLogFiles)
                return;

            var filesToDelete = logFiles.OrderBy(File.GetLastWriteTime).Take(logFiles.Length - MaxLogFiles);
            foreach (var file in filesToDelete)
                File.Delete(file);
        }
    }
}