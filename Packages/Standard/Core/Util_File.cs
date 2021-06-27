using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;



namespace Moenen.Standard {
	public static partial class Util {


		public static string FileToText (string path) {
			StreamReader sr = File.OpenText(path);
			string data = sr.ReadToEnd();
			sr.Close();
			return data;
		}


		public static void TextToFile (string data, string path) {
			FileStream fs = new FileStream(path, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
			sw.Write(data);
			sw.Close();
			fs.Close();
		}


		public static void CreateFolder (string path) {
			if (!string.IsNullOrEmpty(path) && !DirectoryExists(path)) {
				string pPath = GetParentPath(path);
				if (!DirectoryExists(pPath)) {
					CreateFolder(pPath);
				}
				Directory.CreateDirectory(path);
			}
		}


		public static byte[] FileToByte (string path) {
			byte[] bytes = null;
			if (FileExists(path)) {
				bytes = File.ReadAllBytes(path);
			}
			return bytes;
		}


		public static void ByteToFile (byte[] bytes, string path) {
			string parentPath = GetParentPath(path);
			CreateFolder(parentPath);
			FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
			fs.Write(bytes, 0, bytes.Length);
			fs.Close();
			fs.Dispose();
		}


		public static bool HasFileIn (string path, params string[] searchPattern) {
			if (PathIsDirectory(path)) {
				for (int i = 0; i < searchPattern.Length; i++) {
					if (new DirectoryInfo(path).GetFiles(searchPattern[i], SearchOption.AllDirectories).Length > 0) {
						return true;
					}
				}
			}
			return false;
		}


		public static FileInfo[] GetFilesIn (string path, bool topOnly, params string[] searchPattern) {
			var allFiles = new List<FileInfo>();
			if (PathIsDirectory(path)) {
				var option = topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
				if (searchPattern.Length == 0) {
					allFiles.AddRange(new DirectoryInfo(path).GetFiles("*", option));
				} else {
					for (int i = 0; i < searchPattern.Length; i++) {
						allFiles.AddRange(new DirectoryInfo(path).GetFiles(searchPattern[i], option));
					}
				}
			}
			return allFiles.ToArray();
		}


		public static DirectoryInfo[] GetDirectsIn (string path, bool topOnly) {
			var allDirs = new List<DirectoryInfo>();
			if (PathIsDirectory(path)) {
				allDirs.AddRange(new DirectoryInfo(path).GetDirectories("*", topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories));
			}
			return allDirs.ToArray();
		}


		public static void DeleteFile (string path) {
			if (FileExists(path)) {
				File.Delete(path);
			}
		}


		public static void CopyFile (string from, string to) {
			if (FileExists(from)) {
				File.Copy(from, to, true);
			}
		}


		public static bool CopyDirectory (string from, string to, bool copySubDirs, bool ignoreHidden) {

			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(from);

			if (!dir.Exists) {
				return false;
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(to)) {
				Directory.CreateDirectory(to);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files) {
				try {
					string temppath = Path.Combine(to, file.Name);
					if (!ignoreHidden || (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden) {
						file.CopyTo(temppath, false);
					}
				} catch { }
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs) {
				foreach (DirectoryInfo subdir in dirs) {
					try {
						string temppath = Path.Combine(to, subdir.Name);
						if (!ignoreHidden || (subdir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden) {
							CopyDirectory(subdir.FullName, temppath, copySubDirs, ignoreHidden);
						}
					} catch { }
				}
			}
			return true;
		}


		public static void DeleteDirectory (string path) {
			if (DirectoryExists(path)) {
				Directory.Delete(path, true);
			}
		}


		public static void DeleteAllFilesIn (string path, string searchPattern = "*") {
			if (DirectoryExists(path)) {
				var files = GetFilesIn(path, false, searchPattern);
				foreach (var file in files) {
					DeleteFile(file.FullName);
				}
			}
		}


		public static float GetFileSizeInMB (string path) {
			float size = -1f;
			if (FileExists(path)) {
				size = (new FileInfo(path).Length / 1024f) / 1024f;
			}
			return size;
		}


		public static T ReadXML<T> (string path) where T : class {
			var serializer = new XmlSerializer(typeof(T));
			var stream = new FileStream(path, FileMode.Open);
			var container = serializer.Deserialize(stream) as T;
			stream.Close();
			return container;
		}


		public static void WriteXML<T> (T data, string path) where T : class {
			var serializer = new XmlSerializer(typeof(T));
			var stream = new FileStream(path, FileMode.Create);
			serializer.Serialize(stream, data);
			stream.Close();
		}


		public static int GetFileCount (string path, string search = "*", SearchOption option = SearchOption.TopDirectoryOnly) {
			if (DirectoryExists(path)) {
				return Directory.EnumerateFiles(path, search, option).Count();
			}
			return 0;
		}


		public static void MoveFile (string from, string to) {
			if (from != to && FileExists(from)) {
				File.Move(from, to);
			}
		}


		public static bool MoveDirectory (string from, string to) {
			if (from != to && DirectoryExists(from)) {
				try {
					Directory.Move(from, to);
					return true;
				} catch { }
			}
			return false;
		}


	}
}