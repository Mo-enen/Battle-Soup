using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UIGadget;
using BattleSoup;



namespace BattleSoupDemo {
	public class ShipPositionUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {




		#region --- VAR ---


		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] BlocksRenderer m_ShipRenderer = null;

		// Data
		private readonly List<ShipData> Ships = new List<ShipData>();
		private readonly List<ShipPosition> Positions = new List<ShipPosition>();


		#endregion




		#region --- MSG ---


		public void OnBeginDrag (PointerEventData eData) {



		}


		public void OnDrag (PointerEventData eData) {



		}


		public void OnEndDrag (PointerEventData eData) {



		}


		#endregion




		#region --- API ---


		public bool Init (MapData map, List<ShipData> ships) {
			if (map == null || map.Size <= 0 || ships == null || ships.Count == 0) { return false; }
			// Ship
			Ships.Clear();
			Ships.AddRange(ships);
			m_ShipRenderer.GridCountX = map.Size;
			m_ShipRenderer.GridCountY = map.Size;
			// Map
			m_MapRenderer.Map = map;
			// Pos
			RandomPositions();
			return true;
		}


		#endregion




		#region --- LGC ---


		private bool RandomPositions () => Soup.GetRandomShipPositions(
			GetShipList(Ships), m_MapRenderer.Map.Stones, Positions
		);


		#endregion




		#region --- UTL ---


		private List<Ship> GetShipList (List<ShipData> shipDatas) {
			var result = new List<Ship>();
			foreach (var ship in shipDatas) {
				result.Add(ship.Ship);
			}
			return result;
		}


		#endregion




	}
}
