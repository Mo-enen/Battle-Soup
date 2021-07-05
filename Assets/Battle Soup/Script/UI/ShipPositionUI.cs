using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using BattleSoupAI;
using Moenen.Standard;



namespace BattleSoup {
	public class ShipPositionUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {




		#region --- SUB ---


		[System.Serializable] public class VoidEvent : UnityEvent { }


		#endregion




		#region --- VAR ---

		// Api
		public MapData Map => _Map;
		public ShipData[] Ships => _Ships;
		public List<ShipPosition> Positions => _Positions;

		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] ShipRenderer m_ShipRenderer = null;
		[SerializeField] BlocksRenderer m_OverlapRenderer = null;
		[SerializeField] VoidEvent m_OnPositionChanged = null;

		// Data
		private MapData _Map = null;
		private ShipData[] _Ships = new ShipData[0];
		private readonly List<ShipPosition> _Positions = new List<ShipPosition>();
		private int DraggingShipIndex = -1;
		private Vector2Int DraggingOffset = default;
		private Vector2Int DraggingPrev = default;



		#endregion




		#region --- MSG ---


		public void OnBeginDrag (PointerEventData eData) {
			if (_Map == null || _Ships == null || eData.button != PointerEventData.InputButton.Left) { return; }
			var pos = GetMapPos(eData.position, eData.pressEventCamera);
			if (ShipData.Contains(pos.x, pos.y, _Ships, _Positions, out int index)) {
				var sPos = _Positions[index];
				DraggingShipIndex = index;
				DraggingOffset = pos - new Vector2Int(sPos.Pivot.x, sPos.Pivot.y);
				DraggingPrev = pos;
			}
		}


		public void OnDrag (PointerEventData eData) {
			if (DraggingShipIndex < 0 || eData.button != PointerEventData.InputButton.Left) { return; }
			var pos = GetMapPos(eData.position, eData.pressEventCamera);
			if (pos != DraggingPrev) {
				DraggingPrev = pos;
				var sPos = _Positions[DraggingShipIndex];
				sPos.Pivot = new Int2(pos.x - DraggingOffset.x, pos.y - DraggingOffset.y);
				_Positions[DraggingShipIndex] = sPos;
				m_OnPositionChanged.Invoke();
				RefreshShipRenderer();
				RefreshOverlapRenderer();
			}
		}


		public void OnEndDrag (PointerEventData eData) {
			if (DraggingShipIndex < 0 || eData.button != PointerEventData.InputButton.Left) { return; }
			ClampAllShipsInSoup();
			RefreshShipRenderer();
			RefreshOverlapRenderer();
			m_OnPositionChanged.Invoke();
			DraggingShipIndex = -1;
		}


		public void OnPointerDown (PointerEventData eData) {
			if (eData.button != PointerEventData.InputButton.Right) { return; }
			var pos = GetMapPos(eData.position, eData.pressEventCamera);
			if (ShipData.Contains(pos.x, pos.y, _Ships, _Positions, out int index)) {
				var sPos = _Positions[index];
				sPos.Flip = !sPos.Flip;
				_Positions[index] = sPos;
				ClampAllShipsInSoup();
				RefreshShipRenderer();
				RefreshOverlapRenderer();
				m_OnPositionChanged.Invoke();
			}
		}


		#endregion




		#region --- API ---


		public bool Init (MapData map, ShipData[] ships) {

			if (map == null || map.Size <= 0 || ships == null || ships.Length == 0) { return false; }

			// Ship
			_Ships = ships;
			m_ShipRenderer.GridCountX = map.Size;
			m_ShipRenderer.GridCountY = map.Size;

			// Map
			_Map = map;
			m_MapRenderer.LoadMap(map);

			// Pos
			for (int i = 0; i < 5; i++) {
				if (GetRandomShipPositions(_Ships, map, _Positions)) { break; }
			}
			RefreshShipRenderer();
			RefreshOverlapRenderer();
			ClampAllShipsInSoup();

			// Overlap
			m_OverlapRenderer.GridCountX = map.Size;
			m_OverlapRenderer.GridCountY = map.Size;

			return true;
		}


		public bool RefreshOverlapRenderer () => RefreshOverlapRenderer(out _);
		public bool RefreshOverlapRenderer (out string error) {
			error = "";
			if (_Ships == null || _Ships.Length == 0 || _Map == null) { return true; }
			if (_Positions.Count < _Ships.Length) {
				_Positions.AddRange(new ShipPosition[_Ships.Length - _Positions.Count]);
			}
			m_OverlapRenderer.ClearBlock();

			bool success = true;
			var hash = new HashSet<Vector2Int>();

			// Add Stone
			if (_Map.Stones != null) {
				foreach (var pos in _Map.Stones) {
					var v = new Vector2Int(pos.x, pos.y);
					if (!hash.Contains(v)) {
						hash.Add(v);
					}
				}
			}

			// Ship Overlap
			for (int i = 0; i < _Ships.Length; i++) {
				var ship = _Ships[i];
				var sPos = _Positions[i];
				var pivot = sPos.Pivot;
				bool flip = sPos.Flip;
				foreach (var pos in ship.Ship.Body) {
					var finalPos = new Vector2Int(
						pivot.x + (flip ? pos.y : pos.x),
						pivot.y + (flip ? pos.x : pos.y)
					);
					if (hash.Contains(finalPos)) {
						m_OverlapRenderer.AddBlock(finalPos.x, finalPos.y, 0);
						error = "Ships can not overlap with stone or other ship.";
						success = false;
					} else {
						hash.Add(finalPos);
					}
				}
			}

			// Ship Outside
			int mapSize = _Map.Size;
			for (int i = 0; i < _Ships.Length; i++) {
				var ship = _Ships[i];
				var sPos = _Positions[i];
				var pivot = sPos.Pivot;
				bool flip = sPos.Flip;
				foreach (var pos in ship.Ship.Body) {
					var finalPos = new Vector2Int(
						pivot.x + (flip ? pos.y : pos.x),
						pivot.y + (flip ? pos.x : pos.y)
					);
					if (finalPos.x < 0 || finalPos.x >= mapSize || finalPos.y < 0 || finalPos.y >= mapSize) {
						m_OverlapRenderer.AddBlock(finalPos.x, finalPos.y, 0);
						error = "Ships can not be outside the map";
						success = false;
					}
				}
			}

			m_OverlapRenderer.SetVerticesDirty();
			return success;
		}


