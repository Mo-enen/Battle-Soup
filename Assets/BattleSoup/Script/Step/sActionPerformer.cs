using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class sActionPerformer : Step {


		// Short
		private ActionResult LastActionResult => (SelfField.LastActionFrame > OpponentField.LastActionFrame ? SelfField : OpponentField).LastActionResult;

		// Data
		private readonly static HashSet<Vector2Int> c_RandomCache = new();
		private ActionUnit Action { get; init; }
		private eField SelfField { get; init; }
		private eField OpponentField { get; init; }
		private Ship CurrentShip { get; init; }


		// MSG
		public sActionPerformer (ActionUnit action, eField selfField, eField opponentField, Ship ship) {
			Action = action;
			SelfField = selfField;
			OpponentField = opponentField;
			CurrentShip = ship;
		}


		public override StepResult FrameUpdate (Game game) {
			var type = Action.Type;
			var soup = game as BattleSoup;

			switch (type) {

				case ActionType.Pick:
					Perform_Pick();
					break;

				case ActionType.Attack:
					Perform_Attack(soup);
					break;

				case ActionType.Reveal:
					Perform_Reveal(soup);
					break;
				case ActionType.Unreveal:
					Perform_Unreveal(soup);
					break;
				case ActionType.Sonar:
					Perform_Sonar(soup);
					break;
				case ActionType.Expand:
					Perform_ExpandOrShrink(soup, true);
					break;
				case ActionType.Shrink:
					Perform_ExpandOrShrink(soup, false);
					break;

				case ActionType.SunkShip:
					Perform_SunkShip(soup);
					break;
				case ActionType.RevealShip:
					Perform_RevealShip(soup);
					break;
				case ActionType.ExposeShip:
					Perform_ExposeShip(soup);
					break;

				case ActionType.AddCooldown:
					Perform_Cooldown(soup, true, false);
					break;
				case ActionType.ReduceCooldown:
					Perform_Cooldown(soup, false, false);
					break;
				case ActionType.AddMaxCooldown:
					Perform_Cooldown(soup, true, true);
					break;
				case ActionType.ReduceMaxCooldown:
					Perform_Cooldown(soup, false, true);
					break;

				case ActionType.PerformSelfLastUsedAbility:
					soup.UseAbility(SelfField.LastPerformedAbilityID, CurrentShip, SelfField);
					break;
				case ActionType.PerformOpponentLastUsedAbility:
					soup.UseAbility(OpponentField.LastPerformedAbilityID, CurrentShip, SelfField);
					break;

			}
			return StepResult.Over;
		}


		// LGC - Perform
		private void Perform_Pick () {
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
			CellStep.AddToFirst(new sPick(field, Action, CurrentShip, keyword));
		}


		private void Perform_Attack (BattleSoup soup) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sAttack() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_Reveal (BattleSoup soup) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sReveal() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_Unreveal (BattleSoup soup) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sReveal() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_Sonar (BattleSoup soup) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sSonar() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_SunkShip (BattleSoup soup) {
			TriggerForShip(soup, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sAttack() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_RevealShip (BattleSoup soup) {
			TriggerForShip(soup, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sReveal() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_ExposeShip (BattleSoup soup) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sExpose() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Ship = CurrentShip,
				});
			});
		}


		private void Perform_ExpandOrShrink (BattleSoup soup, bool expand) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				if (!pos.InLength(field.MapSize)) return;
				var cell = field[pos.x, pos.y];
				bool revealed = cell.State != CellState.Normal;
				var done = new HashSet<Vector2Int>();
				var queue = new Queue<Vector2Int>();
				var shrinkList = expand ? null : new List<Vector2Int>();
				queue.Enqueue(pos);
				done.Add(pos);
				while (queue.Count > 0) {
					var _pos = queue.Dequeue();
					var _cell = field[_pos.x, _pos.y];
					bool _revealed = _cell.State != CellState.Normal;
					if (revealed == _revealed) {
						// Expand
						var p = _pos;
						p.x = _pos.x - 1;
						if (p.InLength(field.MapSize) && !done.Contains(p)) {
							done.Add(p);
							queue.Enqueue(p);
						}
						p.x = _pos.x + 1;
						if (p.InLength(field.MapSize) && !done.Contains(p)) {
							done.Add(p);
							queue.Enqueue(p);
						}
						p.x = _pos.x;
						p.y = _pos.y - 1;
						if (p.InLength(field.MapSize) && !done.Contains(p)) {
							done.Add(p);
							queue.Enqueue(p);
						}
						p.y = _pos.y + 1;
						if (p.InLength(field.MapSize) && !done.Contains(p)) {
							done.Add(p);
							queue.Enqueue(p);
						}
					} else {
						// Perform
						if (expand) {
							// Expand
							if (revealed) {
								CellStep.AddToFirst(new sBreakCheck(keyword, field));
								CellStep.AddToFirst(new sReveal() {
									X = _pos.x,
									Y = _pos.y,
									Field = field,
									Fast = true,
									Ship = CurrentShip,
								});
							} else {
								CellStep.AddToFirst(new sBreakCheck(keyword, field));
								CellStep.AddToFirst(new sUnreveal() {
									X = _pos.x,
									Y = _pos.y,
									Field = field,
									Fast = true,
									Ship = CurrentShip,
								});
							}
						} else {
							// Shrink
							shrinkList.Add(_pos);
						}
					}
				}
				// Do the Shrink
				if (!expand) {
					foreach (var _pos in shrinkList) {
						var p = _pos;
						p.x = _pos.x - 1;
						if (p.InLength(field.MapSize) && done.Contains(p)) {
							var _cell = field[p.x, p.y];
							bool _reveal = _cell.State != CellState.Normal;
							if (_reveal == revealed) {
								if (revealed) {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sUnreveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								} else {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sReveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								}
							}
						}
						p.x = _pos.x + 1;
						if (p.InLength(field.MapSize) && done.Contains(p)) {
							var _cell = field[p.x, p.y];
							bool _reveal = _cell.State != CellState.Normal;
							if (_reveal == revealed) {
								if (revealed) {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sUnreveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								} else {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sReveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								}
							}
						}
						p.x = _pos.x;
						p.y = _pos.y - 1;
						if (p.InLength(field.MapSize) && done.Contains(p)) {
							var _cell = field[p.x, p.y];
							bool _reveal = _cell.State != CellState.Normal;
							if (_reveal == revealed) {
								if (revealed) {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sUnreveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								} else {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sReveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								}
							}
						}
						p.y = _pos.y + 1;
						if (p.InLength(field.MapSize) && done.Contains(p)) {
							var _cell = field[p.x, p.y];
							bool _reveal = _cell.State != CellState.Normal;
							if (_reveal == revealed) {
								if (revealed) {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sUnreveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								} else {
									CellStep.AddToFirst(new sBreakCheck(keyword, field));
									CellStep.AddToFirst(new sReveal() {
										X = p.x,
										Y = p.y,
										Field = field,
										Fast = true,
										Ship = CurrentShip,
									});
								}
							}
						}
					}
				}
			});
		}


		private void Perform_Cooldown (BattleSoup soup, bool add, bool max) {
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				CellStep.AddToFirst(new sBreakCheck(keyword, field));
				CellStep.AddToFirst(new sCooldown() {
					X = pos.x,
					Y = pos.y,
					Field = field,
					Fast = true,
					Add = add,
					ForMax = max,
					Ship = CurrentShip,
				});

			});
		}


		// LGC
		private void TriggerForShip (BattleSoup soup, System.Action<eField, Vector2Int, ActionKeyword> action) =>
			Trigger(soup, Action.RandomCount, (field, pos, keyword) => {
				if (!pos.InLength(field.MapSize)) return;
				var cell = field[pos.x, pos.y];
				if (cell.ShipIndex < 0) return;
				if (!keyword.Check(cell)) return;
				var ship = field.Ships[cell.ShipIndex];
				for (int i = 0; i < ship.BodyNodes.Length; i++) {
					var node = ship.GetFieldNodePosition(i);
					if (!node.InLength(field.MapSize)) continue;
					action(field, node, keyword);
				}
			});


		private void Trigger (BattleSoup soup, int randomCount, System.Action<eField, Vector2Int, ActionKeyword> action) {
			if (randomCount == 0) {
				TriggerAllOperations(soup, action);
			} else {
				TriggerRandom(soup, action);
			}
		}


		private void TriggerAllOperations (BattleSoup soup, System.Action<eField, Vector2Int, ActionKeyword> action) {
			for (int i = Action.PositionCount - 1; i >= 0; i--) {
				var position = Action.Positions[i];
				Vector2Int pos;
				eField field;
				Action.TryGetKeyword(i, out var keyword);

				// Field
				if (!keyword.HasFlag(ActionKeyword.This)) {
					// Normal
					field = keyword.HasFlag(ActionKeyword.Self) ? SelfField : OpponentField;
				} else {
					// This
					field = keyword.HasFlag(ActionKeyword.Opponent) ? OpponentField : SelfField;
				}

				// Check Trigger
				if (!keyword.CheckTrigger(LastActionResult)) return;

				// Pos
				if (!keyword.HasFlag(ActionKeyword.This)) {
					// Normal
					pos = soup.GetPickedPosition(position.x, position.y);
				} else {
					// This
					pos = new Vector2Int(CurrentShip.FieldX, CurrentShip.FieldY) + position;
					if (CurrentShip.BodyNodes.Length > 0) {
						var node = CurrentShip.GetFieldNodePosition(0);
						pos = new Vector2Int(node.x, node.y) + position;
					}
				}

				// Perform Action
				action(field, pos, keyword);
			}
		}


		private void TriggerRandom (BattleSoup soup, System.Action<eField, Vector2Int, ActionKeyword> action) {
			c_RandomCache.Clear();
			if (Action.PositionCount == 0) {
				// All Map
				var keyword = ActionKeyword.None;
				for (int j = 0; j < Action.KeywordCount; j++) {
					if (Action.TryGetKeyword(j, out var k)) {
						keyword |= k;
					}
				}
				// Field
				if (!keyword.HasFlag(ActionKeyword.This)) {

					// Normal
					var field = keyword.HasFlag(ActionKeyword.Self) ? SelfField : OpponentField;

					// Check Trigger
					if (!keyword.CheckTrigger(LastActionResult)) return;

					// Random for All Map
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
							action(field, pos, keyword);
							break;
						}
					}
				} else {
					// This
					var field = keyword.HasFlag(ActionKeyword.Opponent) ? OpponentField : SelfField;
					// Check Trigger
					if (!keyword.CheckTrigger(LastActionResult)) return;
					// Random Inside This Ship
					for (int i = 0; i < Action.RandomCount; i++) {
						int indexOffset = Random.Range(0, CurrentShip.BodyNodes.Length);
						for (int j = 0; j < CurrentShip.BodyNodes.Length; j++) {
							int index = (j + indexOffset) % CurrentShip.BodyNodes.Length;
							var pos = CurrentShip.GetFieldNodePosition(index);
							if (pos.x < 0 || pos.y < 0 || pos.x >= field.MapSize || pos.y >= field.MapSize) continue;
							if (c_RandomCache.Contains(pos)) continue;
							var cell = field[pos.x, pos.y];
							if (!keyword.Check(cell)) continue;
							c_RandomCache.Add(pos);
							action(field, pos, keyword);
							break;
						}
					}
				}
			} else {
				for (int i = 0; i < Action.RandomCount; i++) {
					// Given Position
					int offset = Random.Range(0, Action.PositionCount);
					for (int j = 0; j < Action.PositionCount; j++) {

						int index = (offset + j) % Action.PositionCount;
						var position = Action.Positions[index];
						Action.TryGetKeyword(index, out var keyword);

						// Field
						eField field;
						Vector2Int pos;
						if (!keyword.HasFlag(ActionKeyword.This)) {
							// Normal
							field = keyword.HasFlag(ActionKeyword.Self) ? SelfField : OpponentField;
							pos = soup.GetPickedPosition(position.x, position.y);
						} else {
							// This
							field = keyword.HasFlag(ActionKeyword.Opponent) ? OpponentField : SelfField;
							pos = new Vector2Int(CurrentShip.FieldX, CurrentShip.FieldY) + position;
						}

						// Check Trigger
						if (!keyword.CheckTrigger(LastActionResult)) return;

						// Final
						if (c_RandomCache.Contains(pos)) continue;
						if (pos.x < 0 || pos.y < 0 || pos.x >= field.MapSize || pos.y >= field.MapSize) continue;
						var cell = field[pos.x, pos.y];
						if (!keyword.Check(cell)) continue;
						c_RandomCache.Add(pos);
						action(field, pos, keyword);
						break;
					}
				}
			}

		}


	}
}