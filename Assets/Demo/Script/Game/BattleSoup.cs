using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;
using Moenen.Standard;
using BattleSoup;



namespace BattleSoupDemo {
	public class BattleSoup : MonoBehaviour {




		#region --- SUB ---



		[System.Serializable]
		public struct CursorData {
			public Texture2D Cursor;
			public Vector2 Offset;
		}



		public enum GameState {
			Config = 0,
			PositionShip = 1,
			Playing = 2,
		}



		public enum BattleMode {
			PvA = 0,
			AvA = 1,
		}



		#endregion




		#region --- VAR ---


		// Ser
		[SerializeField] RectTransform m_LogoPanel = null;
		[SerializeField] RectTransform m_BattlePanel = null;
		[SerializeField] RectTransform m_ShipPanel = null;
		[SerializeField] RectTransform m_MapPanel = null;
		[SerializeField] RectTransform m_ShipPositionPanel = null;
		[SerializeField] RectTransform m_BattleZone = null;
		[SerializeField] RectTransform m_BattleInfo = null;
		[SerializeField] VectorGrid m_GridA = null;
		[SerializeField] VectorGrid m_GridB = null;
		[SerializeField] BlocksRenderer m_ShipRendererA = null;
		[SerializeField] BlocksRenderer m_ShipRendererB = null;
		[SerializeField] BlocksRenderer m_SonarRendererA = null;
		[SerializeField] BlocksRenderer m_SonarRendererB = null;
		[SerializeField] BlocksRenderer m_MapRendererA = null;
		[SerializeField] BlocksRenderer m_MapRendererB = null;
		[SerializeField] Text m_ShipLabelA = null;
		[SerializeField] Text m_ShipLabelB = null;
		[SerializeField] Text m_MapLabelA = null;
		[SerializeField] Text m_MapLabelB = null;
		[SerializeField] Button m_StartButton = null;
		[SerializeField] Text m_StartMessage = null;
		[SerializeField] Toggle[] m_ShipsToggleA = null;
		[SerializeField] Toggle[] m_ShipsToggleB = null;
		[SerializeField] Toggle[] m_MapsToggleA = null;
		[SerializeField] Toggle[] m_MapsToggleB = null;
		[SerializeField] Toggle[] m_BattleModeToggle = null;
		[SerializeField] ShipData[] m_Ships = null;
		[SerializeField] MapData[] m_Maps = null;
		[SerializeField] private CursorData[] m_Cursors = null;

		// Data
		private GameState CurrentState = GameState.Config;

		// Saving
		private readonly SavingString FleetA = new SavingString("BattleSoupDemo.Demo.FleetA", "Coracle+KillerSquid+SeaTurtle+Whale");
		private readonly SavingString FleetB = new SavingString("BattleSoupDemo.Demo.FleetB", "Coracle+KillerSquid+SeaTurtle+Whale");


		#endregion




		#region --- MSG ---


		private void Awake () {
			CursorUI.GetCursorTexture = (index) => (
				index >= 0 ? m_Cursors[index].Cursor : null,
				index >= 0 ? m_Cursors[index].Offset : Vector2.zero
			);
		}


		private void Start () {
			LoadShipSelectionFromSaving(true);
			LoadShipSelectionFromSaving(false);
			RefreshShipLabel();
			RefreshMapLabel();
			RefreshStartButton();
			SetGameState(GameState.Config);
		}


		private void Update () {
			CursorUI.GlobalUpdate();
		}


		#endregion




		#region --- API ---


		public void UI_OpenURL (string url) => Application.OpenURL(Util.GetUrl(url));


		public void UI_ShipChanged (bool groupA) {
			SaveShipSelectionToSaving(groupA);
			RefreshStartButton();
		}


		public void UI_BattleModeChanged () {
			RefreshShipLabel();
			RefreshMapLabel();
			RefreshStartButton();
		}


		public void UI_GotoNextState () {
			switch (CurrentState) {
				case GameState.Config:
					if (!RefreshStartButton()) { break; }
					if (GetSelectingBattleMode() == BattleMode.PvA) {
						SetGameState(GameState.PositionShip);
					} else {
						SetGameState(GameState.Playing);
					}
					break;
				case GameState.PositionShip:

					SetGameState(GameState.Playing);
					break;
				case GameState.Playing:

					SetGameState(GameState.Config);
					break;
			}
		}


		public void UI_GotoPrevState (bool confirm) {
			switch (CurrentState) {
				case GameState.PositionShip:
					SetGameState(GameState.Config);
					break;
				case GameState.Playing:
					if (confirm) {

					} else {
						SetGameState(GameState.Config);
					}
					break;
			}
		}


		#endregion




		#region --- LGC ---


