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
		private bool QuitGameForReal = false;

		// Saving
		private readonly SavingString SelectedFleetA = new SavingString("BattleSoupDemo.Demo.SelectedFleetA", "Coracle+KillerSquid+SeaTurtle+Whale");
		private readonly SavingString SelectedFleetB = new SavingString("BattleSoupDemo.Demo.SelectedFleetB", "Coracle+KillerSquid+SeaTurtle+Whale");
		private readonly SavingInt SelectedMapA = new SavingInt("BattleSoupDemo.Demo.SelectedMapA", 0);
		private readonly SavingInt SelectedMapB = new SavingInt("BattleSoupDemo.Demo.SelectedMapB", 0);
		private readonly SavingBool UseSound = new SavingBool("BattleSoupDemo.Demo.UseSound", true);


		#endregion




		#region --- MSG ---


		private void Awake () {
			CursorUI.GetCursorTexture = (index) => (
				index >= 0 ? m_Resource.Cursors[index].Cursor : null,
				index >= 0 ? m_Resource.Cursors[index].Offset : Vector2.zero
			);
			m_Game.Game.gameObject.SetActive(false);
			m_Game.Game.SetupDelegate();
			// Quit Game Confirm
			Application.wantsToQuit += () => {
#if UNITY_EDITOR
				QuitGameForReal = true;
				return QuitGameForReal;
#else
				if (!QuitGameForReal) {
					m_Panel.QuitGameWindow.gameObject.SetActive(true);
				}
				return QuitGameForReal;
#endif
			};
			// Sound
			AudioListener.volume = UseSound.Value ? 1f : 0f;
			m_UI.SoundTG.SetIsOnWithoutNotify(UseSound.Value);
			// System
			Application.targetFrameRate = 30;
			Util.CreateFolder(Util.GetRuntimeBuiltRootPath());

		}


		private void Start () {
			// Ships
			ReloadShipToggle(Group.A);
			ReloadShipToggle(Group.B);
			m_ShipsToggleA = m_UI.ShipsToggleContainerA.GetComponentsInChildren<Toggle>(true);
			m_ShipsToggleB = m_UI.ShipsToggleContainerB.GetComponentsInChildren<Toggle>(true);
			LoadShipSelectionFromSaving(Group.A);
			LoadShipSelectionFromSaving(Group.B);
			// Maps
			ReloadMapToggle(Group.A);
			ReloadMapToggle(Group.B);
			m_MapsToggleA = m_UI.MapsToggleContainerA.GetComponentsInChildren<Toggle>(true);
			m_MapsToggleB = m_UI.MapsToggleContainerB.GetComponentsInChildren<Toggle>(true);
			LoadMapSelectionFromSaving(Group.A);
			LoadMapSelectionFromSaving(Group.B);
			// Misc
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
					FixContainerVerticalSize(m_UI.ShipsToggleContainerA, m_UI.ShipsToggleContainerB);
					CurrentState = GameState.Ship;
					break;


				// Ship >> Map
				case GameState.Ship:
					if (!RefreshShipButton()) { break; }
					SaveShipSelectionToSaving(Group.A);
					SaveShipSelectionToSaving(Group.B);
					m_Game.Game.gameObject.SetActive(false);
					m_UI.MapLabelA.text = CurrentBattleMode == BattleMode.PvA ? "My Map" : "Robot-A Map";
					m_UI.MapLabelB.text = CurrentBattleMode == BattleMode.PvA ? "Opponent Map" : "Robot-B Map";
					RefreshMapButton();
					RefreshPanelUI(GameState.Map);
					FixContainerVerticalSize(m_UI.MapsToggleContainerA, m_UI.MapsToggleContainerB);
					CurrentState = GameState.Map;
					break;


				// Map >> PositionShip/Playing
				case GameState.Map: {
					if (!RefreshMapButton()) { break; }
					SaveMapSelectionToSaving(Group.A);
					SaveMapSelectionToSaving(Group.B);
					if (CurrentBattleMode == BattleMode.PvA) {
						// Map >> PositionShip
						var map = GetSelectingMap(Group.A);
						if (map == null) { break; }
						var ships = GetSelectingShips(Group.A);
						if (ships == null) { break; }
						var positions = new List<ShipPosition>();
						for (int i = 0; i < 36; i++) {
							if (GetRandomShipPositions(ships, map, positions)) { break; }
						}
						if (positions.Count == 0) { break; }
						m_Game.ShipPositionUI.Init(map, ships, positions);
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
						m_Game.BattleSoupUIA.Init(CurrentBattleMode == BattleMode.AvA);
						m_Game.BattleSoupUIB.Init(true);
						m_Game.BattleSoupUIA.RefreshShipRenderer();
						m_Game.BattleSoupUIB.RefreshShipRenderer();
						RefreshBattleInfoUI();
						ReloadAbilityUI();
						UI_RefreshAbilityUI();
						RefreshPanelUI(GameState.Playing);
						CurrentState = GameState.Playing;
					}
					break;
				}


				// PositionShips >> Playing
				case GameState.PositionShip: {
					if (!RefreshShipPositionButton()) { break; }
					if (!SetupAIBattleSoup(Group.B, out var mapB, out var shipsB, out var positionsB, out string error)) {
						ShowMessage(error);
						break;
					}
					m_Game.Game.Init(CurrentBattleMode, m_Game.ShipPositionUI.Map, mapB, m_Game.ShipPositionUI.Ships, shipsB, m_Game.ShipPositionUI.Positions, positionsB);
					m_Game.Game.gameObject.SetActive(true);
					m_Game.BattleSoupUIA.Init(CurrentBattleMode == BattleMode.AvA);
					m_Game.BattleSoupUIB.Init(true);
					m_Game.BattleSoupUIA.RefreshShipRenderer();
					m_Game.BattleSoupUIB.RefreshShipRenderer();
					RefreshBattleInfoUI();
					ReloadAbilityUI();
					UI_RefreshAbilityUI();
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


		public void UI_ShipChanged () => RefreshShipButton();


		public void UI_MapChanged () => RefreshMapButton();


		public void UI_PositionShipChanged () {
			RefreshShipPositionButton();
		}


		public void UI_SetBattleMode (int id) => CurrentBattleMode = (BattleMode)id;


		public void UI_RefreshAbilityUI () {
			Sprite aIcon = null;
			if (m_Game.Game.AbilityShipIndex >= 0) {
				var ship = m_Game.Game.GetShipData(m_Game.Game.CurrentTurn, m_Game.Game.AbilityShipIndex);
				aIcon = ship.Sprite;
			}
			m_Game.BattleSoupUIA.SetAbilityAimIcon(m_Game.Game.CurrentTurn == Group.B ? aIcon : null);
			m_Game.BattleSoupUIB.SetAbilityAimIcon(m_Game.Game.CurrentTurn == Group.B ? null : aIcon);
			RefreshAbilityUI(m_UI.AbilityContainerA, Group.A);
			RefreshAbilityUI(m_UI.AbilityContainerB, Group.B);
			// Func
			void RefreshAbilityUI (RectTransform container, Group group) {
				int count = container.childCount;
				var opGroup = group == Group.A ? Group.B : Group.A;
				for (int i = 0; i < count; i++) {
					int cooldown = m_Game.Game.GetCooldown(group, i);
					var ability = m_Game.Game.GetAbility(group, i);
					int opPrevUseIndex = group == Group.A ? m_Game.Game.PrevUsedAbilityB : m_Game.Game.PrevUsedAbilityA;

					var grabber = container.GetChild(i).GetComponent<Grabber>();
					var btn = grabber.Grab<Button>();
					btn.interactable = m_Game.Game.CheckShipAlive(i, group) && ability.HasActive && cooldown <= 0;

					var cooldownTxt = grabber.Grab<Text>("Cooldown");
					if (cooldownTxt.gameObject.activeSelf) {
						cooldownTxt.text = cooldown > 0 ? cooldown.ToString() : "";
					}

					grabber.Grab<GreyImage>("Icon").SetGrey(
						ability.HasActive && !btn.interactable
					);
					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(
						!m_Game.Game.CheckShipAlive(i, group)
					);
					grabber.Grab<RectTransform>("Highlight").gameObject.SetActive(
						m_Game.Game.CurrentTurn == group && m_Game.Game.AbilityShipIndex == i
					);

					var copy = grabber.Grab<Image>("Copy");
					bool copyActive = ability.CopyOpponentLastUsed && opPrevUseIndex >= 0;
					copy.gameObject.SetActive(copyActive);
					if (copyActive) {
						copy.sprite = m_Game.Game.GetShipData(opGroup, opPrevUseIndex).Sprite;
					}

				}
			}
		}


		public void UI_RefreshBattleInfoUI () => RefreshBattleInfoUI();


		public void UI_SoundToggle (bool isOn) {
			UseSound.Value = isOn;
			AudioListener.volume = isOn ? 1f : 0f;
		}


		public void UI_SetCheat () => m_Game.Game.Cheated = true;


		public void UI_QuitGame () {
			QuitGameForReal = true;
			Application.Quit();
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
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script");
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif