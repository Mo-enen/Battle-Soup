using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;


namespace BattleSoupDemo {
	public class MapThumbnailRenderer : BlocksRenderer {



		// Api
		public MapData Map {
			get => m_Map;
			set {
				m_Map = value;
				RefreshMap();
				SetVerticesDirty();
			}
		}

		// Ser
		[SerializeField] MapData m_Map = null;
		[SerializeField] VectorGrid m_Grid = null;


		// MSG
		protected override void OnEnable () {
			base.OnEnable();
			RefreshMap();
		}


		private void RefreshMap () {
			ClearBlock();
			if (m_Map != null) {
				GridCountX = m_Map.Size.x;
				GridCountY = m_Map.Size.y;
				m_Grid.X = m_Map.Size.x;
				m_Grid.Y = m_Map.Size.y;
				foreach (var stone in m_Map.Stones) {
					AddBlock(stone.x, stone.y, 0);
				}
			}
		}


	}
}
