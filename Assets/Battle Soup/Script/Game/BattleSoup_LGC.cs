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
			//positions = SoupAI.GetShipPosition(
			//	map.Size, map.Stones, ShipData.GetShips(ships)
			//);
			positions = new List<ShipPosition>();
			for (int i = 0; i < 36; i++) {
				if (GetRandomShipPositions(ships, map, positions)) { break; }
			}
			if (positions == null || positions.Count == 0) {
				error = "Failed to position AI ships.";
				return false;
			}
			return true;
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
			if (!success) {
				result.Clear();
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


		// Map
		private void LoadMapSelectionFromSaving (Group group) {
			var savingIndex = group == Group.A ? SelectedMapA : SelectedMapB;
			var toggles = group == Group.A ? m_MapsToggleA : m_MapsToggleB;
			for (int i = 0; i < toggles.Length; i++) {
				toggles[i].SetIsOnWithoutNotify(i == savingIndex.Value);
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
				avatar.sprite = m_Resource.BattleAvatars[CurrentBattleMode == BattleMode.AvA || !m_Game.Game.Cheated ? 1 : 2];
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
					btn.interactable = ship.Ship.Ability.HasActive && ship.Ship.Ability.Cooldown <= 0;
					if (
						CurrentBattleMode == BattleMode.PvA &&
						group == Group.A &&
						ship.Ship.Ability.HasActive
					) {
						int _index = i;
						btn.onClick.AddListener(() => m_Game.Game.OnAbilityClick(_index));
					}

					var icon = grabber.Grab<GreyImage>("Icon");
					icon.sprite = ship.Sprite;
					icon.SetGrey(ship.Ship.Ability.HasActive && !btn.interactable);

					var cooldown = grabber.Grab<Text>("Cooldown");
					cooldown.gameObject.SetActive(ship.Ship.Ability.HasActive);
					cooldown.text = ship.Ship.Ability.Cooldown.ToString();

					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(false);

					grabber.Grab<Image>("Copy").gameObject.SetActive(false);

				}
			}

		}


		// Misc
		private void ShowMessage (string msg) {
			m_UI.MessageRoot.gameObject.SetActive(true);
			m_UI.MessageText.text = msg;
		}


	}
}
