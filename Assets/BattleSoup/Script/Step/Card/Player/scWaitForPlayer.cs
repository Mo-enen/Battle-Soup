using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scWaitForPlayer : Step {
		private bool RequireStop = false;
		public override void OnStart (Game game) {
			base.OnStart(game);
			RequireStop = false;
		}
		public void StopWaiting () => RequireStop = true;
		public override StepResult FrameUpdate (Game game) => RequireStop ? StepResult.Over : StepResult.Continue;
	}
}