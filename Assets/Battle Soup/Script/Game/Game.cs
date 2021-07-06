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


		private class GameData {


			public MapData Map = null;
			public ShipData[] Ships = null;
			public Tile[,] Tiles = null;
			public bool[] ShipsAlive = null;
			public readonly List<ShipPosition> Positions = new List<ShipPosition>();
			public readonly List<int> Cooldowns = new List<int>();


			public void Clear () {
				Map = null;
				Ships = null;
				Tiles = null;
				Positions.Clear();
				Cooldowns.Clear();
			}


			public void Init (MapData map, ShipData[] ships, List<ShipPosition> positions) {

				Map = map;
				Ships = ships;

				// Pos
				if (positions.Count < ships.Length) {
					positions.AddRange(new ShipPosition[ships.Length - positions.Count]);
				}
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
				ShipsAlive = new bool[Ships.Length];
				for (int i = 0; i < ShipsAlive.Length; i++) {
					ShipsAlive[i] = true;
				}

			}


		}


		#endregion



		#region --- VAR ---


		// Api
		public Group CurrentTurn { get; private set; } = Group.A;

		// Ser
		[SerializeField] BattleSoupUI m_SoupA = null;
		[SerializeField] BattleSoupUI m_SoupB = null;
		[SerializeField] VoidEvent m_OnTurnSwitched = null;
		[SerializeField] VoidVector3Event m_OnShipHitted = null;
		[SerializeField] VoidVector3Event m_OnShipSunk = null;
		[SerializeField] VoidVector3Event m_OnWaterRevealed = null;
		[SerializeField] VoidVector3Event m_OnShipRevealed = null;

		// Data
		private BattleMode CurrentBattleMode = BattleMode.PvA;
		private readonly GameData DataA = new GameData();
		private readonly GameData DataB = new GameData();
		private readonly Attack DEFAULT_ATTACK = new Attack() {
			X = 0,
			Y = 0,
			AvailableTarget = Tile.GeneralWater | Tile.RevealedShip,
			Trigger = AttackTrigger.Picked,
			Type = AttackType.HitTile,
		};
		private readonly Vector3[] WorldCornerCaches = new Vector3[4];


		#endregion




		#region --- MSG ---


		private void Update () {
			Update_Mouse();

		}


		private void Update_Mouse () {


			// ///////////////////	Test ////////////////////
			if (CurrentTurn == Group.B) {
				SwitchTurn();
			}
			// ///////////////////	Test ////////////////////


			if (CurrentBattleMode != BattleMode.PvA || CurrentTurn != Group.A) { return; }


			if (Input.GetMouseButtonDown(0)) {
				// Mouse Left
				if (m_SoupB.GetMapPositionInside(Input.mousePosition, out var pos)) {
					var tile = DataB.Tiles[pos.x, pos.y];


					// Normal Attack
					if (tile == Tile.GeneralWater || tile == Tile.RevealedShip) {
						AttackTile(DEFAULT_ATTACK, pos.x, pos.y, Group.B);
						m_SoupB.RefreshHitRenderer();
						SwitchTurn();
					}



				}
			} else if (Input.GetMouseButtonDown(1)) {
				// Mouse Right
				if (m_SoupB.GetMapPositionInside(Input.mousePosition, out var pos)) {



				}
			}

			float deltaY = Input.mouseScrollDelta.y;
			if (!Mathf.Approximately(deltaY, 0f)) {
				// Mouse Wheel



			}
		}


		#endregion




		#region --- API ---


		public void Init (
			BattleMode battleMode,
			MapData mapA, MapData mapB,
			ShipData[] shipsA, ShipData[] shipsB,
			List<ShipPosition> positionsA, List<ShipPosition> positionsB
		) {
			CurrentBattleMode = battleMode;
			CurrentTurn = Random.value > 0.5f ? Group.A : Group.B;
			DataA.Clear();
			DataB.Clear();
			DataA.Init(mapA, shipsA, positionsA);
			DataB.Init(mapB, shipsB, positionsB);
		}


		public void SetupDelegate () {
			m_SoupA.GetTile = (x, y) => DataA.Tiles[x, y];
			m_SoupB.GetTile = (x, y) => DataB.Tiles[x, y];
			m_SoupA.GetMap = () => DataA.Map;
			m_SoupB.GetMap = () => DataB.Map;
			m_SoupA.GetShips = () => DataA.Ships;
			m_SoupB.GetShips = () => DataB.Ships;
			m_SoupA.GetPositions = () => DataA.Positions;
			m_SoupB.GetPositions = () => DataB.Positions;
			m_SoupA.CheckShipAlive = (index) => CheckShipAlive(index, Group.A);
			m_SoupB.CheckShipAlive = (index) => CheckShipAlive(index, Group.B);
		}


		public int GetCooldown (Group group, int index) {
			var cooldowns = group == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			return cooldowns[Mathf.Clamp(index, 0, cooldowns.Count - 1)];
		}


		public Ability GetAbility (Group group, int index) {
			var ships = group == Group.A ? DataA.Ships : DataB.Ships;
			return ships[Mathf.Clamp(index, 0, ships.Length - 1)].Ship.Ability;
		}


		public void OnAbilityClick (int shipIndex, Ability ability) {
			if (CurrentBattleMode != BattleMode.PvA || CurrentTurn != Group.A) { return; }





		}


		public void AttackTile (Attack attack, int x, int y, Group group) {
			var data = group == Group.A ? DataA : DataB;
			switch (attack.Type) {
				case AttackType.HitTile:
					if (ShipData.Contains(x, y, data.Ships, data.Positions, out int _shipIndex)) {
						data.Tiles[x, y] = Tile.HittedShip;
						RefreshShipsAlive(_shipIndex, group);
						if (data.ShipsAlive[_shipIndex]) {
							m_OnShipHitted.Invoke(GetWorldPosition(x, y, group));
						} else {
							m_SoupA.RefreshShipRenderer(CurrentBattleMode == BattleMode.AvA);
							m_SoupB.RefreshShipRenderer(true);
							m_OnShipSunk.Invoke(GetWorldPosition(x, y, group));
						}
					} else if (data.Map.HasStone(x, y)) {
						data.Tiles[x, y] = Tile.RevealedStone;
						m_OnWaterRevealed.Invoke(GetWorldPosition(x, y, group));
					} else {
						data.Tiles[x, y] = Tile.RevealedWater;
						m_OnWaterRevealed.Invoke(GetWorldPosition(x, y, group));
					}
					break;
				case AttackType.RevealTile:



					break;
				case AttackType.HitWholeShip:
					break;
				case AttackType.RevealWholeShip:
					break;
				case AttackType.Sonar:
					break;
				case AttackType.RevealOwnTile:
					break;
				case AttackType.RevealSelf:
					break;
			}


		}


		public bool CheckShipAlive (int index, Group group) => (group == Group.A ? DataA : DataB).ShipsAlive[index];


		#endregion




		#region --- LGC ---


		private void SwitchTurn () {
			var cooldowns = CurrentTurn == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			for (int i = 0; i < cooldowns.Count; i++) {
				cooldowns[i] = Mathf.Max(cooldowns[i] - 1, 0);
			}
			CurrentTurn = CurrentTurn == Group.A ? Group.B : Group.A;
			m_OnTurnSwitched.Invoke();
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


		#endregion




	}
}
