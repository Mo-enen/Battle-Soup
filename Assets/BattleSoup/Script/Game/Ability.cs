using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class Ability {


		// Api
		public ExecuteUnit[] Units = null;
		public readonly Dictionary<EntranceType, int> EntrancePool = new();
		public bool HasManuallyEntrance = false;
		public bool HasPassiveEntrance = false;


		// API
		public bool Perform (
			EntranceType entrance, in eField selfField, in eField opponentField
		) {

			// Get Start Line from Pool
			if (!EntrancePool.TryGetValue(entrance, out int startLine)) return false;

			bool result = false;

			// Perform all Units
			for (int i = startLine + 1; i < Units.Length; i++) {
				var unit = Units[i];
				switch (unit) {
					case ActionUnit aUnit:
						CellStep.AddToLast(new sActionPerformer(aUnit, selfField, opponentField));
						result = true;
						break;
					case EntranceUnit:
						// End Perform when Hit Another Entrance
						goto EndPerform;
				}
			}
			EndPerform:;

			return result;
		}


	}
}