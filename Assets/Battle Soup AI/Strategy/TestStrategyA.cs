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

			for (int i = 0; i < ownInfo.Cooldowns.Length; i++) {
				if (ownInfo.Cooldowns[i] <= 0) {
					result.AbilityIndex = i;
					result.AbilityDirection = AbilityDirection.Right;
					break;
				}
			}
			int mapSize = opponentInfo.Tiles.GetLength(0);
			var tiles = opponentInfo.Tiles;
			for (int j = 0; j < mapSize; j++) {
				for (int i = 0; i < mapSize; i++) {
					if (tiles[i, j] == Tile.GeneralWater || tiles[i, j] == Tile.RevealedShip) {
						result.TargetPosition = new Int2(i, j);
						j = mapSize;
						break;
					}
				}
			}

			return result;
		}


	}
}
