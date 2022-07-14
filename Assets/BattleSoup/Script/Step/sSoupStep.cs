using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class sSoupStep : Step {


		public eField Field { get; private init; }
		public int X { get; private init; }
		public int Y { get; private init; }
		public bool Fast { get; private init; }
		public bool UseAnimation { get; private set; } = false;
		public Ship Ship { get; private init; }


		protected sSoupStep (int x, int y, eField field, Ship ship, bool fast = false) {
			X = x;
			Y = y;
			Field = field;
			Fast = fast;
			Ship = ship;
		}


		public override StepResult FrameUpdate (Game game) {
			UseAnimation = (game as BattleSoup).UseAnimation;
			return StepResult.Continue;
		}



	}
}
