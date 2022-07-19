using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class RevealAndSnipeAI : SoupAI {


		public override string DisplayName => "Reveal & Snipe (Easy)";
		public override string Description => "Reveal & Snipe AI created by Moenen.";
		public override string Fleet => "Coracle,Whale,KillerSquid,SeaTurtle";


		public override bool Perform (
			in eField ownField, int usingAbilityIndex,
			out Vector2Int attackPosition, out int abilityIndex, out Direction4 abilityDirection
		) {
			attackPosition = default;
			abilityIndex = -1;
			abilityDirection = default;








			return false;
		}


	}
}