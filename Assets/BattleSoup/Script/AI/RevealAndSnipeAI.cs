using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class RevealAndSnipeAI : SoupAI {


		// Const
		private const int CORACLE = 0;
		private const int WHALE = 1;
		private const int SQUID = 2;
		private const int TURTLE = 3;

		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Reveal & Snipe AI created by Moenen.";
		public override string Fleet => "Coracle,Whale,KillerSquid,SeaTurtle";


		// MSG
		protected override int FreeStart () {

			// Coracle
			if (
				ShipIsReady(CORACLE) && RevealedShipCellCount > 0
			) return CORACLE;

			// Whale
			if (
				ShipIsReady(WHALE) &&
				LiveShipCount > 1 &&
				RevealedShipCellCount == 0
			) return WHALE;

			// Squid
			if (
				ShipIsReady(SQUID) &&
				RevealedCellCount > 0
			) return SQUID;

			// Turtle
			if (
				ShipIsReady(TURTLE)
			) return TURTLE;

			// Normal Attack
			return -1;
		}


		protected override PerformResult PerformShip (int shipIndex) {

			var pos = new Vector2Int();
			var dir = Direction4.Up;
			Soup.TryGetAbility(SelfShips[shipIndex].GlobalCode, out var ability);

			switch (shipIndex) {

				case CORACLE: {
					if (TryGetBestRevealedShipCellToSunk(false, out pos)) break;
					pos = GetFirstValidRevealedShipPosition();
					break;
				}

				case WHALE: {
					if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case SQUID: {
					if (RevealedShipCellCount == 0) {
						if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
					} else {
						if (TryGetBestRevealedShipCellToSunk(true, out pos)) break;
					}
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case TURTLE: {
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
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