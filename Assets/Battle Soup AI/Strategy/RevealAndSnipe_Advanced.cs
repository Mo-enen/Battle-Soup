using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class RevealAndSnipe_Advanced : SoupStrategy_Advanced {




		#region --- SUB ---


		private enum Task {
			Search = 0,
			Reveal = 1,
			Snipe = 2,
			Hit = 3,

		}


		#endregion




		#region --- VAR ---


		// Api
		public override string DisplayName => "Reveal & Snipe Pro";
		public override string Description => "Advanced strategy for Reveal&Snipe.";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };


		#endregion




		#region --- API ---


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo oppInfo, int usingAbilityIndex = -1) {

			var result = base.Analyse(ownInfo, oppInfo);
			if (!string.IsNullOrEmpty(result.ErrorMessage)) {
				return result;
			}

			// Task
			switch (GetTask()) {
				default:
				case Task.Search:
					PerformTask_Search();
					break;
				case Task.Reveal:
					PerformTask_Reveal();
					break;
				case Task.Snipe:
					PerformTask_Snipe();
					break;
				case Task.Hit:
					PerformTask_Hit();
					break;
			}

			return result;
		}


		#endregion




		#region --- LGC ---


		private Task GetTask () {
			var result = Task.Search;

			// Brave Analyse
			if (ExposedShipCount == 0) {
				// Ships All Hidden
				result = Task.Search;
			} else if (FoundShipCount == 0) {
				// Ship Exposed but Not Found
				if (TileCount_RevealedShip + TileCount_HittedShip <= 2) {
					// Not to many tile exposed
					if (TileCount_RevealedShip == 0) {
						result = Task.Search;
					} else {
						result = CoracleCooldown != 0 ? Task.Hit : Task.Snipe;
					}
				} else if (TileCount_RevealedShip == 0) {
					// Many tile exposed but no revealed
					result = Task.Reveal;
				} else {
					// Many tile exposed and has revealed
					result = CoracleCooldown != 0 ? Task.Hit : Task.Snipe;
				}
			} else {
				// Ship Found



			}

			// Failback Check





			return result;
		}


		private void PerformTask_Search () {




		}


		private void PerformTask_Reveal () {

		}


		private void PerformTask_Snipe () {

		}


		private void PerformTask_Hit () {

		}


		#endregion





	}
}
