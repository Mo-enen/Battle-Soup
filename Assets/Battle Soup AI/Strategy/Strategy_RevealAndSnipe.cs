using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class Strategy_RevealAndSnipe : SoupStrategy {


		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Knowledge is power! Find your enemy and take them down with precision.";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };
		

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
