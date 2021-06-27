using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;




namespace Moenen.Standard {
	public static class EditorUtil {




		#region --- Path ---


		private static string ROOT_PATH = "";
		private static bool FixPathForIconLoading = true;


		public static string GetRootPath (string rootName, string packageName = "") {
			if (!string.IsNullOrEmpty(ROOT_PATH) && Util.DirectoryExists(ROOT_PATH)) { return ROOT_PATH; }
			var paths = AssetDatabase.GetAllAssetPaths();
			foreach (var path in paths) {
				if (Util.PathIsDirectory(path) && Util.GetNameWithoutExtension(path) == rootName) {
					ROOT_PATH = FixedRelativePath(path);
					break;
				}
			}
			if (string.IsNullOrEmpty(ROOT_PATH) && !string.IsNullOrEmpty(packageName)) {
				ROOT_PATH = "Packages/" + packageName;
				FixPathForIconLoading = false;
			}
			return ROOT_PATH;
		}


		public static Texture2D GetImage (string rootName, string packageName, params string[] imagePath) =>
			GetAsset<Texture2D>(rootName, packageName, imagePath);


		public static T GetAsset<T> (string rootName, string packageName, params string[] assetPath) where T : Object {
			T result = null;
			string path = Util.CombinePaths(assetPath);
			path = Util.CombinePaths(GetRootPath(rootName, packageName), path);
			if (Util.FileExists(path)) {
				result = AssetDatabase.LoadAssetAtPath<T>(
					FixPathForIconLoading ? FixedRelativePath(path) : path
				);
			}
			return result;
		}


		public static string FixedRelativePath (string path) {
			path = Util.FixPath(path);
			if (path.StartsWith("Assets")) {
				return path;
			}
			var fixedDataPath = Util.FixPath(Application.dataPath);
			if (path.StartsWith(fixedDataPath)) {
				return "Assets" + path.Substring(fixedDataPath.Length);
			} else {
				return "";
			}
		}


		#endregion




		#region --- Message ---


		public static bool Dialog (string title, string msg, string ok, string cancel = "") {
			PauseWatch();
			if (string.IsNullOrEmpty(cancel)) {
				bool sure = EditorUtility.DisplayDialog(title, msg, ok);
				RestartWatch();
				return sure;
			} else {
				bool sure = EditorUtility.DisplayDialog(title, msg, ok, cancel);
				RestartWatch();
				return sure;
			}
		}


		public static int DialogComplex (string title, string msg, string ok, string cancel, string alt) {
			//EditorApplication.Beep();
			PauseWatch();
			int index = EditorUtility.DisplayDialogComplex(title, msg, ok, cancel, alt);
			RestartWatch();
			return index;
		}


		public static void ProgressBar (string title, string msg, float value) {
			value = Mathf.Clamp01(value);
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayProgressBar(title, msg, value);
		}


		public static void ClearProgressBar () {
			EditorUtility.ClearProgressBar();
		}


		#endregion




		#region --- Watch ---


		private static System.Diagnostics.Stopwatch TheWatch;


		public static void StartWatch () {
			TheWatch = new System.Diagnostics.Stopwatch();
			TheWatch.Start();
		}


		public static void PauseWatch () {
			if (TheWatch != null) {
				TheWatch.Stop();
			}
		}


		public static void RestartWatch () {
			if (TheWatch != null) {
				TheWatch.Start();
			}
		}


		public static double StopWatchAndGetTime () {
			if (TheWatch != null) {
				TheWatch.Stop();
				return TheWatch.Elapsed.TotalSeconds;
			}
			return 0f;
		}


		#endregion




		#region --- Misc ---


		public static bool GetExpandComponent<T> () where T : Component {
			bool result = false;
			var g = new GameObject("", typeof(T));
			try {
				g.hideFlags = HideFlags.HideAndDontSave;
				result = UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(
					g.GetComponent(typeof(T))
				);
			} catch { }
			Object.DestroyImmediate(g, false);
			return result;
		}


		public static void SetExpandComponent<T> (bool expand) where T : Component {
			var g = new GameObject("", typeof(T));
			try {
				g.hideFlags = HideFlags.HideAndDontSave;
				UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(
					g.GetComponent(typeof(T)), expand
				);
			} catch { }
			Object.DestroyImmediate(g, false);


		}


