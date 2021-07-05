using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;
using BattleSoupAI;

namespace BattleSoup {
	public class MapRenderer : BlocksRenderer {


		// Ser
		[SerializeField] MapData m_Map = null;
		[SerializeField] VectorGrid m_Grid = null;


		// MSG
		protected override void OnEnable () {
			base.OnEnable();
			if (m_Map != null) {
				LoadMap(m_Map);
			}
		}



		// API
		public void LoadMap (MapData map, ShipData[] ships = null, List<ShipPosition> positions = null) {
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
			// Ship
			if (ships != null && positions != null) {
				for (int i = 0; i < ships.Length; i++) {
					var ship = ships[i];
					var sPos = positions[i];
					foreach (var v in ship.Ship.Body) {
						var pos = new Vector2Int(
							sPos.Pivot.x + (sPos.Flip ? v.y : v.x),
							sPos.Pivot.y + (sPos.Flip ? v.x : v.y)
						);
						AddBlock(
							pos.x, pos.y, 1,
							Color.HSVToRGB((float)i / ships.Length, 0.618f, 0.618f)
						);
					}
				}
			}
			SetVerticesDirty();
		}


	}
}
