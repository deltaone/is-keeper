using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

using System.Linq;

using Mono.Options;

using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

[assembly: AssemblyProduct("IS-Keeper")]
[assembly: AssemblyTitle("IS-Keeper")]
[assembly: AssemblyCopyright("Copyright (c) 2019 / de1ta0ne")]
[assembly: Guid("7A8659F1-61B8-4A3E-9201-000020170102")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]
//[assembly: CLSCompliant(true)]

namespace Core
{
    public static partial class GM
    {   // NOTE: for console output from VS - Enable the Visual Studio hosting process from the project's Debug menu        
        private static string assemblyAuthor = "de1ta0ne";
        private static bool assemblyUseConsole = false;
        private static bool assemblySingleInstance = true;
        private static bool assemblyPauseAfterExit = true;
        private static bool assemblyLogEnabled = false;        
        private static string assemblyUsage = "[KEYS]\nKeys:\n   -h  help\n   -d  debug mode\n   -c  show console";
        private static int assemblyArgsMin = 0;
        
        //public static MessageManager Messenger = new MessageManager();
        public static PrivateProfile Profile = new PrivateProfile(Path.Combine(GM.assemblyFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".ini"));
    }

    public static class Program
    {
        public static OptionSet assemblyOptionSet = new OptionSet() { };

        public static string Startup(List<string> args) { return (null); }

        public static void DoTask(List<string> args)
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new Mainframe());
            return;
        }
    }
}