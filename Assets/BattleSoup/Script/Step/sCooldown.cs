using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sCooldown : sSoupStep {



		// Data
		private bool Add { get; init; }
		private bool ForMax { get; init; }


		// MSG
		public sCooldown (int x, int y, eField field, Ship ship, bool add, bool forMax, bool fast = false) : base(x, y, field, ship, fast) {
			Add = add;
			ForMax = forMax;
		}


		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			if (X < 0 || Y < 0 || X >= Field.MapSize || Y >= Field.MapSize) return StepResult.Over;
			var cell = Field[X, Y];
			if (cell.ShipIndex < 0) return StepResult.Over;
			var ship = Field.Ships[cell.ShipIndex];
			if (ForMax) {
				ship.MaxCooldown += Add ? 1 : -1;
			} else {
				ship.CurrentCooldown += Add ? 1 : -1;
			}
			return StepResult.Over;
		}


	}
}
