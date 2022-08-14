using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class ecStunOne : ecStun {
		public override int Stun => 1;
		public override int Wait => 0;
		public override string Description => "Stun ¡Á 1";
	}
	public class ecStunTwo : ecStun {
		public override int Stun => 2;
		public override int Wait => 0;
		public override string Description => "Stun ¡Á 2";
	}
	public abstract class ecStun : EnemyCard {


		// Api
		public abstract int Stun { get; }


		// MSG
		protected override void Perform (BattleSoup soup) {
			CellStep.AddToFirst(new scEnemyStun() { Stun = Stun });
		}


	}
}