		#endregion




		#region --- LGC ---


		private void RefreshShipRenderer () {
			if (_Positions.Count < _Ships.Length) {
				_Positions.AddRange(new ShipPosition[_Ships.Length - _Positions.Count]);
			}
			m_ShipRenderer.ClearBlock();
			for (int i = 0; i < _Ships.Length; i++) {
				m_ShipRenderer.AddShip(_Ships[i], _Positions[i]);
			}
			m_ShipRenderer.SetVerticesDirty();
		}


		private void ClampAllShipsInSoup () {
			if (_Map == null || _Map.Size <= 0) { return; }
			int mapSize = _Map.Size;
			for (int i = 0; i < _Ships.Length; i++) {
				var ship = _Ships[i];
				var sPos = _Positions[i];
				var (min, max) = ship.Ship.GetBounds(sPos);
				sPos.Pivot.x = Mathf.Clamp(sPos.Pivot.x, -min.x, mapSize - max.x - 1);
				sPos.Pivot.y = Mathf.Clamp(sPos.Pivot.y, -min.y, mapSize - max.y - 1);
				_Positions[i] = sPos;
			}
		}


		#endregion




		#region --- UTL ---


		private Vector2Int GetMapPos (Vector2 screenPos, Camera eCamera) {
			var pos01 = (transform as RectTransform).Get01Position(screenPos, eCamera);
			return new Vector2Int(Mathf.FloorToInt(pos01.x * _Map.Size), Mathf.FloorToInt(pos01.y * _Map.Size));
		}


		private bool GetRandomShipPositions (ShipData[] ships, MapData map, List<ShipPosition> result) {

			if (ships == null || ships.Length == 0 || map == null || map.Size <= 0) { return false; }
			bool success = true;
			int mapSize = map.Size;

			// Get Hash
			var hash = new HashSet<Int2>();
			foreach (var stone in map.Stones) {
				if (!hash.Contains(stone)) {
					hash.Add(stone);
				}
			}

			// Get Result
			result.Clear();
			var random = new System.Random(System.DateTime.Now.Millisecond);
			foreach (var ship in ships) {
				random = new System.Random(random.Next());
				var sPos = new ShipPosition();
				var basicPivot = new Int2(random.Next(0, mapSize), random.Next(0, mapSize));
				bool shipSuccess = false;
				// Try Fix Overlap
				for (int j = 0; j < mapSize; j++) {
					for (int i = 0; i < mapSize; i++) {
						sPos.Pivot = new Int2(
							(basicPivot.x + i) % mapSize,
							(basicPivot.y + j) % mapSize
						);
						sPos.Flip = false;
						if (PositionAvailable(ship.Ship, sPos)) {
							AddShipIntoHash(ship.Ship, sPos);
							shipSuccess = true;
							i = mapSize;
							j = mapSize;
							break;
						}
						sPos.Flip = true;
						if (PositionAvailable(ship.Ship, sPos)) {
							AddShipIntoHash(ship.Ship, sPos);
							shipSuccess = true;
							i = mapSize;
							j = mapSize;
							break;
						}
					}
				}
				if (!shipSuccess) { success = false; }
				result.Add(sPos);
			}
			return success;
			// Func
			bool PositionAvailable (Ship _ship, ShipPosition _pos) {
				// Border Check
				var (min, max) = _ship.GetBounds(_pos);
				if (_pos.Pivot.x < -min.x || _pos.Pivot.x > mapSize - max.x - 1 ||
					_pos.Pivot.y < -min.y || _pos.Pivot.y > mapSize - max.y - 1
				) {
					return false;
				}
				// Overlap Check
				foreach (var v in _ship.Body) {
					if (hash.Contains(new Int2(
						_pos.Pivot.x + (_pos.Flip ? v.y : v.x),
						_pos.Pivot.y + (_pos.Flip ? v.x : v.y)
					))) {
						return false;
					}
				}
				return true;
			}
			void AddShipIntoHash (Ship _ship, ShipPosition _pos) {
				foreach (var v in _ship.Body) {
					var shipPosition = new Int2(
						_pos.Pivot.x + (_pos.Flip ? v.y : v.x),
						_pos.Pivot.y + (_pos.Flip ? v.x : v.y)
					);
					if (!hash.Contains(shipPosition)) {
						hash.Add(shipPosition);
					}
				}
			}
		}


		#endregion




	}
}
