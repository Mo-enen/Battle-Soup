using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BattleSoupAI;
using System.Linq;

namespace BattleSoup {
	public class Game : MonoBehaviour {




		#region --- SUB ---


		public delegate ShipData ShipDataStringHandler (string key);

		[System.Serializable] public class VoidVector3Event : UnityEvent<Vector3> { }
		[System.Serializable] public class VoidEvent : UnityEvent { }
		[System.Serializable] public class VoidStringEvent : UnityEvent<string> { }



		public class GameData {

			public SoupStrategy Strategy;
			public MapData Map = null;
			public ShipData[] ShipDatas = null;
			public Ship[] Ships = null;
			public Tile[,] Tiles = null;
			public int[] Cooldowns = null;
			public bool[] ShipsAlive = null;
			public ShipPosition[] Positions = null;
			public ShipPosition?[] KnownPositions = null;
			public List<SonarPosition> Sonars = new List<SonarPosition>();


			// API
			public void Init (SoupStrategy strategy, MapData map, ShipData[] ships, ShipPosition[] positions) {

				Map = map;
				ShipDatas = ships;
				Strategy = strategy;

				// Ships
				Ships = ShipData.GetShips(ships);

				// Pos
				Positions = positions;
				KnownPositions = new ShipPosition?[positions.Length];

				// Tiles
				Tiles = new Tile[map.Size, map.Size];
				for (int j = 0; j < map.Size; j++) {
					for (int i = 0; i < map.Size; i++) {
						Tiles[i, j] = Tile.GeneralWater;
					}
				}
				foreach (var stone in map.Stones) {
					if (stone.x >= 0 && stone.x < map.Size && stone.y >= 0 && stone.y < map.Size) {
						Tiles[stone.x, stone.y] = Tile.GeneralStone;
					}
				}

				// Cooldown
				Cooldowns = new int[ships.Length];
				for (int i = 0; i < ships.Length; i++) {
					Cooldowns[i] = ships[i].Ship.Ability.Cooldown - 1;
				}

				// Ships Alive
				ShipsAlive = new bool[ShipDatas.Length];
				for (int i = 0; i < ShipsAlive.Length; i++) {
					ShipsAlive[i] = true;
				}

				// Super Revealed
				//SuperRevealed = new bool[ShipDatas.Length];

			}


			public void Clear () {
				Strategy = null;
				Map = null;
				ShipDatas = null;
				Ships = null;
				Tiles = null;
				ShipsAlive = null;
				Cooldowns = null;
				Positions = null;
				Sonars.Clear();
			}


			public int GetAliveShipCount () {
				int result = 0;
				foreach (var alive in ShipsAlive) {
					if (alive) {
						result++;
					}
				}
				return result;
			}


			public int GetSunkShipCount () {
				int result = 0;
				foreach (var alive in ShipsAlive) {
					if (!alive) {
						result++;
					}
				}
				return result;
			}


			public int GetTileCount (int index, Tile filter) {
				int result = 0;
				var sPos = Positions[index];
				foreach (var v in Ships[index].Body) {
					var pos = sPos.GetPosition(v);
					if (filter.HasFlag(Tiles[pos.x, pos.y])) {
						result++;
					}
				}
				return result;
			}


		}


		private class AbilityPerformData {
			public int AbilityAttackIndex;
			public bool WaitForPicking;
			public bool PickedPerformed;
			public bool DoTiedup;
			public bool Performed;
			public bool Pickless;
			public Int2 TiedPos;
			public Int2 PrevAttackedPos;
			public void Clear () {
				Performed = false;
				AbilityAttackIndex = 0;
				WaitForPicking = false;
				PickedPerformed = false;
				Pickless = false;
				DoTiedup = false;
				TiedPos = default;
				PrevAttackedPos = default;
			}
		}


		private enum AttackResult {
			None = 0,
			Miss = 1,
			HitShip = 2,
			RevealShip = 3,
			SunkShip = 4,
			Keep = 5,
		}


		#endregion




		#region --- VAR ---


		// Const
		private readonly Attack DEFAULT_ATTACK = new Attack() {
			X = 0,
			Y = 0,
			AvailableTarget = Tile.GeneralWater | Tile.RevealedShip,
			Trigger = AttackTrigger.Picked,
			Type = AttackType.HitTile,
		};

		// Api
		public static ShipDataStringHandler GetShip { get; set; } = null;
		public Group CurrentTurn { get; private set; } = Group.A;
		public int ShipCountA => DataA.Ships.Length;
		public int ShipCountB => DataB.Ships.Length;
		public AbilityDirection AbilityDirection { get; private set; } = AbilityDirection.Up;
		public int AbilityShipIndex { get; private set; } = -1;
		public string PrevUsedAbilityA { get; private set; } = "";
		public string PrevUsedAbilityB { get; private set; } = "";
		public bool Cheated { get; set; } = false;
		public bool DevMode { get; private set; } = false;
		public bool ShipEditing { get; private set; } = false;
		public int DevShipIndexA { get; private set; } = 0;
		public int DevShipIndexB { get; private set; } = 0;

		// Ser
		[SerializeField] BattleSoupUI m_SoupA = null;
		[SerializeField] BattleSoupUI m_SoupB = null;
		[SerializeField] RectTransform m_InfoA = null;
		[SerializeField] RectTransform m_InfoB = null;
		[SerializeField] DevUI m_DevA = null;
		[SerializeField] DevUI m_DevB = null;
		[SerializeField] Image m_Face = null;
		[SerializeField] Toggle m_CheatToggle = null;
		[SerializeField] Toggle m_DevToggle = null;
		[SerializeField] Button m_ReplayButton = null;
		[SerializeField] Button m_AvAControlButton_Play = null;
		[SerializeField] Button m_AvAControlButton_Pause = null;
		[SerializeField] Button m_AvAControlButton_Next = null;
		[SerializeField] Sprite m_AttackBlink = null;
		[SerializeField] Sprite[] m_Faces = null;
		[SerializeField] VoidEvent m_RefreshUI = null;
		[SerializeField] VoidVector3Event m_OnShipHitted = null;
		[SerializeField] VoidVector3Event m_OnShipSunk = null;
		[SerializeField] VoidVector3Event m_OnWaterRevealed = null;
		[SerializeField] VoidVector3Event m_OnShipRevealed = null;
		[SerializeField] VoidVector3Event m_OnSonar = null;
		[SerializeField] VoidStringEvent m_ShowMessage = null;

