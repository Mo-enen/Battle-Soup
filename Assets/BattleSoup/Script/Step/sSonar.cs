using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sSonar : sSoupStep {


		private static readonly int SONAR_CODE = "Water Sonar Frame".AngeHash();


		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			if (X < 0 || X >= Field.MapSize || Y < 0 || Y >= Field.MapSize) return StepResult.Over;
			if (LocalFrame == 0) {
				// Has Ship
				if (Field[X, Y].ShipIndex >= 0) {
					CellStep.AddToFirst(new sAttack() {
						X = X,
						Y = Y,
						Field = Field,
						Fast = Fast,
						Ship = Ship,
					});
					return StepResult.Over;
				}
				// No Ship
				Field.Sonar(X, Y);
			}
			int DURATION = Fast ? 6 : 24;
			if (UseAnimation && LocalFrame < DURATION) {
				if (LocalFrame % 4 < 2) {
					float t01 = (float)LocalFrame / DURATION;
					var (x, y) = Field.Local_to_Global(X, Y, 0);
					CellRenderer.Draw(
						SONAR_CODE,
						x, y + (int)(t01 * SoupConst.ISO_SIZE / 2),
						SoupConst.ISO_SIZE, SoupConst.ISO_SIZE,
						new Color32(255, 255, 255, (byte)Util.Remap(0f, 1f, 255, 0, t01))
					);
				}
				return StepResult.Continue;
			}
			return StepResult.Over;
		}


	}
}