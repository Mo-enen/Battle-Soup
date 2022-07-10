using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sReveal : sSoupStep {



		// Const
		private static readonly int FRAME_CODE = "Water Reveal Frame".AngeHash();


		// MSG
		public sReveal (int x, int y, eField field, bool fast = false) : base(x, y, field, fast) { }


		public override StepResult FrameUpdate (Game game) {
			base.FrameUpdate(game);
			int DURATION = Fast ? 6 : 24;
			var result = Field.Reveal(X, Y);
			var (_x, _y) = Field.Local_to_Global(X, Y, 1);
			eTag.SpawnTag(_x, _y, result);
			if (UseAnimation && LocalFrame < DURATION) {
				if (LocalFrame % 4 < 2) {
					float t01 = (float)LocalFrame / DURATION;
					var (x, y) = Field.Local_to_Global(X, Y, 0);
					CellRenderer.Draw(
						FRAME_CODE,
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