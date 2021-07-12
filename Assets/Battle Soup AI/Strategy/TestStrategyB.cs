using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class TestStrategyB : SoupStrategy {


		public override string DisplayName => "Test B";
		public override string Description => "Test Strategy B";


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, ShipPosition[] ownShipPositions, int usingAbilityIndex) {




			return default;
		}


	}
}
