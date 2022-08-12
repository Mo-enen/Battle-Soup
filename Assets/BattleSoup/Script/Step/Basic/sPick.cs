using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sPick : Step {


		// Api
		public eField TargetField { get; init; } = null;
		public eField SelfField { get; init; } = null;
		public ActionUnit Action { get; init; }
		public ActionKeyword Keyword { get; init; } = default;
		public Ship Ship { get; init; } = null;
		public bool Interactable { get; init; } = true;
		
		// Data
		private bool RequireAbandonAbility = false;
		private bool RequireCancelPick = false;


		// MSG
		public sPick (eField targetField, eField selfField, ActionUnit action, Ship ship, ActionKeyword keyword, bool interactable) {
			TargetField = targetField;
			SelfField = selfField;
			Keyword = keyword;
			Action = action;
			RequireAbandonAbility = false;
			Ship = ship;
			Interactable = interactable;
		}


		public override void OnStart (Game game) {
			base.OnStart(game);

			// Check Any Tile Can Pick
			bool success = false;
			for (int x = 0; x < TargetField.MapSize; x++) {
				for (int y = 0; y < TargetField.MapSize; y++) {
					var cell = TargetField[x, y];
					if (Keyword.Check(cell)) {
						success = true;
						goto Checked;
					}

				}
			}
			Checked:;
			if (!success) {
				RequireAbandonAbility = true;
				return;
			}

			// Pick Info
			var soup = game as BattleSoup;
			if (Action != null && soup.TryGetAbility(Action.AbilityID, out var ability)) {
				TargetField.SetPickingInfo(ability, Keyword, Action.LineIndex);
			} else {
				TargetField.SetPickingInfo(null, Keyword, 0);
			}
		}


		public override void OnEnd (Game game) {
			base.OnEnd(game);
			TargetField.ClearPickingInfo();
			if (RequireAbandonAbility) {
				(game as BattleSoup).AbandonAbility();
			}
		}


		public override StepResult FrameUpdate (Game game) {
			if (RequireAbandonAbility) return StepResult.Over;
			if (RequireCancelPick) return StepResult.Over;
			if (!Interactable) return StepResult.Continue;
			var soup = game as BattleSoup;
			// ESC
			if (FrameInput.CustomKeyDown(KeyCode.Escape)) {
				RequireAbandonAbility = true;
				return StepResult.Over;
			}
			// Rotate
			if (FrameInput.MouseRightDown) {
				soup.SwitchPickingDirection();
				return StepResult.Continue;
			}
			// Left
			if (!FrameInput.MouseLeftDown) {
				return StepResult.Continue;
			}
			var (localX, localY) = TargetField.Global_to_Local(
				FrameInput.MouseGlobalPosition.x,
				FrameInput.MouseGlobalPosition.y,
				1
			);
			if (localX < 0 || localY < 0 || localX >= TargetField.MapSize || localY >= TargetField.MapSize) {
				return StepResult.Continue;
			}
			var cell = TargetField[localX, localY];
			if (!Keyword.Check(cell)) {
				return StepResult.Continue;
			}
			soup.SetPickingPosition(new(localX, localY));
			return StepResult.Over;
		}


		public void CancelPick () => RequireCancelPick = true;


	}
}