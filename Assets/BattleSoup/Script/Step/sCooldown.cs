using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sCooldown : sSoupStep {



		// Data
		public bool Add { get; set; }
		public bool ForMax { get; set; }


		// MSG
		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			if (X < 0 || Y < 0 || X >= Field.MapSize || Y >= Field.MapSize) return StepResult.Over;
			var cell = Field[X, Y];
			if (cell.ShipIndex < 0) return StepResult.Over;
			var ship = Field.Ships[cell.ShipIndex];
			if (ForMax) {
				ship.MaxCooldown += Add ? 1 : -1;
			} else {
				ship.CurrentCooldown = ship.CurrentCooldown.Clamp(0, ship.MaxCooldown);
				ship.CurrentCooldown += Add ? 1 : -1;
				if (Add && ship.CurrentCooldown <= 1) {
					ship.CurrentCooldown = 2;
				}
			}
			return StepResult.Over;
		}


	}
}
