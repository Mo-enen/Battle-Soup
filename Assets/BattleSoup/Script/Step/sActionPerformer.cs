using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sActionPerformer : Step {


		// Data
		private ActionUnit Action { get; init; }
		private eField SelfField { get; init; }
		private eField OpponentField { get; init; }


		// MSG
		public sActionPerformer (ActionUnit action, eField selfField, eField opponentField) {
			Action = action;
			SelfField = selfField;
			OpponentField = opponentField;
		}


		public override StepResult FrameUpdate (Game game) {
			




			return StepResult.Over;
		}


	}
}