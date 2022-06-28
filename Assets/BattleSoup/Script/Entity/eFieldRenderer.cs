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




		#region --- VAR ---


		// Const
		private static readonly int WATER_CODE = "Water".AngeHash();
		private static readonly int WATER_REVEALED_CODE = "Water Revealed".AngeHash();
		private static readonly int WATER_HIT_CODE = "Water Hit".AngeHash();
		private static readonly int WATER_SUNK_CODE = "Water Sunk".AngeHash();
		private static readonly int STONE_CODE = "Stone".AngeHash();
		private static readonly int[] SONAR_CODES = new int[] {
			"Sonar Unknown".AngeHash(),
			"Sonar 1".AngeHash(),
			"Sonar 2".AngeHash(),
			"Sonar 3".AngeHash(),
			"Sonar 4".AngeHash(),
			"Sonar 5".AngeHash(),
			"Sonar 6".AngeHash(),
			"Sonar 7".AngeHash(),
			"Sonar 8".AngeHash(),
			"Sonar 9".AngeHash(),
			"Sonar 9Plus".AngeHash(),
		};

		// Api
		public Field Field { get; set; } = null;

		// Data
		private BattleSoup Game = null;

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
		}


		public override void FrameUpdate () {
			base.FrameUpdate();
			if (Field == null || Game.State != BattleSoup.GameState.Playing) return;
			DrawWaters();
			DrawGizmos();
			DrawUnits();
			DrawEffect();
		}


		private void DrawWaters () {
			int count = Field.MapSize * Field.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y);
				var cell = Field[localPos.x, localPos.y];
				CellRenderer.Draw(
					cell.State switch {
						CellState.Revealed => WATER_REVEALED_CODE,
						CellState.Hit => WATER_HIT_CODE,
						CellState.Sunk => WATER_SUNK_CODE,
						_ => WATER_CODE,
					},
					x, y, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
				);
			}
		}


		private void DrawGizmos () {
			int count = Field.MapSize * Field.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y);
				var cell = Field[localPos.x, localPos.y];
				if (cell.Sonar != 0) {
					CellRenderer.Draw(
						SONAR_CODES[cell.Sonar.Clamp(0, SONAR_CODES.Length - 1)],
						x, y, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE
					);
				}
			}
		}


		private void DrawUnits () {
			int count = Field.MapSize * Field.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Field.IsoArray[i];
				var (x, y) = Field.Local_to_Global(localPos.x, localPos.y, 1);
				var cell = Field[localPos.x, localPos.y];
				// Stone
				if (cell.HasStone) DrawStone(x, y, i);
				// Ship
				if (cell.ShipRenderID != 0) {
					DrawShip(cell.ShipRenderID, x, y, Field.Ships[cell.ShipIndex].Flip);
				}
			}
		}


		private void DrawEffect () {

		}


		#endregion




		#region --- API ---




		#endregion




		#region --- LGC ---


		private void DrawStone (int globalX, int globalY, int index) {
			if (!CellRenderer.TryGetSpriteFromGroup(STONE_CODE, index, out var sprite)) return;
			CellRenderer.Draw(
				sprite.GlobalID,
				new RectInt(
					globalX, globalY,
					SoupConst.ISO_SIZE,
					SoupConst.ISO_SIZE
				).Shrink(24)
			);
		}


		private void DrawShip (int id, int globalX, int globalY, bool flip) {
			if (!CellRenderer.TryGetSprite(id, out var sprite)) return;
			CellRenderer.Draw(
				id,
				flip ? globalX + SoupConst.ISO_SIZE : globalX, globalY,
				flip ? -SoupConst.ISO_SIZE : SoupConst.ISO_SIZE,
				SoupConst.ISO_SIZE * sprite.GlobalHeight / sprite.GlobalWidth
			);
		}


		#endregion




	}
}