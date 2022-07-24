using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class RevealAndSnipeAI : SoupAI {


		public override string DisplayName => "Reveal & Snipe";
		public override string Description => "Reveal & Snipe AI created by Moenen.";
		public override string Fleet => "Coracle,Whale,KillerSquid,SeaTurtle";


		protected override PerformResult PerformShip (int shipIndex) {
			return default;
		}

		protected override bool RequireShip (int shipIndex) {
			return false;
		}


	}
}