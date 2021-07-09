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
		public delegate List<SonarPosition> SonarPositionListHandler ();
		public delegate int IntHandler ();
		public delegate bool BoolIntHandler (int index);


		#endregion




		#region --- VAR ---


		// Api
		public TileIntIntHandler GetTile { get; set; } = null;
		public MapDataHandler GetMap { get; set; } = null;
		public ShipDatasHandler GetShips { get; set; } = null;
		public ShipPositionsHandler GetPositions { get; set; } = null;
		public SonarPositionListHandler GetSonars { get; set; } = null;
		public BoolIntHandler CheckShipAlive { get; set; } = null;


		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] BlocksRenderer m_SonarRenderer = null;
		[SerializeField] ShipRenderer m_ShipsRenderer = null;
		[SerializeField] BlocksRenderer m_HitRenderer = null;
		[SerializeField] SoupHighlightUI m_Highlight = null;


		#endregion




		#region --- API ---


		public void Init () {

			m_MapRenderer.ClearBlock();
			m_HitRenderer.ClearBlock();
			m_SonarRenderer.ClearBlock();
			m_ShipsRenderer.ClearBlock();

			var map = GetMap();

			// Renderer
			m_MapRenderer.LoadMap(map);
			m_MapRenderer.GridCountX = m_MapRenderer.GridCountY = map.Size;
			m_SonarRenderer.GridCountX = m_SonarRenderer.GridCountY = map.Size;
			m_ShipsRenderer.GridCountX = m_ShipsRenderer.GridCountY = map.Size;
			m_HitRenderer.GridCountX = m_HitRenderer.GridCountY = map.Size;

			RefreshShipRenderer();
			RefreshHitRenderer();
		}


		public void RefreshShipRenderer (bool sunkOnly = false) {
			var ships = GetShips();
			var positions = GetPositions();
			m_ShipsRenderer.ClearBlock();
			for (int i = 0; i < ships.Length; i++) {
				if (sunkOnly && CheckShipAlive(i)) { continue; }
				m_ShipsRenderer.AddShip(
					ships[i], positions[i], Color.HSVToRGB((float)i / ships.Length, 0.618f, 0.618f)
				);
			}
			m_ShipsRenderer.SetVerticesDirty();
		}


		public void RefreshHitRenderer () {
			m_HitRenderer.ClearBlock();
			var map = GetMap();
			int size = map.Size;
			for (int i = 0; i < size; i++) {
				for (int j = 0; j < size; j++) {
					var tile = GetTile(i, j);
					if (tile == Tile.HittedShip) {
						m_HitRenderer.AddBlock(i, j, 0);
					} else if (tile == Tile.RevealedWater) {
						m_HitRenderer.AddBlock(i, j, 1);
					} else if (tile == Tile.RevealedShip) {
						m_HitRenderer.AddBlock(i, j, 2);
					}
				}
			}
			m_HitRenderer.SetVerticesDirty();
		}


		public void RefreshSonarRenderer () {
			m_SonarRenderer.ClearBlock();
			var sonars = GetSonars();
			for (int i = 0; i < sonars.Count; i++) {
				var sonar = sonars[i];
				m_SonarRenderer.AddBlock(
					sonar.x, sonar.y,
					Mathf.Clamp(sonar.number - 1, 0, m_SonarRenderer.BlockSpriteCount - 1)
				);
			}
			m_SonarRenderer.SetVerticesDirty();
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


		public bool GetMapPositionInside (Vector2 screenPos, out Vector2Int pos) {
			var map = GetMap();
			var pos01 = (transform as RectTransform).Get01Position(screenPos, Camera.main);
			pos = new Vector2Int(Mathf.FloorToInt(pos01.x * map.Size), Mathf.FloorToInt(pos01.y * map.Size));
			return pos01.x > 0f && pos01.x < 1f && pos01.y > 0f && pos01.y < 1f;
		}


		public void Blink (int x, int y, Color color, Sprite sprite) => m_Highlight.Blink(x, y, color, sprite);


		#endregion




		#region --- LGC ---




		#endregion




	}
}
