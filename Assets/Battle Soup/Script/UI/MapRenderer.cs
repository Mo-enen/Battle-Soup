using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;
using BattleSoupAI;

namespace BattleSoup {
	public class MapRenderer : BlocksRenderer {


		// Ser
		[SerializeField] VectorGrid m_Grid = null;




		// API
		public void LoadMap (MapData map) {
			ClearBlock();
			if (map != null) {
				GridCountX = map.Size;
				GridCountY = map.Size;
				m_Grid.X = map.Size;
				m_Grid.Y = map.Size;
				foreach (var stone in map.Stones) {
					AddBlock(stone.x, stone.y, 0);
				}
			}
			SetVerticesDirty();
		}



	}
}
