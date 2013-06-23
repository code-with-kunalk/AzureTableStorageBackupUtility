using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageBackupUtility
{
    public enum LogType
    {
        Verbose,
        Error
    }

    public class Logging
    {
        private readonly string _backupPath;
        private readonly string _logFilePath;
        private readonly string _resultFilePath;
        private const string _logFileName = "Logs.txt";
        private const string _resultFileName = "Result.txt";

        public string SourceClassName { get; set; }

        public Logging()
        {
            _backupPath = ConfigurationManager.AppSettings["backupPath"] + DateTime.UtcNow.Date.ToString("yyyyMMdd") + "\\" + ConfigurationManager.AppSettings["SourceAccountName"];
            if (!Directory.Exists(_backupPath)) Directory.CreateDirectory(_backupPath);
            _logFilePath = _backupPath + "\\" + _logFileName;
            if (!File.Exists(_logFilePath)) File.Create(_logFilePath);
            _resultFilePath = _backupPath + "\\" + _resultFileName;
            if (!File.Exists(_resultFilePath)) File.Create(_resultFilePath);
        }

        public void Log(string methodName, string message, string parameters)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_logFilePath, true))
                {
                    sw.WriteLine(GetLogText(methodName, LogType.Verbose, message, "", parameters));
                }
            }
            catch
            { }
        }

        public void LogException(string methodName, Exception ex, string parameters)
        {
            try
            {
                var innermostException = GetException(ex);
                using (StreamWriter sw = new StreamWriter(_logFilePath, true))
                {
                    sw.WriteLine(GetLogText(methodName, LogType.Error, ex.Message, ex.StackTrace, parameters));
                }
            }
            catch
            { }
        }

        private Exception GetException(Exception exception)
        {
            if (exception.InnerException != null)
                exception = GetException(exception.InnerException);
            return exception;
        }

        private string GetLogText(string methodName, LogType logType, string message, string stackTrace, string parameters)
        {
            return String.Format(
                "TimeStamp: " + DateTime.Now + Environment.NewLine +
                "Source Class Name: {0}" + Environment.NewLine +
                "Method Name: {1}" + Environment.NewLine +
                "Log Type: {2}" + Environment.NewLine +
                "Message: {3}" + Environment.NewLine +
                "Stack Trace: {4}" + Environment.NewLine +
                "Parameters: {5}{6}", SourceClassName, methodName, logType, message, stackTrace, parameters, Environment.NewLine);
        }

        public void LogCompletion(string tableName, int rowCount, bool isSuccess)
        {
            using (StreamWriter sw = new StreamWriter(_resultFilePath, true))
            {
                sw.WriteLine(String.Format("Table Name: {0}\tRow Count: {1}\tIsSuccess: {2}", tableName, rowCount, isSuccess));
            }
        }
    }
}
