using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {




	public static class SoupAI {



		public enum AbilityDirection {
			Up = 0,
			Down = 1,
			Left = 2,
			Right = 3,
		}


		public static bool Analyse (
			Tile[,] ownTiles, Tile[,] opponentTiles,
			Ship[] ownShips, Ship[] opponentShips,
			List<ShipPosition> ownShipPositions,
			out Int2 targetPosition, out int abilityIndex, out AbilityDirection abilityDirection
		) {
			targetPosition = default;
			abilityIndex = -1;
			abilityDirection = AbilityDirection.Up;






			return false;
		}



		public static List<ShipPosition> GetShipPosition (int mapSize, Int2[] stones, Ship[] ships) {
			var result = new List<ShipPosition>();



			result.AddRange(new ShipPosition[ships.Length]);


			return result;
		}



	}




}
