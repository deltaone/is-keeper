using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;

namespace Core
{
    public static partial class Tools
    {
        public static Process GetProcess(string name)
        {
            return (Process.GetProcessesByName(name).FirstOrDefault());
        }

        public static bool IsRunning(string name)
        {
            return (Process.GetProcessesByName(name).Length > 0);
        }

        public static void DelayedCall(int delay, Action method)
        {
            System.Threading.Timer timer = null;
            var cb = new System.Threading.TimerCallback((state) => { method(); timer.Dispose(); });
            timer = new System.Threading.Timer(cb, null, delay, System.Threading.Timeout.Infinite);
        }

        public static void ThreadUI(this Control @this, Action code)
        {
            if (@this.InvokeRequired)
            {
                @this.BeginInvoke(code);
            }
            else
            {
                code.Invoke();
            }
        }

        public static Stream ResourceGetStream(string path)
        {
            // вариант 1 
            // добавь в проект файл как ресурс (Properties -> Resources -> AddFiles), и дальше в проекте 
            // обращайся Properties.Resources.<имя_ресурса>

            // вариант 2
            // добавь текстовый файл в проект
            // измени его свойство BuildAction на EmbededResource (правый клик на добавленном файле - properties)
            // идентификатор для загрузки ресурса - "text2table.page_begin.txt"

            return (Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + "." + path));
        }

        public static byte[] ResourceGetFile(string path)
        {
            Stream stream = ResourceGetStream(path);

            if (stream == null) 
                return (new byte[] { });
            byte[] buffer = new Byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);

            return (buffer);
        }

        public static string ResourceGetTextFile(string path)
        {
            Stream stream = ResourceGetStream(path);
            
            if (stream == null) 
                return ("");
            StreamReader reader = new StreamReader(stream);

            return (reader.ReadToEnd());
        }

		public static double MeasureAction(Action action)
		{	// Class Profiler
			var st = new Stopwatch();

			st.Start();
			action();
			st.Stop();

			return st.Elapsed.TotalMilliseconds;
		}

		public static bool IsFileReady(string filename, bool checkSize = true)
		{   // var isReady = false; while (!isReady) { isReady = IsFileReady(fileName); }
			// https://stackoverflow.com/questions/1406808/wait-for-file-to-be-freed-by-process/1406853#1406853

			// http://thedailywtf.com/Comments/The-Right-Way-to-Find-a-File.aspx#402913
			// http://stackoverflow.com/a/876513/160173
			try
			{   // If the file can be opened for exclusive access it means that the file is no longer locked by another process.
				using(FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				//using(Stream stream = new FileStream(filename, FileMode.Open)) // !!! fail on RO files
					if(checkSize)
						return (stream.Length > 0); // File/Stream manipulating code here
					else
						return (true);					
			}
			catch
			{
				return (false); //check here why it failed and ask user to retry if the file is in use.
			}
		}

		public static string GetRelativePath(string file, string folder)
		{   // stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
			// stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path/
			// stackoverflow.com/questions/26053830/exclude-base-directory-and-also-file-name-from-full-path
			// stackoverflow.com/questions/17782707/how-to-cut-out-a-part-of-a-path
			// stackoverflow.com/questions/4389775/what-is-a-good-way-to-remove-last-few-directory/4389847
			//string path = file.FullName.ToLower().Replace(pathStorage, "").Trim(Path.DirectorySeparatorChar);

			if(!Path.IsPathRooted(folder)) folder = Path.GetFullPath(folder);

			Uri pathUri = new Uri(file);
			if(!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) // Folders must end in a slash
				folder += Path.DirectorySeparatorChar;
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}

		//---------------------------------------------------------------------
	}
}
