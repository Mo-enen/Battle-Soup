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

		// Data
		private int[,] RIP_Values = null;



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
		public bool CalculatePotentialPositions (Ship[] ships, bool[] shipsAlive, Tile[,] tiles, ShipPosition?[] knownPositions, Tile mustContains, Tile onlyContains, ref List<ShipPosition>[] positions) {

			// Check
			if (ships == null || ships.Length == 0) { return false; }
			if (shipsAlive == null || shipsAlive.Length != ships.Length) { return false; }
			if (
				tiles == null ||
				tiles.Length == 0 ||
				tiles.GetLength(0) != tiles.GetLength(1)
			) { return false; }
			if (positions == null || positions.Length != ships.Length) {
				positions = new List<ShipPosition>[ships.Length];
			}

			int size = tiles.GetLength(1);
			int shipCount = ships.Length;

			// Add Potential Ships
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				if (positions[shipIndex] == null) {
					positions[shipIndex] = new List<ShipPosition>();
				}
				var posList = positions[shipIndex];
				posList.Clear();
				if (!shipsAlive[shipIndex]) { continue; }
				if (knownPositions[shipIndex].HasValue) { continue; }
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						if (!ships[shipIndex].Symmetry && CheckAvailable(shipIndex, x, y, true)) {
							posList.Add(new ShipPosition(x, y, true));
						}
						if (CheckAvailable(shipIndex, x, y, false)) {
							posList.Add(new ShipPosition(x, y, false));
						}
					}
				}
			}

			return true;
			// Func
			bool CheckAvailable (int _shipIndex, int _x, int _y, bool _flip) {
				var body = ships[_shipIndex].Body;
				int _hitCount = 0;
				bool containsTarget = false;
				foreach (var v in body) {
					int _i = _x + (_flip ? v.y : v.x);
					int _j = _y + (_flip ? v.x : v.y);
					if (_i < 0 || _j < 0 || _i >= size || _j >= size) { return false; }
					var tile = tiles[_i, _j];
					if (!onlyContains.HasFlag(tile)) { return false; }
					if (mustContains.HasFlag(tile)) {
						containsTarget = true;
					}
					if (tile == Tile.HittedShip) { _hitCount++; }
				}
				return containsTarget && _hitCount < body.Length;
			}
		}


		public void RemoveImpossiblePositions (Ship[] ships, int mapSize, ref List<ShipPosition>[] positionsA, ref List<ShipPosition>[] positionsB) {
			if (ships == null || ships.Length == 0) { return; }
			if (positionsA == null || positionsA.Length != ships.Length) { return; }
			if (positionsB == null || positionsB.Length != ships.Length) { return; }
			if (RIP_Values == null || RIP_Values.GetLength(0) != mapSize || RIP_Values.GetLength(1) != mapSize) {
				RIP_Values = new int[mapSize, mapSize];
			} else {
				System.Array.Clear(RIP_Values, 0, RIP_Values.Length);
			}
			bool needToDoTheThing = true;
			while (needToDoTheThing) {
				needToDoTheThing = false;
				for (int _index = 0; _index < ships.Length; _index++) {
					// Do the Thing
					var listA = positionsA[_index];
					var listB = positionsB[_index];
					int countAB = listA.Count + listB.Count;
					if (countAB == 1) {
						var sPos = listA.Count > 0 ? listA[0] : listB[0];
						SetValues(_index, sPos, _index + 1);
						for (int _shipIndex = 0; _shipIndex < ships.Length; _shipIndex++) {
							if (_shipIndex == _index) { continue; }
							var checkListA = positionsA[_shipIndex];
							var checkListB = positionsB[_shipIndex];
							bool moreThanOneBefore = checkListA.Count + checkListB.Count > 1;
							for (int i = 0; i < checkListA.Count; i++) {
								if (!CheckValues(_shipIndex, checkListA[i], _shipIndex + 1)) {
									checkListA.RemoveAt(i);
									i--;
								}
							}
							for (int i = 0; i < checkListB.Count; i++) {
								if (!CheckValues(_shipIndex, checkListB[i], _shipIndex + 1)) {
									checkListB.RemoveAt(i);
									i--;
								}
							}
							if (moreThanOneBefore && checkListA.Count + checkListB.Count == 1) {
								needToDoTheThing = true;
							}
						}
					}
				}
			}
			// Func
			void SetValues (int _index, ShipPosition _sPos, int _value) {
				var ship = ships[_index];
				Int2 _pos;
				foreach (var v in ship.Body) {
					_pos = _sPos.GetPosition(v);
					RIP_Values[_pos.x, _pos.y] = _value;
				}
			}
			bool CheckValues (int _index, ShipPosition _sPos, int _value) {
				var ship = ships[_index];
				Int2 _pos;
				foreach (var v in ship.Body) {
					_pos = _sPos.GetPosition(v);
					int tileValue = RIP_Values[_pos.x, _pos.y];
					if (tileValue != 0 && tileValue != _value) {
						return false;
					}
				}
				return true;
			}
		}


		public bool CalculatePotentialValues (Ship[] ships, int mapSize, List<ShipPosition>[] positions, ref float[,,] values, out float minValue, out float maxValue) {

			const int VALUE_ITERATE = 2;

			minValue = maxValue = 0f;

			if (ships == null || ships.Length == 0) { return false; }
			if (positions == null || positions.Length != ships.Length) { return false; }
			if (
				values == null ||
				values.GetLength(0) != ships.Length + VALUE_ITERATE ||
				values.GetLength(1) != mapSize ||
				values.GetLength(2) != mapSize
			) {
				values = new float[ships.Length + VALUE_ITERATE, mapSize, mapSize];
			}

			int shipCount = ships.Length;

			// Init
			System.Array.Clear(values, 0, values.Length);

			// Calculate Values
			minValue = float.MaxValue;
			maxValue = float.MinValue;
			int _x, _y;
			float _newValue;
			for (int shipIndex = 0; shipIndex < shipCount; shipIndex++) {
				foreach (var pos in positions[shipIndex]) {
					(minValue, maxValue) = AddToValues(
						values, pos, shipIndex, minValue, maxValue
					);
				}
			}

			// Iterate
			for (int iter = 1; iter < VALUE_ITERATE; iter++) {
				for (int y = 0; y < mapSize; y++) {
					for (int x = 0; x < mapSize; x++) {
						if (values[shipCount, x, y] <= 0) { continue; }
						Iterate(ref values, iter, x, y);
					}
				}
			}

			return true;
			// Func
			(float min, float max) AddToValues (float[,,] _values, ShipPosition _pos, int _shipIndex, float _minValue, float _maxValue) {
				var _ship = ships[_shipIndex];
				foreach (var v in _ship.Body) {
					_x = _pos.Pivot.x + (_pos.Flip ? v.y : v.x);
					_y = _pos.Pivot.y + (_pos.Flip ? v.x : v.y);
					_newValue = System.Math.Abs(_values[_shipIndex, _x, _y]) + 1f;
					_values[_shipIndex, _x, _y] = _newValue;
					_values[shipCount, _x, _y]++;
					if (_newValue >= 0) {
						_minValue = System.Math.Min(_newValue, _minValue);
						_maxValue = System.Math.Max(_newValue, _maxValue);
					}
				}
				return (_minValue, _maxValue);
			}
			void Iterate (ref float[,,] _values, int _iter, int _x, int _y) {
				int l = System.Math.Max(_x - 1, 0);
				int r = System.Math.Min(_x + 1, mapSize - 1);
				int d = System.Math.Max(_y - 1, 0);
				int u = System.Math.Min(_y + 1, mapSize - 1);
				float sum = 0f;
				float count = 0f;
				for (int _j = d; _j <= u; _j++) {
					for (int _i = l; _i <= r; _i++) {
						sum += _values[shipCount + _iter - 1, _i, _j];
						count++;
					}
				}
				_values[shipCount + _iter, _x, _y] = count > 0f ? sum / count : 0f;
			}
		}


		// Util
		public (int index, int exposure) GetMostExposedPosition (Ship ship, Tile[,] tiles, List<ShipPosition> positions) {
			if (ship == null || positions == null || positions.Count == 0) { return (-1, 0); }
			if (positions.Count == 1) { return (0, Exposure(ship, positions[0])); }
			int maxEx = 0;
			int maxIndex = -1;
			for (int i = 0; i < positions.Count; i++) {
				int ex = Exposure(ship, positions[i]);
				if (ex > maxEx) {
					maxEx = ex;
					maxIndex = i;
				}
			}
			return (maxIndex, maxEx);
			// Func
			int Exposure (Ship _ship, ShipPosition _sPos) {
				int result = 0;
				foreach (var v in _ship.Body) {
					var pos = _sPos.GetPosition(v);
					switch (tiles[pos.x, pos.y]) {
						case Tile.RevealedShip:
						case Tile.HittedShip:
							result++;
							break;
					}
				}
				return result;
			}
		}


		#endregion




	}
}
