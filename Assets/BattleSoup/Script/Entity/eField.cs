using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	// === Main ===
	[EntityCapacity(2)]
	[ExcludeInMapEditor]
	[ForceUpdate]
	[DontDepawnWhenOutOfRange]
	public partial class eField : Entity {




		#region --- VAR ---



		// Const
		private static readonly Matrix2x2 L2G = new(SoupConst.ISO_WIDTH, -SoupConst.ISO_WIDTH, SoupConst.ISO_HEIGHT, SoupConst.ISO_HEIGHT);
		private static Matrix2x2 G2L => _G2L ??= L2G.Inverse(); static Matrix2x2 _G2L = null;

		// Api
		public Cell this[int x, int y] => Cells[x, y];
		public Ship[] Ships { get; private set; } = new Ship[0];
		public int MapSize { get; private set; } = 1;
		public Vector2Int[] IsoArray { get; private set; } = new Vector2Int[0];
		public Vector2Int LocalShift { get; set; } = default;
		public bool Enable { get; set; } = false;
		public bool AllowHoveringOnShip { get; set; } = true;
		public bool AllowHoveringOnWater { get; set; } = true;
		public bool HideInvisibleShip { get; set; } = true;
		public bool ShowShips { get; set; } = true;
		public bool DragToMoveShips { get; set; } = false;
		public bool ClickToAttack { get; set; } = false;
		public bool ClickShipToTriggerAbility { get; set; } = false;

		// Data
		private Cell[,] Cells = null;



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
			if (!Enable) return;
			UpdateRenderCells();
			Update_DragToMoveShips();
			Update_ClickToAttack();
			Update_ClickShipToTriggerAbility();
			DrawWaters();
			DrawGizmos();
			DrawUnits();
		}


		private void Update_DragToMoveShips () {
			if (!DragToMoveShips) return;
			if (FrameInput.MouseLeft) {
				// Mouse Left Holding
				var (localX, localY) = Global_to_Local(
					FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1
				);
				if (FrameInput.MouseLeftDown) {
					// Mouse Left Down
					DraggingShipIndex = HoveringShipIndex;
					if (DraggingShipIndex >= 0) {
						var ship = Ships[DraggingShipIndex];
						DraggingShipLocalOffset.x = localX - ship.FieldX;
						DraggingShipLocalOffset.y = localY - ship.FieldY;
					}
				}
				if (DraggingShipIndex >= 0) {
					// Dragging Ship
					MoveShip(
						DraggingShipIndex,
						localX - DraggingShipLocalOffset.x,
						localY - DraggingShipLocalOffset.y
					);
				}
			} else {
				// Mosue Left Not Holding
				if (DraggingShipIndex >= 0) {
					DraggingShipIndex = -1;
					ClampInvalidShipsInside(true);
				}
			}
			// Mouse Right to Flip
			if (FrameInput.MouseRightDown && HoveringShipIndex >= 0) {
				FlipShip(HoveringShipIndex);
				ClampInvalidShipsInside(true);
			}
		}


		private void Update_ClickToAttack () {
			if (!ClickToAttack || !FrameInput.MouseLeftDown) return;
			var (localX, localY) = Global_to_Local(
				FrameInput.MouseGlobalPosition.x, FrameInput.MouseGlobalPosition.y, 1
			);
			if (localX < 0 || localY < 0 || localX >= MapSize || localY >= MapSize) return;
			var cell = Cells[localX, localY];
			if (cell.HasStone) return;
			if (
				cell.State == CellState.Normal ||
				(cell.ShipIndex >= 0 && cell.State == CellState.Revealed)
			) {
				CellStep.AddToLast(new sAttack(localX, localY, this));
				CellStep.AddToLast(new sSwitchTurn());
			}
		}


		private void Update_ClickShipToTriggerAbility () {
			if (!ClickShipToTriggerAbility || !FrameInput.MouseLeftDown) return;



		}


		#endregion




		#region --- API ---


		// Game
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


		public void GameStart () {
			// Cells
			for (int i = 0; i < MapSize; i++) {
				for (int j = 0; j < MapSize; j++) {
					var cell = Cells[i, j];
					cell.State = CellState.Normal;
					cell.Sonar = 0;
				}
			}
			// Ships
			for (int i = 0; i < Ships.Length; i++) {
				var ship = Ships[i];
				ship.CurrentCooldown = ship.DefaultCooldown - 1;
				ship.Visible = false;
				ship.Alive = true;
			}

		}


		public bool AllShipsSunk () {
			foreach (var ship in Ships) {
				if (ship.Alive) return false;
			}
			return true;
		}


		// Action
		public ActionResult Attack (int x, int y) {
			if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return ActionResult.None;
			var cell = Cells[x, y];
			if (cell.ShipIndex < 0) {
				// No Ship
				cell.State = CellState.Revealed;
				return ActionResult.RevealWater;
			} else {
				// Hit Ship
				cell.State = CellState.Hit;
				RefreshAllShipsAliveState();
				return Ships[cell.ShipIndex].Alive ? ActionResult.Hit : ActionResult.Sunk;
			}
		}


		public ActionResult Reveal (int x, int y) {
			if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return ActionResult.None;
			var cell = Cells[x, y];
			if (cell.ShipIndex < 0) {
				// No Ship
				cell.State = CellState.Revealed;
				return ActionResult.RevealWater;
			} else {
				// Reveal Ship
				cell.State = CellState.Revealed;
				return ActionResult.RevealShip;
			}
		}


		public ActionResult Sonar (int x, int y) {
			if (x < 0 || x >= MapSize || y < 0 || y >= MapSize) return ActionResult.None;
			var cell = Cells[x, y];
			if (cell.ShipIndex < 0) {
				// No Ship
				cell.State = CellState.Revealed;
				// Get Min Distance
				int minDis = int.MaxValue;
				for (int j = 0; j < MapSize; j++) {
					for (int i = 0; i < MapSize; i++) {
						var _cell = Cells[i, j];
						if (_cell.ShipIndex >= 0) {
							minDis = Mathf.Min(Mathf.Abs(i - x) + Mathf.Abs(j - y), minDis);
						}
					}
				}
				if (minDis != int.MaxValue) {
					// Reveal All Cells In Range
					for (int j = 0; j < MapSize; j++) {
						for (int i = 0; i < MapSize; i++) {
							if (Mathf.Abs(i - x) + Mathf.Abs(j - y) < minDis) {
								Cells[i, j].State = CellState.Revealed;
							}
						}
					}
					cell.Sonar = minDis;
				}
				return ActionResult.RevealWater;
			} else {
				// Hit Ship
				cell.State = CellState.Hit;
				RefreshAllShipsAliveState();
				return Ships[cell.ShipIndex].Alive ? ActionResult.Hit : ActionResult.Sunk;
			}
		}


		// Coord
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


		// Map
		public void SetMap (in Map map) {
			MapSize = map.Size;
			Cells = new Cell[MapSize, MapSize];
			IsoArray = GetIsoDistanceArray(MapSize);
			for (int j = 0; j < map.Size; j++) {
				for (int i = 0; i < map.Size; i++) {
					Cells[i, j] = new Cell() {
						HasStone = map[i, j] == 1,
					};
				}
			}
			RefreshShipCache();
			RandomPlaceShips(128);
		}


		// Ship
		public void SetShips (in Ship[] ships) {
			Ships = ships;
			RefreshShipCache();
		}


		public bool RandomPlaceShips (int failCheckCount = 1) {
			bool success = false;
			for (int i = 0; i < failCheckCount; i++) {
				if (RandomPlaceShipsLogic()) {
					success = true;
					break;
				}
			}
			RefreshShipCache();
			return success;
		}


		public void ClampInvalidShipsInside (bool onlyClampCompleteOutsideShips = false) {
			for (int shipIndex = 0; shipIndex < Ships.Length; shipIndex++) {
				var ship = Ships[shipIndex];
				if (!onlyClampCompleteOutsideShips || ShipIsCompleteOutside(ship)) {
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
			RefreshShipCache();
		}


		public bool IsPositionValidForShip (in Ship ship) {
			for (int i = 0; i < ship.BodyNodes.Length; i++) {
				var pos = ship.GetFieldNodePosition(i);
				if (!pos.InLength(MapSize)) return false;
				var cell = Cells[pos.x, pos.y];
				if (cell.HasStone) return false;
				if (cell.ShipIndex >= 0) return false;
			}
			return true;
		}


		public void MoveShip (int shipIndex, int newFieldX, int newFieldY) {
			if (shipIndex < 0 || shipIndex >= Ships.Length) return;
			var ship = Ships[shipIndex];
			if (
				newFieldX != ship.FieldX ||
				newFieldY != ship.FieldY
			) {
				ship.FieldX = newFieldX;
				ship.FieldY = newFieldY;
				RefreshShipCache();
			}
		}


		public void FlipShip (int shipIndex) {
			if (shipIndex < 0 || shipIndex >= Ships.Length) return;
			var ship = Ships[shipIndex];
			ship.Flip = !ship.Flip;
			RefreshShipCache();
		}


		public bool HasShip (int x, int y) {
			if (x < 0 || y < 0 || x >= MapSize || y >= MapSize) return false;
			return Cells[x, y].ShipIndex >= 0;
		}


		#endregion




		#region --- LGC ---


		private int GetWaveOffsetY (int x, int y) => (AngeliaFramework.Game.GlobalFrame + x + y).PingPong(60) / 5 - 6;


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


		private bool RandomPlaceShipsLogic () {

			// Clear Cell Ship Index Cache
			for (int j = 0; j < MapSize; j++) {
				for (int i = 0; i < MapSize; i++) {
					Cells[i, j].ClearShip();
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
			return true;
		}


		private void RefreshShipCache () {
			// Clear
			for (int j = 0; j < MapSize; j++) {
				for (int i = 0; i < MapSize; i++) {
					Cells[i, j].ClearShip();
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


		private bool ShipIsCompleteOutside (Ship ship) {
			for (int i = 0; i < ship.BodyNodes.Length; i++) {
				var pos = ship.GetFieldNodePosition(i);
				if (pos.x >= 0 && pos.x < MapSize && pos.y >= 0 && pos.y < MapSize) return false;
			}
			return true;
		}


		private void RefreshAllShipsAliveState () {
			for (int i = 0; i < Ships.Length; i++) {
				var ship = Ships[i];
				int hitCount = 0;
				bool requireFixState = false;
				for (int j = 0; j < ship.BodyNodes.Length; j++) {
					var pos = ship.GetFieldNodePosition(j);
					var cell = Cells[pos.x, pos.y];
					if (cell.State == CellState.Hit || cell.State == CellState.Sunk) {
						hitCount++;
						if (cell.State == CellState.Hit) requireFixState = true;
					}
				}
				ship.Alive = hitCount < ship.BodyNodes.Length;
				if (requireFixState && !ship.Alive) {
					for (int j = 0; j < ship.BodyNodes.Length; j++) {
						var pos = ship.GetFieldNodePosition(j);
						var cell = Cells[pos.x, pos.y];
						cell.State = CellState.Sunk;
					}
				}
			}
		}


		#endregion




	}
}