namespace Moenen.Standard {
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using UnityEngine;




	public static partial class Util {


		public static string SanitizeFileName (string origFileName) => string.Join(
			"_", origFileName.Split(
				Path.GetInvalidFileNameChars(),
				System.StringSplitOptions.RemoveEmptyEntries)
			).TrimEnd('.');


		public static string GetRuntimeBuiltRootPath () {
#if UNITY_EDITOR
			return CombinePaths(GetParentPath(Application.dataPath), "_Built", $"{Application.productName} v{Application.version}");
#elif UNITY_STANDALONE_OSX
			return Application.dataPath;
#else
			return GetParentPath(Application.dataPath);
#endif
		}


		public static string GetParentPath (string path) => Directory.GetParent(path).FullName;


		public static string GetFullPath (string path) => new FileInfo(path).FullName;


		public static string GetDirectoryFullPath (string path) => new DirectoryInfo(path).FullName;


		public static string CombinePaths (params string[] paths) {
			string path = "";
			for (int i = 0; i < paths.Length; i++) {
				path = Path.Combine(path, paths[i]);
			}
			return path;
		}


		public static string GetExtension (string path) => Path.GetExtension(path);//.txt


		public static string GetNameWithoutExtension (string path) => Path.GetFileNameWithoutExtension(path);


		public static string GetNameWithExtension (string path) => Path.GetFileName(path);


		public static string ChangeExtension (string path, string newEx) => Path.ChangeExtension(path, newEx);


		public static bool DirectoryExists (string path) => Directory.Exists(path);


		public static bool FileExists (string path) => !string.IsNullOrEmpty(path) && File.Exists(path);


		public static bool PathIsDirectory (string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);


		public static bool IsChildPath (string pathA, string pathB) {
			pathA = GetFullPath(pathA);
			pathB = GetFullPath(pathB);
			if (pathA.Length == pathB.Length) {
				return pathA == pathB;
			} else if (pathA.Length > pathB.Length) {
				return IsChildPathCompair(pathA, pathB);
			} else {
				return IsChildPathCompair(pathB, pathA);
			}
		}


		private static bool IsChildPathCompair (string longPath, string path) {
			if (longPath.Length <= path.Length || !PathIsDirectory(path) || !longPath.StartsWith(path)) {
				return false;
			}
			char c = longPath[path.Length];
			if (c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar) {
				return false;
			}
			return true;
		}


		public static string GetUrl (string path) => string.IsNullOrEmpty(path) ? "" : new System.Uri(path).AbsoluteUri;


		public static string GetTimeString () => System.DateTime.Now.ToString("yyyyMMddHHmmssffff");


		public static long GetLongTime () => System.DateTime.Now.Ticks;


		public static string GetDisplayTimeFromTicks (long ticks) => new System.DateTime(ticks).ToString("yyyy-MM-dd HH:mm");


		public static string FixPath (string path, bool forUnity = true) {
			char dsChar = forUnity ? '/' : Path.DirectorySeparatorChar;
			char adsChar = forUnity ? '\\' : Path.AltDirectorySeparatorChar;
			path = path.Replace(adsChar, dsChar);
			path = path.Replace(new string(dsChar, 2), dsChar.ToString());
			while (path.Length > 0 && path[0] == dsChar) {
				path = path.Remove(0, 1);
			}
			while (path.Length > 0 && path[path.Length - 1] == dsChar) {
				path = path.Remove(path.Length - 1, 1);
			}
			return path;
		}


		public static bool IsSamePath (string pathA, string pathB) => FixPath(pathA) == FixPath(pathB);



	}
}