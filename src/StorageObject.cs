using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SevenZipLib;
using Schematrix;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Core
{
	public class StorageObject
	{
		public enum Type { RAR, Z7Z, ZIP, RAW }

		public class ContentEntry
		{
			public string file;
			public ulong size;
			public DateTime modified;
			public uint crc32;
		}

		public string id;
		public FileInfo file;
		public Type type;
		public List<ContentEntry> content;
		public uint crc32;
		public uint crc32Content;

		public bool readed = false;

		public StorageObject(FileInfo source)
		{
			file = source;
			type = Type.RAW;
			content = new List<ContentEntry>();
			crc32 = crc32Content = 0;
			_UpdateID();
		}

		private void _UpdateID()
		{
			id = Path.GetFileNameWithoutExtension(file.Name).ToLower();
			foreach(var item in new string[] { @"[\[\(]\s*(\d{5,})\s*[\)\]]", @"^\s*(\d{5,})[ _]", @"[ _](\d{5,})\s*$" })
			{
				Match match = Regex.Match(id, item, RegexOptions.IgnoreCase);
				if(match.Success)
				{
					id = match.Groups[1].Value;
					break;
				}
			}
		}

		public void Read(bool validateContent = true)
		{
			type = Type.RAW;
			content = new List<ContentEntry>();
			crc32 = crc32Content = 0;

			if(!file.Exists)
				throw new FileNotFoundException();

			if(file.Length == 0)
				throw new CustomException<ExceptionBadSize>(
					string.Format("Файл - нулевой размер: {0}", file.FullName));

			string name = file.Name.TrimStart('0').PadLeft(1, '0');
			if(name != file.Name)
				throw new CustomException<ExceptionWrongName>(
					string.Format("Файл - неверное имя (лидирующий ноль): {0}", file.FullName), name);

			crc32 = crc32Content = CRC32.FromFile(file);

			string[] archiveExtensions = { ".rar", ".zip", ".7z" }; // (stringArray.All(s => stringToCheck.Contains(s)))
			if(!archiveExtensions.Any(Path.GetExtension(file.FullName).ToLower().Contains)) // (stringArray.Any(s => stringToCheck.Contains(s)))
			{
				readed = true;
				return;
			}

			crc32Content = 0;

			if(file.Length < 4)
				throw new CustomException<ExceptionBadSize>(
					string.Format("Архив - менее 4 байт: {0}", file.FullName));

			byte[] bytes = new byte[4];
			using(var stream = file.Open(FileMode.Open, FileAccess.Read))
				stream.Read(bytes, 0, 4);

			byte[] zipSignature = { 0x50, 0x4B, 0x03, 0x04 };
			byte[] rarSignature = { 0x52, 0x61, 0x72, 0x21 };
			byte[] z7zSignature = { 0x37, 0x7A, 0xBC, 0xAF };

			if(bytes.SequenceEqual(rarSignature))
			{
				type = Type.RAR;
				if(file.Extension.ToLower() != ".rar")
					throw new CustomException<ExceptionWrongExtension>(
						string.Format("Архив - неверное расширение: {0} => (.rar)", file.FullName), ".rar");

				using(Unrar unrar = new Unrar())
				{
					unrar.Open(file.FullName, Unrar.OpenMode.List);
					while(unrar.ReadHeader())
					{
						if(unrar.CurrentFile.IsDirectory)
							continue;
						crc32Content += (uint)unrar.CurrentFile.FileCRC; // string.Format("0x{0:X}", crc32content)) // (crc32content << 1 | crc32content >> 31)
						content.Add(new ContentEntry()
						{
							file = unrar.CurrentFile.FileName,
							size = (ulong)unrar.CurrentFile.UnpackedSize,
							modified = unrar.CurrentFile.FileTime,
							crc32 = (uint)unrar.CurrentFile.FileCRC
						});
						if(validateContent)
							unrar.Test();
					}
				}
				readed = true;
				return;
			}
			else if(bytes.SequenceEqual(zipSignature))
			{
				type = Type.ZIP;
				if(file.Extension.ToLower() != ".zip")
					throw new CustomException<ExceptionWrongExtension>(
						string.Format("Архив - неверное расширение: {0} => (.zip)", file.FullName), ".zip");
			}
			else if(bytes.SequenceEqual(z7zSignature))
			{
				type = Type.Z7Z;
				if(file.Extension.ToLower() != ".7z")
					throw new CustomException<ExceptionWrongExtension>(
						string.Format("Архив - неверное расширение: {0} => (.7z)", file.FullName), ".7z");
			}
			else
				throw new CustomException<ExceptionBadSignature>(string.Format("Архив - неверная сигнатура: {0}", file.FullName));

			using(SevenZipArchive archive = new SevenZipArchive(file.FullName, ArchiveFormat.Unkown))
			{
				foreach(ArchiveEntry item in archive)
				{
					if(item.IsDirectory)
						continue;
					crc32Content += item.Crc;
					content.Add(new ContentEntry()
					{
						file = item.FileName,
						size = item.Size,
						modified = (DateTime)item.LastWriteTime,
						crc32 = item.Crc
					});
				}
				if(validateContent && !archive.CheckAll())
					throw new Exception("архив поврежден");
			}
			readed = true;
		}

		public void Test()
		{
			if(!readed)
				throw new Exception(string.Format("объект не считан: {0}", file.FullName));

			if(type == Type.RAW)
				return;

			if(type == Type.RAR)
			{
				using(Unrar unrar = new Unrar())
				{
					unrar.Open(file.FullName, Unrar.OpenMode.List);
					while(unrar.ReadHeader())
					{
						if(unrar.CurrentFile.IsDirectory)
							continue;
						unrar.Test();
					}
				}
				return;
			}

			using(SevenZipArchive archive = new SevenZipArchive(file.FullName, ArchiveFormat.Unkown))
				if(!archive.CheckAll())
					throw new Exception(string.Format("архив поврежден: {0}", file.FullName));
		}

		public void Extract(DirectoryInfo unpackFolder)
		{
			if(!readed)
				throw new Exception(string.Format("объект не считан: {0}", file.FullName));

			if(type == Type.RAW)
				return;

			if(type == Type.RAR)
			{
				using(Unrar unrar = new Unrar())
				{
					unrar.DestinationPath = unpackFolder.FullName;
					unrar.Open(file.FullName, Unrar.OpenMode.Extract);
					while(unrar.ReadHeader())
						unrar.Extract();
				}
				return;
			}

			using(SevenZipArchive archive = new SevenZipArchive(file.FullName, ArchiveFormat.Unkown))
				archive.ExtractAll(unpackFolder.FullName);
		}

		public StorageObject RepackToRAR(DirectoryInfo repackFolder_, DirectoryInfo backupFolder)
		{
			if(!readed)
				throw new Exception(string.Format("объект не считан: {0}", file.FullName));

			if(type == Type.RAR || type == Type.RAW)
				throw new Exception("Неверный тип объекта для перепаковки (RAR/RAW)");

			DirectoryInfo repackFolder = new DirectoryInfo(Path.Combine(GM.assemblyFolder, "temp", Guid.NewGuid().ToString()));
			FileInfo rarFile = new FileInfo(Path.Combine(GM.assemblyFolder, "temp", Path.ChangeExtension(file.Name, ".rar")));

			StorageObject newStorageObject = new StorageObject(rarFile);

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
					if(line.Length > 0 && linesToSkip-- <= 0)
						GM.Log("REPACK", line);
				}
				p.WaitForExit();
				p.Close();

				if(!rarFile.Exists)
					throw new Exception("Перепаковка - архив не создан");

				newStorageObject.Read();
				if(crc32Content != newStorageObject.crc32Content)
				{
					rarFile.Delete();
					throw new Exception("Перепаковка - не совпадают контрольные суммы содержимого");
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
			return (newStorageObject);
		}

		public void SafeMove(DirectoryInfo toFolder, string prefix = null, string postfix = null, string toName = null)
		{
			if(toName == null)
				toName = file.Name;

			if(prefix != null)
				toName = prefix + toName;

			if(postfix != null)
				toName = Path.GetFileNameWithoutExtension(toName) + postfix + Path.GetExtension(toName);

			SafeMove(toFolder, toName);
		}

		public void SafeMove(DirectoryInfo toFolder, string toName = null)
		{
			if(toName == null)
				toName = file.Name;

			string to = Path.Combine(toFolder.FullName, toName);
			if(File.Exists(to))
			{
				int index = 0;
				string extension = file.Extension;
				string name = Path.GetFileNameWithoutExtension(toName);
				while(true)
				{
					to = Path.Combine(toFolder.FullName, name + " (" + index + ")" + extension);
					if(!File.Exists(to))
						break;
					index++;
				}
			}

			File.Move(file.FullName, to);
			file = new FileInfo(to);
			_UpdateID();
		}

		public string GetRelativePath(DirectoryInfo baseFolder)
		{
			string path = Tools.GetRelativePath(file.FullName, baseFolder.FullName).ToLower();

			if(path.Length == file.FullName.Length)
				throw new Exception(string.Format("Файл '{0}' не пренадлежит каталогу '{1}'", file.FullName, baseFolder.FullName));
			return (path);
		}
	}
}