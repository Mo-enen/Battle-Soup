using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;


// Start Remake at 2022/6/23
namespace BattleSoup {
	public partial class BattleSoup : Game {




		#region --- VAR ---


		// Const
		private const string SHIP_EDITOR_FLEET = "Sailboat,SeaMonster,Longboat,MiniSub";
		private readonly Map EMPTY_MAP = new() { Size = 8, Content = new int[8 * 8] };
		private readonly Map SHIP_EDITOR_MAP = new() { Size = 8, Content = new int[] { 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0 }, };

		// Api
		public GameState State { get; private set; } = GameState.Title;
		public GameMode Mode { get; private set; } = GameMode.PvA;
		public Turn CurrentTurn { get; private set; } = Turn.A;
		public string BuiltInShipRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Ships");
		public string CustomShipRoot => Util.CombinePaths(Application.persistentDataPath, "Ships");
		public string MapRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Maps");
		public eField FieldA { get; private set; } = null;
		public eField FieldB { get; private set; } = null;
		public bool Cheating { get; set; } = false;
		public bool Cheated { get; private set; } = false;
		public Vector2Int PickingPosition { get; private set; } = default;
		public Direction4 PickingDirection { get; private set; } = default;

		// Setting
		public bool UseSound { get => s_UseSound.Value; set => s_UseSound.Value = value; }
		public bool AutoPlayForAvA { get => s_AutoPlayForAvA.Value; set => s_AutoPlayForAvA.Value = value; }
		public bool UseAnimation { get => s_UseAnimation.Value; set => s_UseAnimation.Value = value; }

		// Ser
		[SerializeField] GameAsset m_Assets = null;

		// Data
		private readonly Dictionary<int, Ship> ShipPool = new();
		private readonly List<ShipMeta> ShipMetas = new();
		private readonly Dictionary<int, Ability> AbilityPool = new();
		private readonly List<SoupAI> AllAi = new();
		private readonly List<Map> AllMaps = new();
		private readonly List<Sprite> CustomShipSprites = new();
		private bool GameOver = false;
		private bool DevMode = false;
		private bool AvAPlaying = true;
		private bool PrevHasStep = false;
		private int DialogFrame = int.MinValue;
		private SoupAI RobotA = null;
		private SoupAI RobotB = null;

		// Saving
		private readonly SavingString s_PlayerFleet = new("BattleSoup.PlayerFleet", "Sailboat,SeaMonster,Longboat,MiniSub");
		private readonly SavingBool s_UseSound = new("BattleSoup.UseSound", true);
		private readonly SavingBool s_AutoPlayForAvA = new("BattleSoup.AutoPlayForAvA", true);
		private readonly SavingBool s_UseAnimation = new("BattleSoup.UseAnimation", true);
		private readonly SavingBool s_UseScreenEffect = new("BattleSoup.UseScreenEffect", true);
		private readonly SavingInt s_SelectingAiA = new("BattleSoup.SelectingAiA", 0);
		private readonly SavingInt s_SelectingAiB = new("BattleSoup.SelectingAiB", 0);
		private readonly SavingInt s_MapIndexA = new("BattleSoup.MapIndexA", 0);
		private readonly SavingInt s_MapIndexB = new("BattleSoup.MapIndexB", 0);
		private readonly SavingInt s_UiScale = new("BattleSoup.UiScale", 1);


		#endregion




		#region --- MSG ---


		// Init
		protected override void Initialize () {

			base.Initialize();

			CustomShipSprites.Clear();
			int id = "Custom Ship".AngeHash();
			for (int index = 0; CellRenderer.TryGetSpriteFromGroup(id, index, out var sprite, false, false); index++) {
				CustomShipSprites.Add(CellRenderer.CreateUnitySprite(sprite.GlobalID));
			}
			ReloadShipEditorArtworkPopupUI();

			Init_AI();
			SetUiScale(s_UiScale.Value);
			SetUseScreenEffect(s_UseScreenEffect.Value);

			m_Assets.AbilityHintA.Blink(0);
			m_Assets.AbilityHintB.Blink(0);

			ReloadShipDataFromDisk();
			ReloadMapDataFromDisk();

			AddEntity(typeof(eBackgroundAnimation).AngeHash(), 0, 0);
			FieldA = AddEntity(typeof(eField).AngeHash(), 0, 0) as eField;
			FieldB = AddEntity(typeof(eField).AngeHash(), 0, 0) as eField;
			FrameInput.AddCustomKey(KeyCode.Escape);

			OnMapChanged();
			OnFleetChanged();

			SwitchState(GameState.Title);
			SwitchMode(GameMode.PvA);
			RefreshCameraView(true);
		}


		private void Init_AI () {
			AllAi.Clear();
			foreach (var type in typeof(SoupAI).AllChildClass()) {
				if (System.Activator.CreateInstance(type) is SoupAI ai) {
					AllAi.Add(ai);
				}
			}
			s_SelectingAiA.Value = s_SelectingAiA.Value.Clamp(0, AllAi.Count - 1);
			s_SelectingAiB.Value = s_SelectingAiB.Value.Clamp(0, AllAi.Count - 1);

			// UI
			m_Assets.RobotAiA.ClearOptions();
			m_Assets.RobotAiB.ClearOptions();
			var options = new List<string>();
			foreach (var ai in AllAi) {
				options.Add(ai.DisplayName);
			}
			m_Assets.RobotAiA.AddOptions(options);
			m_Assets.RobotAiB.AddOptions(options);
			m_Assets.RobotAiA.SetValueWithoutNotify(s_SelectingAiA.Value);
			m_Assets.RobotAiB.SetValueWithoutNotify(s_SelectingAiB.Value);
		}


		// Update
		protected override void FrameUpdate () {
			base.FrameUpdate();
			AudioPlayer.SetMute(!s_UseSound.Value);
			Update_StateRedirect();
			RefreshCameraView();
			RefreshCanvasSize();
			switch (State) {
				case GameState.Prepare:
					Update_Prepare();
					break;
				case GameState.Playing:
					Update_Playing();
					Update_Robots();
					break;
				case GameState.CardGame:
					Update_Card();
					break;
				case GameState.ShipEditor:
					Update_ShipEditor();
					break;
			}
		}


