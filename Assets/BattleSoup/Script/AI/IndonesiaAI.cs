using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class IndonesiaAI : SoupAI {


		// VAR
		public override string DisplayName => "Indonesia";
		public override string Description => "Created by Moenen";
		public override string Fleet => "SkullCove,SkullCove,SkullCove,SkullCove,SkullCove,SkullCove,SkullCove,SkullCove";


		// API
		protected override int FreeStart () => -1;


		protected override PerformResult PerformShip (int shipIndex) {
			var dir = Direction4.Up;
			if (!TryGetBestPosition_NormalAttack(out var pos)) {
				pos = GetFirstValidHittablePosition();
			}
			return new PerformResult(shipIndex) {
				Position = pos,
				Direction = dir
			};
		}


	}
}