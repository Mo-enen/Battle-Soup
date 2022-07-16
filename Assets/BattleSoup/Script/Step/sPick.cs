using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sPick : Step {


		// Api
		public eField Field { get; init; } = null;
		public ActionUnit Action { get; init; }
		public ActionKeyword Keyword { get; init; } = default;
		public Ship Ship { get; init; } = null;

		// Data
		private bool RequireQuit = false;


		// MSG
		public sPick (eField field, ActionUnit action, Ship ship, ActionKeyword keyword) {
			Field = field;
			Keyword = keyword;
			Action = action;
			RequireQuit = false;
			Ship = ship;
		}


		public override void OnStart (Game game) {
			base.OnStart(game);

			// Check Any Tile Can Pick
			bool success = false;
			for (int x = 0; x < Field.MapSize; x++) {
				for (int y = 0; y < Field.MapSize; y++) {
					var cell = Field[x, y];
					if (Keyword.Check(cell)) {
						success = true;
						goto Checked;
					}

				}
			}
			Checked:;
			if (!success) {
				RequireQuit = true;
				return;
			}

			// Pick Info
			var soup = game as BattleSoup;
			if (soup.TryGetAbility(Action.AbilityID, out var ability)) {
				Field.SetPickingInfo(ability, Keyword, Action.LineIndex);
			}
		}


		public override void OnEnd (Game game) {
			base.OnEnd(game);
			Field.ClearPickingInfo();
			if (RequireQuit) {
				(game as BattleSoup).AbandonAbility();
			}
		}


		public override StepResult FrameUpdate (Game game) {
			if (RequireQuit) return StepResult.Over;
			var soup = game as BattleSoup;
			// ESC
			if (FrameInput.CustomKeyDown(KeyCode.Escape)) {
				RequireQuit = true;
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
			var (localX, localY) = Field.Global_to_Local(
				FrameInput.MouseGlobalPosition.x,
				FrameInput.MouseGlobalPosition.y,
				1
			);
			if (localX < 0 || localY < 0 || localX >= Field.MapSize || localY >= Field.MapSize) {
				return StepResult.Continue;
			}
			var cell = Field[localX, localY];
			if (!Keyword.Check(cell)) {
				return StepResult.Continue;
			}
			soup.SetPickingPosition(new(localX, localY));
			return StepResult.Over;
		}


	}
}