using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {



	public enum CellState {
		Normal = 0,
		Revealed = 1,
		Hit = 2,
		Sunk = 3,
	}



	public class Cell {
		public CellState State = CellState.Normal;
		public bool HasStone = false;
		public int Sonar = 0;
		public int ShipIndex = -1;
		public int ShipRenderID = 0;
		public int ShipRenderID_Add = 0;
	}



	public class Field {




		#region --- VAR ---


		// Const
		private static readonly Matrix2x2 L2G = new(SoupConst.ISO_WIDTH, -SoupConst.ISO_WIDTH, SoupConst.ISO_HEIGHT, SoupConst.ISO_HEIGHT);
		private static Matrix2x2 G2L => _G2L ??= L2G.Inverse(); static Matrix2x2 _G2L = null;

		// Api
		public Cell this[int x, int y] => Cells[x, y];
		public Ship[] Ships { get; init; } = null;
		public int MapSize { get; init; } = 1;
		public Vector2Int[] IsoArray { get; init; } = null;

		// Data
		private Cell[,] Cells { get; init; } = null;
		private Vector2Int LocalShift { get; init; } = default;


		#endregion




		#region --- MSG ---


		public Field (Ship[] ships, Map map, Vector2Int localShift) {
			MapSize = map.Size;
			LocalShift = localShift;
			Ships = ships;
			Cells = new Cell[MapSize, MapSize];
			IsoArray = GetIsoDistanceArray(MapSize);
			for (int j = 0; j < MapSize; j++) {
				for (int i = 0; i < MapSize; i++) {
					Cells[i, j] = new Cell() {
						HasStone = map[i, j] == 1,
					};
				}
			}
			// Ship Index / Renderer ID
			for (int i = 0; i < ships.Length; i++) {
				var ship = ships[i];
				for (int j = 0; j < ship.BodyNodes.Length; j++) {
					var body = ship.BodyNodes[j];
					var pos = ship.GetFieldNodePosition(j);
					if (pos.InLength(MapSize)) {
						var cell = Cells[pos.x, pos.y];
						cell.ShipIndex = i;
						cell.ShipRenderID = $"{ship.GlobalName} {body.x}.{body.y}".AngeHash();
						cell.ShipRenderID_Add = $"{ship.GlobalName}_Add {body.x}.{body.y}".AngeHash();
					}
				}
			}
		}


		#endregion




		#region --- API ---


		public (int globalX, int globalY) Local_to_Global (int localX, int localY, int localZ = 0) {
			localX += LocalShift.x;
			localY += LocalShift.y;
			var point = L2G * new Vector2(localX, localY);
			int globalX = (int)point.x;
			int globalY = (int)point.y + localZ * SoupConst.ISO_HEIGHT * 2;
			return (globalX, globalY);
		}


		public (int localX, int localY) Global_to_Local (int globalX, int globalY, int localZ = 0) {
			globalX -= SoupConst.ISO_WIDTH;
			globalY -= localZ * SoupConst.ISO_HEIGHT * 2;
			var point = G2L * new Vector2(globalX, globalY);
			int localX = (int)point.x.UFloor(1f);
			int localY = (int)point.y.UFloor(1f);
			return (localX - LocalShift.x, localY - LocalShift.y);
		}


		#endregion




		#region --- LGC ---


		private Vector2Int[] GetIsoDistanceArray (int size) {
			var result = new Vector2Int[size * size];
			int index = 0;
			for (int i = 0; i < size; i++) {
				int count = i + 1;
				for (int j = 0; j < count; j++) {
					result[index] = new(j, i - j);
					index++;
				}
			}
			for (int i = 1; i < size; i++) {
				int count = size - i;
				for (int j = 0; j < count; j++) {
					result[index] = new(i + j, size - j - 1);
					index++;
				}
			}
			return result;
		}


		#endregion




	}
}