		private void Update_StateRedirect () {
			switch (State) {
				case GameState.Prepare:
				case GameState.Playing:
					if (ShipPool.Count == 0) {
						SwitchState(GameState.Title);
						m_Assets.NoShipAlert.gameObject.SetActive(true);
					}
					if (AllMaps.Count == 0) {
						SwitchState(GameState.Title);
						m_Assets.NoMapAlert.gameObject.SetActive(true);
					}
					break;
				case GameState.CardGame:

					break;
				case GameState.ShipEditor:
					if (AllMaps.Count == 0) {
						SwitchState(GameState.Title);
						m_Assets.NoMapAlert.gameObject.SetActive(true);
					}
					break;
			}
		}


		private void Update_Prepare () {

			bool preparing = m_Assets.PreparePanel.gameObject.activeSelf;
			m_Assets.PlacePanel.gameObject.SetActive(!preparing);

			// Map
			s_MapIndexA.Value = Mathf.Clamp(s_MapIndexA.Value, 0, AllMaps.Count);
			s_MapIndexB.Value = Mathf.Clamp(s_MapIndexB.Value, 0, AllMaps.Count);

			// Renderer
			FieldA.Enable = !preparing;
			FieldA.ShowShips = !preparing;
			FieldA.DragToMoveShips = !preparing;
			FieldA.RightClickToFlipShips = !preparing;

			// UI
			m_Assets.MapShipSelectorNextButton.interactable = FieldA.Ships.Length > 0;

			// Switch State for AvA
			if (Mode == GameMode.AvA && !preparing) {
				SwitchState(GameState.Playing);
			}

		}


		private void Update_Playing () {
			int dialogCount = m_Assets.DialogRoot.childCount;
			for (int i = 0; i < dialogCount; i++) {
				if (m_Assets.DialogRoot.GetChild(i).gameObject.activeSelf) {
					DialogFrame = GlobalFrame;
					break;
				}
			}
			bool noDialog = GlobalFrame > DialogFrame + 4;
			var currentStep = CellStep.CurrentStep;
			bool PvA = Mode == GameMode.PvA;
			bool aTurn = CurrentTurn == Turn.A;
			bool waitingForPlayer = !GameOver && !DevMode && PvA && aTurn && currentStep == null;
			bool picking = currentStep is sPick;

			FieldA.ShowShips = true;
			FieldA.AllowHoveringOnShip = noDialog && PvA && waitingForPlayer;
			FieldA.AllowHoveringOnWater = noDialog && PvA && waitingForPlayer;
			FieldA.HideInvisibleShip = !PvA && !Cheating;
			FieldA.ClickToAttack = false;
			FieldA.SetPickingDirection(PickingDirection);

			FieldB.ShowShips = true;
			FieldB.AllowHoveringOnShip = false;
			FieldB.AllowHoveringOnWater = noDialog && PvA && waitingForPlayer;
			FieldB.HideInvisibleShip = !Cheating;
			FieldB.ClickToAttack = noDialog && waitingForPlayer;
			FieldB.SetPickingDirection(PickingDirection);

			// On HasStep Changed
			if ((currentStep != null) != PrevHasStep) {
				RefreshShipAbilityUI(m_Assets.AbilityContainerA, FieldA, Mode == GameMode.PvA);
				RefreshShipAbilityUI(m_Assets.AbilityContainerB, FieldB, false);
				PrevHasStep = currentStep != null;
			}

			// Picking Hint
			if (m_Assets.PickingHint.gameObject.activeSelf != picking) {
				m_Assets.PickingHint.gameObject.SetActive(picking);
			}

			StopAbilityOnShipSunk();

			// Cheat
			if (Cheating && !Cheated) {
				Cheated = true;
				if (PvA) m_Assets.AvatarIconB.sprite = m_Assets.AngryRobotAvatarIcon;
			}
			if (m_Assets.CheatTG.isOn != Cheating) m_Assets.CheatTG.SetIsOnWithoutNotify(Cheating);

			// Dev Hit
			if (m_Assets.DevHitTG.gameObject.activeSelf != DevMode) {
				m_Assets.DevHitTG.gameObject.SetActive(DevMode);
			}
			if (m_Assets.DevCookTG.gameObject.activeSelf != DevMode) {
				m_Assets.DevCookTG.gameObject.SetActive(DevMode);
			}

			// Gameover Check
			if (!GameOver) {
				bool aSunk = FieldA.AllShipsSunk();
				bool bSunk = FieldB.AllShipsSunk();
				if (aSunk || bSunk) {
					CellStep.Clear();
					GameOver = true;
					if (PvA) {
						(aSunk ?
							Cheated ? m_Assets.Dialog_LoseCheat : m_Assets.Dialog_Lose :
							Cheated ? m_Assets.Dialog_WinCheat : m_Assets.Dialog_Win
						).gameObject.SetActive(true);
					}
					m_Assets.PlayAvA.gameObject.SetActive(false);
					m_Assets.PauseAvA.gameObject.SetActive(false);
					m_Assets.RestartAvA.gameObject.SetActive(!PvA);
				}
			}


		}


		private void Update_Robots () {
			if (GameOver) return;
			if (DevMode) return;
			if (Mode == GameMode.AvA && !AvAPlaying) return;
			if (CurrentTurn == Turn.A) {
				// Robot A
				if (Mode != GameMode.PvA) {
					if (CellStep.CurrentStep == null) {
						InvokeRobot(RobotA, FieldA, FieldB);
					} else if (CellStep.CurrentStep is sPick pick) {
						InvokeRobotForPick(RobotA, FieldA, FieldB, pick);
					}
				}
			} else {
				// Robot B
				if (CellStep.CurrentStep == null) {
					InvokeRobot(RobotB, FieldB, FieldA);
				} else if (CellStep.CurrentStep is sPick pick) {
					InvokeRobotForPick(RobotB, FieldB, FieldA, pick);
				}
			}
		}


		private void OnApplicationFocus (bool focus) {
			// Back to Ship Editor
			if (focus && State == GameState.ShipEditor) {
				ReloadShipDataFromDisk();
				ReloadShipEditorUI();
			}
		}


		#endregion




		#region --- API ---


