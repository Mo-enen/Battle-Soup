using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BattleSoup {
	public class Battle {




		#region --- VAR ---


		// Api
		public Cell[,] Cells { get; init; } = null;
		public int MapSize { get; init; } = 1;
		public Vector2Int[] IsoArray { get; init; } = null;


		#endregion




		#region --- MSG ---


		public Battle (int mapSize) {
			MapSize = mapSize;
			Cells = new Cell[mapSize, mapSize];
			IsoArray = SoupUtil.GetIsoDistanceArray(mapSize);
			for (int j = 0; j < mapSize; j++) {
				for (int i = 0; i < mapSize; i++) {
					Cells[i, j] = new Cell();
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