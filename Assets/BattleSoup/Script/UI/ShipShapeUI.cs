using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace BattleSoup {
	public class ShipShapeUI : Image {


		// Api
		public Ship Ship {
			get => _Ship;
			set {
				_Ship = value;
				SetVerticesDirty();
			}
		}
		[System.NonSerialized] private Ship _Ship = null;

		// Data
		private static readonly UIVertex[] c_Vertex = new UIVertex[4] {
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
		};


		// MSG
		protected override void OnPopulateMesh (VertexHelper toFill) {
			toFill.Clear();
			if (Ship == null || Ship.BodyNodes.Length == 0) return;
			c_Vertex[0].color = color;
			c_Vertex[1].color = color;
			c_Vertex[2].color = color;
			c_Vertex[3].color = color;
			if (sprite == null) {
				c_Vertex[0].uv0 = new Vector2(0, 0);
				c_Vertex[1].uv0 = new Vector2(0, 1);
				c_Vertex[2].uv0 = new Vector2(1, 1);
				c_Vertex[3].uv0 = new Vector2(1, 0);
			} else {
				c_Vertex[0].uv0 = sprite.uv[0];
				c_Vertex[1].uv0 = sprite.uv[1];
				c_Vertex[2].uv0 = sprite.uv[3];
				c_Vertex[3].uv0 = sprite.uv[2];
			}
			var nodeMin = new Vector2Int(int.MaxValue, int.MaxValue);
			var nodeMax = new Vector2Int(int.MinValue, int.MinValue);
			foreach (var node in Ship.BodyNodes) {
				nodeMin = Vector2Int.Min(nodeMin, node);
				nodeMax = Vector2Int.Max(nodeMax, node);
			}
			var rect = GetPixelAdjustedRect();
			float size = Mathf.Min(rect.width / 3f, rect.height / 3f);
			var _rect = new Rect(0, 0, size, size);
			var offset = new Vector2(
				(rect.width - (nodeMax.x - nodeMin.x + 1) * size) / 2f,
				(rect.height - (nodeMax.y - nodeMin.y + 1) * size) / 2f
			);
			foreach (var node in Ship.BodyNodes) {
				_rect.x = rect.xMin + node.x * _rect.width + offset.x;
				_rect.y = rect.yMin + node.y * _rect.height + offset.y;
				c_Vertex[0].position = new(_rect.xMin, _rect.yMin, 0f);
				c_Vertex[1].position = new(_rect.xMin, _rect.yMax, 0f);
				c_Vertex[2].position = new(_rect.xMax, _rect.yMax, 0f);
				c_Vertex[3].position = new(_rect.xMax, _rect.yMin, 0f);
				toFill.AddUIVertexQuad(c_Vertex);
			}
		}


	}
}