		public void SwitchTurn () {
			CellStep.Clear();
			RobotA.Analyze(FieldA, FieldB);
			RobotB.Analyze(FieldB, FieldA);
			RefreshRobotWeights();
			var field = CurrentTurn == Turn.A ? FieldA : FieldB;
			foreach (var ship in field.Ships) ship.CurrentCooldown--;
			CurrentTurn = CurrentTurn.Opposite();
			RefreshShipAbilityUI(m_Assets.AbilityContainerA, FieldA, Mode == GameMode.PvA);
			RefreshShipAbilityUI(m_Assets.AbilityContainerB, FieldB, false);
			RefreshTurnLabelUI();
		}


		public bool TryGetShip (int id, out Ship ship) => ShipPool.TryGetValue(id, out ship);


		// Ability 
		public bool ClickAbility (Ship ship, eField selfField) {
			if (ship.CurrentCooldown > 0) return false;
			if (!AbilityPool.TryGetValue(ship.GlobalCode, out var ability)) return false;
			if (!ability.HasManuallyEntrance) return false;
			bool result = UseAbility(ship.GlobalCode, ship, selfField, false);
			CellStep.AddToLast(new sSwitchTurn());
			if (result) BlinkAbilityUI(ship, selfField);
			return result;
		}


		public bool UseAbility (int id, Ship ship, eField selfField, bool ignoreCooldown) {
			if (!ignoreCooldown && ship.CurrentCooldown > 0) return false;
			if (!AbilityPool.TryGetValue(id, out var ability)) return false;
			var entrance = EntranceType.OnAbilityUsed;
			if (!ignoreCooldown && ship.CurrentCooldown < 0 && ability.EntrancePool.ContainsKey(EntranceType.OnAbilityUsedOvercharged)) {
				entrance = EntranceType.OnAbilityUsedOvercharged;
			}
			bool performed = PerformAbility(
				ability, ship, entrance,
				selfField,
				selfField == FieldA ? FieldB : FieldA,
				ignoreCooldown
			);
			if (performed) selfField.LastPerformedAbilityID = id;
			return true;
		}


		public bool PerformAbility (Ability ability, Ship ship, EntranceType entrance, eField selfField, eField opponentField, bool ignoreCooldown) {
			if (!entrance.IsManualEntrance()) ignoreCooldown = true;
			if (!ignoreCooldown && ship.CurrentCooldown > 0) return false;
			FieldA.ClearLastActionResult();
			FieldB.ClearLastActionResult();
			bool performed = ability.Perform(ship, entrance, selfField, opponentField);
			if (!ability.HasManuallyEntrance || entrance.IsManualEntrance()) {
				ship.CurrentCooldown = ship.MaxCooldown;
			}
			RefreshShipAbilityUI(m_Assets.AbilityContainerA, FieldA, Mode == GameMode.PvA);
			RefreshShipAbilityUI(m_Assets.AbilityContainerB, FieldB, false);
			return performed;
		}


		public void AbandonAbility () => CellStep.Clear(typeof(sSwitchTurn));


		public bool TryGetAbility (int id, out Ability ability) => AbilityPool.TryGetValue(id, out ability);


		// Picking
		public void SetPickingPosition (Vector2Int pos) => PickingPosition = pos;


		public void SwitchPickingDirection () {
			PickingDirection = PickingDirection switch {
				Direction4.Up => Direction4.Right,
				Direction4.Down => Direction4.Left,
				Direction4.Left => Direction4.Up,
				_ or Direction4.Right => Direction4.Down,
			};
			FieldA.SetPickingDirection(PickingDirection);
			FieldB.SetPickingDirection(PickingDirection);
		}


		public void SetPickingDirection (Direction4 dir) {
			PickingDirection = dir;
			FieldA.SetPickingDirection(dir);
			FieldB.SetPickingDirection(dir);
		}


		public Vector2Int GetPickedPosition (int localX, int localY) => SoupUtil.GetPickedPosition(
			PickingPosition, PickingDirection, localX, localY
		);


		#endregion




		#region --- LGC ---


		private void SwitchState (GameState state) {

			// Check Valid before Play
			if (state == GameState.Playing) {
				if (Mode == GameMode.PvA) {
					if (!FieldA.IsValidForPlay(out _)) {
						m_Assets.FailPlacingShipsDialog.gameObject.SetActive(true);
						return;
					}
				} else {
					if (!FieldA.IsValidForPlay(out _) || !FieldB.IsValidForPlay(out _)) {
						m_Assets.RobotFailedToPlaceShipsDialog.gameObject.SetActive(true);
						return;
					}
				}
			}

			// Panel
			State = state;
			int count = m_Assets.PanelRoot.childCount;
			for (int i = 0; i < count; i++) {
				m_Assets.PanelRoot.GetChild(i).gameObject.SetActive(i == (int)state);
			}

			// For All
			CellStep.Clear();
			Cheating = false;
			Cheated = false;
			GameOver = false;
			FieldA.ClickToAttack = false;
			FieldB.ClickToAttack = false;
			FieldA.DevShipIndex = 0;
			FieldB.DevShipIndex = 0;
			SetDevMode(false);
			if (state != GameState.Playing) {
				RefreshRobotWeights(true);
			}

			// For Each
			switch (state) {
				case GameState.Title:
					FieldA.Enable = false;
					FieldB.Enable = false;
					break;
				case GameState.Prepare:
					SwitchState_Prepare();
					break;
				case GameState.Playing:
					SwitchState_Playing();
					break;
				case GameState.CardGame:
					SwitchState_CardGame();
					break;
				case GameState.ShipEditor:
					SwitchState_ShipEditor();
					break;
			}
		}


		private void SwitchState_Prepare () {
			m_Assets.PreparePanel.gameObject.SetActive(true);
			m_Assets.PlacePanel.gameObject.SetActive(false);

			FieldA.AllowHoveringOnShip = true;
			FieldA.AllowHoveringOnWater = false;
			FieldA.HideInvisibleShip = false;
			FieldA.GameStart();

			FieldB.Enable = false;
			FieldB.AllowHoveringOnShip = false;
			FieldB.AllowHoveringOnWater = false;
			FieldB.HideInvisibleShip = false;
			FieldB.ShowShips = false;
			FieldB.DragToMoveShips = false;
			FieldB.RightClickToFlipShips = false;
			FieldB.GameStart();

			ReloadRobots();
			ReloadFleetRendererUI();
		}


