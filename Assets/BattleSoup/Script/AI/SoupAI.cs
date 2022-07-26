using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class SoupAI {




		#region --- SUB ---



		public enum ShipHuntingMode {
			ExposedThenHidden = 0,
			HiddenThenExposed = 1,
			HiddenOnly = 2,
			ExposedOnly = 3,
		}


		public class AiCell : Cell {
			public bool HasRevealedShip = false;
			public bool HasHitShip = false;
			public bool HasShip => HasRevealedShip || HasHitShip || ShipIndex >= 0;
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
		public List<(ShipPosition pos, int shipCount)>[] HitPositions { get; private set; } = new List<(ShipPosition, int)>[0];
		public int[,,] Weights { get; private set; } = new int[0, 0, 0];
		public int[,,] HitWeights { get; private set; } = new int[0, 0, 0];
		public float[,] CookedWeights { get; private set; } = new float[0, 0];
		public float[,] CookedHitWeights { get; private set; } = new float[0, 0];
		public List<Vector2Int>[] BestWeights { get; private set; } = new List<Vector2Int>[0];
		public List<Vector2Int> BestCookedWeights { get; } = new();
		public List<Vector2Int> BestCookedHitWeights { get; } = new();
		public int ValidCellCount { get; private set; } = 0;
		public int HittableCellCount { get; private set; } = 0;
		public int HitCellCount { get; private set; } = 0;
		public int RevealedShipCellCount { get; private set; } = 0;
		public int RevealedCellCount { get; private set; } = 0;
		public int LiveShipCount { get; private set; } = 0;
		public float MaxCookedWeight { get; private set; } = 0f;
		public float MaxCookedHitWeight { get; private set; } = 0f;
		public Vector2Int? OneShotSunkPosition { get; private set; } = null;
		protected BattleSoup Soup { get; private set; } = null;

		// Data
		private int[,] c_MapInt = new int[0, 0];


		#endregion




		#region --- API ---


		public void Analyze (in eField self, in eField opponent) {

			SyncShipsLogic(self.Ships, SelfShips);
			SyncShipsLogic(opponent.Ships, OpponentShips);
			SyncCellsLogic(opponent);

			if (c_MapInt.GetLength(0) != OpponentMapSize || c_MapInt.GetLength(1) != OpponentMapSize) {
				c_MapInt = new int[OpponentMapSize, OpponentMapSize];
			}

			Analyze_FieldInfo();

			Analyze_CalculateAllPositions();
			Analyze_CleanUpForSunkCheck();
			Analyze_CleanUpForFullOccupyCell();
			Analyze_SortPositions();
			Analyze_FillHitPositions();

			Analyze_CalculateWeights();
			Analyze_CalculateBestWeights();
			Analyze_CalculateHitWeights();
			Analyze_CalculateCookedWeights();
			Analyze_CalculateBestCookedWeights();

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


		// Strategy
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


		protected int GetBestShipTarget (ShipHuntingMode mode = ShipHuntingMode.ExposedThenHidden) {
			int targetShipIndex = -1;
			switch (mode) {
				case ShipHuntingMode.ExposedThenHidden:
					targetShipIndex = GetBestShipTargetLogic(true);
					if (targetShipIndex < 0) targetShipIndex = GetBestShipTargetLogic(false);
					break;
				case ShipHuntingMode.HiddenThenExposed:
					targetShipIndex = GetBestShipTargetLogic(false);
					if (targetShipIndex < 0) targetShipIndex = GetBestShipTargetLogic(true);
					break;
				case ShipHuntingMode.HiddenOnly:
					targetShipIndex = GetBestShipTargetLogic(false);
					break;
				case ShipHuntingMode.ExposedOnly:
					targetShipIndex = GetBestShipTargetLogic(true);
					break;
			}
			if (targetShipIndex < 0 || !OpponentShips[targetShipIndex].Alive) {
				int minCount = int.MaxValue;
				for (int i = 0; i < OpponentShips.Count; i++) {
					var ship = OpponentShips[i];
					if (ship.Alive) {
						int count = AllPositions[i].Count;
						if (count < minCount) {
							targetShipIndex = i;
							minCount = count;
						}
					}
				}
			}
			return targetShipIndex;
		}


		protected bool TryGetBestPosition_NormalAttack (out Vector2Int pos) {

			pos = new Vector2Int(0, 0);
			int size = OpponentMapSize;

			// Try One Shot Sunk
			if (OneShotSunkPosition.HasValue) {
				pos = OneShotSunkPosition.Value;
				return true;
			}

			// Try Get Ship Target
			int targetShipIndex = GetBestShipTarget(ShipHuntingMode.HiddenThenExposed);

			// Check for Ships With Only One Posible Position
			if (targetShipIndex < 0 || AllPositions[targetShipIndex].Count > 1) {
				for (int i = 0; i < AllPositions.Length; i++) {
					if (OpponentShips[i].Alive && AllPositions[i].Count == 1) {
						targetShipIndex = i;
						break;
					}
				}
			}

			// Attack Target Ship if Only One Position
			if (targetShipIndex >= 0) {
				var ship = OpponentShips[targetShipIndex];
				if (ship.Alive) {
					var positions = AllPositions[targetShipIndex];
					if (positions.Count == 1) {
						var sPos = positions[0];
						for (int i = 0; i < ship.BodyNodes.Length; i++) {
							var (x, y) = sPos.GetNodePosition(ship, i);
							if (x < 0 || y < 0 || x >= size || y >= size) continue;
							var cell = OpponentCells[x, y];
							if (cell.IsHittable) {
								pos.x = x;
								pos.y = y;
								return true;
							}
						}
					}
				}
			}

			// Try Random Best Cooked Hit Weight Positions
			if (BestCookedHitWeights.Count > 0) {
				int offset = Random.Range(0, BestCookedHitWeights.Count);
				for (int i = 0; i < BestCookedHitWeights.Count; i++) {
					int index = (i + offset) % BestCookedHitWeights.Count;
					var _p = BestCookedHitWeights[index];
					var cell = OpponentCells[_p.x, _p.y];
					if (cell.IsHittable) {
						pos = _p;
						return true;
					}
				}
			}

			// Try Random Best Cooked Weight Positions
			if (BestCookedWeights.Count > 0) {
				int offset = Random.Range(0, BestCookedWeights.Count);
				for (int i = 0; i < BestCookedWeights.Count; i++) {
					int index = (i + offset) % BestCookedWeights.Count;
					var _p = BestCookedWeights[index];
					var cell = OpponentCells[_p.x, _p.y];
					if (cell.IsHittable) {
						pos = _p;
						return true;
					}
				}
			}

			// Try Random Best Weight Positions
			var bestWeights = BestWeights[targetShipIndex];
			if (bestWeights.Count > 0) {
				int offset = Random.Range(0, bestWeights.Count);
				for (int i = 0; i < bestWeights.Count; i++) {
					int index = (i + offset) % bestWeights.Count;
					var _p = bestWeights[index];
					var cell = OpponentCells[_p.x, _p.y];
					if (cell.IsHittable) {
						pos = _p;
						return true;
					}
				}
			}

			// Try Max Weight
			bool success = false;
			int maxWeight = 0;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					var cell = OpponentCells[i, j];
					if (!cell.IsHittable) continue;
					int w = Weights[targetShipIndex, i, j];
					if (w > maxWeight) {
						maxWeight = w;
						pos.x = i;
						pos.y = j;
						success = true;
					}
				}
			}

			return success;
		}


		protected bool TryGetBestPosition_PureAttackAbility (Ability ability, out Vector2Int pos, out Direction4 dir) {

			pos = new Vector2Int(0, 0);
			dir = default;
			if (ability == null) return false;

			// Try Get Ship Target
			int targetShipIndex = GetBestShipTarget(ShipHuntingMode.HiddenThenExposed);

			// Check for Ships With Only One Posible Position
			if (targetShipIndex < 0 || AllPositions[targetShipIndex].Count > 1) {
				for (int i = 0; i < AllPositions.Length; i++) {
					if (AllPositions[i].Count == 1) {
						targetShipIndex = i;
						break;
					}
				}
			}
			if (targetShipIndex < 0) return false;

			// Try Hit Positions
			bool success = false;
			var hPositions = HitPositions[targetShipIndex];
			var ship = OpponentShips[targetShipIndex];
			if (hPositions.Count > 0) {
				int match = 0;
				foreach (var (sPos, _) in hPositions) {
					int mat = GetBestAttackMatching(ability, ship, sPos, out var aPos, out var aDir);
					if (mat > match) {
						pos = aPos;
						dir = aDir;
						success = true;
					}
				}
			}
			if (success) return true;

			// Try Positions
			var positions = AllPositions[targetShipIndex];
			if (positions.Count > 0) {
				int match = 0;
				foreach (var sPos in positions) {
					int mat = GetBestAttackMatching(ability, ship, sPos, out var aPos, out var aDir);
					if (mat > match) {
						pos = aPos;
						dir = aDir;
						success = true;
					}
				}
			}

			return success;
		}


		// Override
		public virtual PerformResult Perform (BattleSoup soup, int abilityIndex) {

			Soup = soup;

			// One Shot One Kill (if available)
			if (OneShotSunkPosition.HasValue) return new PerformResult(-1) { Position = OneShotSunkPosition.Value };

			// General
			PerformResult result = new(-1);
			if (abilityIndex >= 0 && abilityIndex < SelfShips.Count) {
				// Performing Ability
				result = PerformShip(abilityIndex);
			} else {
				// Free Start
				result.AbilityIndex = FreeStart();
			}

			// Attack
			if (result.AbilityIndex == -1 && TryGetBestPosition_NormalAttack(out var pos)) result.Position = pos;

			return result;
		}


		protected abstract int FreeStart ();


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


		private int GetBestShipTargetLogic (bool forExposedShips) {
			int targetIndex = -1;
			int hitPosCountMin = int.MaxValue;
			int targetPosCount = 0;
			int targetBodySize = 0;
			int targetMaxCooldown = 0;
			for (int i = 0; i < OpponentShips.Count; i++) {
				var ship = OpponentShips[i];
				if (!ship.Alive) continue;
				if (ship.Exposed != forExposedShips) continue;
				int count = AllPositions[i].Count;
				int hitCount = HitPositions[i].Count;
				int bodySize = ship.BodyNodes.Length;
				int maxCooldown = ship.MaxCooldown;
				if (hitCount < hitPosCountMin) {
					// 1. Compare Posible Hit Positions Count
					SetTargetIndex(i);
				} else if (hitCount == hitPosCountMin) {
					// 2. Compare Posible Positions Count
					if (count < targetPosCount) {
						SetTargetIndex(i);
					} else if (count == targetPosCount) {
						// 3. Compare Ship Body Size
						if (bodySize > targetBodySize) {
							SetTargetIndex(i);
						} else if (bodySize == targetBodySize) {
							// 4. Compare Ability Cooldown
							if (maxCooldown < targetMaxCooldown) {
								SetTargetIndex(i);
							}
						}
					}
				}
				// Func
				void SetTargetIndex (int index) {
					targetIndex = index;
					hitPosCountMin = hitCount;
					targetPosCount = count;
					targetBodySize = bodySize;
					targetMaxCooldown = maxCooldown;
				}
			}

			return targetIndex;
		}


		// Info
		private void Analyze_FieldInfo () {
			ValidCellCount = 0;
			HittableCellCount = 0;
			HitCellCount = 0;
			RevealedShipCellCount = 0;
			RevealedCellCount = 0;
			LiveShipCount = OpponentShips.Count(s => s.Alive);
			int size = OpponentMapSize;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					var cell = OpponentCells[i, j];
					// Valid 
					if (!cell.HasStone) ValidCellCount++;
					// Hittable
					if (cell.IsHittable) HittableCellCount++;
					// Hit
					if (cell.State == CellState.Hit) HitCellCount++;
					// Revealed Ship
					if (cell.HasRevealedShip) RevealedShipCellCount++;
					// Revealed
					if (cell.State == CellState.Revealed && !cell.HasStone) RevealedCellCount++;
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


		private void Analyze_FillHitPositions () {
			if (HitPositions.Length != AllPositions.Length) {
				HitPositions = new List<(ShipPosition, int)>[AllPositions.Length];
				for (int i = 0; i < HitPositions.Length; i++) {
					HitPositions[i] = new();
				}
			}
			for (int i = 0; i < HitPositions.Length; i++) {
				var positions = AllPositions[i];
				var hPisitions = HitPositions[i];
				var ship = OpponentShips[i];
				if (hPisitions == null) hPisitions = HitPositions[i] = new();
				foreach (var pos in positions) {
					if (pos.HitCellCount > 0) {
						int shipCount = 0;
						for (int j = 0; j < ship.BodyNodes.Length; j++) {
							var (x, y) = pos.GetNodePosition(ship, j);
							var cell = OpponentCells[x, y];
							if (cell.HasShip) shipCount++;
						}
						hPisitions.Add((pos, shipCount));
					}
				}
				hPisitions.Sort((a, b) => b.shipCount.CompareTo(a.shipCount));
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
						if (cell.IsHittable) {
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


		// Weight
		private void Analyze_CalculateWeights () {

			// Init Array
			int size = OpponentMapSize;
			if (
				Weights.GetLength(0) != OpponentShips.Count ||
				Weights.GetLength(1) != size ||
				Weights.GetLength(2) != size
			) {
				Weights = new int[OpponentShips.Count, size, size];
			}
			System.Array.Clear(Weights, 0, Weights.Length);

			// Calculate
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				var positions = AllPositions[shipIndex];
				foreach (var sPos in positions) {
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = sPos.GetNodePosition(ship, i);
						if (x < 0 || y < 0 || x >= size || y >= size) continue;
						Weights[shipIndex, x, y]++;
					}
				}
			}
		}


		private void Analyze_CalculateHitWeights () {
			int size = OpponentMapSize;
			if (
				HitWeights.GetLength(0) != OpponentShips.Count ||
				HitWeights.GetLength(1) != size ||
				HitWeights.GetLength(2) != size
			) {
				HitWeights = new int[OpponentShips.Count, size, size];
			}
			System.Array.Clear(HitWeights, 0, HitWeights.Length);
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				var positions = AllPositions[shipIndex];
				foreach (var sPos in positions) {
					if (sPos.HitCellCount <= 0) continue;
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = sPos.GetNodePosition(ship, i);
						if (x < 0 || y < 0 || x >= size || y >= size) continue;
						HitWeights[shipIndex, x, y]++;
					}
				}
			}
		}


		private void Analyze_CalculateBestWeights () {
			if (BestWeights.Length != OpponentShips.Count) {
				BestWeights = new List<Vector2Int>[OpponentShips.Count];
			}
			int size = OpponentMapSize;
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				// Get Best Value
				int bestWeight = 0;
				for (int j = 0; j < size; j++) {
					for (int i = 0; i < size; i++) {
						bestWeight = Mathf.Max(bestWeight, Weights[shipIndex, i, j]);
					}
				}
				// Fill Value into List
				var bWeights = BestWeights[shipIndex];
				if (bWeights == null) bWeights = BestWeights[shipIndex] = new();
				bWeights.Clear();
				for (int j = 0; j < size; j++) {
					for (int i = 0; i < size; i++) {
						int w = Weights[shipIndex, i, j];
						if (w >= bestWeight) {
							bWeights.Add(new(i, j));
						}
					}
				}
			}
		}


		private void Analyze_CalculateCookedWeights () {
			// Init Array
			int size = OpponentMapSize;
			if (CookedWeights.GetLength(0) != size || CookedWeights.GetLength(1) != size) {
				CookedWeights = new float[size, size];
			}
			System.Array.Clear(CookedWeights, 0, CookedWeights.Length);

			if (CookedHitWeights.GetLength(0) != size || CookedHitWeights.GetLength(1) != size) {
				CookedHitWeights = new float[size, size];
			}
			System.Array.Clear(CookedHitWeights, 0, CookedHitWeights.Length);

			// Calculate
			MaxCookedWeight = 0f;
			MaxCookedHitWeight = 0f;
			for (int shipIndex = 0; shipIndex < OpponentShips.Count; shipIndex++) {
				var ship = OpponentShips[shipIndex];
				float shipPosCount = AllPositions[shipIndex].Count;
				float hitPosCount = HitPositions[shipIndex].Count;
				if (!ship.Alive || shipPosCount.AlmostZero()) continue;
				for (int j = 0; j < size; j++) {
					for (int i = 0; i < size; i++) {
						var cell = OpponentCells[i, j];
						if (cell.IsHittable) {
							CookedWeights[i, j] += Weights[shipIndex, i, j] / shipPosCount;
							MaxCookedWeight = Mathf.Max(MaxCookedWeight, CookedWeights[i, j]);
						}
						if (hitPosCount.NotAlmostZero()) {
							CookedHitWeights[i, j] += HitWeights[shipIndex, i, j] / hitPosCount;
							MaxCookedHitWeight = Mathf.Max(MaxCookedHitWeight, CookedHitWeights[i, j]);
						}
					}
				}
			}
		}


		private void Analyze_CalculateBestCookedWeights () {
			BestCookedWeights.Clear();
			BestCookedHitWeights.Clear();
			int size = OpponentMapSize;
			// Get Best Value
			float bestWeight = 0f;
			float bestHitWeight = 0f;
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					float w = CookedWeights[i, j];
					if (w.GreaterOrAlmost(bestWeight)) bestWeight = w;
					float hw = CookedHitWeights[i, j];
					if (hw.GreaterOrAlmost(bestHitWeight)) bestHitWeight = hw;
				}
			}
			// Fill Value into List
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					float w = CookedWeights[i, j];
					if (w.GreaterOrAlmost(bestWeight)) BestCookedWeights.Add(new(i, j));
					float hw = CookedHitWeights[i, j];
					if (hw.GreaterOrAlmost(bestHitWeight)) BestCookedHitWeights.Add(new(i, j));
				}
			}
		}


		// Util
		private int AttackMatch (Vector2Int pickingPos, Direction4 pickingDir, Ability ability, Ship ship, ShipPosition position) {
			if (
				!ability.EntrancePool.TryGetValue(EntranceType.OnAbilityUsed, out int start) &&
				!ability.EntrancePool.TryGetValue(EntranceType.OnAbilityUsedOvercharged, out start)
			) return 0;
			int match = 0;
			int size = OpponentMapSize;
			for (int index = start + 1; index < ability.Units.Length; index++) {
				var unit = ability.Units[index];
				if (unit is not ActionUnit act) break;
				if (act.Type != ActionType.Attack) continue;
				for (int j = 0; j < act.Positions.Length; j++) {
					var actPos = act.Positions[j];
					act.TryGetKeyword(j, out var keyword);
					var targetCellPos = SoupUtil.GetPickedPosition(pickingPos, pickingDir, actPos.x, actPos.y);
					if (targetCellPos.x < 0 || targetCellPos.y < 0 || targetCellPos.x >= size || targetCellPos.y >= size) continue;
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var (x, y) = position.GetNodePosition(ship, i);
						if (targetCellPos.x != x || targetCellPos.y != y) continue;
						var cell = OpponentCells[x, y];
						if (!cell.IsHittable) continue;
						if (!keyword.Check(cell)) continue;
						match++;
						break;
					}
				}
			}
			return match;
		}


		private int GetBestAttackMatching (Ability ability, Ship ship, ShipPosition position, out Vector2Int attackPos, out Direction4 attackDir) {
			int match = 0;
			var pos = new Vector2Int();
			var dir = Direction4.Up;
			int size = OpponentMapSize;
			var p = new Vector2Int();
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					p.x = i;
					p.y = j;
					Try(p, Direction4.Up);
					Try(p, Direction4.Down);
					Try(p, Direction4.Left);
					Try(p, Direction4.Right);
				}
			}
			attackPos = pos;
			attackDir = dir;
			return match;
			// Func
			void Try (Vector2Int _pos, Direction4 _dir) {
				int _mat = AttackMatch(_pos, _dir, ability, ship, position);
				if (_mat > match) {
					match = _mat;
					pos = _pos;
					dir = _dir;
				}
			}
		}


		#endregion




	}
}