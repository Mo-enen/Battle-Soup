using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scDemonExplosion : Step {
		private int Duration = 190;
		public override StepResult FrameUpdate (Game game) {

			var soup = game as BattleSoup;
			if (LocalFrame == 0) {
				// Start
				soup.CardAssets.DemonExplosion.gameObject.SetActive(true);
				soup.CardAssets.EnemyAni.SetBool("Lose", true);
				soup.FieldB.Enable = false;
			}

			const float OFFSET_Y = -460f;
			if (LocalFrame < 80) {
				// First Part
				soup.CardAssets.DemonRoot.anchoredPosition = Vector2.LerpUnclamped(
					soup.CardAssets.DemonRoot.anchoredPosition,
					new Vector2(0f, OFFSET_Y),
					Time.deltaTime * 5f
				);
				soup.CardAssets.DemonRoot.localScale = Vector2.LerpUnclamped(
					soup.CardAssets.DemonRoot.localScale,
					Vector2.one * 1.5f,
					Time.deltaTime * 5f
				);
			} else {
				// Last Part
				soup.CardAssets.DemonRoot.anchoredPosition = Vector2.LerpUnclamped(
					soup.CardAssets.DemonRoot.anchoredPosition,
					new Vector2(0f, OFFSET_Y),
					Time.deltaTime * 2f
				);
				soup.CardAssets.DemonRoot.localScale = Vector2.LerpUnclamped(
					soup.CardAssets.DemonRoot.localScale,
					Vector2.zero,
					Time.deltaTime * 1.5f
				);
			}

			if (LocalFrame > Duration) {
				// End
				soup.CardAssets.DemonRoot.gameObject.SetActive(false);
				soup.CardAssets.DemonExplosion.gameObject.SetActive(false);
				soup.CardAssets.EnemyAni.SetBool("Lose", false);
				soup.CardAssets.DemonRoot.anchoredPosition = Vector2.zero;
				soup.CardAssets.DemonRoot.localScale = Vector3.one;
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}