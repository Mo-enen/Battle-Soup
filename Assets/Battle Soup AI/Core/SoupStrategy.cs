using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {



	public class BattleInfo {
		public Tile[,] Tiles;
		public Ship[] Ships;
		public int[] Cooldowns;
	}



	public struct AnalyseResult {

		public bool Success => string.IsNullOrEmpty(ErrorMessage);

		public string ErrorMessage;
		public Int2 TargetPosition;
		public int AbilityIndex;
		public AbilityDirection AbilityDirection;

	}



	public abstract class SoupStrategy {




		#region --- VAR ---


		// Api
		public string FinalDisplayName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : GetType().Name;
		public virtual string DisplayName { get; } = "";
		public virtual string Description { get; } = "";


		#endregion




		#region --- API ---


		public abstract AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo opponentInfo, ShipPosition[] ownShipPositions, int usingAbilityIndex);


		public virtual bool GetShipPosition (int mapSize, Ship[] ships, Int2[] stones, out ShipPosition[] result) => GetRandomShipPositions(mapSize, ships, stones, out result);


		public static bool GetRandomShipPositions (int mapSize, Ship[] ships, Int2[] stones, out ShipPosition[] result) {

			result = null;
			if (ships == null || ships.Length == 0) { return false; }
			bool success = true;

			// Get Hash
			var hash = new HashSet<Int2>();
			if (stones != null) {
				foreach (var stone in stones) {
					if (!hash.Contains(stone)) {
						hash.Add(stone);
					}
				}
			}

			// Get Result
			result = new ShipPosition[ships.Length];
			var random = new System.Random(System.DateTime.Now.Millisecond);
			for (int index = 0; index < ships.Length; index++) {
				var ship = ships[index];
				random = new System.Random(random.Next());
				var sPos = new ShipPosition();
				var basicPivot = new Int2(random.Next(0, mapSize), random.Next(0, mapSize));
				bool shipSuccess = false;
				// Try Fix Overlap
				for (int j = 0; j < mapSize; j++) {
					for (int i = 0; i < mapSize; i++) {
						sPos.Pivot = new Int2(
							(basicPivot.x + i) % mapSize,
							(basicPivot.y + j) % mapSize
						);
						sPos.Flip = false;
						if (PositionAvailable(ship, sPos)) {
							AddShipIntoHash(ship, sPos);
							shipSuccess = true;
							j = mapSize;
							break;
						}
						sPos.Flip = true;
						if (PositionAvailable(ship, sPos)) {
							AddShipIntoHash(ship, sPos);
							shipSuccess = true;
							j = mapSize;
							break;
						}
					}
				}
				if (!shipSuccess) { success = false; }
				result[index] = sPos;
			}
			if (!success) {
				result = null;
			}
			return success;
			// Func
			bool PositionAvailable (Ship _ship, ShipPosition _pos) {
				// Border Check
				var (min, max) = _ship.GetBounds(_pos);
				if (_pos.Pivot.x < -min.x || _pos.Pivot.x > mapSize - max.x - 1 ||
					_pos.Pivot.y < -min.y || _pos.Pivot.y > mapSize - max.y - 1
				) {
					return false;
				}
				// Overlap Check
				foreach (var v in _ship.Body) {
					if (hash.Contains(new Int2(
						_pos.Pivot.x + (_pos.Flip ? v.y : v.x),
						_pos.Pivot.y + (_pos.Flip ? v.x : v.y)
					))) {
						return false;
					}
				}
				return true;
			}
			void AddShipIntoHash (Ship _ship, ShipPosition _pos) {
				foreach (var v in _ship.Body) {
					var shipPosition = new Int2(
						_pos.Pivot.x + (_pos.Flip ? v.y : v.x),
						_pos.Pivot.y + (_pos.Flip ? v.x : v.y)
					);
					if (!hash.Contains(shipPosition)) {
						hash.Add(shipPosition);
					}
				}
			}
		}


		#endregion




		#region --- LGC ---




		#endregion




		#region --- UTL ---




		#endregion












	}
}
