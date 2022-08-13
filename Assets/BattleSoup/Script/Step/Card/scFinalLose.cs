using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scFinalLose : Step {
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			soup.Card_FinalLose();
			return StepResult.Over;
		}
	}
}