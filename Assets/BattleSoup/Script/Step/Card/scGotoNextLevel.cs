using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;
namespace BattleSoup {
	public class scGotoNextLevel : Step {
		private int Duration = 190;
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			var rt = soup.CardAssets.LevelNumberRoot;
			var prt = rt.parent as RectTransform;
			if (LocalFrame == 0) soup.FieldB.Enable = false;
			if (LocalFrame == Duration / 3) {
				soup.Card_GotoNextLevel();
				soup.FieldB.Enable = false;
			}
			if (LocalFrame < Duration * 0.333f) {
				rt.anchoredPosition = Vector2.LerpUnclamped(
					rt.anchoredPosition,
					new Vector2(
						-prt.rect.width / 2f + rt.rect.width / 2, 
						-prt.rect.height / 2f + rt.rect.height / 2
					),
					Time.deltaTime * 10f
				);
				rt.localScale = Vector3.Lerp(
					rt.localScale,
					Vector3.one * 2f,
					Time.deltaTime * 10f
				);
			} else if (LocalFrame > Duration * 0.666f) {
				rt.anchoredPosition = Vector2.LerpUnclamped(
					rt.anchoredPosition,
					Vector2.zero,
					Time.deltaTime * 10f
				);
				rt.localScale = Vector3.Lerp(
					rt.localScale,
					Vector3.one,
					Time.deltaTime * 10f
				);
			}
			if (LocalFrame > Duration) {
				rt.pivot = Vector2.one;
				rt.anchorMin = rt.anchorMax = Vector2.one;
				rt.anchoredPosition3D = Vector3.zero;
				rt.localScale = Vector3.one;
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}