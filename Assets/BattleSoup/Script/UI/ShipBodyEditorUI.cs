using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace BattleSoup {
	public class ShipBodyEditorUI : Image, IPointerDownHandler {



		// VAR
		public bool Interactable { get; set; } = true;
		public List<Vector3Int> Nodes { get; } = new();

		[SerializeField] float m_BlockGap = 0f;
		[SerializeField] Color32 m_GridColor = Color.black;
		[SerializeField] UnityEvent m_OnValueChanged = null;

		private static readonly UIVertex[] c_Vertex = new UIVertex[4] {
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
		};
		private int Size = 1;


		// MSG
		public void OnPointerDown (PointerEventData e) {
			if (!Interactable) return;
			if (e.button != PointerEventData.InputButton.Left) return;
			var pos01 = rectTransform.GetPosition01(e.position, e.pressEventCamera);
			if (!pos01.Inside01()) return;
			var pos = new Vector2Int((int)(pos01.x * Size), (int)(pos01.y * Size));
			int removeCount = Nodes.RemoveAll(n => n.x == pos.x && n.y == pos.y);
			if (removeCount == 0) Nodes.Add(new(pos.x, pos.y, 1));
			if (Nodes.Count == 0) Nodes.Add(new(0, 0));
			m_OnValueChanged.Invoke();
		}


		protected override void OnPopulateMesh (VertexHelper toFill) {

			base.OnPopulateMesh(toFill);
			toFill.Clear();
			if (Nodes.Count == 0) return;
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
			var rect = GetPixelAdjustedRect();
			var _rect = new Rect();
			float bWidth = rect.width / Size;
			float bHeight = rect.height / Size;

			// Draw Grid
			c_Vertex[0].color = c_Vertex[1].color = c_Vertex[2].color = c_Vertex[3].color = m_GridColor;
			SetCacheUV(sprite, true);
			_rect.width = bWidth;
			_rect.height = bHeight;
			for (int j = 0; j < Size; j++) {
				for (int i = 0; i < Size; i++) {
					if (i % 2 == j % 2) continue;
					_rect.x = rect.xMin + i * bWidth;
					_rect.y = rect.yMin + j * bHeight;
					c_Vertex[0].position = new(_rect.xMin, _rect.yMin, 0f);
					c_Vertex[1].position = new(_rect.xMin, _rect.yMax, 0f);
					c_Vertex[2].position = new(_rect.xMax, _rect.yMax, 0f);
					c_Vertex[3].position = new(_rect.xMax, _rect.yMin, 0f);
					toFill.AddUIVertexQuad(c_Vertex);
				}
			}

			// Draw Blocks
			c_Vertex[0].color = c_Vertex[1].color = c_Vertex[2].color = c_Vertex[3].color = color;
			SetCacheUV(sprite);
			_rect = new Rect(0, 0, bWidth - m_BlockGap * 2, bHeight - m_BlockGap * 2);
			foreach (var node in Nodes) {
				_rect.x = rect.xMin + node.x * bWidth + m_BlockGap;
				_rect.y = rect.yMin + node.y * bHeight + m_BlockGap;
				c_Vertex[0].position = new(_rect.xMin, _rect.yMin, 0f);
				c_Vertex[1].position = new(_rect.xMin, _rect.yMax, 0f);
				c_Vertex[2].position = new(_rect.xMax, _rect.yMax, 0f);
				c_Vertex[3].position = new(_rect.xMax, _rect.yMin, 0f);
				toFill.AddUIVertexQuad(c_Vertex);
			}

			// Func
			void SetCacheUV (Sprite sprite, bool mid = false) {
				if (sprite != null) {
					var _rect = sprite.rect;
					float _width = sprite.texture.width;
					float _height = sprite.texture.height;
					_rect.x /= _width;
					_rect.y /= _height;
					_rect.width /= _width;
					_rect.height /= _height;
					if (!mid) {
						c_Vertex[0].uv0 = new Vector2(_rect.xMin, _rect.yMin);
						c_Vertex[1].uv0 = new Vector2(_rect.xMin, _rect.yMax);
						c_Vertex[2].uv0 = new Vector2(_rect.xMax, _rect.yMax);
						c_Vertex[3].uv0 = new Vector2(_rect.xMax, _rect.yMin);
					} else {
						c_Vertex[0].uv0 = c_Vertex[1].uv0 = c_Vertex[2].uv0 = c_Vertex[3].uv0 = _rect.center;
					}
				} else {
					c_Vertex[0].uv0 = default;
					c_Vertex[1].uv0 = default;
					c_Vertex[2].uv0 = default;
					c_Vertex[3].uv0 = default;
				}
			}
		}


		public void SetNodes (Vector3Int[] nodes) {
			Nodes.Clear();
			Nodes.AddRange(nodes);
			Vector3Int max = new(int.MinValue, int.MinValue);
			foreach (var node in Nodes) {
				max = Vector3Int.Max(max, node);
			}
			Size = Mathf.Max(max.x + 1, max.y + 1) + 1;
			Size = Mathf.Max(3, Size);
			SetVerticesDirty();
		}


		public void ResetNodes () {
			Nodes.Clear();
			Nodes.Add(new(0, 0, 1));
			m_OnValueChanged.Invoke();
		}


	}
}
#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEngine;
	using UnityEditor;
	[CustomEditor(typeof(ShipBodyEditorUI))]
	public class ShipBodyEditorUI_Inspector : Editor {
		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script", "m_OnCullStateChanged");
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif