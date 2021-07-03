using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UIGadget;
using Moenen.Standard;
using BattleSoupAI;



namespace BattleSoup {
	public class BattleSoup : MonoBehaviour {




		#region --- SUB ---



		[System.Serializable]
		public struct CursorData {
			public Texture2D Cursor;
			public Vector2 Offset;
		}



		public enum GameState {
			BattleMode = 0,
			Ship = 1,
			Map = 2,
			PositionShip = 3,
			Playing = 4,
		}



		public enum BattleMode {
			PvA = 0,
			AvA = 1,
		}



		[System.Serializable]
		public enum Group {
			A = 0,
			B = 1,
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
		[SerializeField] RectTransform m_ShipsToggleContainerA = null;
		[SerializeField] RectTransform m_ShipsToggleContainerB = null;
		[SerializeField] RectTransform m_MapsToggleContainerA = null;
		[SerializeField] RectTransform m_MapsToggleContainerB = null;
		[SerializeField] VectorGrid m_GridA = null;
		[SerializeField] VectorGrid m_GridB = null;
		[SerializeField] BlocksRenderer m_ShipRendererA = null;
		[SerializeField] BlocksRenderer m_ShipRendererB = null;
		[SerializeField] BlocksRenderer m_SonarRendererA = null;
		[SerializeField] BlocksRenderer m_SonarRendererB = null;
		[SerializeField] BlocksRenderer m_MapRendererA = null;
		[SerializeField] BlocksRenderer m_MapRendererB = null;
		[SerializeField] ShipPositionUI m_ShipPositionUI = null;
		[SerializeField] Text m_ShipLabelA = null;
		[SerializeField] Text m_ShipLabelB = null;
		[SerializeField] Text m_MapLabelA = null;
		[SerializeField] Text m_MapLabelB = null;
		[SerializeField] Button m_StartButton_Ship = null;
		[SerializeField] Button m_StartButton_Map = null;
		[SerializeField] Button m_StartButton_ShipPos = null;
		[SerializeField] Text m_StartMessage_Ship = null;
		[SerializeField] Text m_StartMessage_Map = null;
		[SerializeField] Text m_StartMessage_ShipPos = null;
		[SerializeField] Image m_BattleAvatarA = null;
		[SerializeField] Image m_BattleAvatarB = null;
		[SerializeField] Sprite[] m_BattleAvatars = null;
		[SerializeField] ShipData[] m_Ships = null;
		[SerializeField] MapData[] m_Maps = null;
		[SerializeField] private CursorData[] m_Cursors = null;

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
				index >= 0 ? m_Cursors[index].Cursor : null,
				index >= 0 ? m_Cursors[index].Offset : Vector2.zero
			);
			// Get Toggles 
			m_ShipsToggleA = m_ShipsToggleContainerA.GetComponentsInChildren<Toggle>(true);
			m_ShipsToggleB = m_ShipsToggleContainerB.GetComponentsInChildren<Toggle>(true);
			m_MapsToggleA = m_MapsToggleContainerA.GetComponentsInChildren<Toggle>(true);
			m_MapsToggleB = m_MapsToggleContainerB.GetComponentsInChildren<Toggle>(true);

		}



		private void Start () {
			LoadShipSelectionFromSaving(Group.A);
			LoadShipSelectionFromSaving(Group.B);
			LoadMapSelectionFromSaving(Group.A);
			LoadMapSelectionFromSaving(Group.B);
			m_StartButton_Ship.interactable = true;
			m_StartButton_Map.interactable = true;
			m_StartButton_ShipPos.interactable = true;
			m_StartMessage_Ship.text = "";
			m_StartMessage_Map.text = "";
			m_StartMessage_ShipPos.text = "";
			SetGameState(GameState.BattleMode);
		}


		private void Update () {
			CursorUI.GlobalUpdate();
		}



		#endregion




		#region --- API ---


