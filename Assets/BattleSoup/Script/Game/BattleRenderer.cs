using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	[EntityCapacity(2)]
	[ExcludeInMapEditor]
	[ForceUpdate]
	[DontDepawnWhenOutOfRange]
	public class BattleRenderer : Entity {




		#region --- VAR ---


		// Const
		private static readonly int WATER_CODE = "Water".AngeHash();
		private static readonly int STONE_CODE = "Stone".AngeHash();

		// Api
		public Battle Battle { get; set; } = null;
		public Vector2Int LocalShift { get; set; } = default;

		// Data


		#endregion




		#region --- MSG ---


		public override void OnActived () {
			base.OnActived();
			Reset();
		}
		public override void OnInactived () {
			base.OnInactived();
			Reset();
		}
		private void Reset () {
			Battle = null;
			LocalShift = default;
		}


		public override void FrameUpdate () {
			base.FrameUpdate();
			if (Battle == null) return;
			DrawWaters();
			DrawUnits();
		}


		private void DrawWaters () {
			int count = Battle.MapSize * Battle.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Battle.IsoArray[i] + LocalShift;
				var (x, y) = SoupUtil.Local_to_Global(localPos.x, localPos.y);
				CellRenderer.Draw(WATER_CODE, x, y, SoupConst.ISO_SIZE, SoupConst.ISO_SIZE);
			}
		}


		private void DrawUnits () {
			int count = Battle.MapSize * Battle.MapSize;
			for (int i = count - 1; i >= 0; i--) {
				var localPos = Battle.IsoArray[i];
				var shiftLocalPos = localPos + LocalShift;
				var (x, y) = SoupUtil.Local_to_Global(shiftLocalPos.x, shiftLocalPos.y, 1);
				var cell = Battle.Cells[localPos.x, localPos.y];
				switch (cell.Type) {
					case CellType.Stone:
						if (CellRenderer.TryGetSpriteFromGroup(STONE_CODE, i, out var sprite)) {
							CellRenderer.Draw(
								sprite.GlobalID,
								new RectInt(
									x, y,
									SoupConst.ISO_SIZE,
									SoupConst.ISO_SIZE
								).Shrink(42)
							);
						}
						break;
					case CellType.Ship:

						break;
				}
			}
		}


		#endregion




		#region --- API ---




		#endregion




		#region --- LGC ---





		#endregion




	}
}