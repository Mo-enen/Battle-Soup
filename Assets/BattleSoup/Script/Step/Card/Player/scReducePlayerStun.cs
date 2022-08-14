using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
namespace BattleSoup {
	public class scReducePlayerStun : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).Card_ReducePlayerStun();
			return StepResult.Over;
		}
	}
}