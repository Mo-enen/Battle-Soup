using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class EnemyCard {


		// APi
		public Sprite Icon { get; set; } = null;
		public virtual int Wait { get; } = 0;
		public bool Performed { get; private set; } = false;

		// Data
		public int CurrentTurn { get; private set; } = 0;


		// MSG
		public virtual void Start () {
			CurrentTurn = Wait;
			Performed = false;
		}


		public void Turn (BattleSoup soup) {
			if (CurrentTurn <= 0) {
				Perform(soup);
				Performed = true;
			} else {
				CurrentTurn--;
			}
		}


		protected abstract void Perform (BattleSoup soup);


	}
}