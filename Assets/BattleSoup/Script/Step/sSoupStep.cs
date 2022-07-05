using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class sSoupStep : Step {


		protected eField Field { get; private init; }
		protected int LocalX { get; private init; }
		protected int LocalY { get; private init; }
		protected bool Fast { get; private init; }
		protected bool UseAnimation { get; private set; } = false;


		protected sSoupStep (int x, int y, eField field, bool fast = false) {
			LocalX = x;
			LocalY = y;
			Field = field;
			Fast = fast;
		}


		public override StepResult FrameUpdate (Game game) {
			UseAnimation = (game as BattleSoup).UseAnimation;
			return StepResult.Continue;
		}


	}
}
