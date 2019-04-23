using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading;
using System.Threading.Tasks;

using System.Data;
using System.Data.Common;
using System.Data.SQLite;

using SevenZipLib;
using Schematrix;
using System.Diagnostics;

namespace Core
{
	public static class KeeperCore
	{
		#region CORE
		public static readonly SQLite sql = new SQLite();
		
		public static readonly string pathStorage = GM.Profile.Get("paths", "storage", Path.Combine(GM.assemblyFolder, "storage"), true);
		public static readonly string pathIncome = GM.Profile.Get("paths", "income", Path.Combine(GM.assemblyFolder, "income"), true);

		public static readonly string pathCorrupted = Path.Combine(GM.assemblyFolder, "corrupted");
		public static readonly string pathUpdates = Path.Combine(GM.assemblyFolder, "updates");
		public static readonly string pathNew = Path.Combine(GM.assemblyFolder, "new");
		public static readonly string pathDoubles = Path.Combine(GM.assemblyFolder, "doubles");
		public static readonly string pathDoublesSorted = Path.Combine(pathDoubles, ".sorted");
		public static readonly string pathRepacked = Path.Combine(GM.assemblyFolder, "repacked");

		public static readonly FolderWatcher watcherStorage;
		public static readonly FolderWatcher watcherIncome;

