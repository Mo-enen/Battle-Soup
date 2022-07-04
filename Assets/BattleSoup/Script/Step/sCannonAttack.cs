using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sCannonAttack : Step {



		// Data
		private int LocalX = 0;
		private int LocalY = 0;
		private eField Field = null;


		// MSG
		public sCannonAttack (int x, int y, eField field) {
			LocalX = x;
			LocalY = y;
			Field = field;
		}


		public override StepResult FrameUpdate (Game game) {
			const int FALL_DURATION = 24;
			const int EXP_DURATION = 8;
			var soup = game as BattleSoup;
			if (!soup.NoAnimation) {
				if (LocalFrame < FALL_DURATION) {
					// Falling
					Field.DrawCannonBall(LocalX, LocalY, (float)LocalFrame / FALL_DURATION);
					return StepResult.Continue;
				} else if (LocalFrame < FALL_DURATION + EXP_DURATION) {
					// Explosion
					Field.DrawExplosion(LocalX, LocalY, (float)(LocalFrame - FALL_DURATION) / EXP_DURATION);
					return StepResult.Continue;
				}
			}
			// Over


			return StepResult.Over;
		}


	}
}