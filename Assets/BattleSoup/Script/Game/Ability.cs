using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BattleSoup {
	public class Ability {


		// Api
		public ExecuteUnit[] Units = null;
		public readonly Dictionary<EntranceType, int> EntrancePool = new();


		// API
		public void Perform (EntranceType entrance, ActionResult performingKeyword) {

			// Get Start Line from Pool
			if (!EntrancePool.TryGetValue(entrance, out int startLine)) return;

			// Check Limitation for Entrance
			if (
				Units[startLine] is not EntranceUnit entranceUnit ||
				!entranceUnit.Keyword.HasFlag(performingKeyword)
			) return;

			// Perform all Units
			for (int i = startLine + 1; i < Units.Length; i++) {
				var unit = Units[i];
				switch (unit) {
					case ActionUnit aUnit:
						PerformAction(aUnit, out bool _continue);
						if (!_continue) goto EndPerform;
						break;
					case EntranceUnit:
						// End Perform when Hit Another Entrance
						goto EndPerform;
				}
			}
			EndPerform:;

		}


		// LGC
		private void PerformAction (ActionUnit unit, out bool _continue) {
			_continue = true;
			// Add Step



		}


	}
}