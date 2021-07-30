using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		private readonly List<SoupStrategy> Strategies = new List<SoupStrategy>();
		private GameState CurrentState = GameState.BattleMode;
		private BattleMode CurrentBattleMode = BattleMode.PvA;
		private Toggle[] m_MapsToggleA = null;
		private Toggle[] m_MapsToggleB = null;
		private bool QuitGameForReal = false;

		// Saving
		private readonly SavingString SelectedFleet = new SavingString("BattleSoup.SelectedFleet", "Coracle+KillerSquid+SeaTurtle+Whale");
		private readonly SavingBool UseSound = new SavingBool("BattleSoup.UseSound", true);
		private readonly SavingBool AutoPlayAvA = new SavingBool("BattleSoup.AutoPlayAvA", true);
		private readonly SavingInt SelectedMapA = new SavingInt("BattleSoup.SelectedMapA", 0);
		private readonly SavingInt SelectedMapB = new SavingInt("BattleSoup.SelectedMapB", 0);
		private readonly SavingInt StrategyIndexA = new SavingInt("BattleSoup.StrategyIndexA", 0);
		private readonly SavingInt StrategyIndexB = new SavingInt("BattleSoup.StrategyIndexB", 0);


		#endregion




		#region --- MSG ---


		private void Awake () {

			CursorUI.GetCursorTexture = (index) => (
				index >= 0 ? m_Resource.Cursors[index].Cursor : null,
				index >= 0 ? m_Resource.Cursors[index].Offset : Vector2.zero
			);
			m_Game.Game.gameObject.SetActive(false);
			m_Game.Game.SetupDelegate();
			Game.GetShip = (key) => m_Game.Asset.GetShipData(key);

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
			m_UI.AutoPlayAvATG.SetIsOnWithoutNotify(AutoPlayAvA.Value);

			// System
			Application.targetFrameRate = 30;
			Util.CreateFolder(Util.GetRuntimeBuiltRootPath());

			// Strategy
			Strategies.Clear();
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies) {
				var types = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(SoupStrategy)));
				if (types.Count() == 0) { continue; }
				string assemblyName = assembly.GetName().Name;
				foreach (var type in types) {
					try {
						if (type.IsAbstract) { continue; }
						if (System.Activator.CreateInstance(type) is SoupStrategy strategy) {
							Strategies.Add(strategy);
						}
					} catch { }
				}
			}

			// Strategy UI
			var sNames = new List<string>();
			foreach (var st in Strategies) {
				sNames.Add(st.FinalDisplayName);
			}
			m_UI.StrategyDropA.ClearOptions();
			m_UI.StrategyDropA.AddOptions(sNames);
			m_UI.StrategyDropB.ClearOptions();
			m_UI.StrategyDropB.AddOptions(sNames);
			StrategyIndexA.Value = Mathf.Clamp(StrategyIndexA.Value, 0, Strategies.Count - 1);
			StrategyIndexB.Value = Mathf.Clamp(StrategyIndexB.Value, 0, Strategies.Count - 1);
			m_UI.StrategyDropA.SetValueWithoutNotify(StrategyIndexA.Value);
			m_UI.StrategyDropB.SetValueWithoutNotify(StrategyIndexB.Value);
			RefreshStrategyUI();

		}


		private void Start () {
			// Ships
			ReloadShipButtons();
			LoadShipSelectionFromSaving();
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


				// BattleMode >> Ship/Map
				case GameState.BattleMode:
					m_Game.Game.gameObject.SetActive(false);
					if (CurrentBattleMode == BattleMode.PvA) {
						// Goto Ship
						RefreshShipButton();
						RefreshPanelUI(GameState.Ship);
						FixContainerVerticalSize(m_UI.ShipsButtonContainer, null);
						CurrentState = GameState.Ship;
					} else {
						// Goto Map
						m_UI.MapLabelA.text = "Robot-A Map";
						m_UI.MapLabelB.text = "Robot-B Map";
						RefreshMapButton();
						RefreshPanelUI(GameState.Map);
						FixContainerVerticalSize(m_UI.MapsToggleContainerA, m_UI.MapsToggleContainerB);
						CurrentState = GameState.Map;
					}
					break;


				// BattleMode/Ship >> Map
				case GameState.Ship:
					if (!RefreshShipButton()) { break; }
					SaveShipSelectionToSaving();
					m_Game.Game.gameObject.SetActive(false);
					m_UI.MapLabelA.text = CurrentBattleMode == BattleMode.PvA ? "My Map" : "Robot-A Map";
					m_UI.MapLabelB.text = CurrentBattleMode == BattleMode.PvA ? "Opponent Map" : "Robot-B Map";
					RefreshMapButton();
					RefreshPanelUI(GameState.Map);
					FixContainerVerticalSize(m_UI.MapsToggleContainerA, m_UI.MapsToggleContainerB);
					CurrentState = GameState.Map;
					break;


				// Map >> PositionShip
				case GameState.Map: {
					if (!RefreshMapButton()) { break; }
					SaveMapSelectionToSaving(Group.A);
					SaveMapSelectionToSaving(Group.B);
					if (CurrentBattleMode == BattleMode.PvA) {
						// PvA
						var map = GetSelectingMap(Group.A);
						if (map == null) { break; }
						var shipDatas = GetSelectingShips();
						if (shipDatas == null) { break; }
						ShipPosition[] positions = null;
						var ships = ShipData.GetShips(shipDatas);
						for (int i = 0; i < 36; i++) {
							if (SoupStrategy.PositionShips_Random(map.Size, ships, map.Stones, out positions)) { break; }
						}
						if (positions == null || positions.Length == 0) {
							ShowMessage("Too many ships in this small map");
							break;
						}
						m_Game.ShipPositionUI.gameObject.SetActive(true);
						m_Game.ShipPositionUI.Init(map, shipDatas, positions);
						RefreshShipPositionButton();
					} else {
						// AvA
						m_Game.ShipPositionUI.gameObject.SetActive(false);
					}
					m_UI.StrategyDropA.transform.parent.gameObject.SetActive(CurrentBattleMode == BattleMode.AvA);
					m_UI.StrategyDropB.transform.parent.gameObject.SetActive(true);
					m_UI.PositionShipResetButton.gameObject.SetActive(CurrentBattleMode == BattleMode.PvA);
					m_Game.Game.gameObject.SetActive(false);
					RefreshPanelUI(GameState.PositionShip);
					CurrentState = GameState.PositionShip;
					break;
				}


				// PositionShips >> Playing
				case GameState.PositionShip: {
					if (!RefreshShipPositionButton()) { break; }
					if (!SetupAIBattleSoup(Group.B, Strategies[StrategyIndexB.Value], out var mapB, out ShipData[] shipsB, out var positionsB, out string error)) {
						ShowMessage(error);
						break;
					}
					if (CurrentBattleMode == BattleMode.PvA) {
						m_Game.Game.Init(CurrentBattleMode, Strategies[StrategyIndexA.Value], Strategies[StrategyIndexB.Value], m_Game.ShipPositionUI.Map, mapB, GetSelectingShips(), shipsB, m_Game.ShipPositionUI.Positions, positionsB);
						if (AutoPlayAvA.Value) {
							m_Game.Game.UI_PlayAvA();
						}
					} else {
						if (!SetupAIBattleSoup(Group.A, Strategies[StrategyIndexA.Value], out var mapA, out var shipsA, out var positionsA, out string errorA)) {
							ShowMessage(errorA);
							break;
						}
						m_Game.Game.Init(CurrentBattleMode, Strategies[StrategyIndexA.Value], Strategies[StrategyIndexB.Value], mapA, mapB, shipsA, shipsB, positionsA, positionsB);
						if (AutoPlayAvA.Value) {
							m_Game.Game.UI_PlayAvA();
						}
					}
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
					m_Game.Game.Cheated = false;
					RefreshBattleInfoUI();
					RefreshPanelUI(GameState.BattleMode);
					CurrentState = GameState.BattleMode;
					break;
			}
		}


		public void UI_GotoPrevState () {
			switch (CurrentState) {
				case GameState.ShipEditor:
				case GameState.Ship:
					RefreshPanelUI(GameState.BattleMode);
					CurrentState = GameState.BattleMode;
					break;
				case GameState.Map:
					if (CurrentBattleMode == BattleMode.PvA) {
						RefreshPanelUI(GameState.Ship);
						CurrentState = GameState.Ship;
					} else {
						RefreshPanelUI(GameState.BattleMode);
						CurrentState = GameState.BattleMode;
					}
					break;
				case GameState.PositionShip:
					RefreshPanelUI(GameState.Map);
					CurrentState = GameState.Map;
					break;
				case GameState.Playing:
					m_Game.Game.Cheated = false;
					RefreshPanelUI(GameState.BattleMode);
					CurrentState = GameState.BattleMode;
					break;
			}
		}


		public void UI_OpenShipEditor () {
			m_Game.Game.gameObject.SetActive(false);
			RefreshPanelUI(GameState.ShipEditor);
			CurrentState = GameState.ShipEditor;
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

			// Aim Ship Icon
			Sprite aIcon = null;
			var currentTurn = m_Game.Game.CurrentTurn;
			if (m_Game.Game.AbilityShipIndex >= 0 && m_Game.Game.gameObject.activeSelf) {
				var ship = m_Game.Game.GetShipData(currentTurn, m_Game.Game.AbilityShipIndex);
				if (ship.Ship.Ability.CopyOpponentLastUsed) {
					string oppKey = currentTurn == Group.A ? m_Game.Game.PrevUsedAbilityB : m_Game.Game.PrevUsedAbilityA;
					if (!string.IsNullOrEmpty(oppKey)) {
						var shipData = m_Game.Asset.GetShipData(oppKey);
						if (shipData != null) {
							aIcon = shipData.Sprite;
						}
					}
				} else {
					aIcon = ship.Sprite;
				}
			}

			// Aim Icon
			m_Game.BattleSoupUIA.SetAbilityAimIcon(currentTurn == Group.B ? aIcon : null);
			m_Game.BattleSoupUIB.SetAbilityAimIcon(currentTurn == Group.B ? null : aIcon);

			// Ability in Battle Info
			RefreshAbilityUI(m_UI.AbilityContainerA, Group.A);
			RefreshAbilityUI(m_UI.AbilityContainerB, Group.B);

			// Func
			void RefreshAbilityUI (RectTransform container, Group group) {
				bool devMode = m_Game.Game.DevMode;
				int count = group == Group.A ? m_Game.Game.ShipCountA : m_Game.Game.ShipCountB;
				for (int i = 0; i < count; i++) {

					int cooldown = m_Game.Game.GetCooldown(group, i);
					var ability = m_Game.Game.GetAbility(group, i);
					string opPrevUseID = group == Group.A ? m_Game.Game.PrevUsedAbilityB : m_Game.Game.PrevUsedAbilityA;
					bool alive = m_Game.Game.CheckShipAlive(i, group);

					var grabber = container.GetChild(i).GetComponent<Grabber>();
					var btn = grabber.Grab<Button>();
					if (!devMode) {
						btn.interactable =
							alive && cooldown <= 0 &&
							(ability.HasActive || (ability.CopyOpponentLastUsed && !string.IsNullOrEmpty(opPrevUseID)));
					} else {
						btn.interactable = alive;
					}

					var cooldownTxt = grabber.Grab<Text>("Cooldown");
					cooldownTxt.gameObject.SetActive(!devMode && alive && (ability.CopyOpponentLastUsed || ability.HasActive));
					cooldownTxt.text = cooldown > 0 ? cooldown.ToString() : "";

					grabber.Grab<GreyImage>("Icon").SetGrey(
						!devMode && ability.HasActive && !btn.interactable
					);
					grabber.Grab<RectTransform>("Red Panel").gameObject.SetActive(
						!devMode && !m_Game.Game.CheckShipAlive(i, group)
					);
					if (devMode) {
						grabber.Grab<RectTransform>("Highlight").gameObject.SetActive(
							(group == Group.A ? m_Game.Game.DevShipIndexA : m_Game.Game.DevShipIndexB) == i
						);
					} else {
						grabber.Grab<RectTransform>("Highlight").gameObject.SetActive(
							currentTurn == group && m_Game.Game.AbilityShipIndex == i
						);
					}

					var copy = grabber.Grab<Image>("Copy");
					bool copyActive = !devMode && ability.CopyOpponentLastUsed && !string.IsNullOrEmpty(opPrevUseID);
					copy.gameObject.SetActive(copyActive);
					if (copyActive) {
						var copyShipData = m_Game.Asset.GetShipData(opPrevUseID);
						copy.sprite = copyShipData?.Sprite;
					}

				}

				for (int i = count; i < count + 1; i++) {
					var grabber = container.GetChild(i).GetComponent<Grabber>();
					grabber.gameObject.SetActive(devMode);
					if (devMode) {
						grabber.Grab<RectTransform>("Highlight").gameObject.SetActive(
							(group == Group.A ? m_Game.Game.DevShipIndexA : m_Game.Game.DevShipIndexB) == i
						);
					}
				}

			}
		}


		public void UI_RefreshBattleInfoUI () => RefreshBattleInfoUI();


		public void UI_SetCheat () => m_Game.Game.Cheated = true;


		public void UI_SetSelectingStrategyA (int index) {
			StrategyIndexA.Value = Mathf.Clamp(index, 0, Strategies.Count - 1);
			RefreshStrategyUI();
		}


		public void UI_SetSelectingStrategyB (int index) {
			StrategyIndexB.Value = Mathf.Clamp(index, 0, Strategies.Count - 1);
			RefreshStrategyUI();
		}


		public void UI_ClearShipSelection () => ClearShipSelection();


		public void UI_QuitGame () {
			QuitGameForReal = true;
			Application.Quit();
		}


		// Setting
		public void UI_SoundToggle (bool isOn) {
			UseSound.Value = isOn;
			AudioListener.volume = isOn ? 1f : 0f;
		}


		public void UI_AutoPlayAvAToggle (bool isOn) {
			AutoPlayAvA.Value = isOn;
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