using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BattleSoupAI;
using Moenen.Standard;
using UIGadget;



namespace BattleSoup {
	public partial class BattleSoup {


		// Ship
		private void ReloadShipButtons () {

			var container = m_UI.ShipsButtonContainer;
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
				var grab = rt.GetComponent<Grabber>();
				grab.Grab<Button>().onClick.AddListener(() => AddShipToSelection(rt.name));
				grab.Grab<Text>("Label").text = ship.DisplayName;
				grab.Grab<Image>("Thumbnail").sprite = ship.Sprite;
				grab.Grab<TipUI>().Content = ship.Description;
			}

			// Final
			DestroyImmediate(template.gameObject, false);

		}


		private void AddShipToSelection (string id) {
			var shipData = m_Game.Asset.GetShipData(id);
			if (shipData == null) { return; }
			var grab = Instantiate(m_UI.ShipSelectionItem, m_UI.ShipSelectionContainer);
			var rt = grab.transform as RectTransform;
			rt.anchoredPosition3D = rt.anchoredPosition;
			rt.localScale = Vector3.one;
			rt.localRotation = Quaternion.identity;
			rt.SetAsLastSibling();
			rt.name = id;
			grab.Grab<Button>().onClick.AddListener(() => RemoveShipFromSelection(rt.GetSiblingIndex()));
			grab.Grab<Image>("Icon").sprite = shipData.Sprite;
			RefreshShipButton();
		}


		private void RemoveShipFromSelection (int index) {
			int len = m_UI.ShipSelectionContainer.childCount;
			if (index < 0 || index >= len) { return; }
			DestroyImmediate(m_UI.ShipSelectionContainer.GetChild(index).gameObject, false);
			RefreshShipButton();
		}


		private void ClearShipSelection () {
			int len = m_UI.ShipSelectionContainer.childCount;
			for (int i = 0; i < len; i++) {
				DestroyImmediate(m_UI.ShipSelectionContainer.GetChild(0).gameObject, false);
			}
			RefreshShipButton();
		}


		private void LoadShipSelectionFromSaving () {
			string fleetStr = SelectedFleet.Value;
			if (!string.IsNullOrEmpty(fleetStr)) {
				var fleetNames = fleetStr.Split('+');
				if (fleetNames != null && fleetNames.Length > 0) {
					ClearShipSelection();
					foreach (var _name in fleetNames) {
						AddShipToSelection(_name);
					}
				}
			}
		}


		private void SaveShipSelectionToSaving () {
			string result = "";
			foreach (RectTransform rt in m_UI.ShipSelectionContainer) {
				result += string.IsNullOrEmpty(result) ? rt.name : "+" + rt.name;
			}
			SelectedFleet.Value = result;
		}


		private ShipData[] GetSelectingShips () {
			var result = new List<ShipData>();
			foreach (RectTransform rt in m_UI.ShipSelectionContainer) {
				var data = m_Game.Asset.GetShipData(rt.name);
				if (data != null) {
					result.Add(data);
				}
			}
			return result.ToArray();
		}


		private ShipData[] GetStrategyShips (SoupStrategy strategy) {
			var fleetID = strategy.FleetID;
			if (fleetID == null || fleetID.Length == 0) { return null; }
			var shipDatas = new ShipData[fleetID.Length];
			for (int i = 0; i < fleetID.Length; i++) {
				string shipID = fleetID[i];
				var data = m_Game.Asset.GetShipData(shipID);
				if (data == null) { return null; }
				shipDatas[i] = data;
			}
			return shipDatas;
		}


