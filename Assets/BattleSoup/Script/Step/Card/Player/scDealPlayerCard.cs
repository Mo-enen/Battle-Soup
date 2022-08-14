using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scDealPlayerCard : Step {
		public int Duration = 12;
		public scDealPlayerCard (int duration = 12) {
			Duration = duration;
		}
		public override StepResult FrameUpdate (Game game) {
			if (LocalFrame == 0) {
				(game as BattleSoup).Card_DealForPlayer();
				AudioPlayer.PlaySound("Draw".AngeHash());
			}
			return LocalFrame < Duration ? StepResult.Continue : StepResult.Over;
		}
	}
}