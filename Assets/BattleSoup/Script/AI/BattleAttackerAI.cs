using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class BattleAttackerAI : SoupAI {


		// VAR
		private const int LONGBOAT = 2;
		private const int SAILBOAT = 0;
		private const int MINISUB = 3;
		private const int SEAMONSTER = 1;

		public override string DisplayName => "Battle Attacker";
		public override string Description => "Created by Moenen";
		public override string Fleet => "Sailboat,SeaMonster,Longboat,MiniSub";


		// API
		protected override int FreeStart () {

			// Longboat
			if (
				ShipIsReady(LONGBOAT) &&
				(HitCellCount > 0 || AllPositions.Any(ps => ps.Count == 1))
			) return LONGBOAT;

			// Sailboat
			if (
				ShipIsReady(SAILBOAT)
			) return SAILBOAT;

			// Mini Sub
			if (
				ShipIsReady(MINISUB) &&
				LiveShipCount > 1 &&
				TrySonarInRandomCorner(out _)
			) return MINISUB;

			// Sea Monster
			if (
				ShipIsReady(SEAMONSTER) &&
				(float)HittableCellCount / CellCount > 0.618f
			) return SEAMONSTER;

			// Normal Attack
			return -1;
		}


		protected override PerformResult PerformShip (int shipIndex) {
			var pos = new Vector2Int();
			var dir = Direction4.Up;
			Soup.TryGetAbility(SelfShips[shipIndex].GlobalCode, out var ability);
			switch (shipIndex) {

				case LONGBOAT: {
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case SAILBOAT: {
					if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case MINISUB: {
					if (TrySonarInRandomCorner(out pos)) break;
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition(false);
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