using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class scRefreshFleetUI : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).Card_RefreshFleetUI();
			return StepResult.Over;
		}
	}
}