using UnityEngine;
using UnityEditor;




namespace Moenen.Standard {



	public static class Layout {

		public static GUIStyle BoxMarginless => _BoxMarginless ??= new GUIStyle(GUI.skin.box) {
			margin = new RectOffset(0, 0, 0, 0),
			padding = new RectOffset(4, 12, 0, 0),
		};
		private static GUIStyle _BoxMarginless = null;

		public static Rect Rect (int w, int h) => GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(w == 0), GUILayout.ExpandHeight(h == 0));
		public static Rect LastRect () => GUILayoutUtility.GetLastRect();
		public static Rect AspectRect (float aspect) => GUILayoutUtility.GetAspectRect(aspect);
		public static void Space (int space = 4) => GUILayout.Space(space);
		public static bool Fold (string label, ref bool open, bool box = false, float offset = 0f) {
			using (new GUILayout.VerticalScope(box ? BoxMarginless : GUIStyle.none)) {
				var rect = Rect(0, 22);
				rect.x -= offset;
				rect.width += offset;
				open = EditorGUI.Toggle(rect, open, EditorStyles.foldout);
				rect.x += 14;
				rect.width -= 14;
				GUI.Label(rect, label);
			}
			return open;
		}


		public static bool DownButton (Rect rect, string label, GUIStyle style = null) => DownButton(rect, new GUIContent(label), style);
		public static bool DownButton (Rect rect, GUIContent label, GUIStyle style = null) {
			if (style == null) { style = GUI.skin.button; }
			bool down = Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition);
			GUI.Button(rect, label, style);
			if (down) {
				GUI.changed = true;
			}
			return down;
		}

		public static void FrameGUI (Rect rect, float thickness, Color color) {
			EditorGUI.DrawRect(rect.Expand(0, 0, thickness - rect.height, 0), color);
			EditorGUI.DrawRect(rect.Expand(0, 0, 0, thickness - rect.height), color);
			EditorGUI.DrawRect(rect.Expand(thickness - rect.width, 0, -thickness, -thickness), color);
			EditorGUI.DrawRect(rect.Expand(0, thickness - rect.width, -thickness, -thickness), color);
		}


	}


}