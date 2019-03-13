using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
	public class FolderWatcher : IDisposable
	{
		// TODO: introduce delay timer before adding
		static readonly TimeSpan SafeCheckPeriod = TimeSpan.FromSeconds(30.0);

		private FileSystemWatcher _watcher = new FileSystemWatcher();
		private ConcurrentDictionary<string, bool> _watcherPaths = new ConcurrentDictionary<string, bool>();

		public FolderWatcher(
			string folder,
			bool pause = false,
			string filter = "*",
			NotifyFilters notifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName, // | NotifyFilters.DirectoryName,
			bool includeSubdirectories = true)
		{
			if(!Directory.Exists(folder))
				throw new FileNotFoundException(folder);

			_watcher.IncludeSubdirectories = includeSubdirectories;
			_watcher.Path = folder;
			_watcher.NotifyFilter = notifyFilter;
			_watcher.Filter = filter;
			_watcher.Changed += new FileSystemEventHandler(WatcherOnChange);
			_watcher.Created += new FileSystemEventHandler(WatcherOnChange);
			_watcher.Deleted += new FileSystemEventHandler(WatcherOnDelete);
			_watcher.Renamed += new RenamedEventHandler(WatcherOnRename);
			_watcher.EnableRaisingEvents = !pause;
			GM.Log("WATCHER", "запущено слежение: " + _watcher.Path);
		}

		public void Dispose()
		{
			GM.Log("WATCHER", "завершено слежение: " + _watcher.Path);
			_watcher.Dispose();
		}

		private void WatcherOnChange(object sender, FileSystemEventArgs e)
		{   // e.ChangeType
			//Hi, I was using this approach but when I copy a file the event is raised twice: 
			//one time when file is created empty (copy starts) and one more time when copy finishes.						
			if(!File.Exists(e.FullPath)) // File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory) | File.Exists() / Directory.Exists()
				return;
			_watcherPaths[e.FullPath] = true;
			GM.Log("WATCHER", "добавлено/изменено: " + e.FullPath);
		}

		private void WatcherOnRename(object sender, RenamedEventArgs e)
		{
			if(!File.Exists(e.FullPath) || _watcherPaths.ContainsKey(e.FullPath)) // File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory) | File.Exists() / Directory.Exists()
				return;
			_watcherPaths[e.FullPath] = true;
			_watcherPaths[e.OldFullPath] = false;
			GM.Log("WATCHER", "переименовано: " + e.FullPath + " => " + e.OldFullPath);
		}

		private void WatcherOnDelete(object sender, FileSystemEventArgs e)
		{
			if(_watcherPaths.ContainsKey(e.FullPath)) // File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory) | File.Exists() / Directory.Exists()
				return;
			_watcherPaths[e.FullPath] = false;
			GM.Log("WATCHER", "удалено: " + e.FullPath);
		}

		public void Start()
		{
			_watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			_watcher.EnableRaisingEvents = false;
		}

		public Dictionary<string, bool> DeflateDBEx()
		{
			Dictionary<string, bool> result = new Dictionary<string, bool>(_watcherPaths);
			_watcherPaths.Clear();
			return (result);
		}

		public List<string> DeflateDB()
		{    
			List<string> result = new List<string>(_watcherPaths.Keys);
			_watcherPaths.Clear();
			return (result);
		}

		public List<string> DeflateDB(bool status)
		{
			List<string> result = _watcherPaths.Where(x => x.Value == status).Select(x => x.Key).ToList();
			_watcherPaths.Clear();
			return (result);
		}

	}
}
