namespace Moenen.Standard {
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;


	public static partial class Util {


		private readonly static Dictionary<KeyCode, string> SpecialKeyNames = new Dictionary<KeyCode, string> {
			{ KeyCode.None, "" }, { KeyCode.Alpha0, "0" }, { KeyCode.Alpha1, "1" },{ KeyCode.Alpha2, "2" },{ KeyCode.Alpha3, "3" },{ KeyCode.Alpha4, "4" },{ KeyCode.Alpha5, "5" },{ KeyCode.Alpha6, "6" },{ KeyCode.Alpha7, "7" },{ KeyCode.Alpha8, "8" },{ KeyCode.Alpha9, "9" },{ KeyCode.UpArrow, "¡ü" },{ KeyCode.DownArrow, "¡ý" },{ KeyCode.LeftArrow, "¡û" },{ KeyCode.RightArrow, "¡ú" },{ KeyCode.Escape, "ESC" },{KeyCode.Comma, "," },{ KeyCode.Period, "." },{ KeyCode.Slash, "/" },{ KeyCode.Backslash, "\\" },{ KeyCode.LeftBracket, "[" },{ KeyCode.RightBracket, "]" },{ KeyCode.Return, "Enter" },{ KeyCode.KeypadEnter, "Enter" },{ KeyCode.Semicolon, ";" },{ KeyCode.Equals, "=" },{ KeyCode.Minus, "-" },{ KeyCode.Quote, "\'" },{ KeyCode.BackQuote, "`" },
		};



		public static bool IsTypeing {
			get {
				var g = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
				if (g) {
					var input = g.GetComponent<UnityEngine.UI.InputField>();
					return input && input.isFocused;
				} else {
					return false;
				}
			}
		}


		public static string GetKeyName (KeyCode key) => SpecialKeyNames.ContainsKey(key) ? SpecialKeyNames[key] : key.ToString();


		public static T SpawnUI<T> (T prefab, RectTransform root, string name = "") where T : MonoBehaviour {
			root.gameObject.SetActive(true);
			root.parent.gameObject.SetActive(true);
			var obj = Object.Instantiate(prefab, root);
			var rt = obj.transform as RectTransform;
			rt.name = name;
			rt.SetAsLastSibling();
			rt.localRotation = Quaternion.identity;
			rt.localScale = Vector3.one;
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = rt.offsetMax = Vector2.zero;
			return obj;
		}


		public static Vector3 Vector3Lerp3 (Vector3 a, Vector3 b, float x, float y, float z = 0f) => new Vector3(
			Mathf.LerpUnclamped(a.x, b.x, x),
			Mathf.LerpUnclamped(a.y, b.y, y),
			Mathf.LerpUnclamped(a.z, b.z, z)
		);


		public static Vector3 Vector3InverseLerp3 (Vector3 a, Vector3 b, float x, float y, float z = 0f) => new Vector3(
			RemapUnclamped(a.x, b.x, 0f, 1f, x),
			RemapUnclamped(a.y, b.y, 0f, 1f, y),
			RemapUnclamped(a.z, b.z, 0f, 1f, z)
		);


		public static float RemapUnclamped (float l, float r, float newL, float newR, float t) {
			return l == r ? 0 : Mathf.LerpUnclamped(
				newL, newR,
				(t - l) / (r - l)
			);
		}


		public static float Remap (float l, float r, float newL, float newR, float t) {
			return l == r ? l : Mathf.Lerp(
				newL, newR,
				(t - l) / (r - l)
			);
		}


		public static Vector3 Remap (float l, float r, Vector3 newL, Vector3 newR, float t) => new Vector3(
			Remap(l, r, newL.x, newR.x, t),
			Remap(l, r, newL.y, newR.y, t),
			Remap(l, r, newL.z, newR.z, t)
		);


		// Math
		public static bool PointInTriangle (float px, float py, float p0x, float p0y, float p1x, float p1y, float p2x, float p2y) {
			var s = p0y * p2x - p0x * p2y + (p2y - p0y) * px + (p0x - p2x) * py;
			var t = p0x * p1y - p0y * p1x + (p0y - p1y) * px + (p1x - p0x) * py;
			if ((s < 0) != (t < 0)) { return false; }
			var A = -p1y * p2x + p0y * (p2x - p1x) + p0x * (p1y - p2y) + p1x * p2y;
			return A < 0 ? (s <= 0 && s + t >= A) : (s >= 0 && s + t <= A);
		}


		public static bool PointInTriangle (Vector2 p, Vector2 a, Vector2 b, Vector2 c) => PointInTriangle(p.x, p.y, a.x, a.y, b.x, b.y, c.x, c.y);


		public static float PointLine_Distance (Vector2 p, Vector2 a, Vector2 b) {
			if (a == b) { return Vector2.Distance(p, a); }
			float x0 = p.x;
			float x1 = a.x;
			float x2 = b.x;
			float y0 = p.y;
			float y1 = a.y;
			float y2 = b.y;
			return Mathf.Abs(
				(x2 - x1) * (y1 - y0) - (x1 - x0) * (y2 - y1)
			) / Mathf.Sqrt(
				(x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)
			);
		}




