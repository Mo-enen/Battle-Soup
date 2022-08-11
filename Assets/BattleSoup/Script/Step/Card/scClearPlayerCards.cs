using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class scClearPlayerCards : Step {
		public override StepResult FrameUpdate (Game game) {
			var soup = game as BattleSoup;
			soup.Card_ClearPlayerCards(soup.CardAssets.PlayerSlot_Performing);
			soup.Card_ClearPlayerCards(soup.CardAssets.PlayerSlot_Dock);
			return StepResult.Over;
		}
	}
}
