using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class RevealAndSnipe_Advanced : SoupStrategy_Advanced {




		#region --- SUB ---


		private enum Task {
			Search = 0,
			Reveal = 1,
			Attack = 2,

		}


		#endregion




		#region --- VAR ---


		// Const
		private const int CORACLE_INDEX = 0;
		private const int WHALE_INDEX = 1;
		private const int SQUID_INDEX = 2;
		private const int TURTLE_INDEX = 3;

		// Api
		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Standard Reveal & Snipe strategy created by Moenen. Noob level, easy every time :)";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };


		#endregion




		#region --- API ---


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo oppInfo, int usingAbilityIndex = -1) {

			// Check and Cache
			var result = base.Analyse(ownInfo, oppInfo);
			if (!string.IsNullOrEmpty(result.ErrorMessage)) {
				return result;
			}

			// Perform Task
			return GetTask() switch {
				Task.Search => PerformTask_Search(oppInfo),
				Task.Reveal => PerformTask_Reveal(oppInfo),
				Task.Attack => PerformTask_Attack(oppInfo),
				_ => new AnalyseResult() { ErrorMessage = $"Task not performed" },
			};

		}


		#endregion




		#region --- LGC ---


		private Task GetTask () {
			if (ExposedShipCount == 0) {
				// Ships All Hidden
				return Task.Search;
			} else if (FoundShipCount > 0) {
				// Ship Found
				return Task.Attack;
			} else {
				// Ship Exposed but Not Found
				if (TileCount_RevealedShip > 0) {
					// Has Revealed
					return Task.Attack;
				} else {
					// No Revealed
					return CoracleCooldown <= 2 ? Task.Reveal : Task.Attack;
				}
			}
		}


		private AnalyseResult PerformTask_Search (BattleInfo info) {

			var result = new AnalyseResult() {
				TargetPosition = default,
				AbilityDirection = default,
				AbilityIndex = -1,
				ErrorMessage = "",
			};

			// Best Hidden Pos
			var bestHiddenPos = HiddenValueMax[info.Ships.Length].pos;
			if (
				info.Tiles[bestHiddenPos.x, bestHiddenPos.y] != Tile.GeneralWater &&
				!GetFirstTile(info.Tiles, Tile.GeneralWater | Tile.RevealedShip, out bestHiddenPos)
			) {
				bestHiddenPos = default;
			}

			// Check for Ability
			if (WhaleCooldown == 0) {
				// Use Whale
				if (GetBestValuedTile(HiddenValues, info.Ships.Length, info.Tiles, Tile.All, Tile.All, out var bestWhalePos)) {
					result.TargetPosition = bestWhalePos;
					result.AbilityIndex = WHALE_INDEX;
					LogMessage?.Invoke($"Search/Whale [{result}]");
				} else {
					result.TargetPosition = bestHiddenPos;
					result.AbilityIndex = -1;
					LogMessage?.Invoke($"Search/Normal(Whale Fail) [{result}]");
				}
			} else if (SquidCooldown == 0 && TileCount_RevealedShip + TileCount_RevealedWater > 0) {
				// Use Squid
				if (GetBestValuedTile(HiddenValues, info.Ships.Length, info.Tiles, Tile.RevealedWater | Tile.RevealedShip, Tile.GeneralWater | Tile.RevealedShip, out var bestSquidPos)) {
					result.TargetPosition = bestSquidPos;
					result.AbilityIndex = SQUID_INDEX;
					LogMessage?.Invoke($"Search/Squid [{result}]");
				} else {
					result.TargetPosition = bestHiddenPos;
					result.AbilityIndex = -1;
					LogMessage?.Invoke($"Search/Normal(Squid Fail) [{result}]");
				}
			} else if (TurtleCooldown == 0) {
				// Use Turtle
				result.TargetPosition = bestHiddenPos;
				result.AbilityIndex = TURTLE_INDEX;
				LogMessage?.Invoke($"Search/Turtle [{result}]");
			} else {
				// Use Normal Attack
				result.TargetPosition = bestHiddenPos;
				result.AbilityIndex = -1;
				LogMessage?.Invoke($"Search/Normal [{result}]");
			}


			return result;
		}


		private AnalyseResult PerformTask_Reveal (BattleInfo info) {

			var result = new AnalyseResult() {
				TargetPosition = default,
				AbilityDirection = default,
				AbilityIndex = -1,
				ErrorMessage = "",
			};

			// Best Hidden Pos
			var bestHiddenPos = HiddenValueMax[info.Ships.Length].pos;
			if (
				info.Tiles[bestHiddenPos.x, bestHiddenPos.y] != Tile.GeneralWater &&
				!GetFirstTile(info.Tiles, Tile.GeneralWater | Tile.RevealedShip, out bestHiddenPos)
			) {
				bestHiddenPos = default;
			}

			// Check for Ability
			if (WhaleCooldown == 0) {
				// Use Whale
				if (GetBestValuedTile(HiddenValues, info.Ships.Length, info.Tiles, Tile.HittedShip, Tile.GeneralWater, out var bestWhalePos)) {
					result.TargetPosition = bestWhalePos;
					result.AbilityIndex = WHALE_INDEX;
					LogMessage?.Invoke($"Reveal/Whale [{result}]");
				} else {
					LogMessage("Reveal not performed.");
					return PerformTask_Attack(info);
				}
			} else if (SquidCooldown == 0 && TileCount_RevealedShip + TileCount_RevealedWater > 0) {
				// Use Squid
				if (GetBestValuedTile(HiddenValues, info.Ships.Length, info.Tiles, Tile.RevealedWater | Tile.RevealedShip, Tile.GeneralWater | Tile.RevealedShip, out var bestSquidPos)) {
					result.TargetPosition = bestSquidPos;
					result.AbilityIndex = SQUID_INDEX;
					LogMessage?.Invoke($"Reveal/Squid [{result}]");
				} else {
					LogMessage("Reveal not performed.");
					return PerformTask_Attack(info);
				}
			} else if (TurtleCooldown == 0) {
				// Use Turtle
				result.TargetPosition = bestHiddenPos;
				result.AbilityIndex = TURTLE_INDEX;
				LogMessage?.Invoke($"Reveal/Turtle [{result}]");
			} else {
				// Perform Attack
				LogMessage("Reveal not performed.");
				return PerformTask_Attack(info);
			}

			return result;
		}


		private AnalyseResult PerformTask_Attack (BattleInfo info) {

			var result = new AnalyseResult() {
				TargetPosition = default,
				AbilityDirection = default,
				AbilityIndex = -1,
				ErrorMessage = "",
			};

			// Best Hidden Pos
			var bestHiddenPos = HiddenValueMax[info.Ships.Length].pos;
			if (
				info.Tiles[bestHiddenPos.x, bestHiddenPos.y] != Tile.GeneralWater &&
				!GetFirstTile(info.Tiles, Tile.GeneralWater | Tile.RevealedShip, out bestHiddenPos)
			) {
				bestHiddenPos = default;
			}

			// Sniper Ready
			if (CoracleCooldown == 0) {
				if (
					TryAttackShip(
						info,
						ShipWithMinimalPotentialPos,
						Tile.RevealedShip,
						out var snipePos
					) ||
					GetFirstTile(info.Tiles, Tile.RevealedShip, out snipePos)
				) {
					result.TargetPosition = snipePos;
					result.AbilityIndex = CORACLE_INDEX;
					LogMessage?.Invoke($"Attack/Snipe [{result}]");
					return result;
				}
			}

			// Turtle Ready
			if (TurtleCooldown == 0) {
				if (TryAttackShip(
					info,
					ShipWithMinimalPotentialPos,
					Tile.RevealedShip | Tile.GeneralWater,
					out var tPos
				)) {
					result.TargetPosition = tPos;
					result.AbilityIndex = TURTLE_INDEX;
					LogMessage?.Invoke($"Attack/Turtle [{result}]");
					return result;
				}
			}

			// Use Normal Attack
			if (TryAttackShip(
				info,
				ShipWithMinimalPotentialPos,
				Tile.RevealedShip | Tile.GeneralWater,
				out var attPos
			)) {
				result.TargetPosition = attPos;
				result.AbilityIndex = -1;
				LogMessage?.Invoke($"Attack/Normal [{result}]");
				return result;
			}

			// Search with Normal
			result.TargetPosition = bestHiddenPos;
			result.AbilityIndex = -1;
			LogMessage?.Invoke($"Search/Normal(Attack Failed) [{result}]");
			return result;
		}


		#endregion





	}
}
