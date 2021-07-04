using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BattleSoupAI;


namespace BattleSoup {
	public class BattleSoupUI : MonoBehaviour {




		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] BlocksRenderer m_SonarRenderer = null;
		[SerializeField] ShipRenderer m_ShipsRenderer = null;

		// Data
		private ShipData[] Ships = null;
		private readonly List<ShipPosition> Positions = new List<ShipPosition>();


		// MSG
		private void Update () {





		}


		// API
		public void Init (MapData map, ShipData[] ships, List<ShipPosition> positions) {
			Ships = null;
			Positions.Clear();
			if (map == null) { return; }
			if (positions.Count < ships.Length) {
				positions.AddRange(new ShipPosition[ships.Length - positions.Count]);
			}
			Ships = ships;
			Positions.AddRange(positions);
			m_MapRenderer.Map = map;
			m_MapRenderer.GridCountX = m_MapRenderer.GridCountY = map.Size;
			m_SonarRenderer.GridCountX = m_SonarRenderer.GridCountY = map.Size;
			m_ShipsRenderer.GridCountX = m_ShipsRenderer.GridCountY = map.Size;
			RefreshShipRenderer();
		}



		public void RefreshShipRenderer () {
			m_ShipsRenderer.ClearBlock();
			for (int i = 0; i < Ships.Length; i++) {
				m_ShipsRenderer.AddShip(Ships[i], Positions[i]);
			}
			m_ShipsRenderer.SetVerticesDirty();
		}





	}
}
