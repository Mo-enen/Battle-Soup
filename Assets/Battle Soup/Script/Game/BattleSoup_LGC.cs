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
		private void ReloadShipToggle (Group group) {

			var container = group == Group.A ? m_UI.ShipsToggleContainerA : m_UI.ShipsToggleContainerB;
			var shipMap = m_Game.Asset.ShipMap;

			// Clear UI
			var template = container.GetChild(0) as RectTransform;
			template.SetParent(null);
			int childCount = container.childCount;
			for (int i = 0; i < childCount; i++) {
				DestroyImmediate(container.GetChild(0).gameObject, false);
			}

			// Create UI
			foreach (var pair in shipMap) {
				var ship = pair.Value;
				var rt = Instantiate(template.gameObject, container).transform as RectTransform;
				rt.anchoredPosition3D = rt.anchoredPosition;
				rt.localScale = Vector3.one;
				rt.localRotation = Quaternion.identity;
				rt.SetAsLastSibling();
				rt.gameObject.name = pair.Key;
				// Label
				rt.Find("Label").GetComponent<Text>().text = ship.DisplayName;
				rt.GetComponent<Toggle>().isOn = false;
				rt.Find("Thumbnail").GetComponent<Image>().sprite = ship.Sprite;
			}

			// Final
			DestroyImmediate(template.gameObject, false);

		}


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
			var toggles = group == Group.A ? m_ShipsToggleA : m_ShipsToggleB;
			for (int i = 0; i < toggles.Length; i++) {
				var tg = toggles[i];
				if (tg.isOn) {
					hash.TryAdd(tg.name);
				}
			}
			foreach (var pair in m_Game.Asset.ShipMap) {
				if (hash.Contains(pair.Key)) {
					result.Add(pair.Value);
				}
			}
			result.Sort((a, b) => b.Ship.Body.Length.CompareTo(a.Ship.Body.Length));
			return result.ToArray();
		}


		private bool SetupAIBattleSoup (Group group, out MapData map, out ShipData[] shipDatas, out ShipPosition[] positions, out string error) {
			error = "";
			shipDatas = GetSelectingShips(group);
			var ships = ShipData.GetShips(shipDatas);
			map = GetSelectingMap(group);
			positions = null;
			for (int i = 0; i < 36; i++) {
				if (SoupStrategy.GetRandomShipPositions(map.Size, ships, map.Stones, out positions)) { break; }
			}
			if (positions == null || positions.Length == 0) {
				error = "Failed to position AI ships.";
				return false;
			}
			return true;
		}


		// Map
		private void ReloadMapToggle (Group group) {

			var container = group == Group.A ? m_UI.MapsToggleContainerA : m_UI.MapsToggleContainerB;
			var maps = m_Game.Asset.MapDatas;

			// Clear UI
			var template = container.GetChild(0) as RectTransform;
			template.SetParent(null);
			int childCount = container.childCount;
			for (int i = 0; i < childCount; i++) {
				DestroyImmediate(container.GetChild(0).gameObject, false);
			}

			// Create UI
			foreach (var map in maps) {
				var rt = Instantiate(template.gameObject, container).transform as RectTransform;
				rt.anchoredPosition3D = rt.anchoredPosition;
				rt.localScale = Vector3.one;
				rt.localRotation = Quaternion.identity;
				rt.SetAsLastSibling();
				rt.gameObject.name = $"Map {rt.GetSiblingIndex()}";
				// Label
				rt.GetComponent<Toggle>().isOn = false;
				rt.Find("Label").GetComponent<Text>().text = $"{map.Size}¡Á{map.Size}";
				rt.Find("Thumbnail").GetComponent<MapRenderer>().LoadMap(map);
				var grid = rt.Find("Grid").GetComponent<VectorGrid>();
				grid.X = map.Size;
				grid.Y = map.Size;
			}

			// Final
			DestroyImmediate(template.gameObject, false);
		}


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
			var toggles = group == Group.A ? m_MapsToggleA : m_MapsToggleB;
			for (int i = 0; i < toggles.Length; i++) {
				var tg = toggles[i];
				if (tg.isOn && i < m_Game.Asset.MapDatas.Count) {
					return m_Game.Asset.MapDatas[i];
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
			m_Panel.ShipEditorPanel.gameObject.SetActive(state == GameState.ShipEditor);
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
					rt.name = ship.DisplayName;

					var btn = grabber.Grab<Button>();
					btn.interactable = ship.Ship.Ability.HasActive && ship.Ship.Ability.Cooldown <= 0;
					if (
						CurrentBattleMode == BattleMode.PvA &&
						group == Group.A &&
						(ship.Ship.Ability.HasActive || ship.Ship.Ability.CopyOpponentLastUsed)
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


		private void RefreshStrategyUI () {
			m_UI.StrategyDescriptionA.text = Strategies[Mathf.Clamp(StrategyIndexA.Value, 0, Strategies.Count - 1)].Description;
			m_UI.StrategyDescriptionB.text = Strategies[Mathf.Clamp(StrategyIndexB.Value, 0, Strategies.Count - 1)].Description;
		}


		// Misc
		private void ShowMessage (string msg) {
			m_UI.MessageRoot.gameObject.SetActive(true);
			m_UI.MessageText.text = msg;
		}


		private void FixContainerVerticalSize (RectTransform containerA, RectTransform containerB) {
			var rt = containerA.parent as RectTransform;
			var gridA = containerA.GetComponent<GridLayoutGroup>();
			var gridB = containerB.GetComponent<GridLayoutGroup>();
			var fitterA = containerA.GetComponent<ContentSizeFitter>();
			var fitterB = containerB.GetComponent<ContentSizeFitter>();
			gridA.CalculateLayoutInputHorizontal();
			gridA.CalculateLayoutInputVertical();
			gridA.SetLayoutHorizontal();
			gridA.SetLayoutVertical();
			gridB.CalculateLayoutInputHorizontal();
			gridB.CalculateLayoutInputVertical();
			gridB.SetLayoutHorizontal();
			gridB.SetLayoutVertical();
			fitterA.SetLayoutVertical();
			fitterB.SetLayoutVertical();
			float height = Mathf.Max(containerA.rect.height, containerB.rect.height);
			if (rt.rect.height < height) {
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			}
		}


	}
}
