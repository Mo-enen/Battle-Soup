using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sBreakCheck : Step {
		public ActionKeyword Keyword { get; private init; }
		public eField Field { get; private init; }
		public sBreakCheck (ActionKeyword keyword, eField field) {
			Keyword = keyword;
			Field = field;
		}
		public override StepResult FrameUpdate (Game game) {
			if (Keyword.CheckBreak(Field.LastActionResult)) {
				(game as BattleSoup).AbandonAbility();
			}
			return StepResult.Over;
		}
	}
}