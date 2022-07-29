using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;
using System.Linq;

namespace BattleSoup {
	public class JackSparrowAI : SoupAI {


		// VAR
		private const int CORACLE = 0;
		private const int LONGBOAT = 3;
		private const int CRUSTACEAN = 1;

		public override string DisplayName => "Jack Sparrow";
		public override string Description => "AI Created by Moenen. Fleet from BlackPearl.";
		public override string Fleet => "Coracle,Crustacean,CraftyCrab,Longboat";


		// API
		protected override int FreeStart () {

			// Coracle
			if (
				ShipIsReady(CORACLE) && RevealedShipCellCount > 0
			) return CORACLE;

			// Longboat
			if (
				ShipIsReady(LONGBOAT) &&
				(HitCellCount > 0 || AllPositions.Any(ps => ps.Count == 1))
			) return LONGBOAT;

			// CRUSTACEAN
			if (
				ShipIsReady(CRUSTACEAN)
			) return CRUSTACEAN;

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

				case LONGBOAT: {
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case CRUSTACEAN: {
					if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
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