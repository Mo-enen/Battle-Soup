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
		protected override int FreeStart () {


			return -1;



			if (
				ShipIsReady(LONGBAOT) &&
				(HitCellCount > 0 || AllPositions.Any(ps => ps.Count == 1))
			) return LONGBAOT;


			if (
				ShipIsReady(SAILBOAT)
			) return SAILBOAT;


			if (
				ShipIsReady(MINISUB) &&
				TrySonarInRandomCorner(out _)
			) return MINISUB;


			if (
				ShipIsReady(SEAMONSTER) &&
				(float)HittableCellCount / CellCount > 0.618f
			) return SEAMONSTER;


			return -1;
		}


		protected override PerformResult PerformShip (int shipIndex) {
			var pos = new Vector2Int();
			var dir = Direction4.Up;
			Soup.TryGetAbility(SelfShips[shipIndex].GlobalCode, out var ability);
			switch (shipIndex) {

				case LONGBAOT: {
					// Try Best Position for Ability
					if (TryGetBestPosition_ComboAttack(out pos)) break;
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