		// Data
		private readonly GameData DataA = new GameData();
		private readonly GameData DataB = new GameData();
		private readonly BattleInfo InfoA = new BattleInfo();
		private readonly BattleInfo InfoB = new BattleInfo();
		private readonly Vector3[] WorldCornerCaches = new Vector3[4];
		private readonly AbilityPerformData AbilityData = new AbilityPerformData();
		private BattleMode CurrentBattleMode = BattleMode.PvA;
		private bool AvA_Playing = false;
		private bool AvA_GotoNext = false;
		private float NextUpdateTime = 0f;


		#endregion




		#region --- MSG ---


		private void OnEnable () {
			if (AllShipsSunk(Group.A) || AllShipsSunk(Group.B)) {
				gameObject.SetActive(false);
			}
			RefreshControlButtons();
		}


		private void Update () {
			Update_Aim();
			if (CurrentBattleMode == BattleMode.PvA) {
				// PvA
				if (Time.time > NextUpdateTime) {
					if (CurrentTurn == Group.A) {
						Update_Turn();
					} else {
						Update_Turn();
						NextUpdateTime = Time.time + 0.618f;
					}
				}
			} else {
				// AvA
				if ((AvA_GotoNext || AvA_Playing) && Time.time > NextUpdateTime) {
					NextUpdateTime = Time.time + 0.2f;
					AvA_GotoNext = false;
					Update_Turn();
					RefreshControlButtons();
				}
			}
		}


		private void Update_Turn () {

			if (CurrentTurn == Group.A) {
				// A Turn
				if (CurrentBattleMode == BattleMode.PvA) {
					// Player 
					if (Input.GetMouseButtonDown(0)) {
						// Mouse Left
						if (m_SoupB.GetMapPositionInside(Input.mousePosition, out var pos)) {
							if (AbilityData.WaitForPicking) {
								// Ability Attack
								AbilityData.PickedPerformed = false;
								AbilityData.WaitForPicking = false;
								if (PerformAbility(pos.x, pos.y, out _)) {
									SwitchTurn();
								} else {
									AbilityData.WaitForPicking = true;
									m_RefreshUI.Invoke();
								}
							} else if (AttackTile(DEFAULT_ATTACK, pos.x, pos.y, Group.B) != AttackResult.None) {
								// Normal Attack
								InvokeEvent(SoupEvent.Own_NormalAttack, CurrentTurn);
								InvokeEvent(SoupEvent.Opponent_NormalAttack, CurrentTurn.Opposite());
								SwitchTurn();
								m_SoupB.ClearBlinks();
								m_SoupB.Blink(pos.x, pos.y, Color.white, m_AttackBlink, 0.5f);
							}
						} else {
							CancelAbility(false);
						}
					} else if (Input.GetMouseButtonDown(1)) {
						if (AbilityShipIndex >= 0) {
							AbilityDirection = (AbilityDirection)(((int)AbilityDirection + 1) % 4);
						}
					}
				} else {
					// Robot A
					PerformRobotTurn(Group.A);
				}
			} else {
				// B Turn
				PerformRobotTurn(Group.B);
			}
			// Func
			void PerformRobotTurn (Group group) {
				var ownData = group == Group.A ? DataA : DataB;
				var oppGroup = group == Group.A ? Group.B : Group.A;
				RefreshShipsAlive(-1, group);
				RefreshShipsAlive(-1, oppGroup);
				var ownInfo = group == Group.A ? InfoA : InfoB;
				var oppInfo = group == Group.A ? InfoB : InfoA;
				var result = ownData.Strategy.Analyse(ownInfo, oppInfo, AbilityShipIndex);
				if (result.Success) {
					if (result.AbilityIndex < 0) {
						// Normal Attack
						AttackTile(DEFAULT_ATTACK, result.TargetPosition.x, result.TargetPosition.y, oppGroup);
						InvokeEvent(SoupEvent.Own_NormalAttack, CurrentTurn);
						InvokeEvent(SoupEvent.Opponent_NormalAttack, CurrentTurn.Opposite());
						SwitchTurn();
					} else if (CheckAbilityAvailable(group, result.AbilityIndex)) {
						bool combo = AbilityShipIndex >= 0;
						if (!combo) {
							// First Trigger
							if (!AbilityFirstTrigger(result.AbilityIndex)) {
								combo = true;
							}
						}
						if (combo) {
							// Combo
							AbilityDirection = result.AbilityDirection;
							if (AbilityData.WaitForPicking) {
								// Ability Attack
								AbilityData.PickedPerformed = false;
								AbilityData.WaitForPicking = false;
								if (PerformAbility(result.TargetPosition.x, result.TargetPosition.y, out bool error) || error) {
									SwitchTurn();
								} else {
									AbilityData.WaitForPicking = true;
									m_RefreshUI.Invoke();
								}
							}
						}
					} else {
						SwitchTurn();
					}
				} else {
					Debug.LogWarning(result.ErrorMessage);
					m_ShowMessage?.Invoke(result.ErrorMessage);
					SwitchTurn();
				}
			}
		}


		private void Update_Aim () {
			if (
				CurrentTurn == Group.A &&
				CurrentBattleMode == BattleMode.PvA &&
				AbilityShipIndex >= 0
			) {
				m_SoupB.RefreshAimRenderer();
			} else {
				m_SoupB.ClearAimRenderer();
			}
			m_SoupA.ClearAimRenderer();
		}


		#endregion




		#region --- API ---


