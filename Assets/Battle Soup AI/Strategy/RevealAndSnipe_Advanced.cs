using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class RevealAndSnipe_Advanced : SoupStrategy {




		#region --- SUB ---




		#endregion




		#region --- VAR ---


		// Api
		public override string DisplayName => "Reveal & Snipe Pro";
		public override string Description => "Advanced strategy for Reveal&Snipe.";
		public override string[] FleetID => new string[] { "Coracle", "Whale", "KillerSquid", "SeaTurtle", };

		// Data
		private int SniperCooldown = 0;
		private int WhaleCooldown = 0;
		private int SquidCooldown = 0;
		private int TurtleCooldown = 0;
		private int OpponentAliveShipCount = 0;
		private List<ShipPosition>[] HiddenPotentialPos = null;
		private List<ShipPosition>[] ExposedPotentialPos = null;


		#endregion




		#region --- API ---


		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, int usingAbilityIndex = -1) {

			// Check
			string msg = AvailableCheck(ownInfo);
			if (!string.IsNullOrEmpty(msg)) {
				return new AnalyseResult() { ErrorMessage = msg, };
			}

			// Cache
			FillCache(ownInfo, opponentInfo);





			return default;
		}


		#endregion




		#region --- LGC ---


		private string AvailableCheck (BattleInfo ownInfo) {
			if (ownInfo.Ships.Length != 4) {
				return "There must be 4 ships (Coracle, Whale, KillerSquid, SeaTurtle)";
			}
			if (ownInfo.Ships[0].GlobalID != "Coracle") {
				return "First ship must be Coracle";
			}
			if (ownInfo.Ships[1].GlobalID != "Whale") {
				return "Second ship must be Whale";
			}
			if (ownInfo.Ships[2].GlobalID != "KillerSquid") {
				return "Third ship must be KillerSquid";
			}
			if (ownInfo.Ships[3].GlobalID != "SeaTurtle") {
				return "Fourth ship must be SeaTurtle";
			}

			return "";
		}


		private void FillCache (BattleInfo ownInfo, BattleInfo oppInfo) {

			// Cooldown
			SniperCooldown = ownInfo.Cooldowns[0];
			WhaleCooldown = ownInfo.Cooldowns[1];
			SquidCooldown = ownInfo.Cooldowns[2];
			TurtleCooldown = ownInfo.Cooldowns[3];

			// Alive Ship Count
			OpponentAliveShipCount = 0;
			foreach (var alive in oppInfo.ShipsAlive) {
				if (alive) {
					OpponentAliveShipCount++;
				}
			}

			// Potential
			CalculatePotentialPositions(
				oppInfo,
				Tile.GeneralWater,
				Tile.GeneralWater,
				ref HiddenPotentialPos
			);
			CalculatePotentialPositions(
				oppInfo,
				Tile.HittedShip | Tile.RevealedShip,
				Tile.GeneralWater | Tile.HittedShip | Tile.RevealedShip,
				ref ExposedPotentialPos
			);
			RemoveImpossiblePositions(
				oppInfo, ref HiddenPotentialPos, ref ExposedPotentialPos
			);



		}


		#endregion





	}
}
