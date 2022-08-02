using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sEntranceCallback : Step {


		// Api
		public EntranceType Entrance;
		public eField Field = null;
		public Ship Ship = null;
		public int LocalX = 0;
		public int LocalY = 0;


		// MSG
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			var otherField = Field == soup.FieldA ? soup.FieldB : soup.FieldA;
			switch (Entrance) {

				case EntranceType.OnSelfGetAttack:
					TryInvokeForEntrance(soup, Field, otherField);
					break;
				case EntranceType.OnOpponentGetAttack:
					TryInvokeForEntrance(soup, otherField, Field);
					break;

				case EntranceType.OnSelfShipGetHit:
					TryInvokeForEntrance(soup, Field, otherField);
					break;
				case EntranceType.OnOpponentShipGetHit:
					TryInvokeForEntrance(soup, otherField, Field);
					break;
				case EntranceType.OnCurrentShipGetHit:
					TryInvokeForEntrance(soup, Field, otherField, Ship);
					break;

				case EntranceType.OnSelfShipGetSunk:
					TryInvokeForEntrance(soup, Field, otherField);
					break;
				case EntranceType.OnOpponentShipGetSunk:
					TryInvokeForEntrance(soup, otherField, Field);
					break;
				case EntranceType.OnCurrentShipGetSunk:
					TryInvokeForEntrance(soup, Field, otherField, Ship);
					break;
			}

			return StepResult.Over;
		}


		private void TryInvokeForEntrance (BattleSoup soup, eField selfField, eField opponentField, Ship targetShip = null) {
			foreach (var ship in selfField.Ships) {
				if (targetShip != null && ship != targetShip) continue;
				if (!soup.TryGetAbility(ship.GlobalCode, out var ability)) continue;
				if (!ability.EntrancePool.ContainsKey(Entrance)) continue;
				soup.PerformAbility(ability, ship, Entrance, selfField, opponentField, false);
			}
		}


	}
}