		public void Init (BattleMode battleMode, SoupStrategy strategyA, SoupStrategy strategyB, MapData mapA, MapData mapB, ShipData[] shipsA, ShipData[] shipsB, ShipPosition[] positionsA, ShipPosition[] positionsB, bool shipEditing) {

			CurrentBattleMode = battleMode;
			CurrentTurn = shipEditing || Random.value > 0.5f ? Group.A : Group.B;
			AbilityData.Clear();

			DataA.Clear();
			DataB.Clear();
			DataA.Init(strategyA, mapA, shipsA, positionsA);
			DataB.Init(strategyB, mapB, shipsB, positionsB);

			InfoA.MapSize = DataA.Map.Size;
			InfoA.Ships = DataA.Ships;
			InfoA.ShipsAlive = DataA.ShipsAlive;
			InfoA.Cooldowns = DataA.Cooldowns;
			InfoA.Tiles = DataA.Tiles;
			InfoA.KnownPositions = DataA.KnownPositions;
			InfoA.Sonars = DataA.Sonars;

			InfoB.MapSize = DataB.Map.Size;
			InfoB.Ships = DataB.Ships;
			InfoB.ShipsAlive = DataB.ShipsAlive;
			InfoB.Cooldowns = DataB.Cooldowns;
			InfoB.Tiles = DataB.Tiles;
			InfoB.KnownPositions = DataB.KnownPositions;
			InfoB.Sonars = DataB.Sonars;

			m_CheatToggle.SetIsOnWithoutNotify(false);
			m_DevToggle.SetIsOnWithoutNotify(false);
			PrevUsedAbilityA = "";
			PrevUsedAbilityB = "";
			DevMode = false;
			Cheated = false;
			ShipEditing = shipEditing;

			m_CheatToggle.gameObject.SetActive(!shipEditing);
			m_DevToggle.gameObject.SetActive(!shipEditing);
			m_ReplayButton.gameObject.SetActive(false);
			m_InfoA.gameObject.SetActive(!shipEditing);
			m_InfoB.gameObject.SetActive(!shipEditing);
			m_SoupA.gameObject.SetActive(!shipEditing);

			AvA_Playing = false;
			AvA_GotoNext = false;

			RefreshControlButtons();
			m_DevA.gameObject.SetActive(false);
			m_DevB.gameObject.SetActive(false);

			gameObject.SetActive(true);
			strategyA.OnBattleStart(InfoA, InfoB);
			strategyB.OnBattleStart(InfoB, InfoA);

			var soupBRT = m_SoupB.transform as RectTransform;
			soupBRT.anchorMin = soupBRT.anchorMax = new Vector2(
				shipEditing ? 0f : 1f,
				1f
			);
			soupBRT.pivot = new Vector2(shipEditing ? 0f : 1f, 1f);
			soupBRT.anchoredPosition3D = new Vector2(0f, shipEditing ? -8f : -43f);
			soupBRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, shipEditing ? 360f : 512f);
			soupBRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, shipEditing ? 360f : 512f);

