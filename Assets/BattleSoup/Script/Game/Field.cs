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

		public int ShipIndex => ShipIndexs.Count > 0 ? ShipIndexs[0] : -1;

		public CellState State = CellState.Normal;
		public bool HasStone = false;
		public int Sonar = 0;
		public readonly List<int> ShipIndexs = new();
		public readonly List<int> ShipRenderIDs = new();
		public readonly List<int> ShipRenderIDs_Add = new();

		public void AddShip (int shipIndex, Ship ship, int bodyX, int bodyY) {
			ShipIndexs.Add(shipIndex);
			ShipRenderIDs.Add($"{ship.GlobalName} {bodyX}.{bodyY}".AngeHash());
			ShipRenderIDs_Add.Add($"{ship.GlobalName}_Add {bodyX}.{bodyY}".AngeHash());
		}

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
		public Vector2Int LocalShift { get; set; } = default;

		// Data
		private Cell[,] Cells { get; init; } = null;


		#endregion




		#region --- MSG ---


		public Field (Ship[] ships, Map map, Vector2Int localShift) {
			MapSize = map.Size;
			LocalShift = localShift;
			Ships = ships;
			Cells = new Cell[MapSize, MapSize];
			IsoArray = GetIsoDistanceArray(MapSize);
			RefreshMapCache(map);
			RefreshShipCache();
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


		public void RefreshMapCache (Map map) {
			for (int j = 0; j < MapSize; j++) {
				for (int i = 0; i < MapSize; i++) {
					Cells[i, j] = new Cell() {
						HasStone = map[i, j] == 1,
					};
				}
			}
		}


		public void RefreshShipCache () {
			// Clear
			for (int j = 0; j < MapSize; j++) {
				for (int i = 0; i < MapSize; i++) {
					var cell = Cells[i, j];
					cell.ShipIndexs.Clear();
					cell.ShipRenderIDs.Clear();
					cell.ShipRenderIDs_Add.Clear();
				}
			}
			// Index, Render ID
			for (int i = 0; i < Ships.Length; i++) {
				var ship = Ships[i];
				for (int j = 0; j < ship.BodyNodes.Length; j++) {
					var body = ship.BodyNodes[j];
					var pos = ship.GetFieldNodePosition(j);
					if (pos.InLength(MapSize)) {
						Cells[pos.x, pos.y].AddShip(i, ship, body.x, body.y);
					}
				}
			}
			// Valid
			for (int i = 0; i < Ships.Length; i++) {
				var ship = Ships[i];
				ship.Valid = true;
				for (int j = 0; j < ship.BodyNodes.Length; j++) {
					var pos = ship.GetFieldNodePosition(j);
					// Outside Map
					if (!pos.InLength(MapSize)) {
						ship.Valid = false;
						break;
					}
					// Overlaping Each Other
					if (Cells[pos.x, pos.y].ShipIndexs.Count > 1) {
						ship.Valid = false;
						break;
					}
					// Overlaping Stone
					if (Cells[pos.x, pos.y].HasStone) {
						ship.Valid = false;
						break;
					}
				}
			}
		}


		public bool RandomPlaceShips (int failCheckCount) {
			for (int i = 0; i < failCheckCount; i++) {
				if (RandomPlaceShips()) return true;
			}
			return false;
		}
		public bool RandomPlaceShips () {

			// Clear Cell Ship Index Cache
			for (int j = 0; j < MapSize; j++) {
				for (int i = 0; i < MapSize; i++) {
					var cell = Cells[i, j];
					cell.ShipIndexs.Clear();
					cell.ShipRenderIDs.Clear();
					cell.ShipRenderIDs_Add.Clear();
				}
			}

			// Place Ships
			for (int shipIndex = 0; shipIndex < Ships.Length; shipIndex++) {
				var ship = Ships[shipIndex];
				int offsetX = Random.Range(0, MapSize);
				int offsetY = Random.Range(0, MapSize);
				for (int x = 0; x < MapSize; x++) {
					for (int y = 0; y < MapSize; y++) {
						ship.FieldX = (x + offsetX) % MapSize;
						ship.FieldY = (y + offsetY) % MapSize;
						ship.Flip = true;
						if (IsPositionValidForShip(ship)) goto ShipDone;
						ship.Flip = false;
						if (IsPositionValidForShip(ship)) goto ShipDone;
					}
				}
				// Ship Failed
				RefreshShipCache();
				return false;
				// Ship Success
				ShipDone:;
				for (int i = 0; i < ship.BodyNodes.Length; i++) {
					var pos = ship.GetFieldNodePosition(i);
					if (pos.InLength(MapSize)) {
						var body = ship.BodyNodes[i];
						Cells[pos.x, pos.y].AddShip(shipIndex, ship, body.x, body.y);
					}
				}
			}
			// Final
			ClampInvalidShipsInside();
			RefreshShipCache();
			return true;
		}


		public void ClampInvalidShipsInside () {
			for (int shipIndex = 0; shipIndex < Ships.Length; shipIndex++) {
				var ship = Ships[shipIndex];
				if (!IsPositionValidForShip(ship)) {
					int bodyL = int.MaxValue;
					int bodyR = 0;
					int bodyD = int.MaxValue;
					int bodyU = 0;
					for (int i = 0; i < ship.BodyNodes.Length; i++) {
						var pos = ship.GetFieldNodePosition(i);
						bodyL = Mathf.Min(bodyL, pos.x);
						bodyR = Mathf.Max(bodyR, pos.x);
						bodyD = Mathf.Min(bodyD, pos.y);
						bodyU = Mathf.Max(bodyU, pos.y);
					}
					if (bodyL < 0) ship.FieldX += -bodyL;
					if (bodyR > MapSize - 1) ship.FieldX -= bodyR - MapSize + 1;
					if (bodyD < 0) ship.FieldY += -bodyD;
					if (bodyU > MapSize - 1) ship.FieldY -= bodyU - MapSize + 1;
				}
			}
		}


		public bool IsPositionValidForShip (Ship ship) {
			for (int i = 0; i < ship.BodyNodes.Length; i++) {
				var pos = ship.GetFieldNodePosition(i);
				if (!pos.InLength(MapSize)) return false;
				var cell = Cells[pos.x, pos.y];
				if (cell.HasStone) return false;
				if (cell.ShipIndex >= 0) return false;
			}
			return true;
		}


		public bool IsValidForPlay (out string message) {
			message = "";
			if (MapSize <= 0) {
				message = "No Map Loaded";
				return false;
			}
			if (Ships == null || Ships.Length == 0) {
				message = "No Ship Loaded";
				return false;
			}
			foreach (var ship in Ships) {
				if (!ship.Valid) {
					message = "Ship Position Wrong";
					return false;
				}
			}
			return true;
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