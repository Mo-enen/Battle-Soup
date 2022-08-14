using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class EnemyCard {


		// APi
		public Sprite Icon { get; set; } = null;
		public virtual int Wait { get; } = 0;
		public abstract string Description { get; }
		public bool Performed { get; private set; } = false;

		// Data
		public int CurrentTurn { get; private set; } = 0;


		// MSG
		public virtual void Start () {
			CurrentTurn = Wait;
			Performed = false;
		}


		public bool Turn (BattleSoup soup) {
			if (CurrentTurn <= 0) {
				Perform(soup);
				Performed = true;
				return true;
			} else {
				CurrentTurn--;
				return false;
			}
		}


		protected abstract void Perform (BattleSoup soup);


	}
}