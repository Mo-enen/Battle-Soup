using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class BrutalAttackerAI : SoupAI {


		// VAR
		private const int CATAMARAN = 0;
		private const int LONGBAOT = 3;
		private const int BLUNDER = 2;
		private const int CANNONEER = 1;

		public override string DisplayName => "Brutal Attacker";
		public override string Description => "Created by Moenen";
		public override string Fleet => "Catamaran,ConnivingCannoneer,Blunderbuster,Longboat";


		// API
		protected override int FreeStart () {

			// Cata
			if (
				ShipIsReady(CATAMARAN)
			) return CATAMARAN;

			// Longboat
			if (
				ShipIsReady(LONGBAOT)
			) return LONGBAOT;

			// Blunderbuster
			if (
				ShipIsReady(BLUNDER) &&
				(!ShipIsReady(CANNONEER) || SelfShips[BLUNDER].CurrentCooldown < 0)
			) return BLUNDER;

			// Conniving Cannoneer
			if (
				ShipIsReady(CANNONEER)
			) return CANNONEER;

			// Normal Attack
			return -1;
		}


		protected override PerformResult PerformShip (int shipIndex) {
			var pos = new Vector2Int();
			var dir = Direction4.Up;
			Soup.TryGetAbility(SelfShips[shipIndex].GlobalCode, out var ability);
			switch (shipIndex) {

				case CATAMARAN: {
					if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case LONGBAOT: {
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case BLUNDER: {
					if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case CANNONEER: {
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