using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class BattleAttackerEasyAI : SoupAI {


		private const int SAILBOAT = 0;
		private const int SEAMONSTER = 1;
		private const int LONGBAOT = 2;
		private const int MINISUB = 3;

		public override string DisplayName => "Battle Attacker";
		public override string Description => "Battle Attacker AI created by Moenen.";
		public override string Fleet => "Sailboat,SeaMonster,Longboat,MiniSub";


		// MSG
		public override PerformResult Perform (int abilityIndex) {
			if (OpponentShips == null || OpponentShips.Count != 4) return null;
			return abilityIndex switch {
				-1 => FreeStart(),
				0 => Perform_SailBoat(OpponentShips[abilityIndex]),
				1 => Perform_SeaMonster(OpponentShips[abilityIndex]),
				2 => Perform_Loagboat(OpponentShips[abilityIndex]),
				3 => Perform_MiniSub(OpponentShips[abilityIndex]),
				_ => null,
			};
		}


		protected override PerformResult FreeStart () {
			
			// Check for Loadboat
			if (ShipIsReady(LONGBAOT)) {


			}

			// Check for Sailboat
			if (ShipIsReady(SAILBOAT)) {


			}

			// Check for MiniSub
			if (ShipIsReady(MINISUB)) {


			}

			// Check for SeaMon
			if (ShipIsReady(SEAMONSTER)) {


			}

			return new PerformResult(-1);
		}


		// Ships
		private PerformResult Perform_SailBoat (in Ship ship) {
			if (ship.GlobalName != "Sailboat") return null;
			if (ship.CurrentCooldown > 0) return null;
			if (!ship.Alive) return null;



			return new PerformResult(0);
		}


		private PerformResult Perform_SeaMonster (in Ship ship) {
			if (ship.GlobalName != "SeaMonster") return null;
			if (ship.CurrentCooldown > 0) return null;
			if (!ship.Alive) return null;
			return new PerformResult(1);
		}


		private PerformResult Perform_Loagboat (in Ship ship) {
			if (ship.GlobalName != "Longboat") return null;
			if (ship.CurrentCooldown > 0) return null;
			if (!ship.Alive) return null;



			return new PerformResult(2);
		}


		private PerformResult Perform_MiniSub (in Ship ship) {
			if (ship.GlobalName != "MiniSub") return null;
			if (ship.CurrentCooldown > 0) return null;
			if (!ship.Alive) return null;



			return new PerformResult(3);
		}


	}
}