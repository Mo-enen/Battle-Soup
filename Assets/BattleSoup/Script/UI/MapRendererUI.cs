using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BattleSoup {
	public class MapRendererUI : Image {


		// Api
		public Map Map {
			get => _Map;
			set {
				_Map = value;
				SetVerticesDirty();
			}
		}
		private Map _Map = null;

		// Data
		private static readonly UIVertex[] c_Vertex = new UIVertex[4] {
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
		};


		// MSG
		protected override void OnPopulateMesh (VertexHelper toFill) {
			base.OnPopulateMesh(toFill);
			toFill.Clear();
			if (Map == null) return;
			c_Vertex[0].color = color;
			c_Vertex[1].color = color;
			c_Vertex[2].color = color;
			c_Vertex[3].color = color;
			var rect = GetPixelAdjustedRect();
			var _rect = new Rect(0, 0, rect.width / Map.Size, rect.height / Map.Size);
			for (int j = 0; j < Map.Size; j++) {
				for (int i = 0; i < Map.Size; i++) {
					if (Map[i, j] == 1) {
						_rect.x = i * _rect.width;
						_rect.y = j * _rect.height;
						c_Vertex[0].position = new(_rect.xMin, _rect.yMin, 0f);
						c_Vertex[1].position = new(_rect.xMin, _rect.yMax, 0f);
						c_Vertex[2].position = new(_rect.xMax, _rect.yMax, 0f);
						c_Vertex[3].position = new(_rect.xMax, _rect.yMin, 0f);
						toFill.AddUIVertexQuad(c_Vertex);
					}
				}
			}
		}


	}
}
