using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;


namespace BattleSoup {
	[System.Serializable]
	public class MapData {


		// Api
		public int Size => m_Size;
		public Int2[] Stones => m_Stones;

		// Ser
		[SerializeField] int m_Size = 8;
		[SerializeField] Int2[] m_Stones = new Int2[0];


		// API
		public bool HasStone (int x, int y) {
			for (int i = 0; i < m_Stones.Length; i++) {
				var stone = m_Stones[i];
				if (stone.x == x && stone.y == y) {
					return true;
				}
			}
			return false;
		}


		public bool GetRandomTile (Tile target, Tile[,] tiles, out int x, out int y, System.Func<int, int, bool> check = null) {
			x = Random.Range(0, m_Size);
			y = Random.Range(0, m_Size);
			for (int j = 0; j < m_Size; j++) {
				for (int i = 0; i < m_Size; i++) {
					int _x = (x + i) % m_Size;
					int _y = (y + j) % m_Size;
					if (
						target.HasFlag(tiles[_x, _y]) &&
						(check == null || check(_x, _y))
					) {
						x = _x;
						y = _y;
						return true;
					}
				}
			}
			return false;
		}


	}
}