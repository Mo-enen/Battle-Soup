using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sAttack : sSoupStep {



		// Data
		private bool ShowCrosshair = true;


		// MSG
		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			if (X < 0 || Y < 0 || X >= Field.MapSize || Y >= Field.MapSize) return StepResult.Over;
			int FALL_DURATION = Fast ? 6 : 12;
			int EXP_DURATION = Fast ? 4 : 8;
			int realFallDuration = ShowCrosshair ? FALL_DURATION : FALL_DURATION / 2;
			int realExpDuration = Field.HasShip(X, Y) ? EXP_DURATION : 0;
			if (!UseAnimation || LocalFrame == realFallDuration) {
				var result = Field.Attack(X, Y);
				var (x, y) = Field.Local_to_Global(X, Y, 1);
				eTag.SpawnTag(x, y, result);
				var cell = Field[X, Y];
				var hittedShip = cell.ShipIndex >= 0 ? Field.Ships[cell.ShipIndex] : null;
				// Call Back
				if (result == ActionResult.Hit) {
					InvokeCallback_Hit(hittedShip);
				}
				if (result == ActionResult.Sunk) {
					InvokeCallback_Sunk(hittedShip);
				}
			}
			if (UseAnimation) {
				if (LocalFrame < realFallDuration) {
					// Falling
					Field.DrawCrosshair(X, Y);
					return StepResult.Continue;
				} else if (LocalFrame < realFallDuration + realExpDuration) {
					// Explosion
					Field.DrawExplosion(X, Y, (float)(LocalFrame - realFallDuration) / realExpDuration);
					return StepResult.Continue;
				}
			}
			return StepResult.Over;
		}


		private void InvokeCallback_Hit (Ship hittedShip) {
			CellStep.AddToFirst(new sEntranceCallback() {
				Entrance = EntranceType.OnSelfShipGetHit,
				LocalX = X,
				LocalY = Y,
				Field = Field,
			});
			CellStep.AddToFirst(new sEntranceCallback() {
				Entrance = EntranceType.OnOpponentShipGetHit,
				LocalX = X,
				LocalY = Y,
				Field = Field,
			});
			if (hittedShip != null) {
				CellStep.AddToFirst(new sEntranceCallback() {
					Entrance = EntranceType.OnCurrentShipGetHit,
					LocalX = X,
					LocalY = Y,
					Field = Field,
					Ship = hittedShip,
				});
			}
		}


		private void InvokeCallback_Sunk (Ship hittedShip) {
			CellStep.AddToFirst(new sEntranceCallback() {
				Entrance = EntranceType.OnSelfShipGetSunk,
				LocalX = X,
				LocalY = Y,
				Field = Field,
			});
			CellStep.AddToFirst(new sEntranceCallback() {
				Entrance = EntranceType.OnOpponentShipGetSunk,
				LocalX = X,
				LocalY = Y,
				Field = Field,
			});
			if (hittedShip != null) {
				CellStep.AddToFirst(new sEntranceCallback() {
					Entrance = EntranceType.OnCurrentShipGetSunk,
					LocalX = X,
					LocalY = Y,
					Field = Field,
					Ship = hittedShip,
				});
			}
		}


	}
}