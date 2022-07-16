using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class sSoupStep : Step {


		public eField Field { get; set; } = null;
		public int X { get; set; } = 0;
		public int Y { get; set; } = 0;
		public bool Fast { get; set; } = false;
		public Ship Ship { get; set; } = null;

		public bool UseAnimation { get; private set; } = false;


		public override StepResult FrameUpdate (Game game) {
			UseAnimation = (game as BattleSoup).UseAnimation;
			return StepResult.Continue;
		}



	}
}