		// Misc
		public static Rect Expand (this Rect rect, float offset) => rect.Expand(offset, offset, offset, offset);
		public static Rect Expand (this Rect rect, float l, float r, float d, float u) {
			rect.x -= l;
			rect.y -= d;
			rect.width += l + r;
			rect.height += d + u;
			return rect;
		}
		public static Rect Fit (this Rect rect, float targetAspect) {
			var bgSize = GetFitInSize(rect.width, rect.height, targetAspect);
			return new Rect(rect.x + Mathf.Abs(rect.width - bgSize.x) / 2f, rect.y + Mathf.Abs(rect.height - bgSize.y) / 2f, bgSize.x, bgSize.y);
			Vector2 GetFitInSize (float boxX, float boxY, float aspect) =>
				aspect > boxX / boxY ? new Vector2(boxX, boxX / aspect) : new Vector2(boxY * aspect, boxY);
		}


		public static RectInt Expand (this RectInt rect, int offset) => rect.Expand(offset, offset, offset, offset);
		public static RectInt Expand (this RectInt rect, int l, int r, int d, int u) {
			rect.x -= l;
			rect.y -= d;
			rect.width += l + r;
			rect.height += d + u;
			return rect;
		}


		public static void Clamp (this ref Rect rect, Rect target) => rect = Rect.MinMaxRect(
			Mathf.Max(rect.xMin, target.xMin),
			Mathf.Max(rect.yMin, target.yMin),
			Mathf.Min(rect.xMax, target.xMax),
			Mathf.Min(rect.yMax, target.yMax)
		);
		public static void Clamp (this ref RectInt rect, RectInt target) => rect.SetMinMax(
			Vector2Int.Min(rect.min, target.min),
			Vector2Int.Max(rect.max, target.max)
		);


		public static bool Contains (this RectInt rect, int x, int y) => rect.Contains(new Vector2Int(x, y));


		public static bool IsSame (this Color32 a, Color32 b, bool ignoreAlpha = false) => a.r == b.r && a.g == b.g && a.b == b.b && (ignoreAlpha || a.a == b.a);


		public static Color32 Blend_OneMinusAlpha (Color32 back, Color32 front) {
			if (front.a < 255) {
				Color bg = back;
				Color fg = front;
				float a = 1f - (1f - bg.a) * (1f - fg.a);
				return new Color(
					fg.r * fg.a / a + bg.r * bg.a * (1f - fg.a) / a,
					fg.g * fg.a / a + bg.g * bg.a * (1f - fg.a) / a,
					fg.b * fg.a / a + bg.b * bg.a * (1f - fg.a) / a,
					a
				);
			} else {
				return front;
			}
		}
		public static Color32 Blend_Additive (Color32 back, Color32 front) {
			Color bg = back;
			Color fg = front;
			Color.RGBToHSV(fg, out _, out _, out float v);
			return Color.Lerp(bg, bg + fg, v * fg.a);
		}
		public static Color32 SetAlpha (this Color32 color, float alpha) => color.SetAlpha((byte)Mathf.Clamp(alpha * 255f, 0, 255));
		public static Color32 SetAlpha (this Color32 color, byte alpha) {
			color.a = alpha;
			return color;
		}


		public static void TryAdd<U, V> (this Dictionary<U, V> map, U key, V value) {
			if (!map.ContainsKey(key)) {
				map.Add(key, value);
			}
		}
		public static V GetOrAdd<U, V> (this Dictionary<U, V> map, U key, V defaultValue) {
			if (!map.ContainsKey(key)) {
				map.Add(key, defaultValue);
			}
			return map[key];
		}
		public static V SetOrAdd<U, V> (this Dictionary<U, V> map, U key, V value) {
			if (!map.ContainsKey(key)) {
				map.Add(key, value);
			} else {
				map[key] = value;
			}
			return map[key];
		}

		public static void TryAdd<T> (this HashSet<T> hash, T value) {
			if (!hash.Contains(value)) {
				hash.Add(value);
			}
		}


		public static T[,] GetArray2D<T> (T[] array, int width, int height) {
			if (width <= 0 || height <= 0 || array.Length != width * height) { return null; }
			var result = new T[width, height];
			for (int j = 0; j < height; j++) {
				for (int i = 0; i < width; i++) {
					result[i, j] = array[j * width + i];
				}
			}
			return result;
		}
		public static T[] GetArray1D<T> (T[,] array) {
			int width = array.GetLength(0);
			var result = new T[width * array.GetLength(1)];
			for (int i = 0; i < result.Length; i++) {
				result[i] = array[i % width, i / width];
			}
			return result;
		}





		// Ref
		public static void InvokeMethod<T> (T obj, string methodName, params object[] param) {
			if (obj == null || string.IsNullOrEmpty(methodName)) { return; }
			try {
				obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, param);
			} catch (System.Exception ex) { Debug.LogError(ex); }
		}







	}




}