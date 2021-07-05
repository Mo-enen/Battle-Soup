using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;
using Moenen.Standard;
using BattleSoupAI;



namespace BattleSoup {
	public partial class BattleSoup : MonoBehaviour {




		#region --- VAR ---


		// Ser
		[SerializeField] PanelData m_Panel = default;
		[SerializeField] GameData m_Game = default;
		[SerializeField] UIData m_UI = default;
		[SerializeField] ResourceData m_Resource = default;

		// Data
		private GameState CurrentState = GameState.BattleMode;
		private BattleMode CurrentBattleMode = BattleMode.PvA;
		private Toggle[] m_ShipsToggleA = null;
		private Toggle[] m_ShipsToggleB = null;
		private Toggle[] m_MapsToggleA = null;
		private Toggle[] m_MapsToggleB = null;

		// Saving
		private readonly SavingString SelectedFleetA = new SavingString("BattleSoupDemo.Demo.SelectedFleetA", "Coracle+KillerSquid+SeaTurtle+Whale");
		private readonly SavingString SelectedFleetB = new SavingString("BattleSoupDemo.Demo.SelectedFleetB", "Coracle+KillerSquid+SeaTurtle+Whale");
		private readonly SavingInt SelectedMapA = new SavingInt("BattleSoupDemo.Demo.SelectedMapA", 0);
		private readonly SavingInt SelectedMapB = new SavingInt("BattleSoupDemo.Demo.SelectedMapB", 0);


		#endregion




		#region --- MSG ---


		private void Awake () {
			CursorUI.GetCursorTexture = (index) => (
				index >= 0 ? m_Resource.Cursors[index].Cursor : null,
				index >= 0 ? m_Resource.Cursors[index].Offset : Vector2.zero
			);
			m_Game.Game.gameObject.SetActive(false);
			m_Game.Game.SetupDelegate();
			// Get Toggles 
			m_ShipsToggleA = m_UI.ShipsToggleContainerA.GetComponentsInChildren<Toggle>(true);
			m_ShipsToggleB = m_UI.ShipsToggleContainerB.GetComponentsInChildren<Toggle>(true);
			m_MapsToggleA = m_UI.MapsToggleContainerA.GetComponentsInChildren<Toggle>(true);
			m_MapsToggleB = m_UI.MapsToggleContainerB.GetComponentsInChildren<Toggle>(true);
		}


		private void Start () {
			LoadShipSelectionFromSaving(Group.A);
			LoadShipSelectionFromSaving(Group.B);
			LoadMapSelectionFromSaving(Group.A);
			LoadMapSelectionFromSaving(Group.B);
			m_UI.StartButton_Ship.interactable = true;
			m_UI.StartButton_Map.interactable = true;
			m_UI.StartButton_ShipPos.interactable = true;
			m_UI.StartMessage_Ship.text = "";
			m_UI.StartMessage_Map.text = "";
			m_UI.StartMessage_ShipPos.text = "";
			CurrentState = GameState.BattleMode;
			RefreshPanelUI(GameState.BattleMode);
		}


		private void Update () => CursorUI.GlobalUpdate();



		#endregion




		#region --- API ---


