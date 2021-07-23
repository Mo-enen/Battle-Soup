using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class Strategy_RevealAndSnipe : SoupStrategy {


		// Api
		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Knowledge is power! Find your enemy and take them down with precision.";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };

		// Data
		private List<ShipPosition>[] HiddenPositions = new List<ShipPosition>[0];
		private List<ShipPosition>[] ExposedPositions = new List<ShipPosition>[0];
		private int[,,] Values = new int[0, 0, 0];
		private int MinValue = 0;
		private int MaxValue = 0;


		// API
		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, int usingAbilityIndex = -1) {

			var result = new AnalyseResult() {
				AbilityIndex = -1,
				ErrorMessage = "",
			};
			float highScore;

			// Calculate
			if (!CalculatePotentialPositions(
				opponentInfo.Ships,
				opponentInfo.Tiles,
				opponentInfo.KnownPositions,
				ref HiddenPositions, ref ExposedPositions
			)) {
				result.ErrorMessage = "Fail to calculate potential positions";
				return result;
			}

			if (!CalculatePotentialValues(
				opponentInfo.Ships,
				opponentInfo.MapSize,
				HiddenPositions,
				ExposedPositions,
				ref Values, out MinValue, out MaxValue
			)) {
				result.ErrorMessage = "Fail to calculate potential values";
				return result;
			}

			// Normal
			(result.TargetPosition, highScore) = AnalyseNormalAttack(ownInfo, opponentInfo);

			// Ability
			for (int aIndex = 0; aIndex < ownInfo.Ships.Length; aIndex++) {
				if (ownInfo.ShipsAlive[aIndex] && ownInfo.Cooldowns[aIndex] <= 0) {
					var (_pos, _dir, _score) = AnalyseAbility(aIndex, ownInfo, opponentInfo);
					if (_score > highScore) {
						result.AbilityIndex = aIndex;
						result.TargetPosition = _pos;
						result.AbilityDirection = _dir;
						highScore = _score;
					}
				}
			}
			if (highScore <= 0f) {
				result.ErrorMessage = "Fail to analyse.";
			}

			return result;
		}


		private (Int2 pos, float score) AnalyseNormalAttack (
			BattleInfo ownInfo, BattleInfo opponentInfo
		) {





			return (default, 0f);
		}


		private (Int2 pos, AbilityDirection direction, float score) AnalyseAbility (
			int abilityIndex, BattleInfo ownInfo, BattleInfo opponentInfo
		) {





			return (default, default, 0f);
		}


	}
}
