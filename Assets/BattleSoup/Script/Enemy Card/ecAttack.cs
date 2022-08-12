using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class ecAttackOne : ecAttack {
		public override int Attack => 1;
		public override int Wait => 1;
	}
	public class ecAttackTwo : ecAttack {
		public override int Attack => 2;
		public override int Wait => 1;
	}
	public class ecAttackThree : ecAttack {
		public override int Attack => 3;
		public override int Wait => 1;
	}
	public class ecAttackFour : ecAttack {
		public override int Attack => 4;
		public override int Wait => 2;
	}
	public abstract class ecAttack : EnemyCard {


		// Api
		public abstract int Attack { get; }


		// MSG
		protected override void Perform (BattleSoup soup) {






		}


	}
}
