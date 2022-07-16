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
		public bool HasSolidAction = false;
		public bool HasCopyOpponentAction = false;
		public bool HasCopySelfAction = false;


		// API
		public bool Perform (Ship ship, EntranceType entrance, in eField selfField, in eField opponentField) {

			// Get Start Line from Pool
			if (!EntrancePool.TryGetValue(entrance, out int startLine)) return false;

			// Get End Line
			int endLine = startLine + 1;
			for (int i = startLine + 1; i < Units.Length; i++) {
				if (Units[i] is EntranceUnit) break;
				endLine = i;
			}

			// Perform all Units
			bool result = false;
			for (int i = endLine; i >= startLine + 1; i--) {
				var unit = Units[i];
				if (unit is ActionUnit aUnit) {
					CellStep.AddToFirst(new sActionPerformer(
						aUnit, selfField, opponentField, ship
					));
					result = true;
				}
			}

			return HasSolidAction && result;
		}


	}
}