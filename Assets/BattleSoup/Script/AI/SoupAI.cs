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
		public int CellCount { get; private set; } = 0;
		public int OpponentMapSize { get; private set; } = 0;
		public AiCell[,] OpponentCells { get; private set; } = new AiCell[0, 0];
		public List<Ship> OpponentShips { get; } = new();
		public List<Ship> SelfShips { get; } = new();
		public List<ShipPosition>[] AllPositions { get; private set; } = new List<ShipPosition>[0];
		public int[,,] ShipWeights { get; private set; } = new int[0, 0, 0];
		public int[,,] HitShipWeights { get; private set; } = new int[0, 0, 0];
		public int ValidCellCount { get; private set; } = 0;
		public int HittableCellCount { get; private set; } = 0;
		public int HitCellCount { get; private set; } = 0;
		public int RevealedShipCellCount { get; private set; } = 0;
		public Vector2Int? OneShotSunkPosition { get; private set; } = null;
		public List<Vector2Int>[] BestWeights { get; private set; } = new List<Vector2Int>[0];

		// Data
		private int[,] c_MapInt = new int[0, 0];


		#endregion




		#region --- API ---


		public void Analyze (in eField self, in eField opponent) {

			if (c_MapInt.GetLength(0) != OpponentMapSize || c_MapInt.GetLength(1) != OpponentMapSize) {
				c_MapInt = new int[OpponentMapSize, OpponentMapSize];
			}

			SyncShipsLogic(self.Ships, SelfShips);
			SyncShipsLogic(opponent.Ships, OpponentShips);
			SyncCellsLogic(opponent);

			Analyze_FieldInfo();

			Analyze_CalculateAllPositions();
			Analyze_CleanUpForSunkCheck();
			Analyze_CleanUpForFullOccupyCell();

			Analyze_SortPositions();
			Analyze_CalculateShipWeights();
			Analyze_CalculateHitShipWeights();

			Analyze_OneShotSunk();
		}


		protected bool TrySonarInRandomCorner (out Vector2Int sonarPos) {
			sonarPos = default;
			int offset = Random.Range(0, 4);
			for (int i = 0; i < 4; i++) {
				int index = (i + offset) % 4;
				switch (index) {
					case 0:
						if (TryCell(0, OpponentMapSize - 1, out sonarPos)) return true;
						break;
					case 1:
						if (TryCell(OpponentMapSize - 1, 0, out sonarPos)) return true;
						break;
					case 2:
						if (TryCell(OpponentMapSize - 1, OpponentMapSize - 1, out sonarPos)) return true;
						break;
					case 3:
						if (TryCell(0, 0, out sonarPos)) return true;
						break;
				}
			}
			return false;
			bool TryCell (int x, int y, out Vector2Int result) {
				result = new Vector2Int(x, y);
				var cell = OpponentCells[x, y];
				return cell.Sonar <= 0 && cell.State != CellState.Hit && cell.State != CellState.Sunk;
			}
		}


		protected bool ShipIsReady (int index) {
			if (index < 0 || index >= SelfShips.Count) return false;
			var ship = SelfShips[index];
			return ship.Alive && ship.CurrentCooldown <= 0;
		}


		protected Vector2Int GetFirstValidHittablePosition (bool ignoreSonar = true) {
			for (int j = 0; j < OpponentMapSize; j++) {
				for (int i = 0; i < OpponentMapSize; i++) {
					var cell = OpponentCells[i, j];
					if (cell.State == CellState.Hit || cell.State == CellState.Sunk) continue;
					if (!ignoreSonar && cell.Sonar > 0) continue;
					return new(i, j);
				}
			}
			return default;
		}


		protected Vector2Int GetBestAttackPosition (bool ignoreSonar = true) {
			var pos = new Vector2Int(0, 0);







			return GetFirstValidHittablePosition(ignoreSonar);
		}


		// Override
		public virtual PerformResult Perform (int abilityIndex) {

			// One Shot One Kill (if available)
			if (OneShotSunkPosition.HasValue) return new PerformResult(-1) { Position = OneShotSunkPosition.Value, };

			// General
			PerformResult result = new(-1);
			if (abilityIndex >= 0 && abilityIndex < SelfShips.Count) {
				// Performing Ability
				result = PerformShip(abilityIndex);
			} else {
				// Free Start
				for (int i = 0; i < SelfShips.Count; i++) {
					if (RequireShip(i)) {
						result.AbilityIndex = i;
						break;
					}
				}
			}

			// Attack
			if (result.AbilityIndex == -1) result.Position = GetBestAttackPosition();
			return result;
		}


		protected abstract bool RequireShip (int shipIndex);


		protected abstract PerformResult PerformShip (int shipIndex);


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
				target.Exposed = source.Exposed;
				target.BodyNodes = source.BodyNodes;
				target.CurrentCooldown = source.CurrentCooldown;
				target.IsSymmetric = source.IsSymmetric;

				target.GlobalName = source.GlobalName;
				target.DisplayName = source.DisplayName;

				if (source.Exposed) {
					target.FieldX = source.FieldX;
					target.FieldY = source.FieldY;
					target.Flip = source.Flip;
				}
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
			CellCount = size * size;
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
					if (sourceCell.ShipIndex >= 0 && opponent.Ships[sourceCell.ShipIndex].Exposed) {
						if (targetCell.ShipIndexs.Count == 0) {
							targetCell.ShipIndexs.Add(sourceCell.ShipIndex);
						} else {
							targetCell.ShipIndexs[0] = sourceCell.ShipIndex;
						}
					}
				}
			}
		}


		// Info
		private void Analyze_FieldInfo () {
			ValidCellCount = 0;
			HittableCellCount = 0;
			HitCellCount = 0;
			RevealedShipCellCount = 0;
			int size = OpponentMapSize;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					var cell = OpponentCells[i, j];
					// Valid 
					if (!cell.HasStone) ValidCellCount++;
					// Hittable
					if (cell.State == CellState.Normal || cell.HasRevealedShip) HittableCellCount++;
					// Hit
					if (cell.State == CellState.Hit) HitCellCount++;
					// Revealed Ship
					if (cell.HasRevealedShip) RevealedShipCellCount++;
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
				// Exposed Ship Only Have One Position
				if (ship.Exposed) {
					positions.Add(new ShipPosition(ship.FieldX, ship.FieldY, ship.Flip));
					continue;
				}
				// Get All Positions
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


		private void Analyze_CleanUpForFullOccupyCell () {

			// If all posible positions of a ship both occupy one cell
			// That cell belongs to this ship and can not belongs to other ships
			// So other ship positions contains this cell should be remove

			for (int safe = 0; safe < 1024; safe++) if (!CleanAll()) break;

			// Func
			bool CleanAll () {
				bool changed = false;
				for (int refIndex = 0; refIndex < OpponentShips.Count; refIndex++) {
					var refPositions = AllPositions[refIndex];
					if (refPositions.Count == 0) continue;
					// Fill Cache
					bool validCache = false;
					var refShip = OpponentShips[refIndex];
					System.Array.Clear(c_MapInt, 0, c_MapInt.Length);
					foreach (var refPosition in refPositions) {
						for (int j = 0; j < refShip.BodyNodes.Length; j++) {
							var (x, y) = refPosition.GetNodePosition(refShip, j);
							if (x < 0 || y < 0 || x >= OpponentMapSize || y >= OpponentMapSize) continue;
							int value = c_MapInt[x, y] + 1;
							if (value >= refPositions.Count) validCache = true;
							c_MapInt[x, y] = value;
						}
					}
					// Clean
					if (validCache) changed = CleanForRef(refIndex) || changed;
				}
				return changed;
			}

			bool CleanForRef (int refIndex) {
				bool _changed = false;
				int refMaxValue = AllPositions[refIndex].Count;
				for (int targetIndex = 0; targetIndex < OpponentShips.Count; targetIndex++) {
					if (targetIndex == refIndex) continue;
					var positions = AllPositions[targetIndex];
					if (positions.Count == 0) continue;
					var targetShip = OpponentShips[targetIndex];
					for (int i = 0; i < positions.Count; i++) {
						var sPos = positions[i];
						for (int j = 0; j < targetShip.BodyNodes.Length; j++) {
							var (x, y) = sPos.GetNodePosition(targetShip, j);
							if (c_MapInt[x, y] >= refMaxValue) {
								positions.RemoveAt(i);
								i--;
								_changed = true;
								break;
							}
						}
					}
				}
				return _changed;
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

			// Init Array
			int size = OpponentMapSize;
			if (
				ShipWeights.GetLength(0) != OpponentShips.Count ||
				ShipWeights.GetLength(1) != size ||
				ShipWeights.GetLength(2) != size
			) {
				ShipWeights = new int[OpponentShips.Count, size, size];
			}
			if (BestWeights.Length != OpponentShips.Count) {
				BestWeights = new List<Vector2Int>[OpponentShips.Count];
			}
			System.Array.Clear(ShipWeights, 0, ShipWeights.Length);

			// Calculate
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				// Weights
				int bestWeight = 0;
				var ship = OpponentShips[shipIndex];
				var positions = AllPositions[shipIndex];
				foreach (var sPos in positions) {
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = sPos.GetNodePosition(ship, i);
						if (x < 0 || y < 0 || x >= size || y >= size) continue;
						ShipWeights[shipIndex, x, y]++;
						bestWeight = Mathf.Max(bestWeight, ShipWeights[shipIndex, x, y]);
					}
				}
				// Best Weights
				var bWeights = BestWeights[shipIndex];
				if (bWeights == null) bWeights = BestWeights[shipIndex] = new();
				bWeights.Clear();
				for (int j = 0; j < size; j++) {
					for (int i = 0; i < size; i++) {
						int w = ShipWeights[shipIndex, i, j];
						if (w >= bestWeight) {
							bWeights.Add(new(i, j));
						}
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


		private void Analyze_OneShotSunk () {
			OneShotSunkPosition = null;
			int maxShipNodeCount = 0;
			for (int shipIndex = 0; shipIndex < AllPositions.Length; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				Vector2Int? unhitCellPos = null;
				var positions = AllPositions[shipIndex];
				foreach (var pos in positions) {
					if (pos.UnhitCellCount > 1) goto NextShip;
					// Get Unhit Position for This Ship
					Vector2Int? uPos = null;
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = pos.GetNodePosition(ship, i);
						var cell = OpponentCells[x, y];
						if (cell.State == CellState.Normal || cell.HasRevealedShip) {
							uPos = new Vector2Int(x, y);
							break;
						}
					}
					// Check Unhit Pos
					if (!uPos.HasValue) continue;
					if (unhitCellPos.HasValue && uPos != unhitCellPos) goto NextShip;
					unhitCellPos = uPos;
				}
				// Set as One Shot Ship
				if (unhitCellPos.HasValue) {
					int nCount = OpponentShips[shipIndex].BodyNodes.Length;
					if (nCount > maxShipNodeCount) {
						maxShipNodeCount = nCount;
						OneShotSunkPosition = unhitCellPos;
					}
				}
				NextShip:;
			}
		}


		#endregion




	}
}