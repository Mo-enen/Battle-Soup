using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BattleSoupAI;


namespace BattleSoup {
	public class Game : MonoBehaviour {




		#region --- SUB ---


		[System.Serializable] public class VoidVector3Event : UnityEvent<Vector3> { }
		[System.Serializable] public class VoidEvent : UnityEvent { }
		[System.Serializable] public class VoidStringEvent : UnityEvent<string> { }


		private class GameData {

			public MapData Map = null;
			public ShipData[] ShipDatas = null;
			public Ship[] Ships = null;
			public Tile[,] Tiles = null;
			public bool[] ShipsAlive = null;
			public readonly List<ShipPosition> Positions = new List<ShipPosition>();
			public readonly List<int> Cooldowns = new List<int>();
			public readonly List<SonarPosition> Sonars = new List<SonarPosition>();

			public void Init (MapData map, ShipData[] ships, List<ShipPosition> positions) {

				Map = map;
				ShipDatas = ships;

				// Ships
				Ships = ShipData.GetShips(ships);

				// Pos
				if (positions.Count < ships.Length) {
					positions.AddRange(new ShipPosition[ships.Length - positions.Count]);
				}
				Positions.Clear();
				Positions.AddRange(positions);

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
				for (int i = 0; i < ships.Length; i++) {
					Cooldowns.Add(ships[i].Ship.Ability.Cooldown - 1);
				}

				// Ships Alive
				ShipsAlive = new bool[ShipDatas.Length];
				for (int i = 0; i < ShipsAlive.Length; i++) {
					ShipsAlive[i] = true;
				}

			}

			public void Clear () {
				Map = null;
				ShipDatas = null;
				Ships = null;
				Tiles = null;
				ShipsAlive = null;
				Positions.Clear();
				Cooldowns.Clear();
				Sonars.Clear();
			}

		}


		private struct AbilityPerformData {
			public int AbilityAttackIndex;
			public bool WaitForPicking;
			public bool PickedPerformed;
			public bool DoTiedup;

			public void Clear () {
				AbilityAttackIndex = 0;
				WaitForPicking = false;
				PickedPerformed = false;
				DoTiedup = false;
			}

		}


		[System.Flags]
		private enum AttackResult {
			None = 0,
			Miss = 1 << 0,
			HitShip = 1 << 1,
			RevealShip = 1 << 2,
			SunkShip = 1 << 3,
		}


		#endregion



		#region --- VAR ---


		// Api
		public Group CurrentTurn { get; private set; } = Group.A;
		public int AbilityShipIndex { get; private set; } = -1;

		// Ser
		[SerializeField] BattleSoupUI m_SoupA = null;
		[SerializeField] BattleSoupUI m_SoupB = null;
		[SerializeField] RectTransform m_FaceWin = null;
		[SerializeField] RectTransform m_FaceLose = null;
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
		private readonly Vector3[] WorldCornerCaches = new Vector3[4];
		private readonly Attack DEFAULT_ATTACK = new Attack() {
			X = 0,
			Y = 0,
			AvailableTarget = Tile.GeneralWater | Tile.RevealedShip,
			Trigger = AttackTrigger.Picked,
			Type = AttackType.HitTile,
		};
		private BattleMode CurrentBattleMode = BattleMode.PvA;
		private AbilityPerformData AbilityData = new AbilityPerformData();
		private float AllowUpdateTime = 0f;


		#endregion




		#region --- MSG ---


