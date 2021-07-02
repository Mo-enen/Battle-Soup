using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UIGadget;
using BattleSoupAI;



namespace BattleSoup {
	public class ShipPositionUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {




		#region --- SUB ---


		[System.Serializable] public class VoidEvent : UnityEvent { }


		#endregion




		#region --- VAR ---


		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] ShipRenderer m_ShipRenderer = null;
		[SerializeField] BlocksRenderer m_OverlapRenderer = null;
		[SerializeField] VoidEvent m_OnPositionChanged = null;

		// Data
		private MapData Map = null;
		private ShipData[] Ships = new ShipData[0];
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
			Ships = new ShipData[ships.Count];
			ships.CopyTo(Ships);
			m_ShipRenderer.GridCountX = map.Size;
			m_ShipRenderer.GridCountY = map.Size;

			// Map
			Map = map;
			m_MapRenderer.Map = map;

			// Pos
			Soup.GetRandomShipPositions(
				GetShipList(Ships), Map.Stones, Positions
			);
			RefreshShipRenderer();

			// Overlap
			m_OverlapRenderer.GridCountX = map.Size;
			m_OverlapRenderer.GridCountY = map.Size;

			return true;
		}


		public bool CheckOverlaping () {

			if (Ships == null || Ships.Length == 0) { return true; }
			if (Positions.Count < Ships.Length) {
				Positions.AddRange(new ShipPosition[Ships.Length - Positions.Count]);
			}
			m_OverlapRenderer.ClearBlock();

			bool success = true;
			var hash = new HashSet<Vector2Int>();

			// Add Stone
			if (Map.Stones != null) {
				foreach (var pos in Map.Stones) {
					var v = new Vector2Int(pos.X, pos.Y);
					if (!hash.Contains(v)) {
						hash.Add(v);
					}
				}
			}

			// Ship Overlap
			for (int i = 0; i < Ships.Length; i++) {
				var ship = Ships[i];
				var pivot = Positions[i].Pivot;
				bool flip = Positions[i].Flip;
				foreach (var pos in ship.Ship.Body) {
					var finalPos = new Vector2Int(
						pivot.X + (flip ? pos.Y : pos.X),
						pivot.Y + (flip ? pos.X : pos.Y)
					);
					if (hash.Contains(finalPos)) {
						m_OverlapRenderer.AddBlock(finalPos.x, finalPos.y, 0);
						success = false;
					} else {
						hash.Add(finalPos);
					}
				}
			}

			return success;
		}


		#endregion




		#region --- LGC ---


		private void RefreshShipRenderer () {
			if (Positions.Count < Ships.Length) {
				Positions.AddRange(new ShipPosition[Ships.Length - Positions.Count]);
			}
			m_ShipRenderer.ClearBlock();
			for (int i = 0; i < Ships.Length; i++) {
				m_ShipRenderer.AddShip(Ships[i], Positions[i]);
			}
		}


		#endregion




		#region --- UTL ---


		private List<Ship> GetShipList (ShipData[] shipDatas) {
			var result = new List<Ship>();
			foreach (var ship in shipDatas) {
				result.Add(ship.Ship);
			}
			return result;
		}


		#endregion




	}
}
