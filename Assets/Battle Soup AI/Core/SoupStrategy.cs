using System.Collections;
using System.Collections.Generic;



namespace BattleSoupAI {



	public class BattleInfo {
		public int MapSize;
		public Tile[,] Tiles;
		public Ship[] Ships;
		public int[] Cooldowns;
		public bool[] ShipsAlive;
		public ShipPosition?[] KnownPositions;
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


		public abstract AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, int usingAbilityIndex);


		public virtual void OnBattleStart (BattleInfo ownInfo, BattleInfo opponentInfo) { }


		public virtual void OnBattleEnd (BattleInfo ownInfo, BattleInfo opponentInfo) { }


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
		public virtual bool CalculatePotentialPositions (Ship[] ships, bool[] shipsAlive, Tile[,] tiles, ShipPosition?[] knownPositions, ref List<ShipPosition>[] hiddenPositions, ref List<ShipPosition>[] exposedPositions) {

			// Check
			if (ships == null || ships.Length == 0) { return false; }
			if (knownPositions == null || knownPositions.Length != ships.Length) { return false; }
			if (shipsAlive == null || shipsAlive.Length != ships.Length) { return false; }
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
				if (!shipsAlive[shipIndex]) { continue; }
				if (knownPositions[shipIndex].HasValue) {
					exposedList.Add(knownPositions[shipIndex].Value);
					continue;
				}
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						if (!ships[shipIndex].Symmetry && CheckAvailable(shipIndex, x, y, true, out int exposedCount)) {
							(exposedCount > 0 ? exposedList : hiddenList).Add(new ShipPosition(x, y, true));
						}
						if (CheckAvailable(shipIndex, x, y, false, out exposedCount)) {
							(exposedCount > 0 ? exposedList : hiddenList).Add(new ShipPosition(x, y, false));
						}
					}
				}
			}
			return true;
			// Func
			bool CheckAvailable (int _shipIndex, int _x, int _y, bool _flip, out int _exposedCount) {
				var body = ships[_shipIndex].Body;
				_exposedCount = 0;
				int _hitCount = 0;
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
		}


		public virtual bool CalculatePotentialValues (Ship[] ships, int mapSize, List<ShipPosition>[] hiddenPositions, List<ShipPosition>[] exposedPositions, ref int[,,] values, out int minValue, out int maxValue) {

			const int VALUE_ITERATE = 2;

			minValue = maxValue = 0;

			if (ships == null || ships.Length == 0) { return false; }
			if (hiddenPositions == null || hiddenPositions.Length != ships.Length) { return false; }
			if (
				values == null ||
				values.GetLength(0) != ships.Length + VALUE_ITERATE ||
				values.GetLength(1) != mapSize ||
				values.GetLength(2) != mapSize
			) {
				values = new int[ships.Length + VALUE_ITERATE, mapSize, mapSize];
			}

			int shipCount = ships.Length;

			// Init
			for (int shipIndex = 0; shipIndex < shipCount + 1; shipIndex++) {
				for (int y = 0; y < mapSize; y++) {
					for (int x = 0; x < mapSize; x++) {
						values[shipIndex, x, y] = 0;
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
				foreach (var pos in exposedPositions[shipIndex]) {
					(minValue, maxValue) = AddToValues(
						values, true, pos, shipIndex, minValue, maxValue
					);
				}
			}

			// Iterate
			for (int iter = 1; iter < VALUE_ITERATE; iter++) {
				for (int y = 0; y < mapSize; y++) {
					for (int x = 0; x < mapSize; x++) {
						Iterate(ref values, iter, x, y);
					}
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
					_values[shipCount, _x, _y] += _ship.Symmetry ? 2 : 1;
					if (_newValue >= 0) {
						_minValue = System.Math.Min(_newValue, _minValue);
						_maxValue = System.Math.Max(_newValue, _maxValue);
					}
				}

				return (_minValue, _maxValue);
			}
			void Iterate (ref int[,,] _values, int _iter, int _x, int _y) {
				int l = System.Math.Max(_x - 1, 0);
				int r = System.Math.Min(_x + 1, mapSize - 1);
				int d = System.Math.Max(_y - 1, 0);
				int u = System.Math.Min(_y + 1, mapSize - 1);
				int sum = 0;
				for (int _j = d; _j <= u; _j++) {
					for (int _i = l; _i <= r; _i++) {
						sum += _values[shipCount + _iter - 1, _i, _j];
					}
				}
				_values[shipCount + _iter, _x, _y] = sum;
			}
		}


		#endregion




	}
}
