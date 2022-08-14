using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scShieldPlayer : Step {
		public int Duration = 12;
		public int Shield = 1;
		public scShieldPlayer (int shield, int duration = 12) {
			Duration = duration;
			Shield = shield;
		}
		public override StepResult FrameUpdate (Game game) {
			if (LocalFrame == 0) {
				(game as BattleSoup).Card_ShieldPlayer(Shield);
				AudioPlayer.PlaySound("Shield".AngeHash());
			}
			return LocalFrame < Duration ? StepResult.Continue : StepResult.Over;
		}
	}
}