		public void UI_GotoNextState () {
			switch (CurrentState) {


				// BattleMode >> Ship
				case GameState.BattleMode:
					m_Game.Game.gameObject.SetActive(false);
					m_UI.ShipLabelA.text = CurrentBattleMode == BattleMode.PvA ? "My Ships" : "Robot-A Ships";
					m_UI.ShipLabelB.text = CurrentBattleMode == BattleMode.PvA ? "Opponent Ships" : "Robot-B Ships";
					RefreshShipButton();
					RefreshPanelUI(GameState.Ship);
					CurrentState = GameState.Ship;
					break;


				// Ship >> Map
				case GameState.Ship:
					if (!RefreshShipButton()) { break; }
					m_Game.Game.gameObject.SetActive(false);
					m_UI.MapLabelA.text = CurrentBattleMode == BattleMode.PvA ? "My Map" : "Robot-A Map";
					m_UI.MapLabelB.text = CurrentBattleMode == BattleMode.PvA ? "Opponent Map" : "Robot-B Map";
					RefreshMapButton();
					RefreshPanelUI(GameState.Map);
					CurrentState = GameState.Map;
					break;


				// Map >> PositionShip/Playing
				case GameState.Map:
					if (!RefreshMapButton()) { break; }
					if (CurrentBattleMode == BattleMode.PvA) {
						// Map >> PositionShip
						if (!m_Game.ShipPositionUI.Init(
							GetSelectingMap(Group.A),
							GetSelectingShips(Group.A)
						)) { break; }
						m_Game.Game.gameObject.SetActive(false);
						RefreshShipPositionButton();
						RefreshPanelUI(GameState.PositionShip);
						CurrentState = GameState.PositionShip;
					} else {
						// Map >> Playing
						if (
							!SetupAIBattleSoup(Group.A, out var mapA, out var shipsA, out var positionsA, out string error) ||
							!SetupAIBattleSoup(Group.B, out var mapB, out var shipsB, out var positionsB, out error)
						) {
							ShowMessage(error);
							break;
						}
						m_Game.Game.Init(CurrentBattleMode, mapA, mapB, shipsA, shipsB, positionsA, positionsB);
						m_Game.Game.gameObject.SetActive(true);
						m_Game.BattleSoupUIA.Init();
						m_Game.BattleSoupUIB.Init();
						RefreshBattleInfoUI();
						ReloadAbilityUI();
						RefreshPanelUI(GameState.Playing);
						CurrentState = GameState.Playing;
					}
					break;


				// PositionShips >> Playing
				case GameState.PositionShip: {
					if (!RefreshShipPositionButton()) { break; }
					if (!SetupAIBattleSoup(Group.B, out var mapB, out var shipsB, out var positionsB, out string error)) {
						ShowMessage(error);
						break;
					}
					m_Game.Game.Init(
						CurrentBattleMode,
						m_Game.ShipPositionUI.Map, mapB,
						m_Game.ShipPositionUI.Ships, shipsB,
						m_Game.ShipPositionUI.Positions, positionsB
					);
					m_Game.Game.gameObject.SetActive(true);
					m_Game.BattleSoupUIA.Init();
					m_Game.BattleSoupUIB.Init();
					RefreshBattleInfoUI();
					ReloadAbilityUI();
					RefreshPanelUI(GameState.Playing);
					CurrentState = GameState.Playing;
					break;
				}


				// Playing >> BattleMode
				case GameState.Playing:
					m_Game.Game.gameObject.SetActive(false);
					m_Game.BattleSoupUIA.Clear();
					m_Game.BattleSoupUIB.Clear();
					RefreshPanelUI(GameState.BattleMode);
					CurrentState = GameState.BattleMode;
					break;
			}
		}


		public void UI_GotoPrevState () {
			switch (CurrentState) {
				case GameState.Ship:
					RefreshPanelUI(GameState.BattleMode);
					CurrentState = GameState.BattleMode;
					break;
				case GameState.Map:
					RefreshPanelUI(GameState.Ship);
					CurrentState = GameState.Ship;
					break;
				case GameState.PositionShip:
					RefreshPanelUI(GameState.Map);
					CurrentState = GameState.Map;
					break;
				case GameState.Playing:
					RefreshPanelUI(GameState.BattleMode);
					CurrentState = GameState.BattleMode;
					break;
			}
		}


		public void UI_OpenURL (string url) {
			url = Util.GetUrl(url);
			ShowMessage($"Opening {url}");
			Application.OpenURL(url);
		}


		public void UI_ShipChanged (int group) {
			SaveShipSelectionToSaving(group == 0 ? Group.A : Group.B);
			RefreshShipButton();
		}


		public void UI_MapChanged (int group) {
			SaveMapSelectionToSaving(group == 0 ? Group.A : Group.B);
			RefreshMapButton();
		}


