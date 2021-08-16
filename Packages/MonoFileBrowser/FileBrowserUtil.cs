namespace MonoFileBrowser {
	using UnityEngine;
	using SFB;


	public static class FileBrowserUtil {




		#region --- API ---



		// File
		public static string PickFolderDialog (string title) {
			var lastPickedFolder = PlayerPrefs.GetString(
				"DialogUtil.LastPickedFolder",
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
			);
			var paths = StandaloneFileBrowser.OpenFolderPanel(title, lastPickedFolder, false);
			var path = paths is null || paths.Length == 0 ? "" : paths[0];
			if (!string.IsNullOrEmpty(path)) {
				PlayerPrefs.SetString("DialogUtil.LastPickedFolder", GetParentPath(path));
				return path;
			}
			return "";
		}


		public static string PickFileDialog (string title, string filterName, params string[] filters) {
			var lastPickedFolder = PlayerPrefs.GetString(
				"DialogUtil.LastPickedFolder",
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
			);
			var paths = StandaloneFileBrowser.OpenFilePanel(title, lastPickedFolder, new ExtensionFilter[1] { new ExtensionFilter(filterName, filters) }, false);
			var path = paths is null || paths.Length == 0 ? "" : paths[0];
			if (!string.IsNullOrEmpty(path)) {
				PlayerPrefs.SetString("DialogUtil.LastPickedFolder", GetParentPath(path));
				return path;
			}

			return "";
		}


		public static string CreateFileDialog (string title, string defaultName, string ext) {
			var lastPickedFolder = PlayerPrefs.GetString(
				"DialogUtil.LastPickedFolder",
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
			);
			var path = StandaloneFileBrowser.SaveFilePanel(title, lastPickedFolder, defaultName, ext);
			if (!string.IsNullOrEmpty(path)) {
				PlayerPrefs.SetString("DialogUtil.LastPickedFolder", GetParentPath(path));
				return path;
			}
			return "";
		}


		private static string GetParentPath (string path) => System.IO.Directory.GetParent(path).FullName;



		#endregion



	}
}