		private bool SetupAIBattleSoup (Group group, SoupStrategy strategy, out MapData map, out ShipData[] shipDatas, out ShipPosition[] positions, out string error) {
			error = "";
			map = null;
			positions = null;
			shipDatas = GetStrategyShips(strategy);
			if (shipDatas == null) {
				error = $"Fleet of strategy {strategy.FinalDisplayName} is empty.";
				return false;
			}
			var ships = ShipData.GetShips(shipDatas);
			map = GetSelectingMap(group);
			positions = null;
			for (int i = 0; i < 36; i++) {
				if (SoupStrategy.PositionShips_Random(
					map.Size, ships, map.Stones, out positions
				)) { break; }
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
				rt.GetComponent<Toggle>().SetIsOnWithoutNotify(false);
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
			if (m_UI.ShipSelectionContainer.childCount == 0) {
				m_UI.StartButton_Ship.interactable = false;
				m_UI.StartMessage_Ship.text = "Select at least one ship";
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
				var ships = CurrentBattleMode == BattleMode.PvA && group == Group.A ?
					GetSelectingShips() :
					GetStrategyShips(group == Group.A ? Strategies[StrategyIndexA.Value] : Strategies[StrategyIndexB.Value]);

				// Ability
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

					int _index = i;
					btn.onClick.AddListener(
						() => m_Game.Game.OnAbilityClick(group, _index)
					);

					var icon = grabber.Grab<GreyImage>("Icon");
					icon.sprite = ship.Sprite;
					icon.SetGrey(ship.Ship.Ability.HasActive && !btn.interactable);

					var cooldown = grabber.Grab<Text>("Cooldown");
					cooldown.gameObject.SetActive(ship.Ship.Ability.HasActive);
					cooldown.text = ship.Ship.Ability.Cooldown.ToString();

					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(false);
					grabber.Grab<Image>("Copy").gameObject.SetActive(false);
					grabber.Grab<Text>("Label").text = ship.DisplayName;

				}

				// All
				{
					var grabber = Instantiate(m_Game.AbilityShip, container);
					var rt = grabber.transform as RectTransform;
					rt.anchoredPosition3D = rt.anchoredPosition;
					rt.localRotation = Quaternion.identity;
					rt.localScale = Vector3.one;
					rt.SetAsLastSibling();
					rt.name = "All";

					var btn = grabber.Grab<Button>();
					btn.interactable = true;

					btn.onClick.AddListener(
						() => m_Game.Game.OnAbilityClick(group, ships.Length)
					);

					grabber.Grab<GreyImage>("Icon").sprite = m_UI.DevValueIterIcon;
					grabber.Grab<Text>("Cooldown").gameObject.SetActive(false);
					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(false);
					grabber.Grab<Image>("Copy").gameObject.SetActive(false);
				}

				// Slime
				{
					var grabber = Instantiate(m_Game.AbilityShip, container);
					var rt = grabber.transform as RectTransform;
					rt.anchoredPosition3D = rt.anchoredPosition;
					rt.localRotation = Quaternion.identity;
					rt.localScale = Vector3.one;
					rt.SetAsLastSibling();
					rt.name = "Slime";

					var btn = grabber.Grab<Button>();
					btn.interactable = true;

					btn.onClick.AddListener(
						() => m_Game.Game.OnAbilityClick(group, ships.Length + 1)
					);

					grabber.Grab<GreyImage>("Icon").sprite = m_UI.DevValueSlimeIcon;
					grabber.Grab<Text>("Cooldown").gameObject.SetActive(false);
					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(false);
					grabber.Grab<Image>("Copy").gameObject.SetActive(false);
				}

			}

		}


		private void RefreshStrategyUI (bool refreshFleet = false) {
			var strategyA = Strategies[Mathf.Clamp(StrategyIndexA.Value, 0, Strategies.Count - 1)];
			var strategyB = Strategies[Mathf.Clamp(StrategyIndexB.Value, 0, Strategies.Count - 1)];
			m_UI.StrategyDescriptionA.text = strategyA.Description;
			m_UI.StrategyDescriptionB.text = strategyB.Description;
			if (refreshFleet) {
				RefreshFleet(strategyA.FleetID, m_UI.StrategyFleetContainerA);
				RefreshFleet(strategyB.FleetID, m_UI.StrategyFleetContainerB);
			}
			// Func
			void RefreshFleet (string[] fleetID, RectTransform fleetContainer) {
				int len = fleetContainer.childCount;
				for (int i = 0; i < len; i++) {
					DestroyImmediate(fleetContainer.GetChild(0).gameObject, false);
				}
				foreach (var shipID in fleetID) {
					var shipData = m_Game.Asset.GetShipData(shipID);
					if (shipData == null) { continue; }
					var rt = new GameObject("", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform as RectTransform;
					rt.SetParent(fleetContainer);
					rt.anchoredPosition3D = rt.anchoredPosition;
					rt.localScale = Vector3.one;
					rt.localRotation = Quaternion.identity;
					rt.SetAsLastSibling();
					rt.GetComponent<Image>().sprite = shipData.Sprite;
				}
			}
		}


		// Misc
		private void ShowMessage (string msg) {
			m_UI.MessageRoot.gameObject.SetActive(true);
			m_UI.MessageText.text = msg;
		}


		private void FixContainerVerticalSize (RectTransform containerA, RectTransform containerB) {
			var rt = containerA.parent as RectTransform;
			var gridA = containerA.GetComponent<GridLayoutGroup>();
			var fitterA = containerA.GetComponent<ContentSizeFitter>();
			gridA.CalculateLayoutInputHorizontal();
			gridA.CalculateLayoutInputVertical();
			gridA.SetLayoutHorizontal();
			gridA.SetLayoutVertical();
			fitterA.SetLayoutVertical();
			if (containerB != null) {
				var gridB = containerB.GetComponent<GridLayoutGroup>();
				var fitterB = containerB.GetComponent<ContentSizeFitter>();
				gridB.CalculateLayoutInputHorizontal();
				gridB.CalculateLayoutInputVertical();
				gridB.SetLayoutHorizontal();
				gridB.SetLayoutVertical();
				fitterB.SetLayoutVertical();
			}
			float height = Mathf.Max(containerA.rect.height, containerB != null ? containerB.rect.height : 0f);
			if (rt.rect.height < height) {
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			}
		}


	}
}
