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
	}



	public class Ship {

		public int GlobalID = 0;
		public string GlobalName = "";
		public int FieldX = 0;
		public int FieldY = 0;
		public bool Flip = false;
		public Vector2Int[] Body = null;
		public ShipData Data = null;

		public Vector2Int GetFieldNodePosition (int nodeIndex) {
			var node = Body[nodeIndex];
			return new(
				FieldX + (Flip ? node.y : node.x),
				FieldY + (Flip ? node.x : node.y)
			); ;
		}

	}



	public class Field {




		#region --- VAR ---


		// Api
		public Cell this[int x, int y] => Cells[x, y];
		public int MapSize => Map.Size;
		public Vector2Int[] IsoArray { get; init; } = null;
		public Ship[] Ships { get; init; } = null;
		public MapData Map { get; init; } = null;

		// Data
		private Cell[,] Cells { get; init; } = null;
		private Vector2Int LocalShift { get; init; } = default;


		#endregion




		#region --- MSG ---


		public Field (Ship[] ships, MapData map, Vector2Int localShift) {
			int mapSize = map.Size;
			LocalShift = localShift;
			Ships = ships;
			Map = map;
			Cells = new Cell[mapSize, mapSize];
			IsoArray = GetIsoDistanceArray(mapSize);
			for (int j = 0; j < mapSize; j++) {
				for (int i = 0; i < mapSize; i++) {
					Cells[i, j] = new Cell() {
						HasStone = map[i, j] == 1,
					};
				}
			}
			// Ship Index
			for (int i = 0; i < ships.Length; i++) {
				var ship = ships[i];
				for (int j = 0; j < ship.Body.Length; j++) {
					var body = ship.Body[j];
					var pos = ship.GetFieldNodePosition(j);
					if (pos.InLength(mapSize)) {
						var cell = Cells[pos.x, pos.y];
						cell.ShipIndex = i;
						cell.ShipRenderID = $"{ship.GlobalName} {body.x}.{body.y}".AngeHash();
					}
				}
			}
		}


		#endregion




		#region --- API ---


		public (int globalX, int globalY) Local_to_Global (int localX, int localY, int localZ = 0) {
			localX += LocalShift.x;
			localY += LocalShift.y;
			int globalX = localX * SoupConst.ISO_WIDTH - localY * SoupConst.ISO_WIDTH - SoupConst.ISO_WIDTH;
			int globalY = localX * SoupConst.ISO_HEIGHT + localY * SoupConst.ISO_HEIGHT + localZ * SoupConst.ISO_HEIGHT * 2;
			return (globalX, globalY);
		}


		public (int localX, int localY) Global_to_Local (int globalX, int globalY, int localZ = 0) {
			globalY -= localZ * SoupConst.ISO_HEIGHT * 2;
			globalX += SoupConst.ISO_WIDTH;
			int localX = globalX / (2 * SoupConst.ISO_WIDTH) + globalY / (2 * SoupConst.ISO_HEIGHT);
			int localY = globalY / (2 * SoupConst.ISO_HEIGHT) - globalX / (2 * SoupConst.ISO_WIDTH);
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