		public static Texture2D GetFixedAssetPreview (Object obj) {

			var texture = AssetPreview.GetAssetPreview(obj);
			if (texture == null) { return null; }
			int width = texture.width;
			int height = texture.height;
			if (width == 0 || height == 0) { return null; }
			var pixels = texture.GetPixels32();
			int length = width * height;

			// Remove Background
			Color32 CLEAR = new Color32(0, 0, 0, 0);
			var stack = new Stack<(int x, int y)>();
			RemoveColorAt(0, 0);
			RemoveColorAt(width - 1, 0);
			RemoveColorAt(0, height - 1);
			RemoveColorAt(width - 1, height - 1);

			// Fix Color Brightness
			Color32 pixel;
			for (int i = 0; i < length; i++) {
				pixel = pixels[i];
				if (pixel.a == 0) { continue; }
				pixel.r = (byte)Mathf.Clamp((pixel.r - 128f) * 1.5f + 190f, byte.MinValue, byte.MaxValue);
				pixel.g = (byte)Mathf.Clamp((pixel.g - 128f) * 1.5f + 190f, byte.MinValue, byte.MaxValue);
				pixel.b = (byte)Mathf.Clamp((pixel.b - 128f) * 1.5f + 190f, byte.MinValue, byte.MaxValue);
				pixels[i] = pixel;
			}

			// Final
			var result = new Texture2D(width, height, TextureFormat.RGBA32, false) {
				alphaIsTransparency = true,
				filterMode = FilterMode.Point
			};
			result.SetPixels32(pixels);
			result.Apply();
			return result;

			// === Func ===
			bool SameColor (Color32 colorA, Color32 colorB) =>
				colorA.r == colorB.r &&
				colorA.g == colorB.g &&
				colorA.b == colorB.b &&
				colorA.a == colorB.a;
			void RemoveColorAt (int _x, int _y) {
				var color32 = pixels[_y * width + _x];
				if (color32.a == 0) { return; }
				stack.Clear();
				stack.Push((_x, _y));
				for (int safeCount = 0; safeCount < length * 8 && stack.Count > 0; safeCount++) {
					(int x, int y) = stack.Pop();
					pixels[y * width + x] = CLEAR;
					AddToStack(x, y - 1, color32);
					AddToStack(x, y + 1, color32);
					AddToStack(x - 1, y, color32);
					AddToStack(x + 1, y, color32);
					AddToStack(x - 1, y - 1, color32);
					AddToStack(x - 1, y + 1, color32);
					AddToStack(x + 1, y - 1, color32);
					AddToStack(x + 1, y + 1, color32);
				}
			}
			void AddToStack (int x, int y, Color32 color32) {
				int i = y * width + x;
				if (
					x >= 0 && y >= 0 && x < width && y < height &&
					SameColor(color32, pixels[i])
				) {
					stack.Push((x, y));
				}
			}
		}


		public static List<SerializedProperty> GetProps (ScriptableObject source, SerializedObject sObj) {
			var type = source.GetType();
			var pubList = new List<FieldInfo>(type.GetFields(BindingFlags.Instance | BindingFlags.Public));
			for (int i = 0; i < pubList.Count; i++) {
				var field = pubList[i];
				if (field.GetCustomAttribute<HideInInspector>() != null) {
					pubList.RemoveAt(i);
					i--;
				}
			}
			var priList = new List<FieldInfo>(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic));
			for (int i = 0; i < priList.Count; i++) {
				var field = priList[i];
				if (field.GetCustomAttribute<HideInInspector>() != null || field.GetCustomAttribute<SerializeField>() == null) {
					priList.RemoveAt(i);
					i--;
				}
			}
			var pList = new List<SerializedProperty>();
			foreach (var field in pubList) {
				var p = sObj.FindProperty(field.Name);
				if (p != null) {
					pList.Add(p);
				}
			}
			foreach (var field in priList) {
				var p = sObj.FindProperty(field.Name);
				if (p != null) {
					pList.Add(p);
				}
			}
			return pList;
		}


		#endregion




	}


}