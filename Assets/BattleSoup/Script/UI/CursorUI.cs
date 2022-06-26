namespace BattleSoup {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.EventSystems;
	using UnityEngine.UI;
	using UnityEngine.InputSystem;


	public class CursorUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {



		// VAR
		private static Transform CurrentTF = null;
		private static (Texture2D texture, Vector2 offset) Current = (null, default);
		private static (Texture2D texture, Vector2 offset) Prev = (null, default);

		// Short
		private static Mouse Mouse => _Mouse ??= Mouse.current;

		// Ser
		[SerializeField] private Texture2D m_Texture = null;
		[SerializeField] private Vector2 m_Offset = new(20, 0);

		// Data
		private static Mouse _Mouse = null;
		private Selectable Select = null;


		// MSG
		[RuntimeInitializeOnLoadMethod]
		public static void Initialize () {
			Application.onBeforeRender += () => {
				if (Mouse != null && (Mouse.leftButton.isPressed || Mouse.rightButton.isPressed || Mouse.middleButton.isPressed)) return;
				if (CurrentTF == null) {
					if (Current.texture != Prev.texture) {
						Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
						Prev.texture = Current.texture = null;
					}
				} else {
					if (Current.texture != Prev.texture) {
						Cursor.SetCursor(Current.texture, Current.offset, CursorMode.Auto);
						Prev.texture = Current.texture;
					}
				}
			};
		}


		private void Awake () {
			Select = GetComponent<Selectable>();
		}


		private void OnMouseEnter () {
			if (Select != null && !Select.interactable) { return; }
			CurrentTF = transform;
			Current.texture = m_Texture;
			Current.offset = m_Offset;
		}


		private void OnMouseExit () {
			if (CurrentTF == transform) {
				CurrentTF = null;
				Current.texture = null;
				Current.offset = default;
			}
		}


		private void OnDisable () {
			if (CurrentTF == transform) {
				CurrentTF = null;
				Current.texture = null;
				Current.offset = default;
			}
		}


		public void OnPointerEnter (PointerEventData eventData) {
			if (Select != null && !Select.interactable) { return; }
			CurrentTF = transform;
			Current.texture = m_Texture;
			Current.offset = m_Offset;
		}


		public void OnPointerExit (PointerEventData eventData) {
			if (CurrentTF == transform) {
				CurrentTF = null;
				Current.texture = null;
				Current.offset = default;
			}
		}



	}
}



#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEditor;
	using Moenen.Standard;
	[CustomEditor(typeof(CursorUI))]
	public class OwOCursor_Inspector : Editor {
		private readonly string[] EXCLUDE = new string[] { "m_Script" };
		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, EXCLUDE);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif