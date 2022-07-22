using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class SoupAI {




		#region --- SUB ---



		public class AiCell : Cell {
			public bool HasRevealedShip = false;
			public bool HasHitShip = false;
		}



		public class ShipPosition {

			public int X = 0;
			public int Y = 0;
			public bool Flip = false;
			public int HitCellCount = 0;
			public int UnhitCellCount = 0;

			public ShipPosition (int x, int y, bool flip) {
				X = x;
				Y = y;
				Flip = flip;
				HitCellCount = 0;
			}

			public bool CheckValid (AiCell[,] cells, int size, Ship ship, out int hitCellCount) {
				hitCellCount = 0;
				int len = ship.BodyNodes.Length;
				for (int i = 0; i < len; i++) {
					var (x, y) = GetNodePosition(ship, i);
					if (x < 0 || y < 0 || x >= size || y >= size) return false;
					var cell = cells[x, y];
					if (cell.HasStone) return false;
					if (cell.State == CellState.Sunk) return false;
					if (cell.State == CellState.Revealed && !cell.HasRevealedShip) return false;
					if (cell.State == CellState.Hit) hitCellCount++;
				}
				return true;
			}

			public (int x, int y) GetNodePosition (Ship ship, int index) {
				var node = ship.BodyNodes[index];
				int x = X + (Flip ? node.y : node.x);
				int y = Y + (Flip ? node.x : node.y);
				return (x, y);
			}

			public bool Contains (Ship ship, HashSet<Vector2Int> pool) {
				for (int i = 0; i < ship.BodyNodes.Length; i++) {
					var (_x, _y) = GetNodePosition(ship, i);
					if (pool.Contains(new(_x, _y))) return true;
				}
				return false;
			}

		}



		#endregion




		#region --- VAR ---


		// Api
		public abstract string DisplayName { get; }
		public abstract string Description { get; }
		public abstract string Fleet { get; }

		// Pro
		public int OpponentMapSize { get; private set; } = 0;
		public AiCell[,] OpponentCells { get; private set; } = new AiCell[0, 0];
		public List<Ship> OpponentShips { get; } = new();
		public List<Ship> SelfShips { get; } = new();
		public List<ShipPosition>[] AllPositions { get; private set; } = new List<ShipPosition>[0];
		public int[,,] ShipWeights { get; private set; } = new int[0, 0, 0];
		public int[,,] HitShipWeights { get; private set; } = new int[0, 0, 0];


		#endregion




		#region --- API ---


		public abstract bool Perform (in eField self, int usingAbilityIndex, out Vector2Int attackPosition, out int abilityIndex, out Direction4 abilityDirection);


		public void Analyze (in eField self, in eField opponent) {

			SyncShipsLogic(self.Ships, SelfShips);
			SyncShipsLogic(opponent.Ships, OpponentShips);
			SyncCellsLogic(opponent);

			Analyze_CalculateAllPositions();
			Analyze_CleanUpForAlonePositions();
			Analyze_CleanUpForSunkCheck();

			Analyze_SortPositions();
			Analyze_CalculateShipWeights();
			Analyze_CalculateHitShipWeights();

		}


		#endregion




		#region --- LGC ---


		private void SyncShipsLogic (in Ship[] sourceShips, in List<Ship> targetShips) {
			if (targetShips.Count != sourceShips.Length) {
				targetShips.Clear();
				targetShips.AddRange(new Ship[sourceShips.Length]);
			}
			for (int i = 0; i < sourceShips.Length; i++) {
				var source = sourceShips[i];
				var target = targetShips[i];
				if (target == null) {
					targetShips[i] = target = new Ship();
				}
				target.DefaultCooldown = source.DefaultCooldown;
				target.MaxCooldown = source.MaxCooldown;
				target.GlobalCode = source.GlobalCode;
				target.Visible = source.Visible;
				target.BodyNodes = source.BodyNodes;
				target.CurrentCooldown = source.CurrentCooldown;
				target.IsSymmetric = source.IsSymmetric;

				target.GlobalName = source.GlobalName;
				target.DisplayName = source.DisplayName;
			}
		}


		private void SyncCellsLogic (eField opponent) {
			int size = opponent.MapSize;
			if (OpponentCells.GetLength(0) != size || OpponentCells.GetLength(1) != size) {
				OpponentCells = new AiCell[size, size];
				for (int j = 0; j < size; j++) {
					for (int i = 0; i < size; i++) {
						OpponentCells[i, j] = new AiCell();
					}
				}
			}
			OpponentMapSize = size;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					var sourceCell = opponent[i, j];
					var targetCell = OpponentCells[i, j];
					targetCell.HasStone = sourceCell.HasStone;
					targetCell.State = sourceCell.State;
					targetCell.Sonar = sourceCell.Sonar;
					// Revealed Ship
					if (sourceCell.State == CellState.Revealed && sourceCell.ShipIndex >= 0) {
						targetCell.HasRevealedShip = true;
					}
					if (sourceCell.State == CellState.Hit) {
						targetCell.HasHitShip = true;
					}
					// Add Ship if Visible
					if (sourceCell.ShipIndex >= 0 && opponent.Ships[sourceCell.ShipIndex].Visible) {
						if (targetCell.ShipIndexs.Count == 0) {
							targetCell.ShipIndexs.Add(sourceCell.ShipIndex);
						} else {
							targetCell.ShipIndexs[0] = sourceCell.ShipIndex;
						}
					}
				}
			}
		}


		// All Positions
		private void Analyze_CalculateAllPositions () {

			// Positions
			if (AllPositions.Length != OpponentShips.Count) {
				AllPositions = new List<ShipPosition>[OpponentShips.Count];
				for (int i = 0; i < AllPositions.Length; i++) {
					AllPositions[i] = new();
				}
			}

			// Get Positions for All Ships
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				var positions = AllPositions[shipIndex];
				positions.Clear();
				var ship = OpponentShips[shipIndex];
				if (!ship.Alive) continue;
				for (int j = 0; j < OpponentMapSize; j++) {
					for (int i = 0; i < OpponentMapSize; i++) {

						var pos = new ShipPosition(i, j, false);
						if (pos.CheckValid(OpponentCells, OpponentMapSize, ship, out int hitCellCount)) {
							positions.Add(pos);
							pos.HitCellCount = hitCellCount;
							pos.UnhitCellCount = ship.BodyNodes.Length - hitCellCount;
						}

						if (ship.IsSymmetric) continue;

						pos = new ShipPosition(i, j, true);
						if (pos.CheckValid(OpponentCells, OpponentMapSize, ship, out hitCellCount)) {
							positions.Add(pos);
							pos.HitCellCount = hitCellCount;
							pos.UnhitCellCount = ship.BodyNodes.Length - hitCellCount;
						}
					}
				}
			}

		}


		private void Analyze_CleanUpForAlonePositions () {

			// If a ship only have one posible position
			// other ships must not overlap that position

			var done = new HashSet<int>();
			var ignorePos = new HashSet<Vector2Int>();
			for (int safe = 0; safe < OpponentShips.Count * 2; safe++) {
				for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
					if (done.Contains(shipIndex)) continue;
					var ship = OpponentShips[shipIndex];
					if (!ship.Alive) continue;
					if (AllPositions[shipIndex].Count != 1) continue;
					done.TryAdd(shipIndex);
					Clean(shipIndex);
					goto KeepOn;
				}
				break;
				KeepOn:;
			}
			// Func
			void Clean (int targetShipIndex) {
				// Get Ignored Pos
				ignorePos.Clear();
				var targetPositions = AllPositions[targetShipIndex];
				var targetShip = OpponentShips[targetShipIndex];
				var targetPos = targetPositions[0];
				int size = OpponentMapSize;
				for (int i = 0; i < targetShip.BodyNodes.Length; i++) {
					var (x, y) = targetPos.GetNodePosition(targetShip, i);
					if (x < 0 || y < 0 || x >= size || y >= size) continue;
					ignorePos.TryAdd(new(x, y));
				}
				// Clean
				for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
					if (shipIndex == targetShipIndex) continue;
					var ship = OpponentShips[shipIndex];
					if (!ship.Alive) continue;
					var positions = AllPositions[shipIndex];
					if (positions.Count <= 1) continue;
					// Clean Positions
					int count = positions.Count;
					for (int i = 0; i < count; i++) {
						var pos = positions[i];
						if (pos.Contains(ship, ignorePos)) {
							positions.RemoveAt(i);
							i--;
							count--;
						}
					}
				}
			}
		}


		private void Analyze_CleanUpForSunkCheck () {

			// If a ship position makes a living ship sunk
			// that position should be remove

			for (int shipIndex = 0; shipIndex < AllPositions.Length; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				var positions = AllPositions[shipIndex];
				for (int i = 0; i < positions.Count; i++) {
					if (positions[i].HitCellCount >= ship.BodyNodes.Length) {
						positions.RemoveAt(i);
						i--;
					}
				}
			}
		}


		private void Analyze_SortPositions () {
			foreach (var positions in AllPositions) {
				positions.Sort(
					(a, b) => a.UnhitCellCount.CompareTo(b.UnhitCellCount)
				);
			}
		}


		// Weight
		private void Analyze_CalculateShipWeights () {
			int size = OpponentMapSize;
			if (
				ShipWeights.GetLength(0) != OpponentShips.Count ||
				ShipWeights.GetLength(1) != size ||
				ShipWeights.GetLength(2) != size
			) {
				ShipWeights = new int[OpponentShips.Count, size, size];
			}
			System.Array.Clear(ShipWeights, 0, ShipWeights.Length);
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				var positions = AllPositions[shipIndex];
				foreach (var sPos in positions) {
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = sPos.GetNodePosition(ship, i);
						if (x < 0 || y < 0 || x >= size || y >= size) continue;
						ShipWeights[shipIndex, x, y]++;
					}
				}
			}
		}


		private void Analyze_CalculateHitShipWeights () {
			int size = OpponentMapSize;
			if (
				HitShipWeights.GetLength(0) != OpponentShips.Count ||
				HitShipWeights.GetLength(1) != size ||
				HitShipWeights.GetLength(2) != size
			) {
				HitShipWeights = new int[OpponentShips.Count, size, size];
			}
			System.Array.Clear(HitShipWeights, 0, HitShipWeights.Length);
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				var positions = AllPositions[shipIndex];
				foreach (var sPos in positions) {
					if (sPos.HitCellCount <= 0) continue;
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = sPos.GetNodePosition(ship, i);
						if (x < 0 || y < 0 || x >= size || y >= size) continue;
						HitShipWeights[shipIndex, x, y]++;
					}
				}
			}
		}


		#endregion




	}
}