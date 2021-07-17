using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {



	public class BattleInfo {
		public Tile[,] Tiles;
		public Ship[] Ships;
		public int[] Cooldowns;
		public bool[] ShipsAlive;
	}



	public struct AnalyseResult {

		public bool Success => string.IsNullOrEmpty(ErrorMessage);

		public string ErrorMessage;
		public Int2 TargetPosition;
		public int AbilityIndex;
		public AbilityDirection AbilityDirection;

	}



	public abstract class SoupStrategy {




		#region --- VAR ---


		// Api
		public string FinalDisplayName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : GetType().Name;
		public virtual string DisplayName { get; } = "";
		public virtual string Description { get; } = "";

		// Data
		//private int[,,] PotentialValueCache = new int[0, 0,0];
		//private ShipPosition[,] PotentialShipPositions = new ShipPosition[0,0];


		#endregion




		#region --- API ---


		public abstract AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, ShipPosition[] ownShipPositions, int usingAbilityIndex);


		public virtual bool PositionShips (int mapSize, Ship[] ships, Int2[] stones, out ShipPosition[] result) => PositionShips_Random(mapSize, ships, stones, out result);


		public static bool PositionShips_Random (int mapSize, Ship[] ships, Int2[] stones, out ShipPosition[] result) {

			result = null;
			if (ships == null || ships.Length == 0) { return false; }
			bool success = true;

			// Get Hash
			var hash = new HashSet<Int2>();
			if (stones != null) {
				foreach (var stone in stones) {
					if (!hash.Contains(stone)) {
						hash.Add(stone);
					}
				}
			}

			// Get Result
			result = new ShipPosition[ships.Length];
			var random = new System.Random(System.DateTime.Now.Millisecond);
			for (int index = 0; index < ships.Length; index++) {
				var ship = ships[index];
				random = new System.Random(random.Next());
				var sPos = new ShipPosition();
				var basicPivot = new Int2(random.Next(0, mapSize), random.Next(0, mapSize));
				bool shipSuccess = false;
				// Try Fix Overlap
				for (int j = 0; j < mapSize; j++) {
					for (int i = 0; i < mapSize; i++) {
						sPos.Pivot = new Int2(
							(basicPivot.x + i) % mapSize,
							(basicPivot.y + j) % mapSize
						);
						sPos.Flip = false;
						if (PositionAvailable(ship, sPos)) {
							AddShipIntoHash(ship, sPos);
							shipSuccess = true;
							j = mapSize;
							break;
						}
						sPos.Flip = true;
						if (PositionAvailable(ship, sPos)) {
							AddShipIntoHash(ship, sPos);
							shipSuccess = true;
							j = mapSize;
							break;
						}
					}
				}
				if (!shipSuccess) { success = false; }
				result[index] = sPos;
			}
			if (!success) {
				result = null;
			}
			return success;
			// Func
			bool PositionAvailable (Ship _ship, ShipPosition _pos) {
				// Border Check
				var (min, max) = _ship.GetBounds(_pos);
				if (_pos.Pivot.x < -min.x || _pos.Pivot.x > mapSize - max.x - 1 ||
					_pos.Pivot.y < -min.y || _pos.Pivot.y > mapSize - max.y - 1
				) {
					return false;
				}
				// Overlap Check
				foreach (var v in _ship.Body) {
					if (hash.Contains(new Int2(
						_pos.Pivot.x + (_pos.Flip ? v.y : v.x),
						_pos.Pivot.y + (_pos.Flip ? v.x : v.y)
					))) {
						return false;
					}
				}
				return true;
			}
			void AddShipIntoHash (Ship _ship, ShipPosition _pos) {
				foreach (var v in _ship.Body) {
					var shipPosition = new Int2(
						_pos.Pivot.x + (_pos.Flip ? v.y : v.x),
						_pos.Pivot.y + (_pos.Flip ? v.x : v.y)
					);
					if (!hash.Contains(shipPosition)) {
						hash.Add(shipPosition);
					}
				}
			}
		}


		// Potential
		public static bool CalculatePotentials (
			Ship[] ships, Tile[,] tiles, ShipPosition?[] knownPositions,
			int[,,] values, List<ShipPosition>[] positions
		) {

			// Check
			if (ships == null || ships.Length == 0) { return false; }

			if (knownPositions == null || knownPositions.Length != ships.Length) { return false; }

			if (
				tiles == null ||
				tiles.Length == 0 ||
				tiles.GetLength(0) != ships.Length ||
				tiles.GetLength(1) != tiles.GetLength(2)
			) { return false; }

			if (
				values.GetLength(0) != tiles.GetLength(0) ||
				values.GetLength(1) != tiles.GetLength(1) ||
				values.GetLength(2) != tiles.GetLength(2)
			) {
				values = new int[tiles.GetLength(0), tiles.GetLength(1), tiles.GetLength(2)];
			}


			// Values:
			// 0+	ship count
			// -1	stone, revealed water, sunk ship
			// -2	revealed ship
			// -3	hitted ship

			// Init
			int shipCount = ships.Length;
			int size = tiles.GetLength(1);
			int stoneCount = 0;
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						var tile = tiles[x, y];
						var value = tile switch {
							Tile.None => 0,
							Tile.GeneralWater => 0,
							Tile.GeneralStone => -1,
							Tile.RevealedStone => -1,
							Tile.RevealedWater => -1,
							Tile.SunkShip => -1,
							Tile.RevealedShip => -2,
							Tile.HittedShip => -3,
							_ => -5,
						};
						values[shipIndex, x, y] = value;
						if (tile == Tile.GeneralStone || tile == Tile.RevealedStone) {
							stoneCount++;
						}
					}
				}
			}

			// Add Potential Ships
			if (positions == null || positions.Length != shipCount) {
				positions = new List<ShipPosition>[shipCount];
			}
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				var posList = positions[shipIndex];
				if (knownPositions[shipIndex].HasValue) {
					posList.Add(knownPositions[shipIndex].Value);
					continue;
				}
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						if (CheckAvailable(shipIndex, x, y, true)) {
							posList.Add(new ShipPosition(x, y, true));
						}
						if (CheckAvailable(shipIndex, x, y, false)) {
							posList.Add(new ShipPosition(x, y, false));
						}
					}
				}
			}

			// Smart Fix





			// Calculate Values
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				var posList = positions[shipIndex];
				var ship = ships[shipIndex];
				foreach (var pos in posList) {
					foreach (var v in ship.Body) {
						values[
							shipIndex,
							pos.Pivot.x + (pos.Flip ? v.y : v.x),
							pos.Pivot.y + (pos.Flip ? v.x : v.y)
						]++;
					}
				}
			}

			return true;
			// Func
			bool CheckAvailable (int shipIndex, int _x, int _y, bool _flip) {
				var body = ships[shipIndex].Body;
				foreach (var v in body) {
					var value = values[
						shipIndex,
						_x + (_flip ? v.y : v.x),
						_y + (_flip ? v.x : v.y)
					];
					if (value == -1) { return false; }
				}
				return true;
			}
		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}
