using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class scEnemyStun : Step {
		public int Stun = 1;
		public override StepResult FrameUpdate (Game game) {
			const int DURATION0 = 24;
			const int DURATION1 = 24;
			var soup = game as BattleSoup;
			var asset = soup.CardAssets;
			var hand = asset.EnemyStunHand;
			var targetRot = Quaternion.LookRotation(
				Vector3.forward,
				asset.HeroAvatar.transform.position - asset.EnemySlot_Performing.position
			);
			if (LocalFrame == 0) {
				hand.gameObject.SetActive(true);
				hand.transform.localRotation = Quaternion.identity;
				AudioPlayer.PlaySound("Sword".AngeHash());
			}
			if (LocalFrame < DURATION0) {
				// Step 0
				hand.transform.position = asset.EnemySlot_Performing.position;
				hand.transform.localRotation = Quaternion.LerpUnclamped(
					hand.transform.localRotation,
					targetRot,
					Time.deltaTime * 10f
				);
			} else if (LocalFrame - DURATION0 < DURATION1) {
				// Step 1
				if (LocalFrame == DURATION0) {
					AudioPlayer.PlaySound("Throw".AngeHash());
				}
				hand.transform.localRotation = targetRot;
				hand.transform.position = Vector3.LerpUnclamped(
					hand.transform.position,
					asset.HeroAvatar.transform.position,
					Time.deltaTime * 20f
				);
			} else {
				// End
				soup.Card_StunPlayer(Stun);
				hand.gameObject.SetActive(false);
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}