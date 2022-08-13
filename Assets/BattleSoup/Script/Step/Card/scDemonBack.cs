using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class scDemonBack : Step {
		private int Duration = 80;
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			var asset = soup.CardAssets;
			if (LocalFrame == 0) {
				asset.DemonRoot.gameObject.SetActive(true);
				asset.DemonExplosion.gameObject.SetActive(false);
				asset.EnemyAni.SetBool("Lose", false);
				asset.DemonRoot.anchoredPosition = new Vector2(0f, 200f);
				AudioPlayer.PlaySound("Laugh".AngeHash(), 0.5f);
				asset.EnemyAni.SetTrigger("Perform");
			}

			asset.DemonRoot.anchoredPosition = Vector2.LerpUnclamped(
				asset.DemonRoot.anchoredPosition,
				Vector2.zero,
				Time.deltaTime * 2f
			);

			if (LocalFrame > Duration) {
				soup.FieldB.Enable = true;
				asset.DemonRoot.anchoredPosition = Vector2.zero;
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}