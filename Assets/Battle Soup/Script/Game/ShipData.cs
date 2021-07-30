using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;



namespace BattleSoup {
	[System.Serializable]
	public class ShipData {



		// Api
		public Sprite Sprite { get; set; } = null;
		public int SpriteIndex { get; set; } = 0;
		public string DisplayName => m_DisplayName;
		public string Description => m_Description;
		public Ship Ship {
			get => m_Ship;
			set => m_Ship = value;
		}

		// Ser
		[SerializeField] string m_DisplayName = "";
		[SerializeField] string m_Description = "";
		[SerializeField] Ship m_Ship = default;



		// API
		public static bool Contains (int x, int y, ShipData[] ships, ShipPosition[] positions, out int index) {
			for (int i = ships.Length - 1; i >= 0; i--) {
				var ship = ships[i];
				var sPos = positions[i];
				if (ship.Ship.Contains(x, y, sPos)) {
					index = i;
					return true;
				}
			}
			index = -1;
			return false;
		}


		public static int FindNearestShipDistance (int x, int y, ShipData[] ships, ShipPosition[] positions, out Vector2Int pos) {
			pos = default;
			if (ships == null || ships.Length == 0) { return -1; }
			int minDistance = int.MaxValue;
			bool hasDis = false;
			for (int i = 0; i < ships.Length; i++) {
				var ship = ships[i];
				var sPos = positions[i];
				foreach (var v in ship.Ship.Body) {
					int posX = sPos.Pivot.x + (sPos.Flip ? v.y : v.x);
					int posY = sPos.Pivot.y + (sPos.Flip ? v.x : v.y);
					int dis = Mathf.Abs(posX - x) + Mathf.Abs(posY - y);
					if (dis < minDistance) {
						minDistance = dis;
						pos.x = posX;
						pos.y = posY;
						hasDis = true;
					}
				}
			}
			return hasDis ? minDistance : -1;
		}


		public static Ship[] GetShips (ShipData[] ships) {
			var result = new Ship[ships.Length];
			for (int i = 0; i < ships.Length; i++) {
				result[i] = ships[i].Ship;
			}
			return result;
		}



	}
}