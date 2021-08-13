using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace BattleSoupAI {
	public class BattleAttacker : SoupStrategy_Advanced {



		// Const
		private const string TASK_SEARCH = "S";
		private const string TASK_ATTACK = "A";
		private const string TASK_ATTACK_FOUNDED = "AF";
		private const int SAILBOAT_INDEX = 0;
		private const int SEAMONSTER_INDEX = 1;
		private const int LONGBOAT_INDEX = 2;
		private const int MINISUB_INDEX = 3;

		// Api
		public override string DisplayName => "Battle Attacker";
		public override string Description => "Standard Battle Attacker strategy created by Moenen.";
		public override string[] FleetID => new string[] { "Sailboat", "SeaMonster", "Longboat", "MiniSub", };
		protected int SailBoatCooldown => Cooldowns[SAILBOAT_INDEX];
		protected int SeaMonsterCooldown => Cooldowns[SEAMONSTER_INDEX];
		protected int LongBoatCooldown => Cooldowns[LONGBOAT_INDEX];
		protected int MiniSubCooldown => Cooldowns[MINISUB_INDEX];



		// API
		protected override string GetTask (BattleInfo ownInfo, BattleInfo info) {
			if (FoundShipCount > OpponentSunkShipCount) {
				// Ship Found
				return TASK_ATTACK_FOUNDED;
			} else if (UsingAbilityIndex >= 0) {
				// Longboat Attacking
				return TASK_ATTACK;
			} else if (ExposedShipCount == 0) {
				// Ships All Hidden
				return TASK_SEARCH;
			} else {
				// Has Ship Exposed
				return TASK_ATTACK;
			}
		}


		protected override AnalyseResult PerformTask (BattleInfo ownInfo, BattleInfo oppInfo, string taskID) => taskID switch {
			TASK_SEARCH => PerformTask_Search(oppInfo),
			TASK_ATTACK => PerformTask_Attack(oppInfo),
			TASK_ATTACK_FOUNDED => PerformTask_AttackFounded(ownInfo, oppInfo),
			_ => AnalyseResult.NotPerformed,
		};


		// LGC
		private AnalyseResult PerformTask_Search (BattleInfo info) {

			var result = AnalyseResult.None;

			// Search with Sea Monster
			if (SeaMonsterCooldown == 0 && TileCount_GeneralWater > (int)(info.MapSize * info.MapSize * 0.618f)) {
				result.TargetPosition = default;
				result.AbilityIndex = SEAMONSTER_INDEX;
				return result;
			}

			// Search with MiniSub
			if (MiniSubCooldown == 0) {

				var disCheckFilter = Tile.HittedShip | Tile.RevealedShip | Tile.SunkShip;
				bool success = false;

				if (ContainsTile(info.Tiles, disCheckFilter)) {
					float maxDis = float.MinValue;
					for (int i = 0; i < 4; i++) {
						var checkingPos = new Int2(
							i < 2 ? 0 : info.MapSize - 1,
							i % 2 == 0 ? 0 : info.MapSize - 1
						);
						if (
							CheckCorner(checkingPos.x, checkingPos.y) &&
							NearestPosition(info.Tiles, checkingPos.x, checkingPos.y, disCheckFilter, out _, out var checkedDis)
						) {
							if (checkedDis > maxDis) {
								maxDis = checkedDis;
								result.TargetPosition = checkingPos;
								success = true;
							}
						}
					}
				} else {
					var filter = Tile.GeneralWater | Tile.GeneralStone;
					result.TargetPosition =
						filter.HasFlag(info.Tiles[0, 0]) ? new Int2(0, 0) :
						filter.HasFlag(info.Tiles[info.MapSize - 1, 0]) ? new Int2(info.MapSize - 1, 0) :
						filter.HasFlag(info.Tiles[info.MapSize - 1, info.MapSize - 1]) ? new Int2(info.MapSize - 1, info.MapSize - 1) :
						filter.HasFlag(info.Tiles[0, info.MapSize - 1]) ? new Int2(0, info.MapSize - 1) :
						default;
					success = true;
				}
				if (success) {
					result.AbilityIndex = MINISUB_INDEX;
					return result;
				}
			}

			// Search with Normal Attack
			var bestHiddenPos = HiddenValueMax[info.Ships.Length].pos;
			if (
				info.Tiles[bestHiddenPos.x, bestHiddenPos.y] != Tile.GeneralWater &&
				!GetFirstTile(info.Tiles, Tile.GeneralWater | Tile.RevealedShip, out bestHiddenPos)
			) {
				bestHiddenPos = default;
			}
			result.TargetPosition = bestHiddenPos;
			result.AbilityIndex = -1;

			return result;
			// Func
			bool CheckCorner (int x, int y) =>
				(Tile.GeneralWater | Tile.GeneralStone | Tile.RevealedWater).HasFlag(
					info.Tiles[x, y]
				) && !info.Sonars.Any(pos => pos.x == x && pos.y == y);
		}


		private AnalyseResult PerformTask_Attack (BattleInfo info) {

			var result = AnalyseResult.None;
			bool mustUseLongboat = UsingAbilityIndex == LONGBOAT_INDEX;

			var bestHiddenPos = HiddenValueMax[info.Ships.Length].pos;
			if (
				info.Tiles[bestHiddenPos.x, bestHiddenPos.y] != Tile.GeneralWater &&
				!GetFirstTile(info.Tiles, Tile.GeneralWater | Tile.RevealedShip, out bestHiddenPos)
			) {
				bestHiddenPos = default;
			}

			if (mustUseLongboat || LongBoatCooldown == 0) {
				// Attack with Longboat
				if (
					TileCount_RevealedShip + TileCount_HittedShip > 0 &&
					GetMVT(info.Tiles, Tile.GeneralWater | Tile.RevealedShip, Tile.None, MVPConfig.Exposed, out var bestLongPos)
				) {
					result.AbilityIndex = LONGBOAT_INDEX;
					result.TargetPosition = bestLongPos;
					return result;
				}
			}

			// Longboat Failback
			if (mustUseLongboat) {
				result.AbilityIndex = LONGBOAT_INDEX;
				result.TargetPosition = bestHiddenPos;
				return result;
			}

			if (SailBoatCooldown == 0) {
				// Attack with Sailboat
				var filter = Tile.GeneralWater | Tile.GeneralStone | Tile.RevealedShip;
				if (GetMVT(
					info.Tiles, filter, filter, info.Ships[SAILBOAT_INDEX].Ability.Attacks,
					MVPConfig.Both, out var bestSailPos, out var bestDir
				)) {
					result.AbilityIndex = SAILBOAT_INDEX;
					result.TargetPosition = bestSailPos;
					result.AbilityDirection = bestDir;
					return result;
				}
			}

			// Attack with Normal Attack
			if (TryAttackShip(
				info,
				ShipWithMinimalPotentialPos,
				Tile.RevealedShip | Tile.GeneralWater,
				out var attPos
			)) {
				result.TargetPosition = attPos;
				result.AbilityIndex = -1;
				return result;
			}

			// Fallback Attack
			result.AbilityIndex = -1;
			result.TargetPosition = bestHiddenPos;
			return result;
		}


		private AnalyseResult PerformTask_AttackFounded (BattleInfo ownInfo, BattleInfo oppInfo) {

			var result = AnalyseResult.None;
			bool mustUseLongboat = UsingAbilityIndex == LONGBOAT_INDEX;

			// Attack with Longboat
			if (mustUseLongboat || LongBoatCooldown == 0) {
				for (int i = 0; i < ShipFoundPosition.Length; i++) {
					var kPos = ShipFoundPosition[i];
					if (!kPos.HasValue && oppInfo.ShipsAlive[i]) { continue; }
					if (GetFirstTileInShip(oppInfo.Tiles, Tile.RevealedShip | Tile.GeneralWater, oppInfo.Ships[i], kPos.Value, out var posResult)) {
						result.TargetPosition = posResult;
						result.AbilityIndex = LONGBOAT_INDEX;
						return result;
					}
				}
			}

			// Failback Longboat
			if (mustUseLongboat) {
				return PerformTask_Attack(oppInfo);
			}

			// Attack with Sailboat
			if (SailBoatCooldown == 0) {
				for (int i = 0; i < ShipFoundPosition.Length; i++) {
					var kPos = ShipFoundPosition[i];
					if (!kPos.HasValue && oppInfo.ShipsAlive[i]) { continue; }
					if (GetMVTInShip(oppInfo.Tiles, oppInfo.Ships[i], ownInfo.Ships[SAILBOAT_INDEX].Ability.Attacks, kPos.Value, out var resultPos, out var resultDir)) {
						result.TargetPosition = resultPos;
						result.AbilityDirection = resultDir;
						result.AbilityIndex = LONGBOAT_INDEX;
						return result;
					}
				}
			}

			// Normal Attack
			for (int i = 0; i < ShipFoundPosition.Length; i++) {
				var kPos = ShipFoundPosition[i];
				if (!kPos.HasValue && oppInfo.ShipsAlive[i]) { continue; }
				if (GetFirstTileInShip(oppInfo.Tiles, Tile.RevealedShip | Tile.GeneralWater, oppInfo.Ships[i], kPos.Value, out var posResult)) {
					result.TargetPosition = posResult;
					result.AbilityIndex = -1;
					return result;
				}
			}


			// Failback
			return PerformTask_Attack(oppInfo);
		}


	}
}
