using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	[EntityCapacity(2)]
	[ExcludeInMapEditor]
	[ForceUpdate]
	[DontDepawnWhenOutOfRange]
	public class eFieldRenderer : Entity {




		#region --- SUB ---


		private struct RenderingCell {
			public int WaterOffsetY;
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
		private static readonly int[] SONAR_CODES = new int[] { "Sonar Unknown".AngeHash(), "Sonar 1".AngeHash(), "Sonar 2".AngeHash(), "Sonar 3".AngeHash(), "Sonar 4".AngeHash(), "Sonar 5".AngeHash(), "Sonar 6".AngeHash(), "Sonar 7".AngeHash(), "Sonar 8".AngeHash(), "Sonar 9".AngeHash(), "Sonar 9Plus".AngeHash(), };

		// Api
		public Field Field { get; } = new();
		public bool Enable { get; set; } = true;
		public bool AllowHoveringOnShip { get; set; } = true;
		public bool AllowHoveringOnWater { get; set; } = true;
		public bool HideInvisibleShip { get; set; } = true;
		public bool ShowShips { get; set; } = true;
		public bool DragToMoveShips { get; set; } = false;

		// Data
		private RenderingCell[,] RenderCells = null;
		private int HoveringShipIndex = -1;
		private int DraggingShipIndex = -1;
		private Vector2Int DraggingShipLocalOffset = default;


		#endregion




		#region --- MSG ---


		public override void OnActived () {
			base.OnActived();
			RenderCells = null;
			DraggingShipIndex = -1;
			HoveringShipIndex = -1;
		}


		public override void FrameUpdate () {
			base.FrameUpdate();
			if (Field == null || !Enable) return;
			UpdateCache();
			UpdateDragToMoveShips();
			DrawWaters();
			DrawGizmos();
			DrawUnits();
			DrawEffect();
		}


		private void UpdateCache () {

			int mapSize = Field.MapSize;

			// Render Cells
			if (
				RenderCells == null ||
				RenderCells.GetLength(0) != mapSize ||
				RenderCells.GetLength(1) != mapSize
			) RenderCells = new RenderingCell[Field.MapSize, Field.MapSize];

			var (localMouseX, localMouseY) = Field.Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			for (int i = 0; i < mapSize; i++) {
				for (int j = 0; j < mapSize; j++) {
					ref var cell = ref RenderCells[i, j];
					// Water Offset Y
					if (localMouseX == i && localMouseY == j) {
						// Highlight
						cell.WaterOffsetY = SoupConst.ISO_HEIGHT / 2;
					} else {
						// Back to Normal
						if (cell.WaterOffsetY != 0) cell.WaterOffsetY = cell.WaterOffsetY * 7 / 12;
					}
				}
			}

		}


		private void UpdateDragToMoveShips () {
			if (!DragToMoveShips) return;
			if (FrameInput.MouseLeft) {
				// Mouse Left Holding
				var (localX, localY) = Field.Global_to_Local(
					FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1
				);
				if (FrameInput.MouseLeftDown) {
					// Mouse Left Down
					DraggingShipIndex = HoveringShipIndex;
					if (DraggingShipIndex >= 0) {
						var ship = Field.Ships[DraggingShipIndex];
						DraggingShipLocalOffset.x = localX - ship.FieldX;
						DraggingShipLocalOffset.y = localY - ship.FieldY;
					}
				}
				if (DraggingShipIndex >= 0) {
					// Dragging Ship
					Field.MoveShip(
						DraggingShipIndex,
						localX - DraggingShipLocalOffset.x,
						localY - DraggingShipLocalOffset.y
					);
				}
			} else {
				// Mosue Left Not Holding
				if (DraggingShipIndex >= 0) {
					DraggingShipIndex = -1;
					Field.ClampInvalidShipsInside();
				}
			}
		}


		// Draw
		private void DrawWaters () {
			var (localMouseX, localMouseY) = Field.Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			var hoveringLocalPosition = new Vector2Int(localMouseX, localMouseY);
			int count = Field.MapSize * Field.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y);
				var cell = Field[localPos.x, localPos.y];
				var rCell = RenderCells[localPos.x, localPos.y];
				bool hovering = AllowHoveringOnWater && HoveringShipIndex < 0 && localPos == hoveringLocalPosition;
				int id = hovering ?
					WATER_HIGHLIGHT_CODE :
					cell.State switch {
						CellState.Revealed => WATER_REVEALED_CODE,
						CellState.Hit => WATER_HIT_CODE,
						CellState.Sunk => WATER_SUNK_CODE,
						_ => WATER_CODE,
					};
				CellRenderer.Draw(
					id, x, y + (hovering ? rCell.WaterOffsetY : 0),
					SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
				);
			}
		}


		private void DrawGizmos () {
			int count = Field.MapSize * Field.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y);
				var cell = Field[localPos.x, localPos.y];
				// Sonar Number
				if (cell.Sonar != 0) {
					CellRenderer.Draw(
						SONAR_CODES[cell.Sonar.Clamp(0, SONAR_CODES.Length - 1)],
						x, y,
						SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
					);
				}
			}

		}


		private void DrawUnits () {

			int count = Field.MapSize * Field.MapSize;
			int hoveringShipIndex = -1;
			int mouseX = FrameInput.MouseGlobalPosition.x;
			int mouseY = FrameInput.MouseGlobalPosition.y;
			var (localMouseX, localMouseY) = Field.Global_to_Local(
				FrameInput.MouseGlobalPosition.x,
				FrameInput.MouseGlobalPosition.y,
				1
			);

			// Draw Units
			for (int unitIndex = count - 1; unitIndex >= 0; unitIndex--) {

				var localPos = Field.IsoArray[unitIndex];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y, 1);
				var cell = Field[localPos.x, localPos.y];

				// Stone
				if (cell.HasStone && CellRenderer.TryGetSpriteFromGroup(STONE_CODE, unitIndex, out var spStone)) {
					CellRenderer.Draw(
						spStone.GlobalID,
						new RectInt(
							x, y,
							SoupConst.ISO_SIZE,
							SoupConst.ISO_SIZE
						).Shrink(24)
					);
				}

				// Draw All Ships in Cell
				if (ShowShips) {
					for (int i = 0; i < cell.ShipIndexs.Count; i++) {
						int rID = cell.ShipRenderIDs[i];
						int rID_add = cell.ShipRenderIDsAdd[i];
						int shipIndex = cell.ShipIndexs[i];
						var ship = Field.Ships[shipIndex];
						if (
							shipIndex >= 0 &&
							(!HideInvisibleShip || ship.Visible)
						) {
							var tint = ship.Valid ? cell.State switch {
								CellState.Hit => new Color32(209, 165, 31, 255),
								CellState.Sunk => new Color32(209, 165, 31, 128),
								_ => new Color32(255, 255, 255, 255),
							} : new Color32(255, 16, 16, 255);
							if (cell.State == CellState.Sunk) {
								y -= SoupConst.ISO_HEIGHT * 3 / 2;
							}
							int shipID = HoveringShipIndex != shipIndex ? rID : rID_add;
							if (CellRenderer.TryGetSprite(shipID, out var spShip)) {
								bool flip = ship.Flip;
								ref var rCell = ref CellRenderer.Draw(
									shipID,
									flip ? x + SoupConst.ISO_SIZE : x,
									y + GetWaveOffsetY(x, y),
									flip ? -SoupConst.ISO_SIZE : SoupConst.ISO_SIZE,
									SoupConst.ISO_SIZE * spShip.GlobalHeight / spShip.GlobalWidth,
									tint
								);
								bool localMouseChecked = localMouseX >= localPos.x && localMouseY >= localPos.y;
								int xMin = rCell.Width > 0 ? rCell.X : rCell.X + rCell.Width;
								int xMax = rCell.Width > 0 ? rCell.X + rCell.Width : rCell.X;
								if (
									AllowHoveringOnShip && localMouseChecked &&
									cell.State != CellState.Sunk &&
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


		private void DrawEffect () {

		}


		#endregion




		#region --- API ---





		#endregion




		#region --- LGC ---


		private int GetWaveOffsetY (int x, int y) => (AngeliaFramework.Game.GlobalFrame + x + y).PingPong(60) / 5 - 6;


		#endregion




	}
}