		public void UI_PositionShipChanged () {
			RefreshShipPositionButton();
		}


		public void UI_SetBattleMode (int id) => CurrentBattleMode = (BattleMode)id;


		public void UI_RefreshAbilityUI () {
			RefreshAbilityUI(m_UI.AbilityContainerA, m_Game.BattleSoupUIA, Group.A);
			RefreshAbilityUI(m_UI.AbilityContainerB, m_Game.BattleSoupUIB, Group.B);
		}


		public void UI_RefreshBattleInfoUI () => RefreshBattleInfoUI();


		#endregion




	}
}



#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEditor;

	[CustomEditor(typeof(BattleSoup))]
	public class BattleSoup_Inspector : Editor {


		public override void OnInspectorGUI () {

			// Buttons
			GUILayout.Space(4);
			if (GUI.Button(GUIRect(0, 24), "Reload Ships & Maps")) {
				ReloadShips(serializedObject.FindProperty("m_UI").FindPropertyRelative("ShipsToggleContainerA").objectReferenceValue as RectTransform);
				ReloadShips(serializedObject.FindProperty("m_UI").FindPropertyRelative("ShipsToggleContainerB").objectReferenceValue as RectTransform);
				ReloadMaps(serializedObject.FindProperty("m_UI").FindPropertyRelative("MapsToggleContainerA").objectReferenceValue as RectTransform);
				ReloadMaps(serializedObject.FindProperty("m_UI").FindPropertyRelative("MapsToggleContainerB").objectReferenceValue as RectTransform);
			}
			GUILayout.Space(4);

			// Default
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script");
			serializedObject.ApplyModifiedProperties();
		}


		private void ReloadShips (RectTransform container) {

			// Load All ShipData
			var ships = new List<ShipData>();
			var guids = AssetDatabase.FindAssets("t:ShipData");
			foreach (var guid in guids) {
				ships.Add(AssetDatabase.LoadAssetAtPath<ShipData>(
					AssetDatabase.GUIDToAssetPath(guid))
				);
			}

			// Clear UI
			var template = container.GetChild(0) as RectTransform;
			template.SetParent(null);
			int childCount = container.childCount;
			for (int i = 0; i < childCount; i++) {
				DestroyImmediate(container.GetChild(0).gameObject, false);
			}

			// Create UI
			foreach (var ship in ships) {
				var rt = Instantiate(template.gameObject, container).transform as RectTransform;
				rt.anchoredPosition3D = rt.anchoredPosition;
				rt.localScale = Vector3.one;
				rt.localRotation = Quaternion.identity;
				rt.SetAsLastSibling();
				rt.gameObject.name = ship.name;
				// Label
				rt.Find("Label").GetComponent<Text>().text = ship.DisplayName;
				rt.GetComponent<Toggle>().isOn = false;
				rt.Find("Thumbnail").GetComponent<Image>().sprite = ship.Sprite;
			}

			// Final
			DestroyImmediate(template.gameObject, false);
		}


		private void ReloadMaps (RectTransform container) {

			// Load All MapData
			var maps = new List<MapData>();
			var guids = AssetDatabase.FindAssets("t:MapData");
			foreach (var guid in guids) {
				maps.Add(AssetDatabase.LoadAssetAtPath<MapData>(
					AssetDatabase.GUIDToAssetPath(guid))
				);
			}

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
				rt.gameObject.name = map.name;
				// Label
				rt.GetComponent<Toggle>().isOn = false;
				rt.Find("Label").GetComponent<Text>().text = $"{map.Size}×{map.Size}";
				rt.Find("Thumbnail").GetComponent<MapRenderer>().LoadMap(map);
				var grid = rt.Find("Grid").GetComponent<VectorGrid>();
				grid.X = map.Size;
				grid.Y = map.Size;
			}

			// Final
			DestroyImmediate(template.gameObject, false);

		}


		private Rect GUIRect (int w, int h) => GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(w == 0), GUILayout.ExpandHeight(h == 0));


	}
}
#endif