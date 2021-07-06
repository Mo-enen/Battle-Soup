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


		public void Init (MapData map, ShipData[] ships, List<ShipPosition> positions) {

			if (map == null || map.Size <= 0 || ships == null || ships.Length == 0) { return; }

			// Ship
			_Ships = ships;
			m_ShipRenderer.GridCountX = map.Size;
			m_ShipRenderer.GridCountY = map.Size;

			// Map
			_Map = map;
			m_MapRenderer.LoadMap(map);

			// Pos
			_Positions.Clear();
			_Positions.AddRange(positions);
			RefreshShipRenderer();
			RefreshOverlapRenderer();
			ClampAllShipsInSoup();

			// Overlap
			m_OverlapRenderer.GridCountX = map.Size;
			m_OverlapRenderer.GridCountY = map.Size;

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
				m_ShipRenderer.AddShip(
					_Ships[i], _Positions[i], Color.HSVToRGB((float)i / _Ships.Length, 0.618f, 0.618f)
				);
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




		#endregion




	}
}
