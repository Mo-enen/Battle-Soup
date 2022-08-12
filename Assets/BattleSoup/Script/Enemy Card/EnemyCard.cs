using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class EnemyCard {


		// APi
		public Sprite Icon { get; set; } = null;
		public virtual int Wait { get; } = 0;

		// Data
		public int CurrentTurn { get; private set; } = 0;


		// MSG
		public virtual void Start () {
			CurrentTurn = Wait;
		}


		public virtual void End () {
			CurrentTurn = Wait;
		}


		public bool Turn (BattleSoup soup) {
			if (CurrentTurn <= 0) {
				Perform(soup);
				return true;
			} else {
				CurrentTurn--;
				return false;
			}
		}


		protected abstract void Perform (BattleSoup soup);


	}
}