using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class BattleAttackerEasyAI : SoupAI {


		public override string DisplayName => "Battle Attacker (Easy)";
		public override string Description => "Battle Attacker AI created by Moenen.";
		public override string Fleet => "Sailboat,SeaMonster,Longboat,MiniSub";


		public override bool Perform (
			in Field ownField, int usingAbilityIndex,
			out Vector2Int attackPosition, out int abilityIndex, out Direction4 abilityDirection
		) {
			attackPosition = default;
			abilityIndex = -1;
			abilityDirection = default;








			return false;
		}


	}
}