		private void SwitchState_Playing () {

			if (Random.value > 0.5f) CurrentTurn = CurrentTurn.Opposite();
			s_MapIndexA.Value = s_MapIndexA.Value.Clamp(0, AllMaps.Count - 1);
			s_MapIndexB.Value = s_MapIndexB.Value.Clamp(0, AllMaps.Count - 1);
			bool PvA = Mode == GameMode.PvA;
			RefreshTurnLabelUI();
			AvAPlaying = s_AutoPlayForAvA.Value;
			m_Assets.PlayAvA.gameObject.SetActive(!PvA && !AvAPlaying);
			m_Assets.PauseAvA.gameObject.SetActive(!PvA && AvAPlaying);
			m_Assets.RestartAvA.gameObject.SetActive(false);
			m_Assets.AvatarIconB.sprite = m_Assets.RobotAvatarIcon;

			// A
			var shiftA = new Vector2Int(0, AllMaps[s_MapIndexB.Value].Size + 2);
			FieldA.Enable = true;
			FieldA.ShowShips = false;
			FieldA.DragToMoveShips = false;
			FieldA.RightClickToFlipShips = false;

			if (!PvA) {
				bool successA = FieldA.RandomPlaceShips(256);
				if (!successA) {
					m_Assets.RobotFailedToPlaceShipsDialog.gameObject.SetActive(true);
					SwitchState(GameState.Prepare);
				}
			}
			FieldA.LocalShift = shiftA;
			FieldA.DrawPickingArrow = true;

			// B
			FieldB.Enable = true;
			FieldB.ShowShips = false;
			FieldB.DragToMoveShips = false;
			FieldB.RightClickToFlipShips = false;
			FieldB.DrawPickingArrow = true;
			bool success = FieldB.RandomPlaceShips(256);
			if (!success) {
				m_Assets.RobotFailedToPlaceShipsDialog.gameObject.SetActive(true);
				SwitchState(GameState.Prepare);
			}

			// Final
			ReloadRobots();
			PickingPosition = default;
			PickingDirection = default;
			FieldA.GameStart();
			FieldB.GameStart();
			ReloadShipAbilityUI(FieldA, m_Assets.AbilityContainerA, m_Assets.ShipAbilityItem, PvA);
			ReloadShipAbilityUI(FieldB, m_Assets.AbilityContainerB, m_Assets.ShipAbilityItem, false);

			RobotA.Analyze(FieldA, FieldB);
			RobotB.Analyze(FieldB, FieldA);
			RefreshRobotWeights();
			FieldA.DrawCookedInfo = false;
			FieldB.DrawCookedInfo = false;
			m_Assets.DevHitTG.SetIsOnWithoutNotify(false);
			m_Assets.DevCookTG.SetIsOnWithoutNotify(false);
		}


		private void SwitchMode (GameMode mode) {
			Mode = mode;
			switch (mode) {
				case GameMode.PvA:
					m_Assets.MapSelectorLabelA.text = "Your Map";
					m_Assets.MapSelectorLabelB.text = "Opponent Map";
					m_Assets.FleetSelectorLabelA.text = "Your Fleet";
					m_Assets.FleetSelectorLabelB.text = "Opponent Fleet";
					m_Assets.FleetSelectorPlayer.gameObject.SetActive(true);
					m_Assets.FleetSelectorRobotA.gameObject.SetActive(false);
					m_Assets.AvatarIconA.sprite = m_Assets.PlayerAvatarIcon;
					m_Assets.AvatarIconB.sprite = m_Assets.RobotAvatarIcon;
					m_Assets.AvatarLabelA.text = "Player";
					ReloadFleetRendererUI();
					OnFleetChanged();
					break;
				case GameMode.AvA:
					m_Assets.MapSelectorLabelA.text = "Robot A Map";
					m_Assets.MapSelectorLabelB.text = "Robot B Map";
					m_Assets.FleetSelectorLabelA.text = "Robot A Fleet";
					m_Assets.FleetSelectorLabelB.text = "Robot B Fleet";
					m_Assets.FleetSelectorPlayer.gameObject.SetActive(false);
					m_Assets.FleetSelectorRobotA.gameObject.SetActive(true);
					m_Assets.AvatarIconA.sprite = m_Assets.RobotAvatarIcon;
					m_Assets.AvatarIconB.sprite = m_Assets.RobotAvatarIcon;
					m_Assets.AvatarLabelA.text = "Robot A";
					ReloadFleetRendererUI();
					OnFleetChanged();
					break;
			}
		}


		private void OnMapChanged () {
			FieldA.SetMap(State == GameState.ShipEditor ? EMPTY_MAP : AllMaps[s_MapIndexA.Value = s_MapIndexA.Value.Clamp(0, AllMaps.Count - 1)]);
			FieldB.SetMap(State == GameState.ShipEditor ? SHIP_EDITOR_MAP : AllMaps[s_MapIndexB.Value = s_MapIndexB.Value.Clamp(0, AllMaps.Count - 1)]);
		}


		private void OnFleetChanged (bool ignoreA = false, bool ignoreB = false) {
			if (!ignoreA) {
				FieldA.SetShips(
					GetShipsFromFleetString(Mode == GameMode.PvA ? s_PlayerFleet.Value : GetBotFleetA())
				);
				if (State != GameState.ShipEditor) {
					FieldA.RandomPlaceShips(256);
				} else if (FieldA.Ships.Length > 0) {
					var ship = FieldA.Ships[0];
					ship.FieldX = 1;
					ship.FieldY = 1;
					ship.Flip = false;
					FieldA.RefreshCellShipCache();
					FieldA.ClampInvalidShipsInside();
				}
				m_Assets.RobotDescriptionA.text = RobotA != null ? RobotA.Description : "";
			}
			if (!ignoreB) {
				FieldB.SetShips(
					GetShipsFromFleetString(GetBotFleetB())
				);
				FieldB.RandomPlaceShips(256);
				m_Assets.RobotDescriptionB.text = RobotB != null ? RobotB.Description : "";
			}
			ReloadRobots();
		}


		private Ship[] GetShipsFromFleetString (string fleet) {
			var ships = new List<Ship>();
			var shipNames = fleet.Split(',');
			foreach (var name in shipNames) {
				if (ShipPool.TryGetValue(name.AngeHash(), out var ship)) {
					ships.Add(ship.CreateDataCopy());
				}
			}
			return ships.ToArray();
		}