			m_SoupA.UseAbilityHint = !shipEditing;
			m_SoupB.UseAbilityHint = !shipEditing;

		}


		public void SetupDelegate () {
			m_SoupA.GetTile = (x, y) => DataA.Tiles[x, y];
			m_SoupB.GetTile = (x, y) => DataB.Tiles[x, y];
			m_SoupA.GetMap = () => DataA.Map;
			m_SoupB.GetMap = () => DataB.Map;
			m_SoupA.GetShips = () => DataA.ShipDatas;
			m_SoupB.GetShips = () => DataB.ShipDatas;
			m_SoupA.GetPositions = () => DataA.Positions;
			m_SoupB.GetPositions = () => DataB.Positions;
			m_SoupA.CheckShipAlive = (index) => CheckShipAlive(index, Group.A);
			m_SoupB.CheckShipAlive = (index) => CheckShipAlive(index, Group.B);
			m_SoupA.GetSonars = () => DataA.Sonars;
			m_SoupB.GetSonars = () => DataB.Sonars;
			m_SoupA.GetCurrentAbility = m_SoupB.GetCurrentAbility = () => {
				if (AbilityShipIndex >= 0) {
					return (CurrentTurn == Group.A ? DataA : DataB).Ships[AbilityShipIndex].Ability;
				}
				return null;
			};
			m_SoupA.GetCurrentAbilityDirection = m_SoupB.GetCurrentAbilityDirection = () => AbilityDirection;
			m_SoupA.GetCheating = m_SoupB.GetCheating = () => m_CheatToggle.isOn;
			m_SoupA.CheckShipKnown = (index) => DataA.KnownPositions[index].HasValue;
			m_SoupB.CheckShipKnown = (index) => DataB.KnownPositions[index].HasValue;
			m_SoupA.GetPrevUseShip = () => GetShip(PrevUsedAbilityA);
			m_SoupB.GetPrevUseShip = () => GetShip(PrevUsedAbilityB);
		}


		public void UI_Clear () {
			DataA.Clear();
			DataB.Clear();
			m_DevA.Clear();
			m_DevB.Clear();
			InfoA.Ships = null;
			InfoA.ShipsAlive = null;
			InfoA.Cooldowns = null;
			InfoA.Tiles = null;
			InfoB.Ships = null;
			InfoB.ShipsAlive = null;
			InfoB.Cooldowns = null;
			InfoB.Tiles = null;
		}


		public void UI_PlayAvA () {
			AvA_Playing = true;
			AvA_GotoNext = false;
			RefreshControlButtons();
		}


		public void UI_PauseAvA () {
			AvA_Playing = false;
			AvA_GotoNext = false;
			RefreshControlButtons();
		}


		public void UI_NextAvA () {
			AvA_GotoNext = true;
			AvA_Playing = false;
			RefreshControlButtons();
		}


		public void UI_SetDevMode (bool on) {
			if (on) {
				if (!m_DevA.LoadData(DataA.Strategy, InfoA) || !m_DevB.LoadData(DataB.Strategy, InfoB)) {
					m_ShowMessage.Invoke("Fail to load data");
					on = false;
				}
			}
			m_DevA.RefreshRenderer(DevShipIndexA);
			m_DevB.RefreshRenderer(DevShipIndexB);
			m_DevA.gameObject.SetActive(on);
			m_DevB.gameObject.SetActive(on);
			gameObject.SetActive(!on);
			DevMode = on;
			m_RefreshUI.Invoke();
		}


		public void UI_CancelAbility () => CancelAbility(true);


		// Ship
		public bool CheckShipAlive (int index, Group group) => (group == Group.A ? DataA : DataB).ShipsAlive[index];


		public ShipData GetShipData (Group group, int index) {
			var ships = group == Group.A ? DataA.ShipDatas : DataB.ShipDatas;
			return ships[Mathf.Clamp(index, 0, ships.Length - 1)];
		}


		// Ability
		public int GetCooldown (Group group, int index) {
			var cooldowns = group == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			return cooldowns[Mathf.Clamp(index, 0, cooldowns.Length - 1)];
		}


		public Ability GetAbility (Group group, int index) {
			var ships = group == Group.A ? DataA.ShipDatas : DataB.ShipDatas;
			return ships[Mathf.Clamp(index, 0, ships.Length - 1)].Ship.Ability;
		}


		public void OnAbilityClick (Group group, int shipIndex) {
			if (!DevMode) {
				if (
					gameObject.activeSelf &&
					group == Group.A &&
					CurrentBattleMode == BattleMode.PvA &&
					AbilityShipIndex < 0 &&
					shipIndex >= 0
				) {
					var ship = DataA.Ships[shipIndex];
					if (ship.Ability.HasActive || ship.Ability.CopyOpponentLastUsed) {
						AbilityFirstTrigger(shipIndex);
					}
				}
			} else {
				if (group == Group.A) {
					DevShipIndexA = shipIndex;
				} else {
					DevShipIndexB = shipIndex;
				}
				m_DevA.RefreshRenderer(DevShipIndexA);
				m_DevB.RefreshRenderer(DevShipIndexB);
				m_RefreshUI.Invoke();
			}
		}


		#endregion




		#region --- LGC ---


		private void SwitchTurn () {

			AbilityData.Clear();
			AbilityShipIndex = -1;

			if (!gameObject.activeSelf) { return; }

			// Check Win
			bool gameEnd = false;
			if (AllShipsSunk(Group.A)) {
				// B Win
				if (CurrentBattleMode == BattleMode.PvA) {
					m_Face.gameObject.SetActive(true);
					m_Face.sprite = m_Faces[Cheated ? 3 : 1];
					if (!ShipEditing) {
						m_ShowMessage.Invoke(Cheated ? "You cheated but still lose.\nThat sucks..." : "You Lose");
					}
				} else {
					m_Face.gameObject.SetActive(false);
					m_Face.sprite = null;
					m_ShowMessage.Invoke("Robot B Win");
				}
				gameEnd = true;
			} else if (AllShipsSunk(Group.B)) {
				// A Win
				if (CurrentBattleMode == BattleMode.PvA) {
					m_Face.gameObject.SetActive(true);
					m_Face.sprite = m_Faces[Cheated ? 2 : 0];
					if (!ShipEditing) {
						m_ShowMessage.Invoke(Cheated ? "You didn't win.\nBecause you cheated." : "You Win");
					}
				} else {
					m_Face.gameObject.SetActive(false);
					m_Face.sprite = null;
					m_ShowMessage.Invoke("Robot A Win");
				}
				gameEnd = true;
			}

			if (gameEnd) {
				DataA.Strategy.OnBattleEnd(InfoA, InfoB);
				DataB.Strategy.OnBattleEnd(InfoB, InfoA);
				gameObject.SetActive(false);
				m_CheatToggle.gameObject.SetActive(false);
				m_DevToggle.gameObject.SetActive(false);
				m_RefreshUI.Invoke();
				m_SoupA.SunkOnly = false;
				m_SoupB.SunkOnly = false;
				RefreshAllSoupRenderers();
				m_SoupA.ClearAimRenderer();
				m_SoupB.ClearAimRenderer();
				m_AvAControlButton_Play.gameObject.SetActive(false);
				m_AvAControlButton_Pause.gameObject.SetActive(false);
				m_AvAControlButton_Next.gameObject.SetActive(false);
				m_ReplayButton.gameObject.SetActive(!ShipEditing);
				if (ShipEditing) {
					// Restart
					Init(BattleMode.PvA, DataA.Strategy, DataB.Strategy,
						DataA.Map, DataB.Map, DataA.ShipDatas, DataB.ShipDatas,
						DataA.Positions, DataB.Positions, ShipEditing
					);
					CurrentTurn = Group.A;
					RefreshAllSoupRenderers();
					RefreshShipsAlive(-1, Group.A);
					RefreshShipsAlive(-1, Group.B);
				}
				return;
			}

			// Cooldown
			if (!ShipEditing) {
				var cooldowns = CurrentTurn == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
				for (int i = 0; i < cooldowns.Length; i++) {
					cooldowns[i] = Mathf.Max(cooldowns[i] - 1, 0);
				}
			} else {
				for (int i = 0; i < DataA.Cooldowns.Length; i++) {
					DataA.Cooldowns[i] = 0;
				}
			}

			// Refresh
			RefreshAllSoupRenderers();

			// Turn Change
			CurrentTurn = !ShipEditing && CurrentTurn == Group.A ? Group.B : Group.A;
			InvokeEvent(SoupEvent.Own_TurnStart, CurrentTurn);
			InvokeEvent(SoupEvent.Opponent_TurnStart, CurrentTurn.Opposite());
			m_RefreshUI.Invoke();

		}


		private bool AllShipsSunk (Group group) {
			if (DataA.ShipDatas == null || DataB.ShipDatas == null) { return false; }
			int count = (group == Group.A ? DataA.ShipDatas.Length : DataB.ShipDatas.Length);
			for (int i = 0; i < count; i++) {
				if (CheckShipAlive(i, group)) {
					return false;
				}
			}
			return true;
		}


		private void RefreshShipsAlive (int index, Group group) {
			var data = group == Group.A ? DataA : DataB;
			if (index >= 0) {
				data.ShipsAlive[index] = CheckShipAlive(index);
			} else {
				for (int i = 0; i < data.ShipsAlive.Length; i++) {
					data.ShipsAlive[i] = CheckShipAlive(i);
				}
			}
			// Func
			bool CheckShipAlive (int _index) {
				var ships = data.Ships;
				var positions = data.Positions;
				var map = data.Map;
				var tiles = data.Tiles;
				_index = Mathf.Clamp(_index, 0, ships.Length - 1);
				var ship = ships[_index];
				var body = ship.Body;
				var sPos = positions[_index];
				int aliveTile = 0;
				foreach (var v in body) {
					var pos = new Vector2Int(
						sPos.Pivot.x + (sPos.Flip ? v.y : v.x),
						sPos.Pivot.y + (sPos.Flip ? v.x : v.y)
					);
					if (pos.x >= 0 && pos.x < map.Size && pos.y >= 0 && pos.y < map.Size) {
						var tile = tiles[pos.x, pos.y];
						if (tile != Tile.HittedShip && tile != Tile.SunkShip) {
							aliveTile++;
							if (aliveTile > ship.TerminateHP) {
								return true;
							}
						}
					}
				}
				return false;
			}
		}


		private void RefreshAllSoupRenderers () {
			m_SoupA.RefreshHitRenderer();
			m_SoupB.RefreshHitRenderer();
			m_SoupA.RefreshShipRenderer();
			m_SoupB.RefreshShipRenderer();
			m_SoupA.RefreshSonarRenderer();
			m_SoupB.RefreshSonarRenderer();
		}


		private void RefreshControlButtons () {
			m_AvAControlButton_Play.gameObject.SetActive(
				CurrentBattleMode == BattleMode.AvA && !AvA_Playing && gameObject.activeSelf
			);
			m_AvAControlButton_Pause.gameObject.SetActive(
				CurrentBattleMode == BattleMode.AvA && AvA_Playing && gameObject.activeSelf
			);
			m_AvAControlButton_Next.gameObject.SetActive(
				CurrentBattleMode == BattleMode.AvA && gameObject.activeSelf
			);
		}


		private Vector3 GetWorldPosition (int x, int y, Group group) {
			var rt = (group == Group.A ? m_SoupA.transform : m_SoupB.transform) as RectTransform;
			var map = group == Group.A ? DataA.Map : DataB.Map;
			rt.GetWorldCorners(WorldCornerCaches);
			var min = WorldCornerCaches[0];
			var max = WorldCornerCaches[2];
			return new Vector3(
				Mathf.LerpUnclamped(min.x, max.x, (x + 0.5f) / map.Size),
				Mathf.LerpUnclamped(min.y, max.y, (y + 0.5f) / map.Size),
				rt.position.z
			);
		}


		// Ability
		private bool PerformAbility (int x, int y, out bool error) {

			error = false;
			var aData = AbilityData;
			if (AbilityShipIndex < 0) { error = true; return false; }
			if (!CheckShipAlive(AbilityShipIndex, CurrentTurn)) { error = true; return false; }

			var oppGroup = CurrentTurn.Opposite();
			var data = CurrentTurn == Group.A ? DataA : DataB;
			var oppData = CurrentTurn == Group.A ? DataB : DataA;
			var selfAbility = data.Ships[AbilityShipIndex].Ability;
			var performingAbility = selfAbility;
			string performID = data.ShipDatas[AbilityShipIndex].Ship.GlobalID;
			var oppSoup = CurrentTurn == Group.A ? m_SoupB : m_SoupA;

			if (!aData.Performed) {
				aData.Performed = true;
				oppSoup.ClearBlinks();
			}

			if (selfAbility.CopyOpponentLastUsed) {
				string oppPrevUseAbilityKey = oppGroup == Group.A ? PrevUsedAbilityA : PrevUsedAbilityB;
				var aShip = GetShip(oppPrevUseAbilityKey);
				if (aShip != null) {
					performingAbility = aShip.Ship.Ability;
					performID = aShip.Ship.GlobalID;
				}
			}

			if (performingAbility.Attacks == null || performingAbility.Attacks.Count == 0) { error = true; return false; }

			// Perform Attack
			for (
				aData.AbilityAttackIndex = Mathf.Clamp(aData.AbilityAttackIndex, 0, performingAbility.Attacks.Count - 1);
				aData.AbilityAttackIndex < performingAbility.Attacks.Count;
				aData.AbilityAttackIndex++
			) {
				var attack = performingAbility.Attacks[aData.AbilityAttackIndex];
				var result = AttackResult.None;
				bool isHit = attack.IsHitOpponent;
				bool needBreak = false;
				switch (attack.Trigger) {


					case AttackTrigger.Picked:
						if (aData.Pickless) { break; }
						if (!aData.PickedPerformed) {
							// Check Target
							if (!attack.AvailableTarget.HasFlag(oppData.Tiles[x, y])) {
								error = true;
								return false;
							}
							result = AttackTile(
								attack, x, y,
								oppGroup,
								AbilityShipIndex,
								AbilityDirection
							);
							aData.PickedPerformed = true;
							if (result != AttackResult.None) {
								aData.DoTiedup = true;
								aData.TiedPos = aData.PrevAttackedPos;
							}
							break;
						} else {
							aData.WaitForPicking = true;
							return false;
						}


					case AttackTrigger.TiedUp:
						if (!aData.DoTiedup) { break; }
						result = AttackTile(
							attack, aData.TiedPos.x, aData.TiedPos.y,
							oppGroup,
							AbilityShipIndex,
							AbilityDirection
						);
						break;


					case AttackTrigger.Random:
						result = AttackTile(
							attack, x, y,
							oppGroup,
							AbilityShipIndex,
							AbilityDirection
						);
						if (result != AttackResult.None) {
							aData.DoTiedup = true;
							aData.TiedPos = aData.PrevAttackedPos;
						}
						break;


					case AttackTrigger.Break:
						needBreak = true;
						break;

				}

				// Win Check
				if (AllShipsSunk(oppGroup)) {
					SwitchTurn();
					return true;
				}

				// Break Check
				if (
					needBreak ||
					(performingAbility.BreakOnMiss && isHit && result.HasFlag(AttackResult.Miss)) ||
					(performingAbility.BreakOnSunk && result.HasFlag(AttackResult.SunkShip))
				) { break; }

			}

			// Prev Use
			if (performingAbility.HasActive) {
				if (CurrentTurn == Group.A) {
					PrevUsedAbilityA = performID;
				} else {
					PrevUsedAbilityB = performID;
				}
			}

			// Final
			data.Cooldowns[AbilityShipIndex] = selfAbility.Cooldown;
			aData.Clear();
			return true;
		}


		private void CancelAbility (bool forceCancel) {
			int attIndex = AbilityData.AbilityAttackIndex;
			if (forceCancel || attIndex == 0 || ShipEditing) {
				AbilityShipIndex = -1;
				AbilityData.Clear();
				if (forceCancel && attIndex != 0) {
					SwitchTurn();
				} else {
					m_RefreshUI.Invoke();
				}
			}
		}


		private bool AbilityFirstTrigger (int shipIndex) {
			AbilityShipIndex = shipIndex;
			AbilityData.AbilityAttackIndex = 0;
			AbilityData.WaitForPicking = true;
			AbilityData.PickedPerformed = true;
			AbilityDirection = AbilityDirection.Up;
			if (PerformAbility(0, 0, out _)) {
				SwitchTurn();
				m_RefreshUI.Invoke();
				return true;
			} else {
				m_RefreshUI.Invoke();
				return false;
			}
		}


		private bool CheckAbilityAvailable (Group group, int index) {
			var data = group == Group.A ? DataA : DataB;
			return data.ShipsAlive[index] && data.Cooldowns[index] <= 0;
		}


		// Attack
		private AttackResult AttackTile (Attack attack, int x, int y, Group targetGroup, int attackFromShipIndex = -1, AbilityDirection direction = default, bool blink = true) {

			if (!gameObject.activeSelf) { return AttackResult.None; }

			var data = targetGroup == Group.A ? DataA : DataB;
			var soup = targetGroup == Group.A ? m_SoupA : m_SoupB;
			var ownGroup = targetGroup == Group.A ? Group.B : Group.A;
			var ownData = targetGroup == Group.A ? DataB : DataA;
			var ownSoup = targetGroup == Group.A ? m_SoupB : m_SoupA;
			bool useOwn = attack.Type == AttackType.RevealOwnUnoccupiedTile;
			bool needRefreshShip = false;
			bool needRefreshHit = false;
			bool needRefreshSonar = false;

			if (attack.Type != AttackType.RevealSelf) {
				// Pos
				if (attack.Trigger == AttackTrigger.Random) {
					// Random
					if (!useOwn) {
						// Random in Target Soup
						if (!data.Map.GetRandomTile(attack.AvailableTarget, data.Tiles, out x, out y)) { return AttackResult.None; }
					} else {
						// Random in Own Soup
						if (!ownData.Map.GetRandomTile(
							attack.AvailableTarget,
							ownData.Tiles,
							out x, out y,
							(_x, _y) => !ShipData.Contains(
								_x, _y, ownData.ShipDatas, ownData.Positions, out _)
							)
						) { return AttackResult.None; }
					}
				} else {
					// Aim
					(x, y) = attack.GetPosition(x, y, direction);
				}

				// Inside Check
				if (x < 0 || y < 0 || x >= data.Map.Size || y >= data.Map.Size) { return AttackResult.None; }

				// Target Check
				if (!attack.AvailableTarget.HasFlag(
					(!useOwn ? data : ownData).Tiles[x, y]
				)) { return AttackResult.None; }
			}

			// Do Attack
			var result = AttackResult.None;
			switch (attack.Type) {
				case AttackType.HitTile:
				case AttackType.HitWholeShip: {
					HitTile(x, y, targetGroup, data, attack.Type == AttackType.HitWholeShip, out var hitShip, out var sunkShip);
					needRefreshShip = sunkShip;
					needRefreshHit = true;
					result = hitShip ? AttackResult.HitShip : AttackResult.Miss;
					if (sunkShip) {
						result |= AttackResult.SunkShip;
					}
					Blink(x, y, true);
					break;
				}
				case AttackType.RevealTile:
				case AttackType.RevealWholeShip: {
					RevealTile(x, y, targetGroup, data, attack.Type == AttackType.RevealWholeShip, true, out var revealShip);
					needRefreshHit = true;
					result = revealShip ? AttackResult.RevealShip : AttackResult.Miss;
					if (attack.Type == AttackType.RevealWholeShip) {
						needRefreshShip = true;
					}
					Blink(x, y, false);
					break;
				}
				case AttackType.Sonar: {
					SonarTile(x, y, targetGroup, data, out var hitShip, out var sunkShip);
					needRefreshHit = true;
					needRefreshSonar = true;
					result = hitShip ? AttackResult.HitShip : AttackResult.Miss;
					if (sunkShip) {
						result |= AttackResult.SunkShip;
					}
					Blink(x, y, true);
					break;
				}
				case AttackType.RevealOwnUnoccupiedTile: {
					RevealTile(x, y, ownGroup, ownData, false, true, out _);
					ownSoup.RefreshHitRenderer();
					break;
				}
				case AttackType.RevealSelf: {
					if (attackFromShipIndex < 0) { break; }
					RevealWholeShip(ownData, attackFromShipIndex, ownGroup);
					ownSoup.RefreshHitRenderer();
					break;
				}
				case AttackType.DoNothing: {
					result = AttackResult.Keep;
					break;
				}
			}

			// Refresh
			if (needRefreshHit) {
				soup.RefreshHitRenderer();
			}
			if (needRefreshShip) {
				soup.RefreshShipRenderer();
			}
			if (needRefreshSonar) {
				soup.RefreshSonarRenderer();
			}

			AbilityData.PrevAttackedPos.x = x;
			AbilityData.PrevAttackedPos.y = y;

			return result;
			// Func
			void Blink (int _x, int _y, bool _hitBlink) {
				if (!blink || !_hitBlink) { return; }
				if (attackFromShipIndex < 0) {
					soup.Blink(_x, _y, Color.white, m_AttackBlink, 0.5f);
				} else {
					soup.Blink(
						_x, _y,
						new Color(1f, 1f, 1f, 0.5f),
						ownData.ShipDatas[attackFromShipIndex].Sprite,
						0.1f
					);
				}
			}
		}


		private void HitTile (int x, int y, Group targetGroup, GameData targetData, bool hitWholeShip, out bool hitShip, out bool sunkShip) {
			sunkShip = false;
			hitShip = false;
			var wPos = GetWorldPosition(x, y, targetGroup);
			if (ShipData.Contains(x, y, targetData.ShipDatas, targetData.Positions, out int _shipIndex)) {
				// Hit Ship
				if (!hitWholeShip) {
					// Just Tile
					bool prevAlive = CheckShipAlive(_shipIndex, targetGroup);
					targetData.Tiles[x, y] = Tile.HittedShip;
					RefreshShipsAlive(_shipIndex, targetGroup);
					if (targetData.ShipsAlive[_shipIndex]) {
						// No Sunk
						InvokeEvent(SoupEvent.CurrentShip_GetHit, targetGroup, _shipIndex);
						InvokeMsg_ShipHitted(targetGroup, wPos);
					} else {
						// Sunk
						sunkShip = true;
						SetTilesToSunk(
							targetData.Ships[_shipIndex],
							targetData.Positions[_shipIndex]
						);
						targetData.KnownPositions[_shipIndex] = targetData.Positions[_shipIndex];
						if (prevAlive) {
							InvokeEvent(SoupEvent.CurrentShip_Sunk, targetGroup, _shipIndex);
							InvokeMsg_ShipSunk(targetGroup, wPos);
						} else {
							InvokeMsg_ShipHitted(targetGroup, wPos);
						}
					}
					if (targetData.Ships[_shipIndex].Ability.ResetCooldownOnHit) {
						// Reset Cooldown On Hit
						targetData.Cooldowns[_shipIndex] = 0;
					}
				} else {
					// Whole Ship
					var ship = targetData.ShipDatas[_shipIndex];
					var sPos = targetData.Positions[_shipIndex];
					bool getHit = false;
					foreach (var v in ship.Ship.Body) {
						int _x = sPos.Pivot.x + (sPos.Flip ? v.y : v.x);
						int _y = sPos.Pivot.y + (sPos.Flip ? v.x : v.y);
						var tile = targetData.Tiles[_x, _y];
						if (tile != Tile.HittedShip && tile != Tile.SunkShip) {
							targetData.Tiles[_x, _y] = Tile.HittedShip;
							InvokeMsg_ShipHitted(targetGroup, GetWorldPosition(_x, _y, targetGroup));
							getHit = true;
						}
					}
					if (getHit) {
						InvokeEvent(SoupEvent.CurrentShip_GetHit, targetGroup, _shipIndex);
					}
					RefreshShipsAlive(_shipIndex, targetGroup);
					sunkShip = true;
					SetTilesToSunk(targetData.Ships[_shipIndex], targetData.Positions[_shipIndex]);
					targetData.KnownPositions[_shipIndex] = targetData.Positions[_shipIndex];
					InvokeEvent(SoupEvent.CurrentShip_Sunk, targetGroup, _shipIndex);
					InvokeMsg_ShipSunk(targetGroup, wPos);
				}
				hitShip = true;
			} else if (targetData.Map.HasStone(x, y)) {
				// Hit Stone
				targetData.Tiles[x, y] = Tile.RevealedStone;
				InvokeMsg_WaterReveal(targetGroup, wPos);
				hitShip = false;
			} else {
				// Hit Water
				targetData.Tiles[x, y] = Tile.RevealedWater;
				InvokeMsg_WaterReveal(targetGroup, wPos);
				hitShip = false;
			}
			// Func
			void SetTilesToSunk (Ship ship, ShipPosition position) {
				foreach (var v in ship.Body) {
					targetData.Tiles[
						position.Pivot.x + (position.Flip ? v.y : v.x),
						position.Pivot.y + (position.Flip ? v.x : v.y)
					] = Tile.SunkShip;
				}
			}
		}


		private void RevealTile (int x, int y, Group targetGroup, GameData data, bool revealWholeShip, bool useCallback, out bool revealedShip) {
			var wPos = GetWorldPosition(x, y, targetGroup);
			var tile = data.Tiles[x, y];
			if (ShipData.Contains(x, y, data.ShipDatas, data.Positions, out int _shipIndex)) {
				if (!revealWholeShip) {
					// Just Tile
					if (tile != Tile.HittedShip && tile != Tile.SunkShip && tile != Tile.RevealedShip) {
						data.Tiles[x, y] = Tile.RevealedShip;
						if (useCallback) {
							InvokeEvent(SoupEvent.CurrentShip_GetReveal, targetGroup, _shipIndex);
							InvokeMsg_ShipReveal(targetGroup, wPos);
						}
					}
				} else {
					// Whole Ship
					RevealWholeShip(data, _shipIndex, targetGroup);
				}
				revealedShip = true;
			} else if (data.Map.HasStone(x, y)) {
				// Stone
				if (tile != Tile.RevealedStone) {
					data.Tiles[x, y] = Tile.RevealedStone;
					if (useCallback) {
						InvokeMsg_WaterReveal(targetGroup, wPos);
					}
				}
				revealedShip = false;
			} else {
				// Just Water
				if (tile != Tile.RevealedWater) {
					data.Tiles[x, y] = Tile.RevealedWater;
					if (useCallback) {
						InvokeMsg_WaterReveal(targetGroup, wPos);
					}
				}
				revealedShip = false;
			}
		}


		private void RevealWholeShip (GameData data, int shipIndex, Group targetGroup) {
			var ship = data.ShipDatas[shipIndex];
			var sPos = data.Positions[shipIndex];
			data.KnownPositions[shipIndex] = data.Positions[shipIndex];
			foreach (var v in ship.Ship.Body) {
				int _x = sPos.Pivot.x + (sPos.Flip ? v.y : v.x);
				int _y = sPos.Pivot.y + (sPos.Flip ? v.x : v.y);
				var tile = data.Tiles[_x, _y];
				if (tile != Tile.RevealedShip && tile != Tile.HittedShip && tile != Tile.SunkShip) {
					data.Tiles[_x, _y] = Tile.RevealedShip;
					InvokeEvent(SoupEvent.CurrentShip_GetReveal, targetGroup, shipIndex);
					InvokeMsg_ShipReveal(targetGroup, GetWorldPosition(_x, _y, targetGroup));
				}
			}
		}


		private void SonarTile (int x, int y, Group group, GameData data, out bool hitShip, out bool sunkShip) {
			hitShip = false;
			sunkShip = false;
			if (ShipData.Contains(x, y, data.ShipDatas, data.Positions, out _)) {
				// Hit When Has Ship
				HitTile(x, y, group, data, false, out hitShip, out sunkShip);
			} else {
				// Sonar Reveal When No Ship
				var wPos = GetWorldPosition(x, y, group);
				int mapSize = data.Map.Size;
				int minDis = ShipData.FindNearestShipDistance(
					x, y, data.ShipDatas, data.Positions, out var _pos
				);
				if (minDis == 0) {
					var tile = data.Tiles[_pos.x, _pos.y];
					if (tile != Tile.HittedShip && tile != Tile.SunkShip) {
						HitTile(x, y, group, data, false, out hitShip, out sunkShip);
					}
				} else if (minDis > 0) {
					int l = x - minDis + 1;
					int r = x + minDis - 1;
					int d = y - minDis + 1;
					int u = y + minDis - 1;
					for (int i = l; i <= r; i++) {
						for (int j = d; j <= u; j++) {
							if (i < 0 || i >= mapSize || j < 0 || j >= mapSize) { continue; }
							if (Mathf.Abs(i - x) + Mathf.Abs(j - y) < minDis) {
								RevealTile(i, j, group, data, false, false, out _);
							}
						}
					}
					data.Sonars.Add(new SonarPosition(x, y, minDis));
				}
				InvokeMsg_Sonar(group, wPos);
			}
		}


		// Message
		private void InvokeMsg_ShipHitted (Group group, Vector3 pos) {
			if (ShipEditing && group == Group.A) { return; }
			m_OnShipHitted.Invoke(pos);
		}


		private void InvokeMsg_ShipReveal (Group group, Vector3 pos) {
			if (ShipEditing && group == Group.A) { return; }
			m_OnShipRevealed.Invoke(pos);
		}


		private void InvokeMsg_ShipSunk (Group group, Vector3 pos) {
			if (ShipEditing && group == Group.A) { return; }
			m_OnShipSunk.Invoke(pos);
		}


		private void InvokeMsg_WaterReveal (Group group, Vector3 pos) {
			if (ShipEditing && group == Group.A) { return; }
			m_OnWaterRevealed.Invoke(pos);
		}


		private void InvokeMsg_Sonar (Group group, Vector3 pos) {
			if (ShipEditing && group == Group.A) { return; }
			m_OnSonar.Invoke(pos);
		}


		// Event
		private void InvokeEvent (SoupEvent type, Group ownGroup, int currentIndex = -1) {
			var data = ownGroup == Group.A ? DataA : DataB;
			for (int shipIndex = 0; shipIndex < data.Ships.Length; shipIndex++) {
				var ship = data.Ships[shipIndex];
				int eCount = ship.Ability.Events.Count;
				for (int i = 0; i < eCount; i++) {
					var ev = ship.Ability.Events[i];
					if (ev.Type != type) { continue; }
					if (CheckCondition(
						ev.Condition,
						ev.ConditionCompare,
						ev.ApplyConditionOnOpponent ? ownGroup.Opposite() : ownGroup,
						currentIndex,
						ev.IntParam
					)) {
						// Perform
						switch (ev.Action) {
							case EventAction.PerformAttack: {
								var oldTurn = CurrentTurn;
								int oldAbilityIndex = AbilityShipIndex;
								CurrentTurn = ownGroup;
								AbilityShipIndex = shipIndex;
								AbilityData.AbilityAttackIndex = ev.ActionParam;
								AbilityData.WaitForPicking = true;
								AbilityData.PickedPerformed = true;
								AbilityData.Pickless = true;
								AbilityDirection = AbilityDirection.Up;
								PerformAbility(0, 0, out _);
								CurrentTurn = oldTurn;
								AbilityShipIndex = oldAbilityIndex;
								break;
							}
						}
						if (ev.BreakAfterPerform) { break; }
					}
				}
			}
		}


		private bool CheckCondition (EventCondition condition, EventConditionCompare compare, Group group, int shipIndex, int paramInt) {

			var data = group == Group.A ? DataA : DataB;

			return condition switch {
				EventCondition.AliveShipCount => CheckConpare(data.GetAliveShipCount()),
				EventCondition.SunkShipCount => CheckConpare(data.GetSunkShipCount()),
				EventCondition.CurrentShip_HiddenTileCount => CheckConpare(data.GetTileCount(shipIndex, Tile.GeneralWater)),
				EventCondition.CurrentShip_HitTileCount => CheckConpare(data.GetTileCount(shipIndex, Tile.HittedShip)),
				EventCondition.CurrentShip_RevealTileCount => CheckConpare(data.GetTileCount(shipIndex, Tile.RevealedShip)),
				_ => true,
			};

			// Func
			bool CheckConpare (int value) => compare switch {
				EventConditionCompare.Equal => value == paramInt,
				EventConditionCompare.NotEqual => value != paramInt,
				EventConditionCompare.Less => value < paramInt,
				EventConditionCompare.LessOrEqual => value <= paramInt,
				EventConditionCompare.Greater => value > paramInt,
				EventConditionCompare.GreaterOrEqual => value >= paramInt,
				_ => false,
			};
		}


		#endregion




	}
}
