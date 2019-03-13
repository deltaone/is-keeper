using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Linq;

using Mono.Options;

namespace Core
{
    public static partial class GM
    {   // settings
        public static bool assemblyDebugMode;
        // assembly attributes
        static string _assemblyTitle = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute), false)).Product;
        static string _assemblyNote = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false)).Title;
        static string _assemblyCopyright = ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false)).Copyright;
        public static string assemblyGUID = ((GuidAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(GuidAttribute), false)).Value;
        // version   
        static Version _assemblyVersionRaw = Assembly.GetExecutingAssembly().GetName().Version;
        static readonly string _assemblyVersion = _assemblyVersionRaw.Major.ToString() + "." + _assemblyVersionRaw.Minor.ToString() + " build " + _assemblyVersionRaw.Build;
        static readonly string _assemblyDate = (new DateTime(2000, 1, 1).AddDays(_assemblyVersionRaw.Build).AddSeconds(_assemblyVersionRaw.Revision * 2)).ToString("dd.MM.yyyy");
        public static readonly string assemblyCombinedVersion = "v" + _assemblyVersion + " [" + _assemblyDate + "]";
        // paths
        public static readonly string assemblyFile = Assembly.GetExecutingAssembly().Location;
        public static readonly string assemblyFolder = Path.GetFullPath(Path.GetDirectoryName(assemblyFile) + Path.DirectorySeparatorChar);
        public static readonly string assemblyStartupFolder = Path.GetFullPath(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
        // classes    
        public static LogFile Logger = new LogFile(Path.Combine(assemblyFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".log"), true);
        public static Random Random = new Random(DateTime.Now.Millisecond);
        // other
        public static readonly string assemblyCombinedTitle;
        public static event Action<string> PrintEventHandler;

        static Dictionary<int, Stack<string>> _logScopeStack = new Dictionary<int, Stack<string>>();

        // ------------------------------------------------------------------------

        static GM()
        {
            assemblyCombinedTitle = _assemblyTitle + " " + assemblyCombinedVersion + " / (c) " + assemblyAuthor;
        }

        // ------------------------------------------------------------------------

        [DllImport("User32")]
        public static extern int MessageBox(int hWnd, string text, string caption, int type);

        public static void Print(string message)
        {
			if(message == null)
				return;
            lock ("GM.Print")
            {
                try
                {
                    if (PrintEventHandler != null) PrintEventHandler(message);
                }
                catch (Exception ex)
                {
                    Log("[WARNING] " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        public static void Warning(string message, int callerLevel = 0)
        {
            var i = GetCallerInfo(2 + (callerLevel > 0 ? callerLevel : checked(-callerLevel)));
            Log("[WARNING] " + message + string.Format(" <{0}/{1}()[{2}]>", i["file"], i["method"], i["line"]));
            Logger.FlushLog();
        }

        public static void Error(string message, int callerLevel = 0)
        {
            if (assemblyDebugMode)
            {
                var i = GetCallerInfo(2 + (callerLevel > 0 ? callerLevel : checked(-callerLevel)));
                Print("[ERROR] " + message + string.Format(" <{0}/{1}()[{2}]>", i["file"], i["method"], i["line"]));
                foreach (var e in GetStackTrace(2)) Log("[ERROR] " + e);
            }
            else
                Print("[ERROR] " + message);

            Logger.FlushLog();
        }

        public static void Critical(string message, int callerLevel = 0)
        {
            var i = GetCallerInfo(2 + (callerLevel > 0 ? callerLevel : checked(-callerLevel)));
            message = "[CRITICAL] " + message + string.Format(" <{0}/{1}()[{2}]>", i["file"], i["method"], i["line"]);
            foreach (var e in GetStackTrace(2)) message += "\n   " + e;
            Logger.Write(message);
            Logger.FlushLog();
            MessageBox(0, message, "[ERROR]", 0); // Assembly.GetExecutingAssembly().GetName().Name + " :: Error!"
            Environment.Exit(0);
        }

        public static void Debug(string message, int callerLevel = 0)
        {
            if (!assemblyDebugMode) return;
            var i = GetCallerInfo(2 + (callerLevel > 0 ? callerLevel : checked(-callerLevel)));
            Print(String.Format("[DEBUG] {0}(): {1}", i["method"], message));
            Logger.FlushLog();
        }

        //---------------------------------------------------------------------

        public static Dictionary<string, string> GetCallerInfo(int frame = 1)
        {   // MethodBase.GetCurrentMethod().Name
            var f = new StackFrame(frame, true);
            return (new Dictionary<string, string> { { "method", f.GetMethod().Name }, 
                    { "file", Path.GetFileName(f.GetFileName()) },  { "line", f.GetFileLineNumber().ToString() }});
        }

        public static List<string> GetStackTrace(int skip = 2)
        {
            var result = new List<string>();
            var stack = new System.Diagnostics.StackTrace(true).ToString();
            var trace = new List<string>(stack.Split('\n'));

            trace.RemoveRange(0, skip);
            trace.Reverse();

            bool found = false;
            foreach (string e in trace)
            {
                if (!found && e.Contains("Main")) found = true;
                if (found) result.Add(e.Replace('\n', ' ').Replace("\r", "").Replace('\t', ' '));
            }
            result.Reverse();
            return (result);
        }

        //---------------------------------------------------------------------

        public static void LogScopeSet(string scope)
        {
            lock (_logScopeStack)
            {
                int id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if (!_logScopeStack.ContainsKey(id)) _logScopeStack[id] = new Stack<string>();
                _logScopeStack[id].Push(scope);
            }
        }

        public static void LogScopeRestore()
        {
            lock (_logScopeStack)
            {
                int id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if (!_logScopeStack.ContainsKey(id)) return;
                _logScopeStack[id].Pop();
                if (_logScopeStack[id].Count <= 0) _logScopeStack.Remove(id);
            }
        }

        public static void Log(string scope, string message, bool flush = false)
        {
            if (!assemblyLogEnabled || message == null) return;
            int id = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (String.IsNullOrEmpty(scope) || scope.Trim().Length == 0)
            {
                if (_logScopeStack.ContainsKey(id) && _logScopeStack[id].Count > 0) scope = "[" + _logScopeStack[id].Peek() + "] ";
                else scope = "";
            }
            else scope = "[" + scope + "] ";
            var messages = message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var s in messages)
                Logger.Write(scope + s, flush);
        }

        public static void Log(string message)
        {   
            Log("MAIN", message, false);
        }

        //---------------------------------------------------------------------

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            uint lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            uint hTemplateFile);

        public static void CreateConsole()
        {
            // stackoverflow.com/questions/472282/show-console-in-windows-application
            // stackoverflow.com/questions/41624103/console-out-output-is-showing-in-output-window-needed-in-allocconsole
            // stackoverflow.com/questions/15604014/no-console-output-when-using-allocconsole-and-target-architecture-x86#

            if (!AttachConsole(-1))
            {
                AllocConsole();
                IntPtr stdHandle = CreateFile(
                    "CONOUT$", 0x40000000, // GENERIC_WRITE
                    0x2, // FILE_SHARE_WRITE
                    0, 0x3, // OPEN_EXISTING
                    0, 0);

                SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
                FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                Encoding encoding = System.Text.Encoding.GetEncoding(437);
                StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }
            else
                Console.WriteLine("");
            
        }

        //---------------------------------------------------------------------

        [STAThread]
        static void Main(string[] args)
        {
            System.Threading.Mutex mutex = null;
            if (assemblySingleInstance)
            {
                mutex = new System.Threading.Mutex(true, assemblyGUID, out bool result);
                if (!result)
                {
                    MessageBox(0, "Another instance is already running.", "[CRITICAL]", 0);
                    return;
                }
            }

            try
            {
                #if DEBUG
                    assemblyDebugMode = true;
                    assemblyLogEnabled = true;
                #else
                    assemblyDebugMode = false;
                #endif

                bool showHelp = false;
                OptionSet assemblyOptionSet = new OptionSet()
                {        
                    { "d|debug",    v => {assemblyDebugMode = true; assemblyLogEnabled = true;}},
                    { "c|console",  v => {assemblyUseConsole = true;}},
                    { "h|?|help",   v => {showHelp = true;}}, // { "l|log=",     v => GM.fileLog = v },
                };
                
                foreach (var e in Program.assemblyOptionSet)
                    try { assemblyOptionSet.Add(e); } catch { }

                List<string> extra = assemblyOptionSet.Parse(args);

                if (assemblyUseConsole)
                {   
                    CreateConsole();
                    try
                    {
                        Console.SetWindowSize(120, 42); // Console.OutputEncoding = Encoding.UTF8; // utf16 ? Console.Write('\u2103'); //℃ character code
                    }
                    catch { }
                    GM.PrintEventHandler += Console.WriteLine;                    
                }

                GM.PrintEventHandler += GM.Log;
                GM.Log("Log started: " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));
                
                string message = _assemblyTitle + " " + assemblyCombinedVersion + "\n" + _assemblyCopyright + "\n";
                GM.Print(message);

                if ((assemblyArgsMin > 0 && args.Length < assemblyArgsMin) || showHelp)
                {
                    string usage = "Usage:\n" + "    " + Path.GetFileName(assemblyFile) + " " + assemblyUsage;
                    if (assemblyUseConsole) GM.Print(usage);
                    else MessageBox(0, message + usage, "Usage ...", 0);
                }
                else
                {
                    string error = Program.Startup(extra);
                    if (string.IsNullOrEmpty(error))
                        Program.DoTask(extra);
                    else
                        GM.Error(error);
                }

                if (assemblyUseConsole)
                {
                    GM.Print("\nDone!");
                    //if (assemblyPauseAfterExit) Console.ReadKey(true);
                    if (assemblyPauseAfterExit) MessageBox(0, "Press 'OK' to close console ...", "Information", 0);                    
                }
            }
            catch (Exception ex)
            {
                string message = "Exception:\n   " + ex.Message + "\n\nTrace:\n" + ex.StackTrace;
                Logger.Write(message);
                MessageBox(0, message, "[CRITICAL]", 0);
            }

            if (assemblySingleInstance) GC.KeepAlive(mutex);  // mutex shouldn't be released - important line
        }

        //---------------------------------------------------------------------
    }
}