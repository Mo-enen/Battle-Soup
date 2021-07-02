using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;


namespace BattleSoup {
	public class MapRenderer : BlocksRenderer {



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
				GridCountX = m_Map.Size;
				GridCountY = m_Map.Size;
				m_Grid.X = m_Map.Size;
				m_Grid.Y = m_Map.Size;
				foreach (var stone in m_Map.Stones) {
					AddBlock(stone.X, stone.Y, 0);
				}
			}
		}


	}
}
