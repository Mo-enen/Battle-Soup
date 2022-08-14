using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class scEnemyAttack : Step {
		public int Attack = 1;
		public override StepResult FrameUpdate (Game game) {
			const int DURATION0 = 24;
			const int DURATION1 = 24;
			var soup = game as BattleSoup;
			var asset = soup.CardAssets;
			var sword = asset.EnemySword;
			var targetRot = Quaternion.LookRotation(
				Vector3.forward,
				asset.PlayerHp.transform.position - asset.EnemySlot_Performing.position
			);
			if (LocalFrame == 0) {
				sword.gameObject.SetActive(true);
				sword.transform.localRotation = Quaternion.identity;
				AudioPlayer.PlaySound("Sword".AngeHash());
			}
			if (LocalFrame < DURATION0) {
				// Step 0
				sword.transform.position = asset.EnemySlot_Performing.position;
				sword.transform.localRotation = Quaternion.LerpUnclamped(
					sword.transform.localRotation,
					targetRot,
					Time.deltaTime * 10f
				);
			} else if (LocalFrame - DURATION0 < DURATION1) {
				// Step 1
				if (LocalFrame == DURATION0) {
					AudioPlayer.PlaySound("Throw".AngeHash());
				}
				sword.transform.localRotation = targetRot;
				sword.transform.position = Vector3.LerpUnclamped(
					sword.transform.position,
					asset.PlayerHp.transform.position,
					Time.deltaTime * 20f
				);
			} else {
				// End
				soup.Card_DamagePlayer(Attack);
				sword.gameObject.SetActive(false);
				return StepResult.Over;
			}
			return StepResult.Continue;
		}
	}
}