		public void UI_GotoNextState () {
			switch (CurrentState) {


				// BattleMode >> Ship
				case GameState.BattleMode:
					m_ShipLabelA.text = CurrentBattleMode == BattleMode.PvA ? "My Ships" : "Robot-A Ships";
					m_ShipLabelB.text = CurrentBattleMode == BattleMode.PvA ? "Opponent Ships" : "Robot-B Ships";
					SetGameState(GameState.Ship);
					RefreshShipButton();
					break;


				// Ship >> Map
				case GameState.Ship:
					if (!RefreshShipButton()) { break; }
					m_MapLabelA.text = CurrentBattleMode == BattleMode.PvA ? "My Map" : "Robot-A Map";
					m_MapLabelB.text = CurrentBattleMode == BattleMode.PvA ? "Opponent Map" : "Robot-B Map";
					RefreshMapButton();
					SetGameState(GameState.Map);
					break;


				// Map >> PositionShip/Playing
				case GameState.Map:
					if (!RefreshMapButton()) { break; }
					if (!m_ShipPositionUI.Init(
						GetSelectingMap(Group.A),
						GetSelectingShips(Group.A)
					)) { break; }
					if (CurrentBattleMode == BattleMode.PvA) {
						SetGameState(GameState.PositionShip);
						RefreshShipPositionButton();
					} else {
						RefreshBattleInfo();
						SetGameState(GameState.Playing);
					}
					break;


				// PositionShips >> Playing
				case GameState.PositionShip:
					if (!RefreshShipPositionButton()) { break; }
					SetSoupSize(m_ShipPositionUI.Map.Size);
					SetSoupShips(m_ShipPositionUI.Ships, m_ShipPositionUI.Positions, Group.A);

					RefreshBattleInfo();
					SetGameState(GameState.Playing);
					break;


				// Playing >> BattleMode
				case GameState.Playing:



					SetGameState(GameState.BattleMode);
					break;
			}
		}


		public void UI_GotoPrevState () {
			switch (CurrentState) {
				case GameState.Ship:
					SetGameState(GameState.BattleMode);
					break;
				case GameState.Map:
					SetGameState(GameState.Ship);
					break;
				case GameState.PositionShip:
					SetGameState(GameState.Map);
					break;
			}
		}


		public void UI_OpenURL (string url) => Application.OpenURL(Util.GetUrl(url));


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


		#endregion




		#region --- LGC ---


		// Game State
		private void SetGameState (GameState state) {
			CurrentState = state;
			m_LogoPanel.gameObject.SetActive(true);
			m_BattlePanel.gameObject.SetActive(state == GameState.BattleMode);
			m_ShipPanel.gameObject.SetActive(state == GameState.Ship);
			m_MapPanel.gameObject.SetActive(state == GameState.Map);
			m_ShipPositionPanel.gameObject.SetActive(state == GameState.PositionShip);
			m_BattleZone.gameObject.SetActive(state == GameState.Playing);
			m_BattleInfo.gameObject.SetActive(state == GameState.Playing);
		}


		// Soup
		private void SetSoupSize (int size) {
			m_GridA.X = size;
			m_GridA.Y = size;
			m_GridB.X = size;
			m_GridB.Y = size;
			m_ShipRendererA.GridCountX = size;
			m_ShipRendererA.GridCountY = size;
			m_ShipRendererB.GridCountX = size;
			m_ShipRendererB.GridCountY = size;
			m_SonarRendererA.GridCountX = size;
			m_SonarRendererA.GridCountY = size;
			m_SonarRendererB.GridCountX = size;
			m_SonarRendererB.GridCountY = size;
			m_MapRendererA.GridCountX = size;
			m_MapRendererA.GridCountY = size;
			m_MapRendererB.GridCountX = size;
			m_MapRendererB.GridCountY = size;
		}


		private void SetSoupShips (ShipData[] ships, List<ShipPosition> positions, Group group) {




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
			foreach (var ship in m_Ships) {
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
					foreach (var map in m_Maps) {
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
				m_StartButton_Ship.interactable = false;
				m_StartMessage_Ship.text = "Select ships for all players";
				return false;
			}

			// End
			m_StartButton_Ship.interactable = true;
			m_StartMessage_Ship.text = "";
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
				m_StartButton_Map.interactable = false;
				m_StartMessage_Map.text = "Select map for all players";
				return false;
			}

			// End
			m_StartMessage_Map.text = "";
			m_StartButton_Map.interactable = true;
			return true;
		}


		private bool RefreshShipPositionButton () {
			if (!m_ShipPositionUI.RefreshOverlapRenderer(out string error)) {
				m_StartButton_ShipPos.interactable = false;
				m_StartMessage_ShipPos.text = error;
				return false;
			}
			m_StartButton_ShipPos.interactable = true;
			m_StartMessage_ShipPos.text = "";
			return true;
		}


		private void RefreshBattleInfo () {
			m_BattleAvatarA.sprite = m_BattleAvatars[(int)CurrentBattleMode];
			m_BattleAvatarB.sprite = m_BattleAvatars[1];
		}


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
				ReloadShips(serializedObject.FindProperty("m_ShipsToggleContainerA").objectReferenceValue as RectTransform);
				ReloadShips(serializedObject.FindProperty("m_ShipsToggleContainerB").objectReferenceValue as RectTransform);
				ReloadMaps(serializedObject.FindProperty("m_MapsToggleContainerA").objectReferenceValue as RectTransform);
				ReloadMaps(serializedObject.FindProperty("m_MapsToggleContainerB").objectReferenceValue as RectTransform);
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
				rt.Find("Thumbnail").GetComponent<MapRenderer>().Map = map;
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