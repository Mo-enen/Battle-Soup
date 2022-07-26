using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class BattleAttackerAI : SoupAI {


		// VAR
		private const int LONGBAOT = 2;
		private const int SAILBOAT = 0;
		private const int MINISUB = 3;
		private const int SEAMONSTER = 1;

		public override string DisplayName => "Battle Attacker";
		public override string Description => "Battle Attacker AI created by Moenen.";
		public override string Fleet => "Sailboat,SeaMonster,Longboat,MiniSub";


		// API
		protected override int FreeStart () {

			// Longboat
			if (
				ShipIsReady(LONGBAOT) &&
				(HitCellCount > 0 || AllPositions.Any(ps => ps.Count == 1))
			) return LONGBAOT;

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

				case LONGBAOT: {
					// Try Attack Best Place as Normal Attack
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					// Failback
					pos = GetFirstValidHittablePosition();
					break;
				}

				case SAILBOAT: {
					// Try Best Position for Ability
					if (TryGetBestPosition_PureAttackAbility(ability, out pos, out dir)) break;
					// Try Attack Best Place as Normal Attack
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					// Failback
					pos = GetFirstValidHittablePosition();
					break;
				}

				case MINISUB: {
					// Try Attack Corner
					if (TrySonarInRandomCorner(out pos)) break;
					// Try Attack Best Place as Normal Attack
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					// Failback
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