		private void SetUiScale (int id) {
			s_UiScale.Value = id.Clamp(0, 2);
			m_Assets.CanvasScaler.referenceResolution = new(
				1024,
				s_UiScale.Value == 0 ? 1200 :
				s_UiScale.Value == 1 ? 1024 : 910
			);
			m_Assets.UiScaleTGs[0].SetIsOnWithoutNotify(s_UiScale.Value == 0);
			m_Assets.UiScaleTGs[1].SetIsOnWithoutNotify(s_UiScale.Value == 1);
			m_Assets.UiScaleTGs[2].SetIsOnWithoutNotify(s_UiScale.Value == 2);
		}


		private void SetUseScreenEffect (bool use) {
			s_UseScreenEffect.Value = use;
			m_Assets.UseEffectTG.SetIsOnWithoutNotify(use);
			m_Assets.EffectVolume.enabled = use;
		}


		private void BlinkAbilityUI (Ship ship, eField field) {
			var img = field == FieldA ? m_Assets.AbilityHintA : m_Assets.AbilityHintB;
			img.Image.sprite = ship.Icon;
			img.Blink(2f);
		}


		private void StopAbilityOnShipSunk () {
			var step = CellStep.CurrentStep;
			if (step != null) {
				Ship ship = null;
				switch (step) {
					case sSoupStep sStep:
						ship = sStep.Ship;
						break;
					case sPick pick:
						ship = pick.Ship;
						break;
				}
				if (ship != null && !ship.Alive) {
					AbandonAbility();
				}
			}
		}


		private void RefreshCanvasSize () {
			var canvas = m_Assets.Canvas;
			if (canvas.renderMode != RenderMode.WorldSpace) return;
			var camera = CellRenderer.MainCamera;
			var rt = canvas.transform as RectTransform;
			float cameraHeight = 2f * camera.orthographicSize;
			rt.localScale = new Vector3(
				cameraHeight / rt.rect.height,
				cameraHeight / rt.rect.height,
				cameraHeight / rt.rect.height
			);
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rt.rect.height * camera.aspect);
		}


		// Dev
		private void SetDevMode (bool devMode) {
			DevMode = devMode;
			FieldA.DrawDevInfo = devMode;
			FieldB.DrawDevInfo = devMode;
			m_Assets.DevTG.SetIsOnWithoutNotify(devMode);
			RefreshShipAbilityUI(m_Assets.AbilityContainerA, FieldA, Mode == GameMode.PvA);
			RefreshShipAbilityUI(m_Assets.AbilityContainerB, FieldB, false);
		}


		// Robot
		private void InvokeRobot (SoupAI robot, eField selfField, eField opponentField) {

			robot.Analyze(selfField, opponentField);

			// Perform
			var result = robot.Perform(this, -1);
			int abilityIndex = result.AbilityIndex;
			var pos = result.Position;
			// Add All Steps
			if (abilityIndex < 0) {
				// Attack
				CellStep.AddToFirst(new sAttack() {
					X = pos.x,
					Y = pos.y,
					Ship = null,
					Field = opponentField,
					Fast = false,
				});
				CellStep.AddToLast(new sSwitchTurn());
			} else {
				// Ability
				bool performed = false;
				if (abilityIndex >= 0 && abilityIndex < selfField.Ships.Length) {
					performed = ClickAbility(
						selfField.Ships[abilityIndex],
						selfField
					);
				}
				if (!performed) SwitchTurn();
			}

		}


		private void InvokeRobotForPick (SoupAI robot, eField selfField, eField opponentField, sPick pick) {

			robot.Analyze(selfField, opponentField);

			// Perform
			int usingAbilityIndex = -1;
			for (int i = 0; i < selfField.Ships.Length; i++) {
				if (pick.Ship == selfField.Ships[i]) {
					usingAbilityIndex = i;
					break;
				}
			}

			if (usingAbilityIndex < 0) {
				SwitchTurn();
				Debug.LogWarning("Failed to get usingAbilityIndex.");
				return;
			}

			var result = robot.Perform(this, usingAbilityIndex);
			if (result.AbilityIndex != usingAbilityIndex) {
				SwitchTurn();
				return;
			}

			// Pick
			pick.CancelPick();
			SetPickingDirection(result.Direction);
			SetPickingPosition(result.Position);

		}


		private void ReloadRobots () {
			var typeA = AllAi[s_SelectingAiA.Value.Clamp(0, AllAi.Count)].GetType();
			RobotA = System.Activator.CreateInstance(typeA) as SoupAI;
			var typeB = AllAi[s_SelectingAiB.Value.Clamp(0, AllAi.Count)].GetType();
			RobotB = System.Activator.CreateInstance(typeB) as SoupAI;
		}


		private void RefreshRobotWeights (bool clear = false) {
			if (!clear) {
				FieldA.Weights = RobotB.Weights;
				FieldB.Weights = RobotA.Weights;
				FieldA.HitWeights = RobotB.HitWeights;
				FieldB.HitWeights = RobotA.HitWeights;
				FieldA.CookedWeights = RobotB.CookedWeights;
				FieldB.CookedWeights = RobotA.CookedWeights;
				FieldA.CookedHitWeights = RobotB.CookedHitWeights;
				FieldB.CookedHitWeights = RobotA.CookedHitWeights;
				FieldA.MaxCookedWeight = RobotB.MaxCookedWeight;
				FieldB.MaxCookedWeight = RobotA.MaxCookedWeight;
				FieldA.MaxCookedHitWeight = RobotB.MaxCookedHitWeight;
				FieldB.MaxCookedHitWeight = RobotA.MaxCookedHitWeight;
			} else {
				FieldA.Weights = null;
				FieldB.Weights = null;
				FieldA.HitWeights = null;
				FieldB.HitWeights = null;
				FieldA.CookedWeights = null;
				FieldB.CookedWeights = null;
				FieldA.CookedHitWeights = null;
				FieldB.CookedHitWeights = null;
				FieldA.MaxCookedHitWeight = 0f;
				FieldB.MaxCookedHitWeight = 0f;
			}
		}


