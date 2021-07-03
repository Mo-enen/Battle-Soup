using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;


namespace BattleSoup {
	public class ShipRenderer : BlocksRenderer {



		public void AddShip (ShipData ship, ShipPosition pos) {
			int id = ship.GlobalID;
			foreach (var block in ship.Ship.Body) {
				AddBlock(
					pos.Pivot.x + (pos.Flip ? block.y : block.x),
					pos.Pivot.y + (pos.Flip ? block.x : block.y),
					id
				);
			}
		}



	}
}
