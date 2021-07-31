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


		// Api
		public override string DisplayName => "Reveal & Snipe Pro";
		public override string Description => "Advanced strategy for Reveal&Snipe.";
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
				Task.Search => PerformTask_Search(),
				Task.Reveal => PerformTask_Reveal(),
				Task.Attack => PerformTask_Attack(),
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
				if (TileCount_RevealedShip + TileCount_HittedShip <= 2) {
					// Not to many tile exposed
					return TileCount_RevealedShip == 0 || CoracleCooldown > 0 ?
						Task.Search : Task.Attack;
				} else {
					// Many tile exposed
					if (TileCount_RevealedShip > 0) {
						// Has Revealed
						return Task.Attack;
					} else {
						// No Revealed
						return CoracleCooldown <= 2 ? Task.Reveal : Task.Attack;
					}
				}
			}
		}


		private AnalyseResult PerformTask_Search () {






			return new AnalyseResult() {
				ErrorMessage = "",

			};
		}


		private AnalyseResult PerformTask_Reveal () {





			return new AnalyseResult() {
				ErrorMessage = "",

			};
		}


		private AnalyseResult PerformTask_Attack () {





			return new AnalyseResult() {
				ErrorMessage = "",

			};
		}


		#endregion





	}
}
