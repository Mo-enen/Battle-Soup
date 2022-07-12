using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sActionPerformer : Step {


		// Data
		private readonly static HashSet<Vector2Int> c_RandomCache = new();
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
			var type = Action.Type;
			var soup = game as BattleSoup;
			switch (type) {


				case ActionType.Pick: {
					var field = OpponentField;
					var keyword = ActionKeyword.None;
					if (Action.KeywordCount > 0) {
						foreach (var k in Action.Keywords) {
							if (k.HasValue) keyword |= k.Value;
						}
						if (keyword.HasFlag(ActionKeyword.Self)) {
							field = SelfField;
						}
					}
					CellStep.AddToFirst(new sPick(field, Action, keyword));
					break;
				}


				case ActionType.Attack: {
					if (Action.RandomCount == 0) {
						// Normal
						TriggerAllOperations(soup, (field, pos) => {
							CellStep.AddToFirst(new sAttack(pos.x, pos.y, field, true));
						});
					} else {
						// Random
						TriggerRandom(soup, (field, pos) => {
							CellStep.AddToFirst(new sAttack(pos.x, pos.y, field, true));
						});
					}
					break;
				}
				case ActionType.Reveal:
					TriggerAllOperations(soup, (field, pos) => {
						CellStep.AddToFirst(new sReveal(pos.x, pos.y, field, true));
					});
					break;
				case ActionType.Unreveal:
					break;
				case ActionType.Sonar:
					break;
				case ActionType.Expand:
					break;
				case ActionType.Shrink:
					break;


				case ActionType.SunkShip:
					break;
				case ActionType.RevealShip:
					break;
				case ActionType.ExposeShip:
					break;


				case ActionType.AddCooldown:
					break;
				case ActionType.ReduceCooldown:
					break;
				case ActionType.AddMaxCooldown:
					break;
				case ActionType.ReduceMaxCooldown:
					break;


				case ActionType.PerformLastUsedAbility:
					break;

			}

			return StepResult.Over;
		}


		// LGC
		private void TriggerAllOperations (BattleSoup soup, System.Action<eField, Vector2Int> action) {
			for (int i = Action.PositionCount - 1; i >= 0; i--) {
				var position = Action.Positions[i];
				var pos = soup.GetPickedPosition(position.x, position.y);
				Action.TryGetKeyword(i, out var keyword);
				var field = keyword.HasFlag(ActionKeyword.Self) ? SelfField : OpponentField;
				action(field, pos);
			}
		}


		private void TriggerRandom (BattleSoup soup, System.Action<eField, Vector2Int> action) {
			c_RandomCache.Clear();
			if (Action.PositionCount == 0) {
				// All Map
				var keyword = ActionKeyword.None;
				for (int j = 0; j < Action.KeywordCount; j++) {
					if (Action.TryGetKeyword(j, out var k)) {
						keyword |= k;
					}
				}
				var field = keyword.HasFlag(ActionKeyword.Self) ? SelfField : OpponentField;
				// Random
				for (int i = 0; i < Action.RandomCount; i++) {
					int offsetX = Random.Range(0, field.MapSize);
					int offsetY = Random.Range(0, field.MapSize);
					for (int j = 0; j < field.MapSize * field.MapSize; j++) {
						var pos = new Vector2Int(
							(offsetX + (j % field.MapSize)) % field.MapSize,
							(offsetY + (j / field.MapSize)) % field.MapSize
						);
						if (c_RandomCache.Contains(pos)) continue;
						var cell = field[pos.x, pos.y];
						if (!keyword.Check(cell)) continue;
						c_RandomCache.Add(pos);
						action(field, pos);
						break;
					}
				}
			} else {
				for (int i = 0; i < Action.RandomCount; i++) {
					// Given Position
					int offset = Random.Range(0, Action.PositionCount);
					for (int j = 0; j < Action.PositionCount; j++) {
						int index = (offset + j) % Action.PositionCount;
						var position = Action.Positions[index];
						var field = OpponentField;
						Action.TryGetKeyword(index, out var keyword);
						if (keyword.HasFlag(ActionKeyword.Self)) {
							field = SelfField;
						}
						var pos = soup.GetPickedPosition(position.x, position.y);
						if (c_RandomCache.Contains(pos)) continue;
						if (pos.x < 0 || pos.y < 0 || pos.x >= field.MapSize || pos.y >= field.MapSize) continue;
						var cell = field[pos.x, pos.y];
						if (!keyword.Check(cell)) continue;
						c_RandomCache.Add(pos);
						action(field, pos);
						break;
					}
				}
			}

		}


	}
}