		// Refresh UI
		private void RefreshCameraView (bool immediately = false) {

			bool availableA = FieldA.Enable;
			bool availableB = FieldB.Enable;

			if (!availableA && !availableB) return;

			const int MIN_WIDTH = SoupConst.ISO_SIZE * 10;
			const int MIN_HEIGHT = SoupConst.ISO_SIZE * 10;

			int sizeA = availableA ? FieldA.MapSize : 0;
			int sizeB = availableB ? FieldB.MapSize : 0;

			int l0 = int.MaxValue;
			int r0 = int.MinValue;
			int d0 = int.MaxValue;
			int u0 = int.MinValue;
			if (availableA) {
				(l0, _) = FieldA.Local_to_Global(0, sizeA - 1);
				(r0, _) = FieldA.Local_to_Global(sizeA, 0);
				(_, d0) = FieldA.Local_to_Global(0, 0);
				(_, u0) = FieldA.Local_to_Global(sizeA, sizeA, 1);
			}

			int l1 = int.MaxValue;
			int r1 = int.MinValue;
			int d1 = int.MaxValue;
			int u1 = int.MinValue;
			if (availableB) {
				(l1, _) = FieldB.Local_to_Global(0, sizeB - 1);
				(r1, _) = FieldB.Local_to_Global(sizeB, 0);
				(_, d1) = FieldB.Local_to_Global(0, 0);
				(_, u1) = FieldB.Local_to_Global(sizeB, sizeB, 1);
			}

			int minX = Mathf.Min(l0, l1);
			int maxX = Mathf.Max(r0, r1) + SoupConst.ISO_SIZE / 2;
			int minY = Mathf.Min(d0, d1);
			int maxY = Mathf.Max(u0, u1);
			int uiTopSize = SoupConst.ISO_SIZE;
			int uiBottomSize = SoupConst.ISO_SIZE / 2;
			int viewHeight = ViewRect.height;
			if (State == GameState.Playing) {
				float top01 = m_Assets.TopUI.rect.height / (m_Assets.TopUI.parent as RectTransform).rect.height;
				uiTopSize = (int)(viewHeight * top01) + SoupConst.ISO_SIZE / 2;
				float bottom01 = m_Assets.BottomUI.rect.height / (m_Assets.BottomUI.parent as RectTransform).rect.height;
				uiBottomSize = (int)(viewHeight * bottom01);
			} else if (State == GameState.ShipEditor) {
				float bottom01 = m_Assets.ShipEditorBottomUI.rect.height / (m_Assets.ShipEditorBottomUI.parent as RectTransform).rect.height;
				uiBottomSize = (int)(viewHeight * bottom01);
			}
			var rect = new RectInt(minX, minY, maxX - minX, maxY - minY);
			rect = rect.Expand(SoupConst.ISO_SIZE / 2, SoupConst.ISO_SIZE / 2, uiBottomSize, uiTopSize);
			if (rect.width < MIN_WIDTH) {
				int exp = (MIN_WIDTH - rect.width) / 2;
				rect = rect.Expand(exp, exp, 0, 0);
			}
			if (rect.height < MIN_HEIGHT) {
				int exp = (MIN_HEIGHT - rect.height) / 2;
				rect = rect.Expand(0, 0, exp, exp);
			}
			var targetCameraRect = rect.ToRect().Envelope((float)Screen.width / Screen.height);

			SetViewPositionDely(
				(int)(targetCameraRect.x + targetCameraRect.width / 2) - ViewRect.width / 2,
				(int)targetCameraRect.y, immediately ? 1000 : 220
			);
			if (Mathf.Abs(ViewRect.height - targetCameraRect.height) > 4) {
				SetViewSizeDely((int)targetCameraRect.height, immediately ? 1000 : 220);
			}
		}


		private void RemoveShipFromFleetString (SavingString sString, int index) {
			var ships = new List<string>(sString.Value.Split(','));
			if (index >= 0 && index < ships.Count) {
				ships.RemoveAt(index);
				sString.Value = ships.Count > 0 ? string.Join(',', ships) : "";
			}
		}


		private void ReloadFleetRendererUI () {
			ReloadFleet(
				Mode == GameMode.PvA ? s_PlayerFleet : null,
				Mode == GameMode.PvA ? s_PlayerFleet.Value : GetBotFleetA(),
				m_Assets.FleetRendererA
			);
			ReloadFleet(
				null,
				GetBotFleetB(),
				m_Assets.FleetRendererB
			);
			// Func
			void ReloadFleet (SavingString fleet, string fleetStr, RectTransform container) {
				container.DestroyAllChirldrenImmediate();
				var ships = fleetStr.Split(',');
				for (int i = 0; i < ships.Length; i++) {
					int shipIndex = i;
					string shipName = ships[shipIndex];
					if (!ShipPool.TryGetValue(shipName.AngeHash(), out var ship) || ship.Icon == null) continue;
					// Spawn Item
					var grab = Instantiate(m_Assets.FleetRendererItem, container);
					grab.ReadyForInstantiate();
					var img = grab.Grab<Image>();
					var btn = grab.Grab<Button>();
					img.sprite = ship.Icon;
					btn.interactable = fleet != null;
					if (fleet != null) {
						btn.onClick.AddListener(() => {
							RemoveShipFromFleetString(fleet, shipIndex);
							ReloadFleetRendererUI();
							OnFleetChanged();
						});
					}
				}
			}
		}


		private void ReloadShipAbilityUI (eField field, RectTransform container, Grabber shipItem, bool interactable) {
			container.DestroyAllChirldrenImmediate();
			for (int i = 0; i < field.Ships.Length; i++) {
				int shipIndex = i;
				var ship = field.Ships[shipIndex];
				var grab = Instantiate(shipItem, container);
				grab.gameObject.name = "Ship";
				grab.ReadyForInstantiate();
				var btn = grab.Grab<Button>();
				btn.interactable = interactable;
				btn.onClick.AddListener(() => {
					if (!DevMode) {
						if (interactable) ClickAbility(ship, field);
					} else {
						field.DevShipIndex = shipIndex;
						RefreshShipAbilityUI(m_Assets.AbilityContainerA, FieldA, Mode == GameMode.PvA);
						RefreshShipAbilityUI(m_Assets.AbilityContainerB, FieldB, false);
					}
				});
				var img = grab.Grab<Image>("Ship");
				img.color = Color.white;
				var icon = grab.Grab<Image>("Icon");
				icon.sprite = ship.Icon;
				var label = grab.Grab<Text>("Label");
				label.text = ship.DisplayName;
				var tooltip = grab.Grab<TooltipUI>();
				tooltip.Tooltip = ship.Description;
				var cooldown = grab.Grab<Text>("Cooldown");
				cooldown.text = ship.DefaultCooldown.ToString();
				var shape = grab.Grab<ShipShapeUI>();
				shape.Ship = ship;
				var mIcon = grab.Grab<Image>("Mirror Icon");
				mIcon.sprite = m_Assets.EmptyMirrorShipIcon;
				if (AbilityPool.TryGetValue(ship.GlobalCode, out var ability)) {
					cooldown.gameObject.SetActive(ability.HasManuallyEntrance);
					mIcon.gameObject.SetActive(ability.HasCopySelfAction || ability.HasCopyOpponentAction);
					if (!ability.HasManuallyEntrance) {
						var cursor = grab.Grab<CursorUI>();
						cursor.enabled = false;
					}
				} else {
					mIcon.gameObject.SetActive(false);
				}
				var bImg = grab.Grab<BlinkImage>();
				bImg.enabled = false;
				var error = grab.Grab<RectTransform>("Error");
				error.gameObject.SetActive(!AbilityPool.ContainsKey(ship.GlobalCode));
			}
			RefreshShipAbilityUI(container, field, interactable);
		}


