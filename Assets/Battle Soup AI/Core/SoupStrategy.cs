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
		private int[] PosGroupsIndex = new int[0];


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
		public virtual bool CalculatePotentialPositions (
			Ship[] ships,
			Tile[,] tiles,
			ShipPosition?[] knownPositions,
			ref List<ShipPosition>[] hiddenPositions,
			ref List<ShipPosition>[] exposedPositions
		) {

			// Check
			if (ships == null || ships.Length == 0) { return false; }
			if (knownPositions == null || knownPositions.Length != ships.Length) { return false; }
			if (
				tiles == null ||
				tiles.Length == 0 ||
				tiles.GetLength(0) != tiles.GetLength(1)
			) { return false; }
			if (hiddenPositions == null || hiddenPositions.Length != ships.Length) {
				hiddenPositions = new List<ShipPosition>[ships.Length];
			}
			if (exposedPositions == null || exposedPositions.Length != ships.Length) {
				exposedPositions = new List<ShipPosition>[ships.Length];
			}

			int size = tiles.GetLength(1);
			int shipCount = ships.Length;

			// Add Potential Ships
			if (hiddenPositions == null || hiddenPositions.Length != shipCount) {
				hiddenPositions = new List<ShipPosition>[shipCount];
			}
			if (exposedPositions == null || exposedPositions.Length != shipCount) {
				exposedPositions = new List<ShipPosition>[shipCount];
			}
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				if (hiddenPositions[shipIndex] == null) {
					hiddenPositions[shipIndex] = new List<ShipPosition>();
				}
				if (exposedPositions[shipIndex] == null) {
					exposedPositions[shipIndex] = new List<ShipPosition>();
				}
				var hiddenList = hiddenPositions[shipIndex];
				var exposedList = exposedPositions[shipIndex];
				hiddenList.Clear();
				exposedList.Clear();
				if (knownPositions[shipIndex].HasValue) {
					exposedList.Add(knownPositions[shipIndex].Value);
					continue;
				}
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						if (!ships[shipIndex].Symmetry && CheckAvailable(shipIndex, x, y, true, out bool exposed)) {
							(exposed ? exposedList : hiddenList).Add(new ShipPosition(x, y, true));
						}
						if (CheckAvailable(shipIndex, x, y, false, out exposed)) {
							(exposed ? exposedList : hiddenList).Add(new ShipPosition(x, y, false));
						}
					}
				}
			}

			return true;
			// Func
			bool CheckAvailable (int _shipIndex, int _x, int _y, bool _flip, out bool _isExposed) {
				var body = ships[_shipIndex].Body;
				_isExposed = false;
				bool available = true;
				int size = tiles.GetLength(0);
				foreach (var v in body) {
					int _i = _x + (_flip ? v.y : v.x);
					int _j = _y + (_flip ? v.x : v.y);
					if (_i < 0 || _j < 0 || _i >= size || _j >= size) {
						_isExposed = false;
						return false;
					}
					switch (tiles[_i, _j]) {
						case Tile.RevealedShip:
						case Tile.HittedShip:
							_isExposed = true;
							break;
						case Tile.GeneralStone:
						case Tile.RevealedStone:
						case Tile.RevealedWater:
						case Tile.SunkShip:
							available = false;
							break;
					}
					if (!available && _isExposed) { break; }
				}
				// Sunk Check
				if (available && _isExposed) {
					bool alive = false;
					foreach (var v in body) {
						var tile = tiles[
							_x + (_flip ? v.y : v.x),
							_y + (_flip ? v.x : v.y)
						];
						if (tile == Tile.SunkShip) {
							alive = false;
							break;
						} else if (tile != Tile.HittedShip) {
							alive = true;
							break;
						}
					}
					if (!alive) {
						available = false;
					}
				}
				return available;
			}

		}


		public virtual bool CalculateExposedMap (
			Ship[] ships,
			Tile[,] tiles,
			ref Dictionary<Int2, int>[] exposedMap
		) {

			if (ships == null || ships.Length == 0) { return false; }
			if (tiles == null || tiles.GetLength(0) != tiles.GetLength(1)) { return false; }
			if (exposedMap == null || exposedMap.Length != ships.Length) {
				exposedMap = new Dictionary<Int2, int>[ships.Length];
				for (int i = 0; i < exposedMap.Length; i++) {
					exposedMap[i] = new Dictionary<Int2, int>();
				}
			}

			int shipCount = ships.Length;
			int size = tiles.GetLength(0);

			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				var map = exposedMap[shipIndex];
				Int2 tilePos = default;
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						var tile = tiles[x, y];
						tilePos.x = x;
						tilePos.y = y;
						if (tile == Tile.HittedShip || tile == Tile.RevealedShip) {
							if (!map.ContainsKey(tilePos)) {
								map.Add(tilePos, -1);
							}
						} else {
							if (map.ContainsKey(tilePos)) {
								map.Remove(tilePos);
							}
						}
					}
				}
			}
			return true;
		}


		public virtual bool SmartFixExposedPositions (
			Ship[] ships,
			Tile[,] tiles,
			Dictionary<Int2, int>[] exposedMap,
			ref List<ShipPosition>[] exposedPositions
		) {

			// Check
			if (ships == null || ships.Length == 0) { return false; }
			if (tiles == null || tiles.Length == 0 || tiles.GetLength(0) != tiles.GetLength(1)) { return false; }
			if (exposedMap == null || exposedMap.Length != ships.Length) { return false; }
			if (exposedPositions == null || exposedPositions.Length != ships.Length) {
				exposedPositions = new List<ShipPosition>[ships.Length];
			}
			if (PosGroupsIndex.Length != ships.Length) {
				PosGroupsIndex = new int[ships.Length];
			} else {
				System.Array.Clear(PosGroupsIndex, 0, PosGroupsIndex.Length);
			}

			int shipCount = ships.Length;
			int hittedCount = GetTileCount(tiles, Tile.HittedShip);

			// Remove When Hitted Tile Don't Runout







			return true;
		}


		public virtual bool CalculatePotentialValues (
			Ship[] ships,
			int mapSize,
			List<ShipPosition>[] positions,
			bool resetValues,
			ref int[,,] values,
			out int minValue,
			out int maxValue
		) {
			minValue = maxValue = 0;

			if (ships == null || ships.Length == 0) { return false; }
			if (positions == null || positions.Length != ships.Length) { return false; }
			if (
				values == null ||
				values.GetLength(0) != ships.Length ||
				values.GetLength(1) != mapSize ||
				values.GetLength(2) != mapSize
			) {
				values = new int[ships.Length, mapSize, mapSize];
			}

			int shipCount = ships.Length;

			// Values:
			// 0+	ship count
			// -1	stone, revealed water
			// -2	revealed ship
			// -3	hit ship
			// -4	sunk ship

			// Init
			if (resetValues) {
				for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
					for (int y = 0; y < mapSize; y++) {
						for (int x = 0; x < mapSize; x++) {
							values[shipIndex, x, y] = 0;
						}
					}
				}
			}

			// Calculate Values
			minValue = int.MaxValue;
			maxValue = int.MinValue;
			int _x, _y, _newValue;
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				(minValue, maxValue) = AddToValues(values, positions[shipIndex], shipIndex, minValue, maxValue);
			}

			return true;
			// Func
			(int min, int max) AddToValues (int[,,] _values, List<ShipPosition> _list, int _shipIndex, int _minValue, int _maxValue) {
				var _ship = ships[_shipIndex];
				foreach (var pos in _list) {
					foreach (var v in _ship.Body) {
						_x = pos.Pivot.x + (pos.Flip ? v.y : v.x);
						_y = pos.Pivot.y + (pos.Flip ? v.x : v.y);
						_newValue = _values[_shipIndex, _x, _y] + 1;
						_values[_shipIndex, _x, _y] = _newValue;
						if (_newValue >= 0) {
							_minValue = System.Math.Min(_newValue, _minValue);
							_maxValue = System.Math.Max(_newValue, _maxValue);
						}
					}
				}
				return (_minValue, _maxValue);
			}
		}


		#endregion




		#region --- LGC ---


		private int GetTileCount (Tile[,] tiles, Tile target) {
			int sizeX = tiles.GetLength(0);
			int sizeY = tiles.GetLength(1);
			int count = 0;
			for (int j = 0; j < sizeY; j++) {
				for (int i = 0; i < sizeX; i++) {
					if (tiles[i, j] == target) {
						count++;
					}
				}
			}
			return count;
		}


		private bool ToNextPosGroupsIndex (List<ShipPosition>[] posList) {
			int len = PosGroupsIndex.Length;
			for (int i = len - 1; i >= 0; i--) {
				if (PosGroupsIndex[i] < posList[i].Count - 1) {
					PosGroupsIndex[i]++;
					return true;
				} else if (i > 0 && PosGroupsIndex[i - 1] < posList[i].Count - 1) {
					PosGroupsIndex[i] = 0;
					PosGroupsIndex[i - 1]++;
					return true;
				}
			}
			return false;
		}


		#endregion




	}
}