		// Soup
		private void SetSoupSize (int width, int height) {
			m_BattleZone.SetSizeWithCurrentAnchors(
				RectTransform.Axis.Vertical,
				m_ShipRendererA.rectTransform.rect.width * height / width
			);
			m_GridA.X = width;
			m_GridA.Y = height;
			m_GridB.X = width;
			m_GridB.Y = height;
			m_ShipRendererA.GridCountX = width;
			m_ShipRendererA.GridCountY = height;
			m_ShipRendererB.GridCountX = width;
			m_ShipRendererB.GridCountY = height;
			m_SonarRendererA.GridCountX = width;
			m_SonarRendererA.GridCountY = height;
			m_SonarRendererB.GridCountX = width;
			m_SonarRendererB.GridCountY = height;
			m_MapRendererA.GridCountX = width;
			m_MapRendererA.GridCountY = height;
			m_MapRendererB.GridCountX = width;
			m_MapRendererB.GridCountY = height;
			m_BattleZone.gameObject.SetActive(false);
			m_BattleZone.gameObject.SetActive(true);
		}


		// Ship
		private void LoadShipSelectionFromSaving (bool groupA) {
			var savingHash = new HashSet<string>();
			string fleetStr = groupA ? FleetA.Value : FleetB.Value;
			if (!string.IsNullOrEmpty(fleetStr)) {
				var fleetNames = fleetStr.Split('+');
				if (fleetNames != null && fleetNames.Length > 0) {
					foreach (var _name in fleetNames) {
						savingHash.TryAdd(_name);
					}
				}
			}
			foreach (var tg in groupA ? m_ShipsToggleA : m_ShipsToggleB) {
				tg.isOn = savingHash.Contains(tg.name);
			}
		}


		private void SaveShipSelectionToSaving (bool groupA) {
			string result = "";
			foreach (var tg in groupA ? m_ShipsToggleA : m_ShipsToggleB) {
				if (tg.isOn) {
					result += string.IsNullOrEmpty(result) ? tg.name : "+" + tg.name;
				}
			}
			var saving = groupA ? FleetA : FleetB;
			saving.Value = result;
		}


		private void RefreshShipLabel () {
			var mode = GetSelectingBattleMode();
			m_ShipLabelA.text = mode == BattleMode.PvA ? "My Ships" : "Robot-A Ships";
			m_ShipLabelB.text = mode == BattleMode.PvA ? "Opponent Ships" : "Robot-B Ships";
		}


		// Map
		private void RefreshMapLabel () {
			var mode = GetSelectingBattleMode();
			m_MapLabelA.text = mode == BattleMode.PvA ? "My Map" : "Robot-A Map";
			m_MapLabelB.text = mode == BattleMode.PvA ? "Opponent Map" : "Robot-B Map";
		}


		// Config UI
		private BattleMode GetSelectingBattleMode () {
			foreach (var tg in m_BattleModeToggle) {
				if (tg.isOn && System.Enum.TryParse<BattleMode>(tg.name, out var mode)) {
					return mode;
				}
			}
			return BattleMode.PvA;
		}


		private MapData GetSelectingMap (bool groupA) {
			foreach (var tg in groupA ? m_MapsToggleA : m_MapsToggleB) {
				if (tg.isOn) {
					foreach (var map in m_Maps) {
						if (map.name == tg.name) {
							return map;
						}
					}
				}
			}
			return null;
		}


		private List<ShipData> GetSelectingShips (bool groupA) {
			var result = new List<ShipData>();
			var hash = new HashSet<string>();
			foreach (var tg in groupA ? m_ShipsToggleA : m_ShipsToggleB) {
				hash.TryAdd(tg.name);
			}
			foreach (var ship in m_Ships) {
				if (hash.Contains(ship.name)) {
					result.Add(ship);
				}
			}
			return result;
		}


		// Misc
		private bool RefreshStartButton () {

			// Has Ship Selected
			bool hasShipA = false;
			bool hasShipB = false;
			foreach (var tg in m_ShipsToggleA) {
				if (tg.isOn) { hasShipA = true; break; }
			}
			foreach (var tg in m_ShipsToggleB) {
				if (tg.isOn) { hasShipB = true; break; }
			}
			if (!hasShipA || !hasShipB) {
				m_StartButton.interactable = false;
				m_StartMessage.text = "Select ships for all players";
				return false;
			}

			// Has Map Selected
			bool hasMapA = false;
			bool hasMapB = false;
			foreach (var tg in m_MapsToggleA) {
				if (tg.isOn) { hasMapA = true; break; }
			}
			foreach (var tg in m_MapsToggleB) {
				if (tg.isOn) { hasMapB = true; break; }
			}
			if (!hasMapA || !hasMapB) {
				m_StartButton.interactable = false;
				m_StartMessage.text = "Select map for all players";
				return false;
			}

			// End
			m_StartMessage.text = "";
			m_StartButton.interactable = true;
			return true;
		}


		private void SetGameState (GameState state) {

			CurrentState = state;

			// UI
			m_LogoPanel.gameObject.SetActive(state != GameState.Playing);

			m_BattlePanel.gameObject.SetActive(state == GameState.Config);
			m_ShipPanel.gameObject.SetActive(state == GameState.Config);
			m_MapPanel.gameObject.SetActive(state == GameState.Config);

			m_ShipPositionPanel.gameObject.SetActive(state == GameState.PositionShip);

			m_BattleZone.gameObject.SetActive(state == GameState.Playing);
			m_BattleInfo.gameObject.SetActive(state == GameState.Playing);

		}


		#endregion




	}
}