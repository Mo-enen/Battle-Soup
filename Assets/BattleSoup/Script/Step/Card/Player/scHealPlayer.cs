using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scHealPlayer : Step {
		public int Duration = 12;
		public int Heal = 1;
		public scHealPlayer (int heal, int duration = 12) {
			Duration = duration;
			Heal = heal;
		}
		public override StepResult FrameUpdate (Game game) {
			if (LocalFrame == 0) {
				(game as BattleSoup).Card_HealPlayer(Heal);
				AudioPlayer.PlaySound("Heal".AngeHash());
			}
			return LocalFrame < Duration ? StepResult.Continue : StepResult.Over;
		}
	}
}