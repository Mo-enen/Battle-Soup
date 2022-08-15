using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scDemonExplosion : Step {
		private int Duration = 196;
		private AudioSource Audio = null;
		public override StepResult FrameUpdate (Game game) {

			var soup = game as BattleSoup;
			var asset = soup.CardAssets;
			if (LocalFrame == 0) {
				// Start
				asset.DemonExplosion.gameObject.SetActive(true);
				asset.EnemyAni.SetBool("Lose", true);
				soup.FieldB.Enable = false;
				if (Audio == null) {
					Audio = asset.DemonExplosion.GetComponent<AudioSource>();
				}
				if (Audio != null && soup.UseSound) {
					Audio.Play();
					Audio.volume = 1f;
				}
			}

			if (LocalFrame == 80) {
				AudioPlayer.PlaySound("Fall".AngeHash());
			}

			const float OFFSET_Y = -460f;
			if (LocalFrame < 80) {
				// First Part
				asset.DemonRoot.anchoredPosition = Vector2.LerpUnclamped(
					asset.DemonRoot.anchoredPosition,
					new Vector2(0f, OFFSET_Y),
					Time.deltaTime * 5f
				);
				asset.DemonRoot.localScale = Vector2.LerpUnclamped(
					asset.DemonRoot.localScale,
					Vector2.one * 1.5f,
					Time.deltaTime * 5f
				);
			} else {
				// Last Part
				asset.DemonRoot.anchoredPosition = Vector2.LerpUnclamped(
					asset.DemonRoot.anchoredPosition,
					new Vector2(0f, OFFSET_Y),
					Time.deltaTime * 2f
				);
				asset.DemonRoot.localScale = Vector2.LerpUnclamped(
					asset.DemonRoot.localScale,
					Vector2.zero,
					Time.deltaTime * 1.5f
				);
				if (Audio != null) {
					Audio.volume = Mathf.Lerp(Audio.volume, 0f, Time.deltaTime);
				}
			}

			if (LocalFrame > Duration) {
				// End
				asset.DemonRoot.gameObject.SetActive(false);
				asset.DemonExplosion.gameObject.SetActive(false);
				asset.EnemyAni.SetBool("Lose", false);
				asset.DemonRoot.anchoredPosition = Vector2.zero;
				asset.DemonRoot.localScale = Vector3.one;
				if (Audio != null) {
					Audio.volume = 0f;
				}
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}