using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class TestStrategyA : SoupStrategy {


		public override string DisplayName => "Test A";
		public override string Description => "Test Strategy A";


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, ShipPosition[] ownShipPositions, int usingAbilityIndex = -1) {
			var result = new AnalyseResult() {
				AbilityIndex = usingAbilityIndex,
				ErrorMessage = "",
			};

			float highScore = 0f;
			if (usingAbilityIndex < 0) {
				(result.TargetPosition, highScore) = AnalyseNormalAttack(
					ownInfo, opponentInfo, ownShipPositions
				);
			}
			for (int i = 0; i < ownInfo.Ships.Length; i++) {
				if (ownInfo.ShipsAlive[i] && ownInfo.Cooldowns[i] <= 0) {
					var (_pos, _dir, _score) = AnalyseAbility(
						i, ownInfo, opponentInfo, ownShipPositions
					);
					if (_score > highScore) {
						result.TargetPosition = _pos;
						result.AbilityDirection = _dir;
					}
				}
			}

			//for (int i = 0; i < ownInfo.Ships.Length; i++) {
			//	if (ownInfo.ShipsAlive[i] && ownInfo.Cooldowns[i] <= 0) {
			//		result.AbilityIndex = i;
			//		result.AbilityDirection = AbilityDirection.Right;
			//		break;
			//	}
			//}
			//int mapSize = opponentInfo.Tiles.GetLength(0);
			//var tiles = opponentInfo.Tiles;
			//for (int j = 0; j < mapSize; j++) {
			//	for (int i = 0; i < mapSize; i++) {
			//		if (tiles[i, j] == Tile.GeneralWater || tiles[i, j] == Tile.RevealedShip) {
			//			result.TargetPosition = new Int2(i, j);
			//			j = mapSize;
			//			break;
			//		}
			//	}
			//}
			return result;
		}


		private (Int2 pos, float score) AnalyseNormalAttack (
			BattleInfo ownInfo, BattleInfo opponentInfo, ShipPosition[] ownShipPositions
		) {





			return (default, 0f);
		}


		private (Int2 pos, AbilityDirection direction, float score) AnalyseAbility (
			int abilityIndex, BattleInfo ownInfo, BattleInfo opponentInfo, ShipPosition[] ownShipPositions
		) {





			return (default, default, 0f);
		}


	}
}
