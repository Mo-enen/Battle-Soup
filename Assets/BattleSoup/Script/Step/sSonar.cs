using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sSonar : sSoupStep {


		private static readonly int SONAR_CODE = "Water Sonar Frame".AngeHash();


		public sSonar (int x, int y, eField field, bool fast = false) : base(x, y, field, fast) { }


		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			if (LocalX < 0 || LocalX >= Field.MapSize || LocalY < 0 || LocalY >= Field.MapSize) return StepResult.Over;
			// Has Ship
			if (Field[LocalX, LocalY].ShipIndex >= 0) {
				CellStep.AddToNext(new sAttack(LocalX, LocalY, Field, Fast, false));
				return StepResult.Over;
			}
			// No Ship
			Field.Sonar(LocalX, LocalY);
			int DURATION = Fast ? 6 : 24;
			if (UseAnimation && LocalFrame < DURATION) {
				if (LocalFrame % 4 < 2) {
					float t01 = (float)LocalFrame / DURATION;
					var (x, y) = Field.Local_to_Global(LocalX, LocalY, 0);
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