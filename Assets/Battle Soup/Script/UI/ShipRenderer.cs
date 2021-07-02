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
					pos.Pivot.X + (pos.Flip ? block.Y : block.X),
					pos.Pivot.Y + (pos.Flip ? block.X : block.Y),
					id
				);
			}
		}



	}
}
