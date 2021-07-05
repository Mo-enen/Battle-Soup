using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleSoupAI;


namespace BattleSoup {
	public class Game : MonoBehaviour {




		#region --- SUB ---


		private class GameData {


			public MapData Map = null;
			public ShipData[] Ships = null;
			public Tile[,] Tiles = null;
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

			}


		}


		#endregion



		#region --- VAR ---


		// Api
		public Group CurrentTurn { get; private set; } = Group.A;

		// Ser
		[SerializeField] BattleSoupUI m_SoupA = null;
		[SerializeField] BattleSoupUI m_SoupB = null;

		// Data
		private BattleMode CurrentBattleMode = BattleMode.PvA;
		private readonly GameData DataA = new GameData();
		private readonly GameData DataB = new GameData();


		#endregion




		#region --- MSG ---


		private void Update () {
			Update_Mouse();

		}


		private void Update_Mouse () {
			if (CurrentBattleMode != BattleMode.PvA || CurrentTurn != Group.A) { return; }


			if (Input.GetMouseButtonDown(0)) {
				// Mouse Left
				if (m_SoupB.GetMapPositionInside(Input.mousePosition, out var pos)) {



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
		}


		public int GetCooldown (Group group, int index) {
			var cooldowns = group == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			return cooldowns[Mathf.Clamp(index, 0, cooldowns.Count - 1)];
		}


		public Ability GetAbility (Group group, int index) {
			var ships = group == Group.A ? DataA.Ships : DataB.Ships;
			return ships[Mathf.Clamp(index, 0, ships.Length - 1)].Ship.Ability;
		}


		#endregion




		#region --- LGC ---


		private void SwitchTurn () {
			var cooldowns = CurrentTurn == Group.A ? DataA.Cooldowns : DataB.Cooldowns;
			for (int i = 0; i < cooldowns.Count; i++) {
				cooldowns[i] = Mathf.Max(cooldowns[i] - 1, 0);
			}
			CurrentTurn = CurrentTurn == Group.A ? Group.B : Group.A;
		}


		#endregion




	}
}
