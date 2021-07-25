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
		private (Int2 pos, int max, bool alone) ValueMax0 = default;
		private (Int2 pos, int max, bool alone) ValueMax1 = default;


		// API
		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, int usingAbilityIndex = -1) {

			var result = new AnalyseResult() {
				AbilityIndex = -1,
				ErrorMessage = "",
			};
			float highScore;

			// Calculate
			string msg = CalculateCache(opponentInfo);
			if (!string.IsNullOrEmpty(msg)) {
				result.ErrorMessage = msg;
				return result;
			}

			// Normal
			(result.TargetPosition, highScore) = AnalyseNormalAttack(opponentInfo);

			// Ability
			for (int aIndex = 0; aIndex < ownInfo.Ships.Length; aIndex++) {
				if (ownInfo.ShipsAlive[aIndex] && ownInfo.Cooldowns[aIndex] <= 0) {
					var (_pos, _dir, _score) = AnalyseAbility(aIndex, opponentInfo);
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


		// LGC
		private string CalculateCache (BattleInfo opponentInfo) {

			if (!CalculatePotentialPositions(
				opponentInfo.Ships,
				opponentInfo.ShipsAlive,
				opponentInfo.Tiles,
				opponentInfo.KnownPositions,
				ref HiddenPositions, ref ExposedPositions
			)) {
				return "Fail to calculate potential positions";
			}

			if (!CalculatePotentialValues(
				opponentInfo.Ships,
				opponentInfo.MapSize,
				HiddenPositions,
				ExposedPositions,
				ref Values, out _, out _
			)) {
				return "Fail to calculate potential values";
			}

			int shipCount = opponentInfo.Ships.Length;
			int mapSize = opponentInfo.MapSize;
			ValueMax0.alone = true;
			ValueMax1.alone = true;
			ValueMax0.max = 0;
			ValueMax1.max = 0;
			for (int j = 0; j < mapSize; j++) {
				for (int i = 0; i < mapSize; i++) {
					int v0 = Values[shipCount, i, j];
					if (v0 > ValueMax0.max) {
						ValueMax0.max = v0;
						ValueMax0.pos.x = i;
						ValueMax0.pos.y = j;
					} else if (v0 == ValueMax0.max) {
						ValueMax0.alone = false;
					}
					int v1 = Values[shipCount + 1, i, j];
					if (v1 > ValueMax1.max) {
						ValueMax1.max = v1;
						ValueMax1.pos.x = i;
						ValueMax1.pos.y = j;
					} else if (v1 == ValueMax1.max) {
						ValueMax1.alone = false;
					}
				}
			}

			return "";
		}


		private (Int2 pos, float score) AnalyseNormalAttack (BattleInfo opponentInfo) {
			Int2 maxPos = ValueMax0.alone ? ValueMax0.pos : ValueMax1.pos;





			return (default, 0f);
		}


		private (Int2 pos, AbilityDirection direction, float score) AnalyseAbility (
			int abilityIndex, BattleInfo opponentInfo
		) {





			return (default, default, 0f);
		}


	}
}
