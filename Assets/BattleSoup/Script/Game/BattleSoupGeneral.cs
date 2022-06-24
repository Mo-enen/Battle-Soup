using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;
namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }


namespace BattleSoup {


	public enum GameState {
		Title = 0,
		Prepare = 1,
		Playing = 2,
	}


	public enum GameMode {
		PvA = 0,
		AvA = 1,
		Card = 2,
	}


	public enum CellType {
		Water = 0,
		Stone = 1,
		Ship = 2,
	}


	public class Cell {
		public CellType Type = CellType.Water;
		public bool Revealed = false;
		public bool Hit = false;
		public int ShipID = 0;
	}



	public static class SoupConst {
		public const int ISO_WIDTH = 32 * 7; // 231
		public const int ISO_HEIGHT = 16 * 7; // 119
		public const int ISO_SIZE = 65 * 7; // 455

	}


	public static class SoupUtil {


		public static (int globalX, int globalY) Local_to_Global (int localX, int localY, int localZ = 0) => (
			localX * SoupConst.ISO_WIDTH - localY * SoupConst.ISO_WIDTH - SoupConst.ISO_WIDTH,
			localX * SoupConst.ISO_HEIGHT + localY * SoupConst.ISO_HEIGHT + localZ * SoupConst.ISO_HEIGHT * 2
		);


		public static (int localX, int localY) Global_to_Local (int globalX, int globalY, int localZ = 0) {
			int localX = 0;
			int localY = 0;
			globalY -= localZ * SoupConst.ISO_HEIGHT;




			return (localX, localY);
		}


		public static Vector2Int[] GetIsoDistanceArray (int size) {
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


	}
}