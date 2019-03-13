using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Core
{
    public class LogFile
    {
        private string _logPath = "./debug.log"; // log file path
        private int _maxLogAge = 5; // Max Age in seconds
#if DEBUG
		private int _queueSize = 0; // Max Queue Size
#else
		private int _queueSize = 15; // Max Queue Size
#endif
		private bool _logOnlyTime = true;
        private Queue<LogEntry> _logQueue = new Queue<LogEntry>();
        private DateTime _lastFlushed = DateTime.Now;

        public class LogEntry
        {
            public string time { get; set; }
            public string date { get; set; }
            public string message { get; set; }

            public LogEntry(string message)
            {
                date = DateTime.Now.ToString("yyyy-MM-dd");
                time = DateTime.Now.ToString("HH:mm:ss.fff");
                this.message = message;
            }
        }

        //private static readonly Destructor Finalise = new Destructor();
        //private sealed class Destructor { ~Destructor() { if (_logQueue.Count > 0) FlushLog(); }}

        //static LogFile() { AppDomain.CurrentDomain.ProcessExit += ClassDestructor; // DomainUnload }
        //static void ClassDestructor(object sender, EventArgs e) { if (_logQueue.Count > 0) FlushLog(); }

        public LogFile()
        {
            //Write("Log started: " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));
        }

        public LogFile(string path, bool overwrite = true)
        {
            if (overwrite) File.Delete(path);
            SetLogPath(path);            
            //Write("Log started: " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));
        }

        ~LogFile()
        {            
            //Write("Log closed: " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));
            FlushLog();
        }

        public void SetLogPath(string path) 
        { 
            _logPath = path; 
        }

        public string GetLogPath() 
        { 
            return (_logPath); 
        }

        public void SetLogMaxAge(int age)
        {
            _maxLogAge = age;
        }

        public int GetLogMaxAge()
        {
            return (_maxLogAge);
        }

        public void SetLogMaxQueueSize(int size)
        {
            _queueSize = size;
        }

        public int GetLogMaxQueueSize()
        {
            return (_queueSize);
        }

        public void Write(string message, bool flush = false)
        {
            lock (_logQueue) // Lock the queue while writing to prevent contention for the log file
            {
                LogEntry logEntry = new LogEntry(message.Trim('\r', '\n')); // Create the entry and push to the Queue
                _logQueue.Enqueue(logEntry);
                if (flush || _logQueue.Count >= _queueSize || DoPeriodicFlush()) FlushLog(); // If we have reached the Queue Size then flush the Queue
            }
        }

        public void Write(string message) 
        { 
            Write(message, false); 
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - _lastFlushed;
            if (logAge.TotalSeconds >= _maxLogAge)
            {
                _lastFlushed = DateTime.Now;
                return(true);
            }
            return(false);
        }

        public void FlushLog()
        {
            if (_logQueue.Count <= 0) return;

            using (FileStream fs = File.Open(_logPath, FileMode.Append, FileAccess.Write))
                using (StreamWriter log = new StreamWriter(fs))
                    while (_logQueue.Count > 0)
                    {
                        LogEntry entry = _logQueue.Dequeue();
                        string timestamp;
                        if(_logOnlyTime) timestamp = entry.time.Trim();
                        else timestamp = entry.date.Trim() + " " + entry.time.Trim();
                        log.WriteLine(string.Format("[{0}]: {1}", timestamp, entry.message));
                    }
        }
    }
}