using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;


namespace BattleSoup {
	public class ShipRenderer : BlocksRenderer {


		// API
		public void AddShip (ShipData ship, ShipPosition pos, Color color, bool useBG = true) {
			int id = ship.GlobalID;
			foreach (var block in ship.Ship.Body) {
				int x = pos.Pivot.x + (pos.Flip ? block.y : block.x);
				int y = pos.Pivot.y + (pos.Flip ? block.x : block.y);
				if (useBG) {
					AddBlock(x, y, 0, color);
				}
				AddBlock(x, y, id + 1, 0.618f);
			}
		}





	}
}