		private void Update () {

			Update_Aim();

			if (Time.time < AllowUpdateTime) { return; }

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
								if (PerformAbility(pos.x, pos.y)) {
									DelayUpdate(0.1f);
									SwitchTurn();
								} else {
									AbilityData.WaitForPicking = true;
									m_RefreshUI.Invoke();
								}
							} else if (AttackTile(DEFAULT_ATTACK, pos.x, pos.y, -1, Group.B) != AttackResult.None) {
								// Normal Attack
								DelayUpdate(0.1f);
								SwitchTurn();
							}
						} else if (AbilityData.AbilityAttackIndex == 0) {
							OnAbilityCancel();
						}
					}

				} else {
					// Robot A
					if (SoupAI.Analyse(
						DataA.Tiles, DataB.Tiles,
						DataA.Ships, DataB.Ships,
						DataA.Positions,
						out _, out _, out _
					)) {



					}
					SwitchTurn();
					DelayUpdate(0.1f);
				}
			} else {
				// B Turn
				if (SoupAI.Analyse(
					DataB.Tiles, DataA.Tiles,
					DataB.Ships, DataA.Ships,
					DataB.Positions,
					out _, out _, out _
				)) {



				}
				SwitchTurn();
				DelayUpdate(0.1f);
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


		public void Init (BattleMode battleMode, MapData mapA, MapData mapB, ShipData[] shipsA, ShipData[] shipsB, List<ShipPosition> positionsA, List<ShipPosition> positionsB) {
			CurrentBattleMode = battleMode;
			CurrentTurn = Random.value > 0.5f ? Group.A : Group.B;
			AbilityData.Clear();
			DataA.Clear();
			DataB.Clear();
			DataA.Init(mapA, shipsA, positionsA);
			DataB.Init(mapB, shipsB, positionsB);
			m_RefreshUI.Invoke();
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
		}


		public void UI_Clear () {
			DataA.Clear();
			DataB.Clear();
		}


		// Ship
		public bool CheckShipAlive (int index, Group group) => (group == Group.A ? DataA : DataB).ShipsAlive[index];


		public ShipData GetShipData (Group group, int index) {
			var ships = group == Group.A ? DataA.ShipDatas : DataB.ShipDatas;
			return ships[Mathf.Clamp(index, 0, ships.Length - 1)];
		}


		// Ability
		public int GetCooldown (Group group, int index) {
			var cooldowns = group == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			return cooldowns[Mathf.Clamp(index, 0, cooldowns.Count - 1)];
		}


		public Ability GetAbility (Group group, int index) {
			var ships = group == Group.A ? DataA.ShipDatas : DataB.ShipDatas;
			return ships[Mathf.Clamp(index, 0, ships.Length - 1)].Ship.Ability;
		}


		public void OnAbilityClick (int shipIndex) {
			if (!gameObject.activeSelf || CurrentBattleMode != BattleMode.PvA || CurrentTurn != Group.A) { return; }
			AbilityShipIndex = shipIndex;
			AbilityData.AbilityAttackIndex = 0;
			AbilityData.WaitForPicking = true;
			AbilityData.PickedPerformed = true;
			if (PerformAbility(0, 0)) {
				DelayUpdate(0.1f);
				SwitchTurn();
			} else {
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
			if (AllShipsSunk(Group.A)) {
				if (CurrentBattleMode == BattleMode.PvA) {
					m_FaceWin.gameObject.SetActive(false);
					m_FaceLose.gameObject.SetActive(true);
					m_ShowMessage.Invoke("You Lose");
				} else {
					m_FaceWin.gameObject.SetActive(false);
					m_FaceLose.gameObject.SetActive(false);
					m_ShowMessage.Invoke("Robot B Win");
				}
				gameObject.SetActive(false);
				m_RefreshUI.Invoke();
			} else if (AllShipsSunk(Group.B)) {
				if (CurrentBattleMode == BattleMode.PvA) {
					m_ShowMessage.Invoke("You Win");
					m_FaceWin.gameObject.SetActive(true);
					m_FaceLose.gameObject.SetActive(false);
				} else {
					m_ShowMessage.Invoke("Robot A Win");
					m_FaceWin.gameObject.SetActive(false);
					m_FaceLose.gameObject.SetActive(false);
				}
				gameObject.SetActive(false);
				m_RefreshUI.Invoke();
				return;
			}

			// Cooldown
			var cooldowns = CurrentTurn == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			for (int i = 0; i < cooldowns.Count; i++) {
				cooldowns[i] = Mathf.Max(cooldowns[i] - 1, 0);
			}

			// Turn Change
			CurrentTurn = CurrentTurn == Group.A ? Group.B : Group.A;
			m_RefreshUI.Invoke();

			// Func
			bool AllShipsSunk (Group group) {
				int count = (group == Group.A ? DataA.ShipDatas.Length : DataB.ShipDatas.Length);
				for (int i = 0; i < count; i++) {
					if (CheckShipAlive(i, group)) {
						return false;
					}
				}
				return true;
			}
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
				var ships = data.ShipDatas;
				var positions = data.Positions;
				var map = data.Map;
				var tiles = data.Tiles;
				_index = Mathf.Clamp(_index, 0, ships.Length - 1);
				var body = ships[_index].Ship.Body;
				var sPos = positions[_index];
				foreach (var v in body) {
					var pos = new Vector2Int(
						sPos.Pivot.x + (sPos.Flip ? v.y : v.x),
						sPos.Pivot.y + (sPos.Flip ? v.x : v.y)
					);
					if (pos.x >= 0 && pos.x < map.Size && pos.y >= 0 && pos.y < map.Size) {
						if (tiles[pos.x, pos.y] != Tile.HittedShip) {
							return true;
						}
					}
				}
				return false;
			}
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


		private void DelayUpdate (float second) => AllowUpdateTime = Time.time + second;


		// Ability
		private bool PerformAbility (int x, int y) {

			var aData = AbilityData;
			if (AbilityShipIndex < 0) { return false; }

			var opponentGroup = CurrentTurn == Group.A ? Group.B : Group.A;
			var data = CurrentTurn == Group.A ? DataA : DataB;
			var opData = CurrentTurn == Group.A ? DataB : DataA;
			var ability = data.Ships[AbilityShipIndex].Ability;
			if (ability.Attacks == null || ability.Attacks.Count == 0) { return false; }
			int attIndex = Mathf.Clamp(aData.AbilityAttackIndex, 0, ability.Attacks.Count - 1);

			// Perform Attack
			for (int i = attIndex; i < ability.Attacks.Count; i++) {
				var attack = ability.Attacks[i];
				aData.AbilityAttackIndex = i;
				AttackResult result = AttackResult.None;
				switch (attack.Trigger) {
					case AttackTrigger.Picked:
						if (!aData.PickedPerformed) {
							// Check Target
							if (!attack.AvailableTarget.HasFlag(opData.Tiles[x, y])) {
								return false;
							}
							result = AttackTile(
								attack, x, y,
								AbilityShipIndex,
								opponentGroup
							);
							aData.PickedPerformed = true;
							if (result != AttackResult.None) {
								aData.DoTiedup = true;
							}
							break;
						} else {
							aData.WaitForPicking = true;
							return false;
						}
					case AttackTrigger.TiedUp:
						if (!aData.DoTiedup) { break; }
						result = AttackTile(
							attack, x, y,
							AbilityShipIndex,
							opponentGroup
						);
						break;
					case AttackTrigger.Random:
						result = AttackTile(
							attack, x, y,
							AbilityShipIndex,
							opponentGroup
						);
						break;
				}

				// Break Check
				if (
					(ability.BreakOnMiss && result.HasFlag(AttackResult.Miss)) ||
					(ability.BreakOnSunk && result.HasFlag(AttackResult.SunkShip))
				) { break; }

			}

			// Final
			data.Cooldowns[AbilityShipIndex] = ability.Cooldown;
			aData.Clear();
			return true;
		}


		private void OnAbilityCancel () {
			AbilityShipIndex = -1;
			AbilityData.Clear();
			m_RefreshUI.Invoke();
		}


		// Attack
		private AttackResult AttackTile (Attack attack, int x, int y, int attackFromShipIndex, Group group) {

			if (!gameObject.activeSelf) { return AttackResult.None; }

			var data = group == Group.A ? DataA : DataB;
			var soup = group == Group.A ? m_SoupA : m_SoupB;
			var ownGroup = group == Group.A ? Group.B : Group.A;
			var ownData = group == Group.A ? DataB : DataA;
			var ownSoup = group == Group.A ? m_SoupB : m_SoupA;
			bool useOwn = attack.Type == AttackType.RevealOwnUnoccupiedTile;
			bool needRefreshShip = false;
			bool needRefreshHit = false;
			bool needRefreshSonar = false;

			if (attack.Type != AttackType.RevealSelf) {
				// Pos
				if (attack.Trigger == AttackTrigger.Random || attack.Trigger == AttackTrigger.PassiveRandom) {
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
					x += attack.X;
					y += attack.Y;
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
					HitTile(x, y, group, data, attack.Type == AttackType.HitWholeShip, out var hitShip, out var sunkShip);
					needRefreshShip = sunkShip;
					needRefreshHit = true;
					result = hitShip ? AttackResult.HitShip : AttackResult.Miss;
					if (sunkShip) {
						result |= AttackResult.SunkShip;
					}
					break;
				}
				case AttackType.RevealTile:
				case AttackType.RevealWholeShip: {
					RevealTile(x, y, group, data, attack.Type == AttackType.RevealWholeShip, true, out var revealShip);
					needRefreshHit = true;
					result = revealShip ? AttackResult.RevealShip : AttackResult.Miss;
					break;
				}
				case AttackType.Sonar: {
					SonarTile(x, y, group, data, out var hitShip, out var sunkShip);
					needRefreshHit = true;
					needRefreshSonar = true;
					result = hitShip ? AttackResult.HitShip : AttackResult.Miss;
					if (sunkShip) {
						result |= AttackResult.SunkShip;
					}
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
			}

			// Refresh
			if (needRefreshHit) {
				soup.RefreshHitRenderer();
			}
			if (needRefreshShip) {
				soup.RefreshShipRenderer(group != Group.A || CurrentBattleMode == BattleMode.AvA);
			}
			if (needRefreshSonar) {
				soup.RefreshSonarRenderer();
			}

			return result;
		}


		private void HitTile (int x, int y, Group group, GameData data, bool hitWholeShip, out bool hitShip, out bool sunkShip) {
			sunkShip = false;
			hitShip = false;
			var wPos = GetWorldPosition(x, y, group);
			if (ShipData.Contains(x, y, data.ShipDatas, data.Positions, out int _shipIndex)) {
				// Hit Ship
				if (!hitWholeShip) {
					// Just Tile
					data.Tiles[x, y] = Tile.HittedShip;
					RefreshShipsAlive(_shipIndex, group);
					if (data.ShipsAlive[_shipIndex]) {
						// No Sunk
						m_OnShipHitted.Invoke(wPos);
					} else {
						// Sunk
						sunkShip = true;
						m_OnShipSunk.Invoke(wPos);
					}
				} else {
					// Whole Ship
					var ship = data.ShipDatas[_shipIndex];
					var sPos = data.Positions[_shipIndex];
					foreach (var v in ship.Ship.Body) {
						int _x = sPos.Pivot.x + (sPos.Flip ? v.y : v.x);
						int _y = sPos.Pivot.y + (sPos.Flip ? v.x : v.y);
						if (data.Tiles[_x, _y] != Tile.HittedShip) {
							data.Tiles[_x, _y] = Tile.HittedShip;
							m_OnShipHitted.Invoke(GetWorldPosition(_x, _y, group));
						}
					}
					RefreshShipsAlive(_shipIndex, group);
					sunkShip = true;
					m_OnShipSunk.Invoke(wPos);
				}
				hitShip = true;
			} else if (data.Map.HasStone(x, y)) {
				// Hit Stone
				data.Tiles[x, y] = Tile.RevealedStone;
				m_OnWaterRevealed.Invoke(wPos);
				hitShip = false;
			} else {
				// Hit Water
				data.Tiles[x, y] = Tile.RevealedWater;
				m_OnWaterRevealed.Invoke(wPos);
				hitShip = false;
			}
		}


		private void RevealTile (int x, int y, Group group, GameData data, bool revealWholeShip, bool useCallback, out bool revealedShip) {
			var wPos = GetWorldPosition(x, y, group);
			var tile = data.Tiles[x, y];
			if (ShipData.Contains(x, y, data.ShipDatas, data.Positions, out int _shipIndex)) {
				if (!revealWholeShip) {
					// Just Tile
					if (tile != Tile.HittedShip && tile != Tile.RevealedShip) {
						data.Tiles[x, y] = Tile.RevealedShip;
						if (useCallback) {
							m_OnShipRevealed.Invoke(wPos);
						}
					}
				} else {
					// Whole Ship
					RevealWholeShip(data, _shipIndex, group);
				}
				revealedShip = true;
			} else if (data.Map.HasStone(x, y)) {
				// Stone
				if (tile != Tile.RevealedStone) {
					data.Tiles[x, y] = Tile.RevealedStone;
					if (useCallback) {
						m_OnWaterRevealed.Invoke(wPos);
					}
				}
				revealedShip = false;
			} else {
				// Just Water
				if (tile != Tile.RevealedWater) {
					data.Tiles[x, y] = Tile.RevealedWater;
					if (useCallback) {
						m_OnWaterRevealed.Invoke(wPos);
					}
				}
				revealedShip = false;
			}
		}


		private void RevealWholeShip (GameData data, int shipIndex, Group group) {
			var ship = data.ShipDatas[shipIndex];
			var sPos = data.Positions[shipIndex];
			foreach (var v in ship.Ship.Body) {
				int _x = sPos.Pivot.x + (sPos.Flip ? v.y : v.x);
				int _y = sPos.Pivot.y + (sPos.Flip ? v.x : v.y);
				if (data.Tiles[_x, _y] != Tile.RevealedShip) {
					data.Tiles[_x, _y] = Tile.RevealedShip;
					m_OnShipRevealed.Invoke(GetWorldPosition(_x, _y, group));
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
					if (data.Tiles[_pos.x, _pos.y] != Tile.HittedShip) {
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
				m_OnSonar.Invoke(wPos);
			}
		}


		#endregion




	}
}
