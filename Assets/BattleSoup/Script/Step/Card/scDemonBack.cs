using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class scDemonBack : Step {
		private int Duration = 80;
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			if (LocalFrame == 0) {
				soup.CardAssets.DemonRoot.gameObject.SetActive(true);
				soup.CardAssets.DemonExplosion.gameObject.SetActive(false);
				soup.CardAssets.EnemyAni.SetBool("Lose", false);
				soup.CardAssets.DemonRoot.anchoredPosition = new Vector2(0f, 200f);
			}

			soup.CardAssets.DemonRoot.anchoredPosition = Vector2.LerpUnclamped(
				soup.CardAssets.DemonRoot.anchoredPosition,
				Vector2.zero,
				Time.deltaTime * 2f
			);

			if (LocalFrame > Duration) {
				soup.FieldB.Enable = true;
				soup.CardAssets.DemonRoot.anchoredPosition = Vector2.zero;
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}