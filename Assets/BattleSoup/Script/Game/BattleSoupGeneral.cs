using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;
namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }


namespace BattleSoup {


	[System.Serializable]
	public struct Int2 {
		public int x;
		public int y;
		public Int2 (int x, int y) {
			this.x = x;
			this.y = y;
		}
	}


	public static class SoupConst {
		public const int ISO_WIDTH = 32 * 7; // 231
		public const int ISO_HEIGHT = 16 * 7; // 119
		public const int ISO_SIZE = 65 * 7; // 455

	}



	// Ship
	[System.Serializable]
	public class ShipData {

		public string DisplayName = "";
		public string Discription = "";
		public int DefaultCooldown = 1;
		public int MaxCooldown = 1;
		public string Body = "1";
		public string Ability = "";

		public Vector2Int[] GetBodyArray () {
			var result = new List<Vector2Int>();
			int x = 0;
			int y = 0;
			for (int i = 0; i < Body.Length; i++) {
				char c = Body[i];
				switch (c) {
					case '0':
						x++;
						break;
					case '1':
						result.Add(new(x, y));
						x++;
						break;
					case ',':
						y++;
						x = 0;
						break;
				}
			}
			return result.ToArray();
		}

	}


	// Map
	[System.Serializable]
	public class MapData {

		public int this[int x, int y] => Content[y * Size + x];
		public int Size = 8;
		public int[] Content = new int[64]; // 0:Water 1:Stone

	}



}