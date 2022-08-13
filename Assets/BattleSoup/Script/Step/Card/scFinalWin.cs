using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;
namespace BattleSoup {
	public class scFinalWin : Step {
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			soup.Card_FinalWin();
			if (LocalFrame == 0) {
				AudioPlayer.PlaySound("Win".AngeHash());
			}
			if (LocalFrame > 60) {
				AudioPlayer.PlaySound("Clap".AngeHash());
			}
			return LocalFrame > 60 ? StepResult.Over : StepResult.Continue;
		}
	}
}