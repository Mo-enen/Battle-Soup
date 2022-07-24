using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class BattleAttackerEasyAI : SoupAI {


		// VAR
		private const int LONGBAOT = 2;
		private const int SAILBOAT = 0;
		private const int MINISUB = 3;
		private const int SEAMONSTER = 1;

		public override string DisplayName => "Battle Attacker";
		public override string Description => "Battle Attacker AI created by Moenen.";
		public override string Fleet => "Sailboat,SeaMonster,Longboat,MiniSub";


		// API
		protected override bool RequireShip (int shipIndex) =>
			ShipIsReady(shipIndex) && shipIndex switch {

				// Require If Any Alive Ship have Any Hit Cell
				// Or Any Ship has Only One Posible Position
				LONGBAOT => HitCellCount > 0 || AllPositions.Any(ps => ps.Count == 1),

				// Always Require
				SAILBOAT => true,

				// Require If Any Corner Don't Have Sonar
				MINISUB => TrySonarInRandomCorner(out _),

				// Require If Enough Cell is Hittable
				SEAMONSTER => (float)HittableCellCount / CellCount > 0.618f,

				_ => false,
			};


		protected override PerformResult PerformShip (int shipIndex) {
			var pos = new Vector2Int();
			var dir = Direction4.Up;
			int size = OpponentMapSize;

			switch (shipIndex) {

				case LONGBAOT: {




					// Try Attack Best Place as Normal Attack
					pos = GetBestAttackPosition(false);

					break;
				}

				case SAILBOAT: {




					// Try Attack Best Place as Normal Attack
					pos = GetBestAttackPosition(false);

					break;
				}

				case MINISUB: {
					// Try Attack Corner
					if (TrySonarInRandomCorner(out pos)) break;
					// Try Attack Best Place as Normal Attack
					pos = GetBestAttackPosition(false);
					break;
				}

			}

			return new PerformResult(shipIndex) {
				Position = pos,
				Direction = dir
			};
		}


	}
}