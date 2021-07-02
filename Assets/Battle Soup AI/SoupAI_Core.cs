using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {




	public class Soup {



		public void Analyse (
			Tile[,] ownTiles, Tile[,] opponentTiles,
			(Ship, ShipPosition)[] ownShips, Ship[] opponentShips
		) {








		}



		public static bool GetRandomShipPositions (List<Ship> ships, Int2[] stonePositions, List<ShipPosition> result) {
			if (ships == null || ships.Count == 0) { return false; }



			result.Clear();
			foreach (var ship in ships) {
				//////////////// TEST /////////////
				result.Add(new ShipPosition() {
					Flip = false,
					Pivot = new Int2(0, 0)
				});
				//////////////// TEST /////////////
			}
			return true;
		}



	}




}
