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
		public Field Field { get; set; } = null;
		public bool AllowHoveringOnShip { get; set; } = true;
		public bool AllowHoveringOnWater { get; set; } = true;
		public bool HideInvisibleShip { get; set; } = true;

		// Data
		private BattleSoup Game = null;
		private int HoveringShipIndex = -1;
		private RenderingCell[,] RenderCells = null;


		#endregion




		#region --- MSG ---



		public override void OnInitialize (Game game) {
			base.OnInitialize(game);
			Game = game as BattleSoup;
		}


		public override void OnActived () {
			base.OnActived();
			Reset();
		}
		public override void OnInactived () {
			base.OnInactived();
			Reset();
		}
		private void Reset () {
			Field = null;
			RenderCells = null;
		}


		public override void FrameUpdate () {
			base.FrameUpdate();
			if (Field == null || Game.State != BattleSoup.GameState.Playing) return;
			if (RenderCells == null) RenderCells = new RenderingCell[Field.MapSize, Field.MapSize];
			UpdateRenderCell();
			DrawWaters();
			DrawGizmos();
			DrawUnits();
			DrawEffect();
		}


		private void UpdateRenderCell () {
			int count = Field.MapSize;
			var (localMouseX, localMouseY) = Field.Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			for (int i = 0; i < count; i++) {
				for (int j = 0; j < count; j++) {
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


		private void DrawWaters () {
			var (localMouseX, localMouseY) = Field.Global_to_Local(FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1);
			var hoveringLocalPosition = new Vector2Int(localMouseX, localMouseY);
			int count = Field.MapSize * Field.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y);
				var cell = Field[localPos.x, localPos.y];
				var rCell = RenderCells[localPos.x, localPos.y];
				int id = AllowHoveringOnWater && HoveringShipIndex < 0 && localPos == hoveringLocalPosition ?
					WATER_HIGHLIGHT_CODE :
					cell.State switch {
						CellState.Revealed => WATER_REVEALED_CODE,
						CellState.Hit => WATER_HIT_CODE,
						CellState.Sunk => WATER_SUNK_CODE,
						_ => WATER_CODE,
					};
				CellRenderer.Draw(id, x, y + rCell.WaterOffsetY, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE);
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

			// Draw Units
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y, 1);
				var cell = Field[localPos.x, localPos.y];
				// Stone
				if (cell.HasStone && CellRenderer.TryGetSpriteFromGroup(STONE_CODE, i, out var spStone)) {
					CellRenderer.Draw(
						spStone.GlobalID,
						new RectInt(
							x, y,
							SoupConst.ISO_SIZE,
							SoupConst.ISO_SIZE
						).Shrink(24)
					);
				}
				// Draw Ship
				if (
					cell.ShipIndex >= 0 &&
					(!HideInvisibleShip || Field.Ships[cell.ShipIndex].Visible)
				) {
					var tint = cell.State switch {
						CellState.Hit => new Color32(209, 165, 31, 255),
						CellState.Sunk => new Color32(209, 165, 31, 128),
						_ => new Color32(255, 255, 255, 255),
					};
					if (cell.State == CellState.Sunk) {
						y -= SoupConst.ISO_HEIGHT * 3 / 2;
					}
					// Draw Ship
					if (CellRenderer.TryGetSprite(cell.ShipRenderID, out var spShip)) {
						bool flip = Field.Ships[cell.ShipIndex].Flip;
						ref var rCell = ref CellRenderer.Draw(
							cell.ShipRenderID,
							flip ? x + SoupConst.ISO_SIZE : x,
							y + GetWaveOffsetY(x, y),
							flip ? -SoupConst.ISO_SIZE : SoupConst.ISO_SIZE,
							SoupConst.ISO_SIZE * spShip.GlobalHeight / spShip.GlobalWidth,
							tint
						);
						if (
							AllowHoveringOnShip &&
							mouseX > rCell.X &&
							mouseX < rCell.X + rCell.Width &&
							mouseY > rCell.Y &&
							mouseY < rCell.Y + rCell.Height
						) {
							hoveringShipIndex = cell.ShipIndex;
						}
					}
					// Ship Highlight
					if (
						HoveringShipIndex == cell.ShipIndex &&
						CellRenderer.TryGetSprite(cell.ShipRenderID_Add, out var spShipAdd)
					) {
						bool flip = Field.Ships[cell.ShipIndex].Flip;
						CellRenderer.Draw(
							cell.ShipRenderID_Add,
							flip ? x + SoupConst.ISO_SIZE : x,
							y + GetWaveOffsetY(x, y),
							flip ? -SoupConst.ISO_SIZE : SoupConst.ISO_SIZE,
							SoupConst.ISO_SIZE * spShipAdd.GlobalHeight / spShipAdd.GlobalWidth
						);
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