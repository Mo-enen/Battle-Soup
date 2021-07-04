using System.Collections;
using System.Collections.Generic;
using Moenen.Standard;
using UnityEngine;



namespace BattleSoup {
	public partial class BattleSoup {


		// Game State
		private void SetGameState (GameState state) {
			CurrentState = state;
			m_Panel.LogoPanel.gameObject.SetActive(true);
			m_Panel.BattlePanel.gameObject.SetActive(state == GameState.BattleMode);
			m_Panel.ShipPanel.gameObject.SetActive(state == GameState.Ship);
			m_Panel.MapPanel.gameObject.SetActive(state == GameState.Map);
			m_Panel.ShipPositionPanel.gameObject.SetActive(state == GameState.PositionShip);
			m_Panel.BattleZonePanel.gameObject.SetActive(state == GameState.Playing);
		}


		// Ship
		private void LoadShipSelectionFromSaving (Group group) {
			var savingHash = new HashSet<string>();
			string fleetStr = group == Group.A ? SelectedFleetA.Value : SelectedFleetB.Value;
			if (!string.IsNullOrEmpty(fleetStr)) {
				var fleetNames = fleetStr.Split('+');
				if (fleetNames != null && fleetNames.Length > 0) {
					foreach (var _name in fleetNames) {
						savingHash.TryAdd(_name);
					}
				}
			}
			foreach (var tg in group == Group.A ? m_ShipsToggleA : m_ShipsToggleB) {
				tg.isOn = savingHash.Contains(tg.name);
			}
		}


		private void SaveShipSelectionToSaving (Group group) {
			string result = "";
			foreach (var tg in group == Group.A ? m_ShipsToggleA : m_ShipsToggleB) {
				if (tg.isOn) {
					result += string.IsNullOrEmpty(result) ? tg.name : "+" + tg.name;
				}
			}
			var saving = group == Group.A ? SelectedFleetA : SelectedFleetB;
			saving.Value = result;
		}


		private List<ShipData> GetSelectingShips (Group group) {
			var result = new List<ShipData>();
			var hash = new HashSet<string>();
			foreach (var tg in group == Group.A ? m_ShipsToggleA : m_ShipsToggleB) {
				if (tg.isOn) {
					hash.TryAdd(tg.name);
				}
			}
			foreach (var ship in m_Resource.Ships) {
				if (hash.Contains(ship.name)) {
					result.Add(ship);
				}
			}
			return result;
		}


		// Map
		private void LoadMapSelectionFromSaving (Group group) {
			var savingIndex = group == Group.A ? SelectedMapA : SelectedMapB;
			var toggles = group == Group.A ? m_MapsToggleA : m_MapsToggleB;
			if (savingIndex.Value >= 0 && savingIndex.Value < toggles.Length) {
				toggles[savingIndex.Value].SetIsOnWithoutNotify(true);
			} else {
				toggles[0].SetIsOnWithoutNotify(true);
			}
		}


		private void SaveMapSelectionToSaving (Group group) {
			var savingIndex = group == Group.A ? SelectedMapA : SelectedMapB;
			int index = 0;
			foreach (var tg in group == Group.A ? m_MapsToggleA : m_MapsToggleB) {
				if (tg.isOn) {
					savingIndex.Value = index;
					break;
				}
				index++;
			}
		}


		private MapData GetSelectingMap (Group group) {
			foreach (var tg in group == Group.A ? m_MapsToggleA : m_MapsToggleB) {
				if (tg.isOn) {
					foreach (var map in m_Resource.Maps) {
						if (map.name == tg.name) {
							return map;
						}
					}
				}
			}
			return null;
		}


		// Refresh UI
		private bool RefreshShipButton () {

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
				m_UI.StartButton_Ship.interactable = false;
				m_UI.StartMessage_Ship.text = "Select ships for all players";
				return false;
			}

			// End
			m_UI.StartButton_Ship.interactable = true;
			m_UI.StartMessage_Ship.text = "";
			return true;
		}


		private bool RefreshMapButton () {

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
				m_UI.StartButton_Map.interactable = false;
				m_UI.StartMessage_Map.text = "Select map for all players";
				return false;
			}

			// End
			m_UI.StartMessage_Map.text = "";
			m_UI.StartButton_Map.interactable = true;
			return true;
		}


		private bool RefreshShipPositionButton () {
			if (!m_Game.ShipPositionUI.RefreshOverlapRenderer(out string error)) {
				m_UI.StartButton_ShipPos.interactable = false;
				m_UI.StartMessage_ShipPos.text = error;
				return false;
			}
			m_UI.StartButton_ShipPos.interactable = true;
			m_UI.StartMessage_ShipPos.text = "";
			return true;
		}


		private void RefreshBattleInfo () {
			foreach (var avatar in m_UI.BattleAvatarA) {
				avatar.sprite = m_Resource.BattleAvatars[(int)CurrentBattleMode];
			}
			foreach (var avatar in m_UI.BattleAvatarB) {
				avatar.sprite = m_Resource.BattleAvatars[1];
			}
		}


	}
}
