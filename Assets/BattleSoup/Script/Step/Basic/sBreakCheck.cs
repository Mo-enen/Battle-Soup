using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sBreakCheck : Step {
		public ActionKeyword Keyword { get; private init; }
		public eField SeflField { get; private init; }
		public eField Field { get; private init; }
		public sBreakCheck (ActionKeyword keyword, eField field, eField selfField) {
			Keyword = keyword;
			Field = field;
			SeflField = selfField;
		}
		public override StepResult FrameUpdate (Game game) {
			if (Keyword.CheckBreak(Field.LastActionResult, SeflField.AliveShipCount)) {
				(game as BattleSoup).AbandonAbility();
			}
			return StepResult.Over;
		}
	}
}