using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {




	public static class SoupAI {



		public static void Analyse (
			Tile[,] ownTiles, Tile[,] opponentTiles,
			(Ship, ShipPosition)[] ownShips, Ship[] opponentShips
		) {








		}



		public static List<ShipPosition> GetShipPosition (int mapSize, Int2[] stones, Ship[] ships) {
			var result = new List<ShipPosition>();



			result.AddRange(new ShipPosition[ships.Length]);


			return result;
		}



	}




}
