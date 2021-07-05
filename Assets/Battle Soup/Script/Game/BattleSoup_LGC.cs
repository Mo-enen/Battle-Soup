using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleSoupAI;
using Moenen.Standard;
using UIGadget;



namespace BattleSoup {
	public partial class BattleSoup {



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


		private ShipData[] GetSelectingShips (Group group) {
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
			result.Sort((a, b) => b.Ship.Body.Length.CompareTo(a.Ship.Body.Length));
			return result.ToArray();
		}


		private bool SetupAIBattleSoup (Group group, out MapData map, out ShipData[] ships, out List<ShipPosition> positions, out string error) {
			error = "";
			ships = GetSelectingShips(group);
			map = GetSelectingMap(group);
			positions = SoupAI.GetShipPosition(
				map.Size, map.Stones, ShipData.GetShips(ships)
			);
			if (positions == null || positions.Count == 0) {
				error = "Failed to position AI ships.";
				return false;
			}
			return true;
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


		private void RefreshBattleInfoUI () {
			foreach (var avatar in m_UI.BattleAvatarA) {
				avatar.sprite = m_Resource.BattleAvatars[(int)CurrentBattleMode];
			}
			foreach (var avatar in m_UI.BattleAvatarB) {
				avatar.sprite = m_Resource.BattleAvatars[1];
			}
			// Arrow
			m_UI.TurnArrowA.gameObject.SetActive(m_Game.Game.CurrentTurn == Group.A);
			m_UI.TurnArrowB.gameObject.SetActive(m_Game.Game.CurrentTurn == Group.B);

		}


		private void RefreshPanelUI (GameState state) {
			m_Panel.LogoPanel.gameObject.SetActive(true);
			m_Panel.BattlePanel.gameObject.SetActive(state == GameState.BattleMode);
			m_Panel.ShipPanel.gameObject.SetActive(state == GameState.Ship);
			m_Panel.MapPanel.gameObject.SetActive(state == GameState.Map);
			m_Panel.ShipPositionPanel.gameObject.SetActive(state == GameState.PositionShip);
			m_Panel.BattleZonePanel.gameObject.SetActive(state == GameState.Playing);
		}


		private void ReloadAbilityUI () {

			// Clear
			int childCountA = m_UI.AbilityContainerA.childCount;
			int childCountB = m_UI.AbilityContainerB.childCount;
			for (int i = 0; i < childCountA; i++) {
				DestroyImmediate(m_UI.AbilityContainerA.GetChild(0).gameObject, false);
			}
			for (int i = 0; i < childCountB; i++) {
				DestroyImmediate(m_UI.AbilityContainerB.GetChild(0).gameObject, false);
			}

			// Add New
			DoAbilityUI(m_UI.AbilityContainerA, Group.A);
			DoAbilityUI(m_UI.AbilityContainerB, Group.B);

			// Func
			void DoAbilityUI (RectTransform container, Group group) {
				var ships = GetSelectingShips(group);
				for (int i = 0; i < ships.Length; i++) {
					var ship = ships[i];
					var grabber = Instantiate(m_Game.AbilityShip, container);

					var rt = grabber.transform as RectTransform;
					rt.anchoredPosition3D = rt.anchoredPosition;
					rt.localRotation = Quaternion.identity;
					rt.localScale = Vector3.one;
					rt.SetAsLastSibling();
					rt.name = ship.name;

					var btn = grabber.Grab<Button>();
					btn.interactable = ship.Ship.Ability.Type == AbilityType.Active && ship.Ship.Ability.Cooldown <= 0;
					if (
						CurrentBattleMode == BattleMode.PvA &&
						group == Group.A &&
						ship.Ship.Ability.Type == AbilityType.Active
					) {
						btn.onClick.AddListener(() => OnAbilityClick(i, ship.Ship.Ability));
					}

					var icon = grabber.Grab<GreyImage>("Icon");
					icon.sprite = ship.Sprite;
					icon.SetGrey(ship.Ship.Ability.Type == AbilityType.Active && !btn.interactable);

					var cooldown = grabber.Grab<Text>("Cooldown");
					cooldown.gameObject.SetActive(ship.Ship.Ability.Type == AbilityType.Active);
					cooldown.text = ship.Ship.Ability.Cooldown.ToString();

					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(false);

				}
			}

		}


		private void RefreshAbilityUI (RectTransform container, BattleSoupUI soup, Group group) {
			int count = container.childCount;
			for (int i = 0; i < count; i++) {
				int cooldown = m_Game.Game.GetCooldown(group, i);
				var ability = m_Game.Game.GetAbility(group, i);

				var grabber = container.GetChild(i).GetComponent<Grabber>();
				var btn = grabber.Grab<Button>();
				btn.interactable = ability.Type == AbilityType.Active && cooldown <= 0;

				var cooldownTxt = grabber.Grab<Text>("Cooldown");
				if (cooldownTxt.gameObject.activeSelf) {
					cooldownTxt.text = cooldown > 0 ? cooldown.ToString() : "";
				}

				grabber.Grab<GreyImage>("Icon").SetGrey(
					ability.Type == AbilityType.Active && !btn.interactable
				);
				grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(
					!soup.CheckShipAlive(i)
				);

			}
		}


		// Misc
		private void ShowMessage (string msg) {
			m_UI.MessageRoot.gameObject.SetActive(true);
			m_UI.MessageText.text = msg;
		}


		private void OnAbilityClick (int shipIndex, Ability ability) {





		}


		private void PlayAudio (int index) {
			if (index >= 0 && index < m_Game.AudioSources.Length) {
				m_Game.AudioSources[index].Play(0);
			}
		}


	}
}
