using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;
using Moenen.Standard;



namespace BattleSoup {
	public class BattleSoupUI : MonoBehaviour {




		#region --- SUB ---


		public delegate Tile TileIntIntHandler (int x, int y);
		public delegate MapData MapDataHandler ();
		public delegate ShipData[] ShipDatasHandler ();
		public delegate List<ShipPosition> ShipPositionsHandler ();
		public delegate int IntHandler ();


		#endregion




		#region --- VAR ---


		// Api
		public TileIntIntHandler GetTile { get; set; } = null;
		public MapDataHandler GetMap { get; set; } = null;
		public ShipDatasHandler GetShips { get; set; } = null;
		public ShipPositionsHandler GetPositions { get; set; } = null;

		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] BlocksRenderer m_SonarRenderer = null;
		[SerializeField] ShipRenderer m_ShipsRenderer = null;
		[SerializeField] BlocksRenderer m_HitRenderer = null;


		#endregion




		#region --- API ---


		public void Init () {

			m_MapRenderer.ClearBlock();
			m_HitRenderer.ClearBlock();
			m_SonarRenderer.ClearBlock();
			m_ShipsRenderer.ClearBlock();

			var map = GetMap();

			// Renderer
			m_MapRenderer.LoadMap(map, GetShips(), GetPositions());
			m_MapRenderer.GridCountX = m_MapRenderer.GridCountY = map.Size;
			m_SonarRenderer.GridCountX = m_SonarRenderer.GridCountY = map.Size;
			m_ShipsRenderer.GridCountX = m_ShipsRenderer.GridCountY = map.Size;
			m_HitRenderer.GridCountX = m_HitRenderer.GridCountY = map.Size;

			RefreshShipRenderer();
			RefreshHitRenderer();
		}


		public void RefreshShipRenderer () {
			var ships = GetShips();
			var positions = GetPositions();
			m_ShipsRenderer.ClearBlock();
			for (int i = 0; i < ships.Length; i++) {
				m_ShipsRenderer.AddShip(ships[i], positions[i]);
			}
			m_ShipsRenderer.SetVerticesDirty();
		}


		public void RefreshHitRenderer () {
			m_HitRenderer.ClearBlock();
			var map = GetMap();
			int size = map.Size;
			for (int i = 0; i < size; i++) {
				for (int j = 0; j < size; j++) {
					if (GetTile(i, j).HasFlag(Tile.HittedShip)) {
						m_HitRenderer.AddBlock(i, j, 0);
					}
				}
			}
			m_HitRenderer.SetVerticesDirty();
		}


		public void Clear () {
			m_MapRenderer.ClearBlock();
			m_SonarRenderer.ClearBlock();
			m_ShipsRenderer.ClearBlock();
			m_HitRenderer.ClearBlock();
			m_MapRenderer.SetVerticesDirty();
			m_SonarRenderer.SetVerticesDirty();
			m_ShipsRenderer.SetVerticesDirty();
			m_HitRenderer.SetVerticesDirty();
		}


		public bool CheckShipAlive (int index) {
			var ships = GetShips();
			var positions = GetPositions();
			index = Mathf.Clamp(index, 0, ships.Length - 1);
			var body = ships[index].Ship.Body;
			var sPos = positions[index];
			var map = GetMap();
			foreach (var v in body) {
				var pos = new Vector2Int(
					sPos.Pivot.x + (sPos.Flip ? v.y : v.x),
					sPos.Pivot.y + (sPos.Flip ? v.x : v.y)
				);
				if (pos.x >= 0 && pos.x < map.Size && pos.y >= 0 && pos.y < map.Size) {
					if (!GetTile(pos.x, pos.y).HasFlag(Tile.HittedShip)) {
						return true;
					}
				}
			}
			return false;
		}


		public bool GetMapPositionInside (Vector2 screenPos, out Vector2Int pos) {
			var map = GetMap();
			var pos01 = (transform as RectTransform).Get01Position(screenPos, Camera.main);
			pos = new Vector2Int(Mathf.FloorToInt(pos01.x * map.Size), Mathf.FloorToInt(pos01.y * map.Size));
			return pos01.x > 0f && pos01.x < 1f && pos01.y > 0f && pos01.y < 1f;
		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}
