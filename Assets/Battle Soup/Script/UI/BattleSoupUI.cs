using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleSoupAI;
using Moenen.Standard;



namespace BattleSoup {
	public class BattleSoupUI : MonoBehaviour {




		#region --- SUB ---


		public delegate Tile TileIntIntHandler (int x, int y);
		public delegate MapData MapDataHandler ();
		public delegate ShipData[] ShipDatasHandler ();
		public delegate ShipData ShipDataHandler ();
		public delegate ShipPosition[] ShipPositionsHandler ();
		public delegate List<SonarPosition> SonarPositionsHandler ();
		public delegate int IntHandler ();
		public delegate bool BoolIntHandler (int index);
		public delegate bool BoolHandler ();
		public delegate Ability AbilityHandler ();
		public delegate AbilityDirection AbilityDirectionHandler ();
		public delegate string StringHandler ();


		#endregion




		#region --- VAR ---


		// Api
		public TileIntIntHandler GetTile { get; set; } = null;
		public MapDataHandler GetMap { get; set; } = null;
		public ShipDatasHandler GetShips { get; set; } = null;
		public ShipPositionsHandler GetPositions { get; set; } = null;
		public SonarPositionsHandler GetSonars { get; set; } = null;
		public BoolIntHandler CheckShipAlive { get; set; } = null;
		public BoolIntHandler CheckShipKnown { get; set; } = null;
		public AbilityHandler GetCurrentAbility { get; set; } = null;
		public AbilityDirectionHandler GetCurrentAbilityDirection { get; set; } = null;
		public BoolHandler GetCheating { get; set; } = null;
		public ShipDataHandler GetPrevUseShip { get; set; } = null;
		public bool SunkOnly { get; set; } = false;
		public bool UseAbilityHint { get; set; } = true;

		// Ser
		[SerializeField] MapRenderer m_MapRenderer = null;
		[SerializeField] BlocksRenderer m_SonarRenderer = null;
		[SerializeField] ShipRenderer m_ShipsRenderer = null;
		[SerializeField] BlocksRenderer m_HitRenderer = null;
		[SerializeField] SoupHighlightUI m_Highlight = null;
		[SerializeField] BlocksRenderer m_AimRenderer = null;
		[SerializeField] Image m_AbilityIcon = null;
		[SerializeField] RectTransform m_AbilityAimHint = null;

		// Data
		private Vector2Int? PrevMousePosForAim = null;
		private AbilityDirection PrevAbilityDirection = AbilityDirection.Up;


		#endregion




		#region --- API ---


		public void Init (bool sunkOnly) {

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
			m_AimRenderer.GridCountX = m_AimRenderer.GridCountY = map.Size;
			SunkOnly = sunkOnly;

			RefreshShipRenderer();
			RefreshHitRenderer();
		}


		public void RefreshShipRenderer () {
			var ships = GetShips();
			var positions = GetPositions();
			bool cheating = GetCheating();
			m_ShipsRenderer.ClearBlock();
			for (int i = 0; i < ships.Length; i++) {
				bool superRevealed = CheckShipKnown(i);
				if (!cheating && !superRevealed && SunkOnly && CheckShipAlive(i)) { continue; }
				var color = Color.HSVToRGB((float)i / ships.Length, 0.618f, 0.618f);
				color.a = superRevealed ? 0.618f : 1f;
				m_ShipsRenderer.AddShip(ships[i], positions[i], color, !superRevealed);
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
					} else if (tile == Tile.RevealedWater || tile == Tile.RevealedStone) {
						m_HitRenderer.AddBlock(i, j, 1);
					} else if (tile == Tile.RevealedShip) {
						m_HitRenderer.AddBlock(i, j, 2);
					} else if (tile == Tile.SunkShip) {
						m_HitRenderer.AddBlock(i, j, 3);
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


		public void SetAbilityAimIcon (Sprite icon) {
			m_AbilityIcon.gameObject.SetActive(icon != null);
			m_AbilityIcon.sprite = icon;
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


		public void Blink (int x, int y, Color color, Sprite sprite, float alpha = 0f, float duration = 0.618f, int count = 4) => m_Highlight.Blink(x, y, color, sprite, alpha, duration, count);


		public void ClearBlinks () => m_Highlight.ClearAllBlinks();


		public void RefreshAimRenderer () {
			var ability = GetCurrentAbility();
			bool inside = false;
			if (ability != null) {
				if (ability.NeedAim && GetMapPositionInside(Input.mousePosition, out var pos)) {
					inside = true;
					var dir = GetCurrentAbilityDirection();
					if (!PrevMousePosForAim.HasValue || PrevMousePosForAim.Value != pos || PrevAbilityDirection != dir) {
						var map = GetMap();
						PrevMousePosForAim = pos;
						PrevAbilityDirection = dir;
						m_AimRenderer.ClearBlock();
						var attacks = ability.Attacks;
						if (ability.CopyOpponentLastUsed) {
							var oppPrevShip = GetPrevUseShip();
							if (oppPrevShip != null) {
								attacks = oppPrevShip.Ship.Ability.Attacks;
							}
						}
						foreach (var att in attacks) {
							if (att.Type != AttackType.HitTile && att.Type != AttackType.HitWholeShip) { continue; }
							if (att.Trigger != AttackTrigger.Picked && att.Trigger != AttackTrigger.TiedUp) { continue; }
							var (x, y) = att.GetPosition(pos.x, pos.y, dir);
							if (x >= 0 && x < map.Size && y >= 0 && y < map.Size) {
								if (x != pos.x || y != pos.y) {
									m_AimRenderer.AddBlock(x, y, 0);
								}
							}
						}
					}
				}
				m_AimRenderer.SetVerticesDirty();
			}
			if (!inside) {
				m_AimRenderer.ClearBlock();
				if (PrevMousePosForAim.HasValue) {
					PrevMousePosForAim = null;
					m_AimRenderer.SetVerticesDirty();
				}
			}
			bool showHint = ability != null && UseAbilityHint;
			if (m_AbilityAimHint != null && m_AbilityAimHint.gameObject.activeSelf != showHint) {
				m_AbilityAimHint.gameObject.SetActive(showHint);
			}
		}


		public void ClearAimRenderer () {
			m_AimRenderer.ClearBlock();
			m_AimRenderer.SetVerticesDirty();
			PrevMousePosForAim = null;
			if (m_AbilityAimHint != null && m_AbilityAimHint.gameObject.activeSelf) {
				m_AbilityAimHint.gameObject.SetActive(false);
			}
		}


		#endregion



	}
}