		static KeeperCore()
		{
			foreach(var item in new string[] { pathStorage, pathIncome, pathCorrupted, pathUpdates, pathNew, pathDoubles, pathDoublesSorted, pathRepacked })
				if(!Directory.Exists(item))
					Directory.CreateDirectory(item);

			pathStorage = Path.GetFullPath(pathStorage).ToLower();
			pathIncome = Path.GetFullPath(pathIncome).ToLower();
			pathCorrupted = Path.GetFullPath(pathCorrupted).ToLower();
			pathUpdates = Path.GetFullPath(pathUpdates).ToLower();
			pathNew = Path.GetFullPath(pathNew).ToLower();
			pathDoubles = Path.GetFullPath(pathDoubles).ToLower();
			pathDoublesSorted = Path.GetFullPath(pathDoublesSorted).ToLower();
			pathRepacked = Path.GetFullPath(pathRepacked).ToLower();

			watcherStorage = new FolderWatcher(pathStorage);
			watcherIncome = new FolderWatcher(pathIncome);

			sql.Execute(@"
					CREATE TABLE IF NOT EXISTS [storage] (
					[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
					[path] text NOT NULL,
					[name] text NOT NULL,
					[size] integer NOT NULL,
					[created] datetime NOT NULL,
					[modified] datetime NOT NULL,					
					[crc32] integer NOT NULL,
					[crc32content] integer NOT NULL
				);");
			sql.Execute(@"
					CREATE TABLE IF NOT EXISTS [content] (
					[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
					[container] text NOT NULL,
					[file] text NOT NULL,
					[size] integer NOT NULL,
					[modified] datetime NOT NULL,
					[crc32] integer NOT NULL
				);");
			sql.Execute(@"
					CREATE TABLE IF NOT EXISTS [sorted] (
					[id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
					[name] text NOT NULL,
					[size] integer NOT NULL,
					[crc32] integer NOT NULL
				);");

			sql.Execute(@"CREATE INDEX IF NOT EXISTS [storage_idx] ON [storage] ([crc32], [crc32content], [name]);");
			sql.Execute(@"CREATE INDEX IF NOT EXISTS [content_idx] ON [content] ([crc32]);");
			sql.Execute(@"CREATE INDEX IF NOT EXISTS [sorted_idx] ON [sorted] ([crc32]);");
		}

		public static void Shutdown()
		{
			sql.Dispose();
			watcherStorage.Dispose();
			watcherIncome.Dispose();
		}

		public static void ClearIncomeHistory()
		{
			sql.Delete("sorted");
		}

		public static void ClearDB()
		{
			sql.Delete("storage");
			sql.Delete("content");
			sql.Delete("sorted");
		}

		private static void _SafeMoveTo(FileInfo file, string toFolder, string toName = null)
		{
			if(toName == null)
				toName = file.Name;

			string to = Path.Combine(toFolder, toName);
			if(File.Exists(to))
			{
				int index = 0;
				string extension = file.Extension;
				string name = Path.GetFileNameWithoutExtension(toName);
				while(true)
				{
					to = Path.Combine(toFolder, name + " (" + index + ")" + extension);
					if(!File.Exists(to))
						break;
					index++;
				}
			}
			try
			{
				File.Move(file.FullName, to);
				GM.Log(string.Format("Файл перемещен : {0} => {1}", file.FullName, to));
			}
			catch(IOException ex)
			{
				GM.Error("Ошибка перемешения файла:" + ex.Message);
			}
		}

		private static void _GetFileContent(FileInfo file, out uint crc32, out uint crc32content, out List<object[]> content)
		{
			if(!File.Exists(file.FullName))
				throw new Exception("[NotFound]");

			if(file.Length == 0)
				throw new Exception("[Size0Bytes]");

			crc32 = crc32content = CRC32.FromFile(file);
			content = new List<object[]>();

			string[] archiveExtensions = { ".rar", ".zip", ".7z" }; // (stringArray.All(s => stringToCheck.Contains(s)))
			if(!archiveExtensions.Any(Path.GetExtension(file.FullName).ToLower().Contains)) // (stringArray.Any(s => stringToCheck.Contains(s)))
				return;

			crc32content = 0;

			if(file.Length < 4)
				throw new Exception("[Size4Bytes]");

			byte[] bytes = new byte[4];
			using(var stream = file.Open(FileMode.Open, FileAccess.Read))
				stream.Read(bytes, 0, 4);

			byte[] zip = { 0x50, 0x4B, 0x03, 0x04 };
			byte[] rar = { 0x52, 0x61, 0x72, 0x21 };
			byte[] z7z = { 0x37, 0x7A, 0xBC, 0xAF };

			if(bytes.SequenceEqual(rar))
			{
				if(file.Extension.ToLower() != ".rar")
				{
					Exception exception = new Exception("[ArchiveWrongExtension]");
					exception.Data.Add("newExtension", ".rar");
					throw exception;
				}
				using(Unrar unrar = new Unrar())
				{
					unrar.Open(file.FullName, Unrar.OpenMode.List);
					while(unrar.ReadHeader())
					{
						if(unrar.CurrentFile.IsDirectory)
							continue;						
						crc32content += (uint)unrar.CurrentFile.FileCRC; // string.Format("0x{0:X}", crc32content)) // (crc32content << 1 | crc32content >> 31)
						content.Add(new object[] { unrar.CurrentFile.FileName, unrar.CurrentFile.UnpackedSize,
												unrar.CurrentFile.FileTime, unrar.CurrentFile.FileCRC});
						unrar.Test();
					}
				}
				return;
			}
			else if(bytes.SequenceEqual(zip))
			{
				if(file.Extension.ToLower() != ".zip")
				{
					Exception exception = new Exception("[ArchiveWrongExtension]");
					exception.Data.Add("newExtension", ".zip");
					throw exception;
				}
			}
			else if(bytes.SequenceEqual(z7z))
			{
				if(file.Extension.ToLower() != ".7z")
				{
					Exception exception = new Exception("[ArchiveWrongExtension]");
					exception.Data.Add("newExtension", ".7z");
					throw exception;
				}
			}
			else
				throw new Exception("[ArchiveBadSignature]");

			using(SevenZipArchive archive = new SevenZipArchive(file.FullName, ArchiveFormat.Unkown))
			{
				foreach(ArchiveEntry item in archive)
				{
					if(item.IsDirectory)
						continue;
					crc32content += (uint)item.Crc;
					content.Add(new object[] { item.FileName, item.Size, item.LastWriteTime, item.Crc });
				}
				if(!archive.CheckAll())
					throw (new Exception("Архив поврежден!"));
			}
		}

		private static FileInfo _RepackToRAR(FileInfo file)
		{
			if(!file.Exists)
				throw new FileNotFoundException();

			if(Path.GetExtension(file.Name).ToLower() == ".rar")
				throw new Exception("Неверное расширение архива - '.rar'");

			string guid = Guid.NewGuid().ToString();
			DirectoryInfo repackFolder = new DirectoryInfo(Path.Combine(GM.assemblyFolder, "temp", guid));
			FileInfo rarFile = new FileInfo(Path.Combine(GM.assemblyFolder, "temp", Path.ChangeExtension(file.Name, ".rar")));
			
			GM.Log("REPACK", "Перепаковка: " + file.FullName + " => " + rarFile.FullName);

			try
			{
				repackFolder.Create();

				using(SevenZipArchive archive = new SevenZipArchive(file.FullName, ArchiveFormat.Unkown))
					archive.ExtractAll(repackFolder.FullName);

				Process p = new Process();
				p.StartInfo.FileName = Path.Combine(GM.assemblyFolder, "rar.exe");
				p.StartInfo.Arguments = string.Format("a -ma4 -y -ep1 -r \"{0}\" *", rarFile.FullName); // "a -ma4 -y -t -ep1 -r \"{0}\" \"{1}\\*\"", rarFile, repackFolder
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.WorkingDirectory = repackFolder.FullName;
				p.Start();
				int linesToSkip = 3;
				while(!p.StandardOutput.EndOfStream)
				{
					string line = p.StandardOutput.ReadLine();
					if(line.Length > 0  && linesToSkip-- <= 0)
						GM.Log("REPACK", line);
				}
				p.WaitForExit();
				p.Close();

				if(!rarFile.Exists)
					throw new Exception("Перепаковка - архив не создан!");

				_GetFileContent(file, out uint srcCRC32, out uint srcCRC32Content, out List<object[]> srcContent);
				_GetFileContent(rarFile, out uint dstCRC32, out uint dstCRC32Content, out List<object[]> dstContent);

				if(srcCRC32Content != dstCRC32Content)
				{
					rarFile.Delete();
					throw new Exception("Перепаковка - не совпадают контрольные суммы содержимого!");
				}
			}
			catch
			{				
				throw;
			}
			finally
			{
				if(repackFolder.Exists) repackFolder.Delete(true);
			}

			return (rarFile);
		}

		#endregion

		#region Storage indexing

		private static void _IndexerProcessFile(FileInfo file, TaskContext context)
		{
			string path = Tools.GetRelativePath(file.FullName, pathStorage).ToLower();

			sql.Delete("storage", "path = '" + path + "'");
			sql.Delete("content", "container = '" + path + "'");

			if(path.Length == file.FullName.Length)
			{
				GM.Log(string.Format("Файл '{0}' не пренадлежит каталогу '{1}'", file.FullName, pathStorage));
				return;
			}

			if(!File.Exists(file.FullName))
				return;

			string name = file.Name.TrimStart('0').PadLeft(1, '0');
			if(name != file.Name)
			{
				GM.Log(string.Format("Переименование файла (лидирующий ноль): {0}", file.FullName));
				_SafeMoveTo(file, pathStorage, name);
				return;
			}

			uint crc32, crc32content;
			List<object[]> content;
			try
			{
				_GetFileContent(file, out crc32, out crc32content, out content);
			}
			catch(Exception ex)
			{
				switch(ex.Message)
				{
					case ("[NotFound]"):
						GM.Log("INDEX", string.Format("Файл не найден: {0}", file.FullName));
						break;
					case ("[Size0Bytes]"):
						GM.Log("INDEX", string.Format("Файл нулевого размера: {0}", file.FullName));
						_SafeMoveTo(file, pathCorrupted, "[Size0Bytes] " + file.Name);
						break;
					case ("[Size4Bytes]"):
						GM.Log("INDEX", string.Format("Архив - менее 4 байт: {0}", file.FullName));
						_SafeMoveTo(file, pathCorrupted, "[Size4Bytes] " + file.Name);
						break;
					case ("[ArchiveWrongExtension]"):
						GM.Log("INDEX", string.Format("Архив - неверное расширение: {0}", file.FullName));
						_SafeMoveTo(file, pathStorage, Path.ChangeExtension(file.Name, (string)ex.Data["newExtension"]));
						break;
					case ("[ArchiveBadSignature]"):
						GM.Log("INDEX", string.Format("Архив - неверная сигнатура: {0}", file.FullName));
						_SafeMoveTo(file, pathCorrupted, "[ArchiveBadSignature] " + file.Name);
						break;
					default:
						GM.Error(string.Format("[INDEX] Архив - ошибка обработки: {0} => {1} ", file.FullName, ex.Message));
						_SafeMoveTo(file, pathCorrupted);
						break;
				}
				return;
			}

			string[] archiveExtensions = { ".zip", ".7z" }; // (stringArray.All(s => stringToCheck.Contains(s)))
			if(archiveExtensions.Any(Path.GetExtension(file.Name).ToLower().Contains)) // (stringArray.Any(s => stringToCheck.Contains(s)))
			{
				try
				{
					FileInfo rarFile = _RepackToRAR(file);
					_SafeMoveTo(file, pathRepacked);
					_SafeMoveTo(rarFile, pathStorage);
				}
				catch(Exception ex)
				{
					GM.Error(string.Format("[INDEX] Архив - ошибка перепаковки: {0} => {1} ", file.FullName, ex.Message));
				}
				return;
			}

			name = Path.GetFileNameWithoutExtension(file.Name).ToLower();
			foreach(var item in new string[] { @"[\[\(]\s*(\d{5,})\s*[\)\]]", @"^\s*(\d{5,})[ _]", @"[ _](\d{5,})\s*$" })
			{
				Match match = Regex.Match(name, item, RegexOptions.IgnoreCase);
				if(match.Success)
				{
					name = match.Groups[1].Value;
					break;
				}
			}

			sql.Insert("storage", new Dictionary<string, object> {
				{"path", path},
				{"name", name},
				{"size", file.Length},
				{"created", file.CreationTime},
				{"modified", file.LastWriteTime},
				{"crc32", crc32},
				{"crc32content", crc32content},
			});
			content.ForEach(_ => sql.Insert("content", new Dictionary<string, object> {
				{"container", path},
				{"file", _[0]},
				{"size", _[1]},
				{"modified", _[2]},
				{"crc32", _[3]},
			}));
		}

		private static List<FileInfo> _IndexerFilesToIndex()
		{
			List<FileInfo> _toIndex = new List<FileInfo>();
			List<string> _toIndexPaths = new List<string>();
			List<string> _toClear = new List<string>();

			try
			{
				DirectoryInfo _di = new DirectoryInfo(pathStorage);
				_toIndex = _di.GetFiles("*", SearchOption.AllDirectories).ToList();
			}
			catch(Exception ex)
			{
				GM.Error("Невозможно провести анализ папки-хранилища: " + ex.Message);
				return (_toIndex);
			}

			_toIndex.ForEach(item =>
				_toIndexPaths.Add(item.FullName.Replace(pathStorage, "").Trim(Path.DirectorySeparatorChar).ToLower()));

			DataTable _table = sql.GetDataTable("SELECT * FROM storage;");

			foreach(DataRow row in _table.Rows)
			{   // int index = myList.FindIndex(a => a.Prop == oProp);
				int _index = _toIndexPaths.IndexOf((string)row["path"]); // _table.Rows[0][3] / _table.Rows[0]["column name"]
				if(_index != -1
					&& _toIndex[_index].Length == (long)row["size"]
					&& _toIndex[_index].CreationTime == (DateTime)row["created"]
					&& _toIndex[_index].LastWriteTime == (DateTime)row["modified"])
				{
					_toIndex.RemoveAt(_index);
					_toIndexPaths.RemoveAt(_index);
				}
				else
				{
					GM.Log("INDEX", "изменено/удалено: " + (string)row["path"]);
					_toClear.Add((string)row["path"]);
				}
			}

			_toClear.ForEach(_ => _toIndex.Add(new FileInfo(Path.Combine(pathStorage, _))));

			_table.Clear();
			_table.Dispose();

			return(_toIndex);
		}

		#endregion

		#region Sorting

		private static void _SorterProcessFile(FileInfo file, Dictionary<int, string> sorterCache, TaskContext context)
		{
			uint crc32, crc32content;
			List<object[]> content;
			try
			{
				_GetFileContent(file, out crc32, out crc32content, out content);
			}
			catch(Exception ex)
			{
				switch(ex.Message)
				{
					case ("[NotFound]"):
						GM.Log("SORT", string.Format("Файл не найден: {0}", file.FullName));
						break;
					case ("[Size0Bytes]"):
						GM.Log("SORT", string.Format("Файл нулевого размера: {0}", file.FullName));
						_SafeMoveTo(file, pathCorrupted, "[Size0Bytes] " + file.Name);
						break;
					case ("[Size4Bytes]"):
						GM.Log("SORT", string.Format("Архив - менее 4 байт: {0}", file.FullName));
						_SafeMoveTo(file, pathCorrupted, "[Size4Bytes] " + file.Name);
						break;
					case ("[ArchiveWrongExtension]"):
						GM.Log("SORT", string.Format("Архив - неверное расширение: {0}", file.FullName));
						_SafeMoveTo(file, pathIncome, Path.ChangeExtension(file.Name, (string)ex.Data["newExtension"]));
						break;
					case ("[ArchiveBadSignature]"):
						GM.Log("SORT", string.Format("Архив - неверная сигнатура: {0}", file.FullName));
						_SafeMoveTo(file, pathCorrupted, "[ArchiveBadSignature] " + file.Name);
						break;
					default:
						GM.Error(string.Format("[SORT] Архив - ошибка обработки: {0} => {1} ", file.FullName, ex.Message));
						_SafeMoveTo(file, pathCorrupted);
						break;
				}
				return;
			}

			if(sorterCache.ContainsKey((int)crc32))
			{
				GM.Log("SORT", "Повторная сортировка (crc32): " + file.FullName + " => " + sorterCache[(int)crc32]);
				_SafeMoveTo(file, pathDoublesSorted);
				return;
			}
			else
				sorterCache.Add((int)crc32, file.Name);

			using(SQLiteDataReader reader = sql.ExecuteReader(string.Format("SELECT name FROM sorted WHERE crc32 = '{0}';", (int)crc32)))
				if(reader.HasRows)
				{
					while(reader.Read())
						GM.Log("SORT", "Повторная сортировка (crc32): " + file.FullName + " => " + reader[0]);
					_SafeMoveTo(file, pathDoublesSorted);
					return;
				}
				else
				{
					sql.Insert("sorted", new Dictionary<string, object> {
						{"name", file.Name},
						{"size", file.Length},
						{"crc32", crc32},
					});
				}

			string[] archiveExtensions = { ".zip", ".7z" }; // (stringArray.All(s => stringToCheck.Contains(s)))
			if(archiveExtensions.Any(Path.GetExtension(file.Name).ToLower().Contains)) // (stringArray.Any(s => stringToCheck.Contains(s)))
			{
				try
				{
					FileInfo rarFile = _RepackToRAR(file);
					_SafeMoveTo(file, pathRepacked);
					_SafeMoveTo(rarFile, Path.GetDirectoryName(file.FullName));
				}
				catch(Exception ex)
				{
					GM.Error(string.Format("[SORT] Архив - ошибка перепаковки: {0} => {1} ", file.FullName, ex.Message));
				}
				return;
			}

			long result = (long)sql.ExecuteScalar(
				string.Format("SELECT COUNT(*) FROM storage WHERE crc32 = '{0}';", (int)crc32));
			if(result > 0)
			{
				GM.Log("SORT", "Дубль (crc32): " + file.FullName);
				_SafeMoveTo(file, pathDoubles);
				if(result > 1)
					GM.Error("База содержит более одной записи c одинаковой контрольной суммой (crc32): " + (int)crc32);
				return;
			}

			result = (long)sql.ExecuteScalar(
				string.Format("SELECT COUNT(*) FROM storage WHERE crc32content = '{0}';", (int)crc32content));
			if(result > 0)
			{
				GM.Log("SORT", "Дубль (crc32content): " + file.FullName);
				_SafeMoveTo(file, pathDoubles);
				if(result > 1)
					GM.Error("База содержит более одной записи c одинаковой контрольной суммой (crc32content): " + (int)crc32content);
				return;
			}

			result = (long)sql.ExecuteScalar(
							string.Format("SELECT COUNT(*) FROM content WHERE crc32 = '{0}';", (int)crc32));
			if(result > 0)
			{
				GM.Log("SORT", "Дубль (находится в архиве): " + file.FullName);
				_SafeMoveTo(file, pathDoubles, "[archived] " + file.Name);
				if(result > 1)
					GM.Error("База содержит более одной записи c одинаковой контрольной суммой (content-crc32): " + (int)crc32);
				return;
			}

			string name = Path.GetFileNameWithoutExtension(file.Name).ToLower();
			foreach(var item in new string[] { @"[\[\(]\s*(\d{5,})\s*[\)\]]", @"^\s*(\d{5,})[ _]", @"[ _](\d{5,})\s*$" })
			{
				Match match = Regex.Match(name, item, RegexOptions.IgnoreCase);
				if(match.Success)
				{
					name = match.Groups[1].Value;
					break;
				}
			}

			using(SQLiteDataReader reader = sql.ExecuteReader(string.Format("SELECT DISTINCT path FROM storage WHERE name = '{0}';", name)))
			{
				if(!reader.HasRows)
				{
					GM.Log("SORT", "Новый: " + file.FullName);
					_SafeMoveTo(file, pathNew);
					return;
				}
				while(reader.Read())
				{
					DataTable _table = sql.GetDataTable(string.Format("SELECT * FROM content WHERE container = '{0}';", reader[0]));
					bool partialDouble = true;
					foreach(var item in content)
						if(_table.Select("crc32=" + item[3]).Length < 1)
						{
							partialDouble = false;
							break;
						}
					_table.Dispose();
					if(partialDouble)
					{
						GM.Log("SORT", "Дубль (частичный): " + file.FullName + " содержится в: " + reader[0]);
						_SafeMoveTo(file, pathDoubles, "[partial] " + file.Name);
						return;
					}
				}
			}

			GM.Log("SORT", "Обновление: " + file.FullName);
			_SafeMoveTo(file, pathUpdates);
		}

		private static List<FileInfo> _SorterFilesToSort()
		{
			try
			{
				//return (Tools.GetFiles(pathIncome, "*"));
				DirectoryInfo _di = new DirectoryInfo(pathIncome);
				return _di.GetFiles("*", SearchOption.AllDirectories).ToList();
			}
			catch(Exception ex)
			{
				GM.Error("Невозможно получить файлы папки поступлений: " + ex.Message);
				return (new List<FileInfo>());
			}
		}

		#endregion

		public static void TaskMain(object taskContext)
		{			
			TaskContext context = (TaskContext)taskContext;
			context.mainframe.SetTaskProgress((int)Task.CurrentId, 0, "Ожидание", "Индексация"); // Thread.CurrentThread.ManagedThreadId
			context.mainframe.SetTaskProgress((int)Task.CurrentId+1, 0, "Ожидание", "Сортировка"); // Thread.CurrentThread.ManagedThreadId

			GM.Print("Started!");

			// TODO: clear doubles in content
			// https://stackoverflow.com/questions/1786533/find-rows-that-have-the-same-value-on-a-column-in-mysql
			//select * from storage where crc32content in (
			//	select crc32content from storage group by crc32content having count(*) > 1
			//) order by crc32content

			List<FileInfo> _toIndex = _IndexerFilesToIndex();
			List<FileInfo> _toSort = _SorterFilesToSort();

			Dictionary<int, string> _sorterCache = new Dictionary<int, string>();

			do
			{
				watcherStorage.DeflateDB().ForEach(item => _toIndex.Add(new FileInfo(item)));
				_toIndex = Tools.FilterFiles(_toIndex, pathStorage);

				if(_toIndex.Count > 0)
				{
					sql.Execute("begin");
					GM.Print("{Maroon}Файлов к индексации: " + _toIndex.Count);
					var time = Tools.MeasureAction(() =>
					{
						int _total = _toIndex.Count;
						int _oldPercent = 100;
						for(int i = _toIndex.Count - 1; i >= 0; i--)
						{
							//GM.Log("INDEX", _toIndex[i].FullName);

							int percent = ((_total - i) * 100 / _total);
							if(_oldPercent != percent)
							{
								context.mainframe.SetTaskProgress((int)Task.CurrentId, percent - 1, "Индексация [" + (_total - i) + "/" + _total + "]: " + _toIndex[i]);
								_oldPercent = percent;
							}
							try
							{
								if(File.Exists(_toIndex[i].FullName) && !Tools.IsFileReady(_toIndex[i].FullName, false))
									GM.Log("INDEX", "Файл заблокирован сторонней программой: " + _toIndex[i].FullName);
								else
								{
									_IndexerProcessFile(_toIndex[i], context);
									_toIndex.RemoveAt(i);
								}
							}
							catch(Exception ex)
							{
								GM.Error("Ошибка индексирования: " + ex.Message);
							}
							while(context.pause && !context.token.WaitHandle.WaitOne(1000)) { }
							if(context.token.IsCancellationRequested) break;							
						}
					});
					sql.Execute("end");
					GM.Print("Индексация заняла: " + time.Milliseconds().Format());
					context.mainframe.SetTaskProgress((int)Task.CurrentId, 0, "Ожидание");
				}

				if(_toIndex.Count == 0 && !context.token.IsCancellationRequested)
				{					
					watcherIncome.DeflateDB(true).ForEach(item => _toSort.Add(new FileInfo(item)));
					_toSort = Tools.FilterFiles(_toSort, pathIncome);

					if(_toSort.Count > 0)
					{
						sql.Execute("begin");
						_sorterCache.Clear();

						GM.Print("{Maroon}Файлов к сортировке: " + _toSort.Count);
						var time = Tools.MeasureAction(() =>
						{
							int _total = _toSort.Count;
							int _oldPercent = 100;
							for(int i = _toSort.Count - 1; i >= 0; i--)
							{
								//GM.Log("SORT", _toSort[i].FullName);
								//if(i % 100 == 0)
								//{
								//	sql.Execute("end");
								//	sql.Execute("begin");
								//}

								int percent = ((_total - i) * 100 / _total);
								if(_oldPercent != percent)
								{
									context.mainframe.SetTaskProgress((int)Task.CurrentId + 1, percent - 1, "Сортировка [" + (_total - i) + "/" + _total + "]: " + _toSort[i]);
									_oldPercent = percent;
								}
								try
								{
									if(File.Exists(_toSort[i].FullName) && !Tools.IsFileReady(_toSort[i].FullName, false))
										GM.Log("SORT", "Файл заблокирован сторонней программой: " + _toSort[i].FullName);
									else
									{
										_SorterProcessFile(_toSort[i], _sorterCache, context);
										_toSort.RemoveAt(i);
									}
								}
								catch(Exception ex)
								{
									GM.Error("Ошибка сортировки: " + ex.Message);
								}
								while(context.pause && !context.token.WaitHandle.WaitOne(1000)) { }
								if(context.token.IsCancellationRequested) break;								
							}
						});
						sql.Execute("end");
						GM.Print("Сортировка заняла: " + time.Milliseconds().Format());
						context.mainframe.SetTaskProgress((int)Task.CurrentId + 1, 0, "Ожидание");
					}
				}
			} while(!context.token.WaitHandle.WaitOne(5000));
			GM.Print("Stopped!");
		}		
	}
}
