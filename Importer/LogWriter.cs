using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer
{
    internal  class LogWriter
    {
        private readonly ILog log4NetAdapter;

        public LogWriter(Type type)
        {
            this.log4NetAdapter = LogManager.GetLogger(type);
            log4net.GlobalContext.Properties["host"] = Environment.MachineName;
        }
 
        public void LogDebug(string message)
        {
            this.log4NetAdapter.Debug(message);
        }
 
        public void LogDebug(string message, Exception exception)
        {
            this.log4NetAdapter.Debug(message, exception);
        }
 
        public void LogError(string message)
        {
            this.log4NetAdapter.Error(message);
        }
 
        public void LogError(string message, Exception exception)
        {
            this.log4NetAdapter.Error(message, exception);
        }
 
        public void LogFatal(string message)
        {
            this.log4NetAdapter.Fatal(message);
        }
 
        public void LogFatal(string message, Exception exception)
        {
            this.log4NetAdapter.Fatal(message, exception);
        }
 
        public void LogInfo(string message)
        {
            this.log4NetAdapter.Info(message);
        }
 
        public void LogInfo(string message, Exception exception)
        {
            this.log4NetAdapter.Info(message, exception);
        }
 
        public void LogWarning(string message)
        {
            this.log4NetAdapter.Warn(message);
        }
 
        public void LogWarning(string message, Exception exception)
        {
            this.log4NetAdapter.Warn(message, exception);
        }
 
        //public static void WriteLog(string path, string Mesg)
        //{
        //    StreamWriter sm = new StreamWriter(path, true);
        //    sm.WriteLine(Mesg);
        //    sm.Flush();
        //    sm.Close();
        //}
    }

    public enum ErrorType
    {
        Info=1,
        Error=2
    }

}
