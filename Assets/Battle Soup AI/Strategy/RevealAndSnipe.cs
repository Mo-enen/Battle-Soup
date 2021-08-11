﻿using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class RevealAndSnipe_Advanced : SoupStrategy_Advanced {




		#region --- VAR ---


		// Const
		private const string TASK_SEARCH = "S";
		private const string TASK_REVEAL = "R";
		private const string TASK_ATTACK = "A";
		private const int CORACLE_INDEX = 0;
		private const int WHALE_INDEX = 1;
		private const int SQUID_INDEX = 2;
		private const int TURTLE_INDEX = 3;

		// Api
		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Standard Reveal & Snipe strategy created by Moenen. Noob level, easy every time :)";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };
		protected int CoracleCooldown => Cooldowns[CORACLE_INDEX];
		protected int WhaleCooldown => Cooldowns[WHALE_INDEX];
		protected int SquidCooldown => Cooldowns[SQUID_INDEX];
		protected int TurtleCooldown => Cooldowns[TURTLE_INDEX];


		#endregion




		#region --- API ---


		protected override string GetTask (BattleInfo info) {
			if (ExposedShipCount == 0) {
				// Ships All Hidden
				return TASK_SEARCH;
			} else if (FoundShipCount > 0) {
				// Ship Found
				return TASK_ATTACK;
			} else {
				// Ship Exposed but Not Found
				if (TileCount_RevealedShip > 0) {
					// Has Revealed
					return TASK_ATTACK;
				} else {
					// No Revealed
					return CoracleCooldown <= 2 ? TASK_REVEAL : TASK_ATTACK;
				}
			}
		}


		protected override AnalyseResult PerformTask (BattleInfo oppInfo, string taskID) => taskID switch {
			TASK_SEARCH => PerformTask_Search(oppInfo),
			TASK_REVEAL => PerformTask_Reveal(oppInfo),
			TASK_ATTACK => PerformTask_Attack(oppInfo),
			_ => AnalyseResult.NotPerformed,
		};


		#endregion




		#region --- LGC ---


		private AnalyseResult PerformTask_Search (BattleInfo info) {

			var result = AnalyseResult.None;

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
				if (GetTileMVP(info.Tiles, Tile.All, Tile.All, MVPConfig.Hidden, out var bestWhalePos)) {
					result.TargetPosition = bestWhalePos;
					result.AbilityIndex = WHALE_INDEX;
				} else {
					result.TargetPosition = bestHiddenPos;
					result.AbilityIndex = -1;
				}
			} else if (SquidCooldown == 0 && TileCount_RevealedShip + TileCount_RevealedWater > 0) {
				// Use Squid
				if (GetTileMVP(info.Tiles, Tile.RevealedWater | Tile.RevealedShip, Tile.GeneralWater | Tile.RevealedShip, MVPConfig.Hidden, out var bestSquidPos)) {
					result.TargetPosition = bestSquidPos;
					result.AbilityIndex = SQUID_INDEX;
				} else {
					result.TargetPosition = bestHiddenPos;
					result.AbilityIndex = -1;
				}
			} else if (TurtleCooldown == 0) {
				// Use Turtle
				result.TargetPosition = bestHiddenPos;
				result.AbilityIndex = TURTLE_INDEX;
				////LogMessage?.Invoke($"Search/Turtle [{result}]");
			} else {
				// Use Normal Attack
				result.TargetPosition = bestHiddenPos;
				result.AbilityIndex = -1;
				////LogMessage?.Invoke($"Search/Normal [{result}]");
			}


			return result;
		}


		private AnalyseResult PerformTask_Reveal (BattleInfo info) {

			var result = AnalyseResult.None;

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
				if (GetTileMVP(info.Tiles, Tile.HittedShip, Tile.GeneralWater, MVPConfig.Hidden, out var bestWhalePos)) {
					result.TargetPosition = bestWhalePos;
					result.AbilityIndex = WHALE_INDEX;
				} else {
					return PerformTask_Attack(info);
				}
			} else if (SquidCooldown == 0 && TileCount_RevealedShip + TileCount_RevealedWater > 0) {
				// Use Squid
				if (GetTileMVP(info.Tiles, Tile.RevealedWater | Tile.RevealedShip, Tile.GeneralWater | Tile.RevealedShip, MVPConfig.Hidden, out var bestSquidPos)) {
					result.TargetPosition = bestSquidPos;
					result.AbilityIndex = SQUID_INDEX;
				} else {
					return PerformTask_Attack(info);
				}
			} else if (TurtleCooldown == 0) {
				// Use Turtle
				result.TargetPosition = bestHiddenPos;
				result.AbilityIndex = TURTLE_INDEX;
				//LogMessage?.Invoke($"Reveal/Turtle [{result}]");
			} else {
				// Perform Attack
				//LogMessage?.Invoke("Reveal not performed.");
				return PerformTask_Attack(info);
			}

			return result;
		}


		private AnalyseResult PerformTask_Attack (BattleInfo info) {

			var result = AnalyseResult.None;

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
				// Find Min Hit Slime Pos
				int minSlime = int.MaxValue;
				int minHit = int.MaxValue;
				Int2 minPos = default;
				for (int y = 0; y < info.MapSize; y++) {
					for (int x = 0; x < info.MapSize; x++) {
						if (info.Tiles[x, y] != Tile.RevealedShip) { continue; }
						int value = SlimeValues_HittedOnly[x, y];
						if (value <= 0) { continue; }
						int hitCount = CountNeighborTile(info.Tiles, x, y, Tile.HittedShip);
						if (value < minSlime || (value == minSlime && hitCount < minHit)) {
							minSlime = value;
							minHit = hitCount;
							minPos.x = x;
							minPos.y = y;
						}
					}
				}
				if (minSlime < int.MaxValue && (OpponentAliveShipCount <= 2 || minSlime <= 2)) {
					result.TargetPosition = minPos;
					result.AbilityIndex = CORACLE_INDEX;
					//LogMessage?.Invoke($"Attack/Snipe [{result}]");
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
					//LogMessage?.Invoke($"Attack/Turtle [{result}]");
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
				//LogMessage?.Invoke($"Attack/Normal [{result}]");
				return result;
			}

			// Search with Normal
			result.TargetPosition = bestHiddenPos;
			result.AbilityIndex = -1;
			//LogMessage?.Invoke($"Search/Normal(Attack Failed) [{result}]");
			return result;
		}


		#endregion





	}
}
