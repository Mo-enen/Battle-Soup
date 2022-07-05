using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sAttack : sSoupStep {



		private bool ShowCrosshair = true;

		// MSG
		public sAttack (int x, int y, eField field, bool fast = false, bool showCrosshair = true) : base(x, y, field, fast) {
			ShowCrosshair = showCrosshair;
		}


		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			int FALL_DURATION = Fast ? 12 : 24;
			int EXP_DURATION = Fast ? 4 : 8;
			int realFallDuration = ShowCrosshair ? FALL_DURATION : FALL_DURATION / 2;
			int realExpDuration = Field.HasShip(LocalX, LocalY) ? EXP_DURATION : 0;
			if (LocalFrame == realFallDuration) {
				Field.Attack(LocalX, LocalY);
			}
			if (UseAnimation) {
				if (LocalFrame < realFallDuration) {
					// Falling
					Field.DrawCannonBall(LocalX, LocalY, (float)(LocalFrame + (ShowCrosshair ? 0f : 0.5f)) / FALL_DURATION);
					return StepResult.Continue;
				} else if (LocalFrame < realFallDuration + realExpDuration) {
					// Explosion
					Field.DrawExplosion(LocalX, LocalY, (float)(LocalFrame - realFallDuration) / realExpDuration);
					return StepResult.Continue;
				}
			}
			return StepResult.Over;
		}


	}
}