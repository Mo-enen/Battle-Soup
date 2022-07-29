using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class FlexibleAttackerAI : SoupAI {


		// VAR
		private const int RAFT = 0;
		private const int GALLEON = 1;
		private const int BLUNDER = 2;
		private const int KAYAK = 3;

		public override string DisplayName => "Flexible Attacker";
		public override string Description => "Created by Moenen";
		public override string Fleet => "Raft,MediumGalleon,Blunderbuster,Kayak";


		// API
		protected override int FreeStart () {

			// Cata
			if (
				ShipIsReady(RAFT)
			) return RAFT;

			// Longboat
			if (
				ShipIsReady(GALLEON)
			) return GALLEON;

			// Blunderbuster
			if (
				ShipIsReady(BLUNDER) &&
				(!ShipIsReady(KAYAK) || SelfShips[BLUNDER].CurrentCooldown < 0)
			) return BLUNDER;

			// Conniving Cannoneer
			if (
				ShipIsReady(KAYAK)
			) return KAYAK;

			// Normal Attack
			return -1;
		}


		protected override PerformResult PerformShip (int shipIndex) {
			var pos = new Vector2Int();
			var dir = Direction4.Up;
			Soup.TryGetAbility(SelfShips[shipIndex].GlobalCode, out var ability);
			switch (shipIndex) {

				case RAFT: {
					if (TryGetBestPerformForPureAbility(ability, out pos, out dir)) break;
					if (TryGetBestPosition_NormalAttack(out pos)) break;
					pos = GetFirstValidHittablePosition();
					break;
				}

				case GALLEON: {
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

				case KAYAK: {
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