		private void RefreshShipAbilityUI (RectTransform container, eField field, bool interactable) {
			int count = container.childCount;
			for (int i = 0; i < count && i < field.Ships.Length; i++) {

				var ship = field.Ships[i];
				var grab = container.GetChild(i).GetComponent<Grabber>();
				if (grab == null) continue;

				var btn = grab.Grab<Button>();
				if (!AbilityPool.TryGetValue(ship.GlobalCode, out var ability)) continue;
				if (DevMode) {
					btn.interactable = ship.Alive;
				} else {
					btn.interactable = ship.Alive && interactable && !GameOver && (!ability.HasManuallyEntrance || ship.CurrentCooldown <= 0) && CellStep.CurrentStep == null;
				}

				var block = btn.colors;
				block.disabledColor = ship.Alive ? new Color32(200, 200, 200, 128) : Color.white;
				btn.colors = block;

				var cooldown = grab.Grab<Text>("Cooldown");
				cooldown.text = ship.Alive && !GameOver && ship.CurrentCooldown > 0 ? ship.CurrentCooldown.ToString() : "";

				var img = grab.Grab<Image>("Ship");
				img.color = ship.Alive ? Color.white : new Color32(242, 76, 46, 255);

				var mIcon = grab.Grab<Image>("Mirror Icon");
				mIcon.gameObject.SetActive(ability.HasCopySelfAction || ability.HasCopyOpponentAction);
				if (ability.HasCopySelfAction || ability.HasCopyOpponentAction) {
					// Mirror Icon
					var opponentField = field == FieldA ? FieldB : FieldA;
					int selfAbilityID = field.LastPerformedAbilityID;
					int opponentAbilityID = opponentField.LastPerformedAbilityID;
					int lastUsedAbility = ability.HasCopySelfAction ? selfAbilityID : opponentAbilityID;
					if (ShipPool.TryGetValue(lastUsedAbility, out var targetShip)) {
						mIcon.sprite = targetShip.Icon != null ? targetShip.Icon : m_Assets.EmptyMirrorShipIcon;
					} else {
						mIcon.sprite = m_Assets.EmptyMirrorShipIcon;
					}
				}

				// Dev Highlight
				var dev = grab.Grab<RectTransform>("Dev");
				dev.gameObject.SetActive(
					DevMode && !field.DrawCookedInfo && field.DevShipIndex == i && ship.Alive
				);

				// Mirror Interactable
				if (btn.interactable && !ability.HasSolidAction) {
					bool mInter = false;
					var opponentField = field == FieldA ? FieldB : FieldA;
					int selfAbilityID = field.LastPerformedAbilityID;
					int opponentAbilityID = opponentField.LastPerformedAbilityID;
					if (ability.HasCopySelfAction && selfAbilityID != 0) mInter = true;
					if (ability.HasCopyOpponentAction && opponentAbilityID != 0) mInter = true;
					if (!mInter) btn.interactable = false;
				}

				// Overcooldown
				var bImg = grab.Grab<BlinkImage>();
				bImg.enabled =
					ship.Alive &&
					ability.EntrancePool.ContainsKey(EntranceType.OnAbilityUsedOvercharged) &&
					ship.CurrentCooldown < 0;
			}
		}


		private void RefreshTurnLabelUI () => m_Assets.TurnLabel.text = CurrentTurn == Turn.A ? Mode == GameMode.PvA ? "Player's Turn" : "Robot A's Turn" : "Robot B's Turn";


		// Load Data
		private void ReloadShipDataFromDisk () {

			// Create Folder
			try {
				Util.CreateFolder(BuiltInShipRoot);
				Util.CreateFolder(CustomShipRoot);
			} catch (System.Exception ex) { Debug.LogException(ex); }

			// Ship Pool
			try {
				ShipPool.Clear();
				ShipMetas.Clear();
				AbilityPool.Clear();
				// Built In
				foreach (var folder in Util.GetFoldersIn(BuiltInShipRoot, true)) {
					LoadShipIntoPool(folder.FullName, true);
				}
				// Custom
				foreach (var folder in Util.GetFoldersIn(CustomShipRoot, true)) {
					LoadShipIntoPool(folder.FullName, false);
				}
				// Meta
				ShipMetas.Sort(
					(a, b) => a.IsBuiltIn != b.IsBuiltIn ?
					a.IsBuiltIn ? 1 : -1 :
					a.DisplayName.CompareTo(b.DisplayName)
				);
			} catch (System.Exception ex) { Debug.LogException(ex); }

			// Ship UI
			m_Assets.FleetSelectorPlayerContent.DestroyAllChirldrenImmediate();
			foreach (var meta in ShipMetas) {
				if (!ShipPool.TryGetValue(meta.ID, out var ship)) continue;
				var grab = Instantiate(m_Assets.FleetSelectorShipItem, m_Assets.FleetSelectorPlayerContent);
				var rt = grab.transform as RectTransform;
				var img = grab.Grab<Image>("Icon");
				img.sprite = ship.Icon != null ? ship.Icon : m_Assets.DefaultShipIcon;
				var label = grab.Grab<Text>("Label");
				label.text = ship.DisplayName;
				var btn = grab.Grab<Button>();
				btn.onClick.AddListener(() => {
					if (string.IsNullOrEmpty(s_PlayerFleet.Value) || s_PlayerFleet.Value.EndsWith(',')) {
						s_PlayerFleet.Value += $"{ship.GlobalName}";
					} else {
						s_PlayerFleet.Value += $",{ship.GlobalName}";
					}
					ReloadFleetRendererUI();
					OnFleetChanged();
				});
				var tooltip = grab.Grab<TooltipUI>();
				tooltip.Tooltip = ship.Description;
				var shape = grab.Grab<ShipShapeUI>();
				shape.Ship = ship;
				var cooldown = grab.Grab<Text>("Cooldown");
				cooldown.text = ship.MaxCooldown.ToString();
				var error = grab.Grab<RectTransform>("Error");
				error.gameObject.SetActive(!AbilityPool.ContainsKey(ship.GlobalCode));
			}
		}


