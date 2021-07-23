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
		public virtual string[] FleetID { get; } = { "Sailboat", "SeaMonster", "Longboat", "MiniSub", };


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
		public virtual bool CalculatePotentialPositions (Ship[] ships, Tile[,] tiles, ShipPosition?[] knownPositions, ref List<ShipPosition>[] hiddenPositions, ref List<(ShipPosition pos, int exCount)>[] exposedPositions) {

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
				exposedPositions = new List<(ShipPosition, int)>[ships.Length];
			}

			int size = tiles.GetLength(1);
			int shipCount = ships.Length;

			// Add Potential Ships
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				if (hiddenPositions[shipIndex] == null) {
					hiddenPositions[shipIndex] = new List<ShipPosition>();
				}
				if (exposedPositions[shipIndex] == null) {
					exposedPositions[shipIndex] = new List<(ShipPosition, int)>();
				}
				var hiddenList = hiddenPositions[shipIndex];
				var exposedList = exposedPositions[shipIndex];
				hiddenList.Clear();
				exposedList.Clear();
				if (knownPositions[shipIndex].HasValue) {
					var sPos = knownPositions[shipIndex].Value;
					exposedList.Add((sPos, GetExposedCount(shipIndex, sPos.Pivot.x, sPos.Pivot.y, sPos.Flip)));
					continue;
				}
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						if (!ships[shipIndex].Symmetry && CheckAvailable(shipIndex, x, y, true, out int exposedCount, out _)) {
							if (exposedCount > 0) {
								exposedList.Add((new ShipPosition(x, y, true), exposedCount));
							} else {
								hiddenList.Add(new ShipPosition(x, y, true));
							}
						}
						if (CheckAvailable(shipIndex, x, y, false, out exposedCount, out _)) {
							if (exposedCount > 0) {
								exposedList.Add((new ShipPosition(x, y, false), exposedCount));
							} else {
								hiddenList.Add(new ShipPosition(x, y, false));
							}
						}
					}
				}
				exposedList.Sort((a, b) => b.exCount.CompareTo(a.exCount));
			}

			return true;
			// Func
			bool CheckAvailable (int _shipIndex, int _x, int _y, bool _flip, out int _exposedCount, out int _hitCount) {
				var body = ships[_shipIndex].Body;
				_exposedCount = 0;
				_hitCount = 0;
				bool available = true;
				foreach (var v in body) {
					int _i = _x + (_flip ? v.y : v.x);
					int _j = _y + (_flip ? v.x : v.y);
					if (_i < 0 || _j < 0 || _i >= size || _j >= size) {
						_exposedCount = 0;
						return false;
					}
					switch (tiles[_i, _j]) {
						case Tile.RevealedShip:
							_exposedCount++;
							break;
						case Tile.HittedShip:
							_exposedCount++;
							_hitCount++;
							break;
						case Tile.GeneralStone:
						case Tile.RevealedStone:
						case Tile.RevealedWater:
						case Tile.SunkShip:
							available = false;
							break;
					}
					if (!available) { break; }
				}
				// Sunk Check
				if (available && _hitCount >= body.Length) {
					available = false;
				}
				return available;
			}
			int GetExposedCount (int _shipIndex, int _x, int _y, bool _flip) {
				var body = ships[_shipIndex].Body;
				int result = 0;
				foreach (var v in body) {
					int _i = _x + (_flip ? v.y : v.x);
					int _j = _y + (_flip ? v.x : v.y);
					if (_i < 0 || _j < 0 || _i >= size || _j >= size) {
						return 0;
					}
					switch (tiles[_i, _j]) {
						case Tile.RevealedShip:
						case Tile.HittedShip:
						case Tile.SunkShip:
							result++;
							break;
					}
				}
				return result;
			}
		}


		public virtual bool CalculateExposedMap (Ship[] ships, Tile[,] tiles, ref Dictionary<Int2, int> exposedMap) {
			if (ships == null || ships.Length == 0) { return false; }
			if (tiles == null || tiles.GetLength(0) != tiles.GetLength(1)) { return false; }
			if (exposedMap == null) {
				exposedMap = new Dictionary<Int2, int>();
			}
			int size = tiles.GetLength(0);
			Int2 tilePos = default;
			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					var tile = tiles[x, y];
					tilePos.x = x;
					tilePos.y = y;
					if (tile == Tile.HittedShip || tile == Tile.RevealedShip) {
						if (!exposedMap.ContainsKey(tilePos)) {
							exposedMap.Add(tilePos, -1);
						}
					} else {
						if (exposedMap.ContainsKey(tilePos)) {
							exposedMap.Remove(tilePos);
						}
					}
				}
			}
			return true;
		}


		public virtual bool CalculatePotentialValues (Ship[] ships, int mapSize, List<ShipPosition>[] hiddenPositions, List<(ShipPosition, int)>[] exposedPositions, bool resetValues, ref int[,,] values, out int minValue, out int maxValue) {
			minValue = maxValue = 0;

			if (ships == null || ships.Length == 0) { return false; }
			if (hiddenPositions == null || hiddenPositions.Length != ships.Length) { return false; }
			if (
				values == null ||
				values.GetLength(0) != ships.Length ||
				values.GetLength(1) != mapSize ||
				values.GetLength(2) != mapSize
			) {
				values = new int[ships.Length, mapSize, mapSize];
			}

			int shipCount = ships.Length;

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
				foreach (var pos in hiddenPositions[shipIndex]) {
					(minValue, maxValue) = AddToValues(
						values, false, pos, shipIndex, minValue, maxValue
					);
				}
			}
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				foreach (var (pos, _) in exposedPositions[shipIndex]) {
					(minValue, maxValue) = AddToValues(
						values, true, pos, shipIndex, minValue, maxValue
					);
				}
			}
			return true;
			// Func
			(int min, int max) AddToValues (int[,,] _values, bool _exposed, ShipPosition _pos, int _shipIndex, int _minValue, int _maxValue) {
				var _ship = ships[_shipIndex];
				foreach (var v in _ship.Body) {
					_x = _pos.Pivot.x + (_pos.Flip ? v.y : v.x);
					_y = _pos.Pivot.y + (_pos.Flip ? v.x : v.y);
					_newValue = System.Math.Abs(_values[_shipIndex, _x, _y]) + 1;
					_values[_shipIndex, _x, _y] = _exposed ? -_newValue : _newValue;
					if (_newValue >= 0) {
						_minValue = System.Math.Min(_newValue, _minValue);
						_maxValue = System.Math.Max(_newValue, _maxValue);
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


		#endregion




	}
}
