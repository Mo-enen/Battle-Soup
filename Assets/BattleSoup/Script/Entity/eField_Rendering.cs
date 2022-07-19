using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	// === Rendering ===
	public partial class eField {




		#region --- SUB ---


		private struct RenderingCell {
			public int WaterOffsetY;
		}


		private class PickingCell {
			public int LocalX;
			public int LocalY;
			public Color32 Tint;
		}


		#endregion




		#region --- VAR ---


		// Const
		private static readonly int WATER_CODE = "Water".AngeHash();
		private static readonly int WATER_REVEALED_CODE = "Water Revealed".AngeHash();
		private static readonly int WATER_HIT_CODE = "Water Hit".AngeHash();
		private static readonly int WATER_SUNK_CODE = "Water Sunk".AngeHash();
		private static readonly int STONE_CODE = "Stone".AngeHash();
		private static readonly int WATER_HIGHLIGHT_CODE = "Water Highlight".AngeHash();
		private static readonly int WATER_REVEAL_SHIP_CODE = "Water Reveal Ship".AngeHash();
		private static readonly int WATER_REVEAL_FULL_CODE = "Water Reveal Fullsize".AngeHash();
		private static readonly int CANNON_CODE = "CannonBall".AngeHash();
		private static readonly int CROSSHAIR_CODE = "Crosshair".AngeHash();
		private static readonly int[] SONAR_CODES = new int[] { "Sonar Unknown".AngeHash(), "Sonar 1".AngeHash(), "Sonar 2".AngeHash(), "Sonar 3".AngeHash(), "Sonar 4".AngeHash(), "Sonar 5".AngeHash(), "Sonar 6".AngeHash(), "Sonar 7".AngeHash(), "Sonar 8".AngeHash(), "Sonar 9".AngeHash(), "Sonar 9Plus".AngeHash(), };
		private static readonly int EXPLOSION_CODE_0 = "Explosion 0".AngeHash();
		private static readonly int EXPLOSION_CODE_1 = "Explosion 1".AngeHash();
		private static readonly int ISO_PIXEL_CODE = "ISO Pixel".AngeHash();
		private static readonly int ARROW_CODE = "Arrow".AngeHash();

		// Data
		private readonly List<PickingCell> PickingLocalPositions = new();
		private Direction4 PickingDirection = Direction4.Up;
		private ActionKeyword PickingKeyword = ActionKeyword.None;
		private RenderingCell[,] RenderCells = null;
		private int HoveringShipIndex = -1;
		private int DraggingShipIndex = -1;
		private Vector2Int DraggingShipLocalOffset = default;


		#endregion




		#region --- MSG ---


		private void UpdateRenderCells () {

			// Render Cells
			if (
				RenderCells == null ||
				RenderCells.GetLength(0) != MapSize ||
				RenderCells.GetLength(1) != MapSize
			) RenderCells = new RenderingCell[MapSize, MapSize];

			var (localMouseX, localMouseY) = Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			for (int i = 0; i < MapSize; i++) {
				for (int j = 0; j < MapSize; j++) {
					ref var cell = ref RenderCells[i, j];
					// Water Offset Y
					if (localMouseX == i && localMouseY == j) {
						// Highlight
						cell.WaterOffsetY = SoupConst.ISO_HEIGHT / 3;
					} else {
						// Back to Normal
						if (cell.WaterOffsetY != 0) cell.WaterOffsetY = cell.WaterOffsetY * 7 / 12;
					}
				}
			}

		}


		// Draw
		private void DrawWaters () {
			var (localMouseX, localMouseY) = Global_to_Local(
				FrameInput.MouseGlobalPosition.x,
				FrameInput.MouseGlobalPosition.y,
				1
			);
			var hoveringLocalPosition = new Vector2Int(localMouseX, localMouseY);
			int count = MapSize * MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = IsoArray[i];
				var (x, y) = Local_to_Global(localPos.x, localPos.y);
				var cell = Cells[localPos.x, localPos.y];
				var rCell = RenderCells[localPos.x, localPos.y];
				bool hovering = AllowHoveringOnWater && HoveringShipIndex < 0 && localPos == hoveringLocalPosition;
				int id = hovering && cell.State == CellState.Normal ?
					WATER_HIGHLIGHT_CODE :
					cell.State switch {
						CellState.Revealed => cell.HasStone ? WATER_REVEAL_FULL_CODE : WATER_REVEALED_CODE,
						CellState.Hit => WATER_HIT_CODE,
						CellState.Sunk => WATER_SUNK_CODE,
						_ => WATER_CODE,
					};
				CellRenderer.Draw(
					id, x, y + (hovering ? rCell.WaterOffsetY : 0),
					SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
				);
				if (cell.State == CellState.Revealed && cell.ShipIndex >= 0) {
					CellRenderer.Draw(
						WATER_REVEAL_SHIP_CODE, x, y + (hovering ? rCell.WaterOffsetY : 0),
						SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
					);
				}
			}
		}


		private void DrawUnits () {

			int count = MapSize * MapSize;
			int hoveringShipIndex = -1;
			int mouseX = FrameInput.MouseGlobalPosition.x;
			int mouseY = FrameInput.MouseGlobalPosition.y;
			var (localMouseX, localMouseY) = Global_to_Local(
				FrameInput.MouseGlobalPosition.x,
				FrameInput.MouseGlobalPosition.y,
				1
			);

			// Draw Units
			for (int unitIndex = count - 1; unitIndex >= 0; unitIndex--) {

				var localPos = IsoArray[unitIndex];
				var (x, y) = Local_to_Global(localPos.x, localPos.y, 1);
				var cell = Cells[localPos.x, localPos.y];

				// Stone
				if (cell.HasStone && CellRenderer.TryGetSpriteFromGroup(STONE_CODE, unitIndex, out var spStone)) {
					CellRenderer.Draw(
						spStone.GlobalID,
						new RectInt(
							x, y,
							SoupConst.ISO_SIZE,
							SoupConst.ISO_SIZE
						).Shrink(24),
						cell.State == CellState.Normal ? Const.WHITE : new Color32(200, 200, 200, 255)
					);
				}

				// Draw All Ships in Cell
				if (ShowShips) {
					for (int i = 0; i < cell.ShipIndexs.Count; i++) {
						int rID = cell.ShipRenderIDs[i];
						int rID_add = cell.ShipRenderIDsAdd[i];
						int rID_sunk = cell.ShipRenderIDsSunk[i];
						int shipIndex = cell.ShipIndexs[i];
						var ship = Ships[shipIndex];
						if (
							shipIndex >= 0 &&
							(!HideInvisibleShip || ship.Visible || !ship.Alive)
						) {

							var tint = ship.Valid ? cell.State switch {
								CellState.Hit => new Color32(209, 165, 31, 255),
								_ => new Color32(255, 255, 255, 255),
							} : new Color32(255, 16, 16, 255);
							if (ship.Visible && HideInvisibleShip && ship.Alive) {
								tint.a = 128;
							}
							bool sunk = cell.State == CellState.Sunk;
							int shipID =
								HoveringShipIndex == shipIndex ? rID_add :
								ship.Alive ? rID : rID_sunk;

							if (CellRenderer.TryGetSprite(shipID, out var spShip)) {
								bool flip = ship.Flip;
								ref var rCell = ref CellRenderer.Draw(
									shipID,
									flip ? x + SoupConst.ISO_SIZE : x,
									y + (!sunk ? GetWaveOffsetY(x, y) : 0),
									flip ? -SoupConst.ISO_SIZE : SoupConst.ISO_SIZE,
									SoupConst.ISO_SIZE * spShip.GlobalHeight / spShip.GlobalWidth,
									tint
								);
								bool localMouseChecked = localMouseX >= localPos.x && localMouseY >= localPos.y;
								int xMin = rCell.Width > 0 ? rCell.X : rCell.X + rCell.Width;
								int xMax = rCell.Width > 0 ? rCell.X + rCell.Width : rCell.X;
								if (
									AllowHoveringOnShip && localMouseChecked && !sunk &&
									mouseX > xMin &&
									mouseX < xMax &&
									mouseY > rCell.Y &&
									mouseY < rCell.Y + rCell.Height
								) {
									hoveringShipIndex = shipIndex;
								}
							}
						}
					}
				}
			}
			HoveringShipIndex = hoveringShipIndex;
		}


		private void DrawGizmos () {
			var (localMouseX, localMouseY) = Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			bool mouseInMap = localMouseX >= 0 && localMouseX < MapSize && localMouseY >= 0 && localMouseY < MapSize;
			int count = MapSize * MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = IsoArray[i];
				var (x, y) = Local_to_Global(localPos.x, localPos.y);
				var cell = Cells[localPos.x, localPos.y];
				// Sonar Number
				if (cell.Sonar != 0) {
					CellRenderer.Draw(
						SONAR_CODES[cell.Sonar.Clamp(0, SONAR_CODES.Length - 1)],
						x, y + (cell.HasStone ? SoupConst.ISO_SIZE / 8 : 0),
						SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
					);
				}
			}
			// Picking Zone
			if (PickingLocalPositions.Count > 0) {
				for (int i = count - 1; i >= 0; i--) {
					var localPos = IsoArray[i];
					var cell = Cells[localPos.x, localPos.y];
					if (!PickingKeyword.Check(cell)) {
						var (x, y) = Local_to_Global(localPos.x, localPos.y, 0);
						CellRenderer.Draw(
							ISO_PIXEL_CODE,
							x, y, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE,
							new Color32(255, 0, 0, 128)
						);
					}
				}
			}
			// Picking Crosshair
			if (mouseInMap) {
				var mouseLocalPos = new Vector2Int(localMouseX, localMouseY);
				foreach (var cell in PickingLocalPositions) {
					var pickedPos = SoupUtil.GetPickedPosition(mouseLocalPos, PickingDirection, cell.LocalX, cell.LocalY);
					if (pickedPos.NotInLength(MapSize)) continue;
					var (_x, _y) = Local_to_Global(pickedPos.x, pickedPos.y, 0);
					CellRenderer.Draw(
						CROSSHAIR_CODE,
						new RectInt(_x, _y, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE).Shrink(12),
						cell.Tint
					);
				}
			}
			// Sonar "?"
			if (mouseInMap) {
				var cell = Cells[localMouseX, localMouseY];
				if (cell.Sonar > 0) {
					for (int i = -cell.Sonar; i <= cell.Sonar; i++) {
						int x = localMouseX + i;
						if (x < 0 || x >= MapSize) continue;
						int y0 = localMouseY + (cell.Sonar - Mathf.Abs(i));
						if (y0 >= 0 && y0 < MapSize) {
							var _cell = Cells[x, y0];
							if (_cell.State == CellState.Normal && !_cell.HasStone) {
								var (gx, gy) = Local_to_Global(x, y0, 0);
								CellRenderer.Draw(
									SONAR_CODES[0], gx, gy, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
								);
							}
						}
						int y1 = localMouseY - (cell.Sonar - Mathf.Abs(i));
						if (y0 != y1 && y1 >= 0 && y1 < MapSize) {
							var _cell = Cells[x, y1];
							if (_cell.State == CellState.Normal && !_cell.HasStone) {
								var (gx, gy) = Local_to_Global(x, y1, 0);
								CellRenderer.Draw(
									SONAR_CODES[0], gx, gy, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
								);
							}
						}
					}
				}
			}
		}


		private void Update_AbilityPerformingArrow () {
			if (CellStep.CurrentStep is not sPick pick || pick.TargetField != this) return;
			var (mX, mY) = Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			if (!new Vector2Int(mX, mY).InLength(MapSize)) return;
			var otherField = this == Soup.FieldA ? Soup.FieldB : Soup.FieldA;
			var ship = pick.Ship;
			var shipPos = new Vector2Int(ship.FieldX, ship.FieldY);
			if (ship.BodyNodes.Length > 0) shipPos = ship.GetFieldNodePosition(0);
			var (startX, startY) = otherField.Local_to_Global(shipPos.x, shipPos.y, 1);
			startX += SoupConst.ISO_WIDTH;
			startY += SoupConst.ISO_HEIGHT;
			var (endX, endY) = Local_to_Global(mX, mY, 1);
			endX += SoupConst.ISO_WIDTH;
			endY += SoupConst.ISO_HEIGHT;
			int rot = (int)Vector2.Angle(Vector2.up, new Vector2(endX - startX, endY - startY));
			CellRenderer.Draw_9Slice(
				ARROW_CODE,
				startX, startY,
				500, 0, rot,
				SoupConst.ISO_SIZE * 2 / 3, (int)Vector2.Distance(new(startX, startY), new(endX, endY)),
				120, 120, 32, 160,
				new(0, 255, 0, 255)
			);
		}


		#endregion




		#region --- API ---


		public void DrawCannonBall (int localX, int localY, float t01) {
			var (x, y) = Local_to_Global(localX, localY, 0);
			if (t01 < 0.5f) {
				// Crosshair
				if (Game.GlobalFrame % 4 < 2) {
					CellRenderer.Draw(
						CROSSHAIR_CODE,
						x, y,
						SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
					);
				}
			} else {
				// Falling Cannon Ball
				t01 = (t01 - 0.5f) * 2f;
				CellRenderer.Draw(
					CANNON_CODE,
					x + SoupConst.ISO_SIZE / 2,
					y + (int)Mathf.LerpUnclamped(SoupConst.ISO_SIZE * 6, SoupConst.ISO_SIZE, t01),
					500, 500, 0,
					SoupConst.ISO_SIZE - 128, SoupConst.ISO_SIZE - 128
				);
			}

		}


		public void DrawExplosion (int localX, int localY, float t01) {
			var (x, y) = Local_to_Global(localX, localY, 1);
			t01 = t01 * 0.25f + 0.75f;
			t01 = 1f - (1f - t01) * (1f - t01);
			CellRenderer.Draw(
				Game.GlobalFrame % 4 < 2 ? EXPLOSION_CODE_0 : EXPLOSION_CODE_1,
				x + SoupConst.ISO_SIZE / 2, y + SoupConst.ISO_SIZE / 3,
				500, 500, 0,
				(int)((SoupConst.ISO_SIZE + 12) * t01 * t01 * t01),
				(int)((SoupConst.ISO_SIZE + 12) * t01 * t01 * t01)
			);
		}


		public void SetPickingInfo (Ability ability, ActionKeyword keyword, int actionLineIndex) {

			PickingKeyword = keyword;
			PickingLocalPositions.Clear();
			if (ability == null) return;
			for (int i = actionLineIndex + 1; i < ability.Units.Length; i++) {
				var unit = ability.Units[i];
				if (unit is not ActionUnit aUnit) break;
				if (aUnit.PositionCount == 0) continue;
				var tint = new Color32(255, 255, 255, 255);
				switch (aUnit.Type) {
					case ActionType.None:
					case ActionType.Pick:
					case ActionType.PerformSelfLastUsedAbility:
					case ActionType.PerformOpponentLastUsedAbility:
						continue;
					case ActionType.Reveal:
					case ActionType.Unreveal:
						tint = new Color32(255, 255, 128, 128);
						break;
					case ActionType.Sonar:
					case ActionType.Expand:
					case ActionType.Shrink:
						tint = new Color32(128, 128, 255, 255);
						break;
					case ActionType.AddCooldown:
					case ActionType.ReduceCooldown:
					case ActionType.AddMaxCooldown:
					case ActionType.ReduceMaxCooldown:
						tint = new Color32(128, 255, 128, 255);
						break;
				}
				for (int j = 0; j < aUnit.PositionCount; j++) {
					var pos = aUnit.Positions[j];
					PickingLocalPositions.Add(new PickingCell() {
						LocalX = pos.x,
						LocalY = pos.y,
						Tint = tint,
					});
				}
			}
		}


		public void SetPickingDirection (Direction4 dir) => PickingDirection = dir;


		public void ClearPickingInfo () {
			PickingKeyword = ActionKeyword.None;
			PickingLocalPositions.Clear();
		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}
