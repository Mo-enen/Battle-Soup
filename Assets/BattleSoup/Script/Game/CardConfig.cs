using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BattleSoup {
	[CreateAssetMenu(fileName = "Card Config", menuName = "Card Config", order = 99)]
	public class CardConfig : ScriptableObject {

		public AnimationCurve Flip;
		public Sprite[] TypeIcons;


	}
}