		private void LoadShipIntoPool (string folderName, bool builtIn) {
			try {
				int globalID = 0;
				Ship ship = null;
				var meta = new ShipMeta();
				// Info
				string infoPath = Util.CombinePaths(folderName, "Info.json");
				if (Util.FileExists(infoPath)) {
					ship = JsonUtility.FromJson<Ship>(Util.FileToText(infoPath));
					if (ship == null) return;
					globalID = ship.GlobalCode;
					ship.BuiltIn = builtIn;
					if (ShipPool.TryAdd(ship.GlobalCode, ship)) {
						meta.ID = ship.GlobalCode;
						meta.DisplayName = ship.DisplayName;
						meta.IsBuiltIn = builtIn;
					}
				} else return;
				// Icon
				if (ship != null) {
					string iconPath = Util.CombinePaths(folderName, "Icon.png");
					if (!Util.FileExists(iconPath)) iconPath = Util.CombinePaths(folderName, "Icon.jpg");
					if (!Util.FileExists(iconPath)) iconPath = Util.CombinePaths(folderName, "Icon.jpeg");
					if (Util.FileExists(iconPath)) {
						var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false) {
							filterMode = FilterMode.Bilinear,
							anisoLevel = 0,
							wrapMode = TextureWrapMode.Clamp,
						};
						texture.LoadImage(Util.FileToByte(iconPath));
						ship.Icon = Sprite.Create(
							texture,
							new Rect(0, 0, texture.width, texture.height),
							Vector2.one * 0.5f
						);
					}
					if (ship.Icon == null) ship.Icon = m_Assets.DefaultShipIcon;
					meta.Icon = ship.Icon;
				}
				ShipMetas.Add(meta);
				// Ability
				string abPath = Util.CombinePaths(folderName, "Ability.txt");
				if (Util.FileExists(abPath)) {
					string code = Util.FileToText(abPath);
					var exe = AbilityCompiler.Compile(globalID, code, out string error);
					if (exe != null) {
						AbilityPool.TryAdd(globalID, exe);
					} else {
						Debug.LogError(folderName + "\n" + error);
					}
				}
			} catch (System.Exception ex) { Debug.LogWarning(folderName); Debug.LogException(ex); }

		}


		private void RemoveShipFromPool (int id) {
			ShipMetas.RemoveAll(meta => meta.ID == id);
			ShipPool.Remove(id);
			AbilityPool.Remove(id);
		}


		private void ReloadMapDataFromDisk () {

			// Load Data from Disk
			try {
				AllMaps.Clear();
				foreach (var file in Util.GetFilesIn(MapRoot, false, "*.png")) {
					try {
						var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false) {
							filterMode = FilterMode.Point,
							anisoLevel = 0,
							wrapMode = TextureWrapMode.Clamp,
						};
						texture.LoadImage(Util.FileToByte(file.FullName));
						if (texture.width != texture.height) {
							Debug.LogWarning($"Map texture \"{file.Name}\" have differect width and height.");
							continue;
						}
						if (texture.width == 0 || texture.height == 0) continue;
						var pixels = texture.GetPixels32();
						var map = new Map() {
							Size = texture.width,
							Content = new int[pixels.Length],
						};
						for (int i = 0; i < pixels.Length; i++) {
							map.Content[i] = pixels[i].r < 128 ? 1 : 0;
						}
						AllMaps.Add(map);
					} catch (System.Exception ex) { Debug.LogException(ex); }
				}
				AllMaps.Sort((a, b) => a.Size.CompareTo(b.Size));
			} catch (System.Exception ex) { Debug.LogException(ex); }

			s_MapIndexA.Value = s_MapIndexA.Value.Clamp(0, AllMaps.Count - 1);
			s_MapIndexB.Value = s_MapIndexB.Value.Clamp(0, AllMaps.Count - 1);

			// Reload UI
			ReloadMapUI(m_Assets.MapSelectorItem, m_Assets.MapSelectorContentA, s_MapIndexA.Value);
			ReloadMapUI(m_Assets.MapSelectorItem, m_Assets.MapSelectorContentB, s_MapIndexB.Value);

			// Func
			void ReloadMapUI (Grabber itemSource, RectTransform content, int selectingIndex) {
				content.DestroyAllChirldrenImmediate();
				var group = content.GetComponent<ToggleGroup>();
				foreach (var map in AllMaps) {
					var item = Instantiate(itemSource, content);
					item.gameObject.SetActive(true);
					item.transform.SetAsLastSibling();
					var mapRenderer = item.Grab<MapRendererUI>();
					var tg = item.Grab<Toggle>();
					var label = item.Grab<Text>();
					mapRenderer.Map = map;
					tg.SetIsOnWithoutNotify(item.transform.GetSiblingIndex() == selectingIndex);
					tg.group = group;
					label.text = $"{map.Size}¡Á{map.Size}";
				}
			}

		}


		private string GetBotFleetA () => State == GameState.ShipEditor ?
			(ShipPool.TryGetValue(EditingID, out var ship) ? ship.GlobalName : "") :
			AllAi[s_SelectingAiA.Value.Clamp(0, AllAi.Count - 1)].Fleet;


		private string GetBotFleetB () => State == GameState.ShipEditor ?
			SHIP_EDITOR_FLEET :
			AllAi[s_SelectingAiB.Value.Clamp(0, AllAi.Count - 1)].Fleet;


		#endregion




	}
}