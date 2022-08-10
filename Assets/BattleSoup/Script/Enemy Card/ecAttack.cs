using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;

namespace BattleSoup {
	public class ecAttackOne : ecAttack { protected override int Attack => 1; }
	public class ecAttackTwo : ecAttack { protected override int Attack => 2; }
	public class ecAttackThree : ecAttack {
		protected override int Attack => 3;
		public override int Wait => 1;
	}
	public class ecAttackFour : ecAttack {
		protected override int Attack => 4;
		public override int Wait => 2;
	}
	public abstract class ecAttack : EnemyCard {


		// Api
		protected abstract int Attack { get; }


		// MSG
		protected override void Perform (BattleSoup soup) {






		}


	}
}
