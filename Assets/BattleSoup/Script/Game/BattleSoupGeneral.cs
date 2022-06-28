using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;
namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }


namespace BattleSoup {





	public enum ActionType {

		None,

		// Position Buffer
		Pick,
		This,
		Clear,

		// Operations
		Attack,
		Reveal,
		Unreveal,
		Sonar,

		SunkShip,
		RevealShip,
		AddCooldown,
		ReduceCooldown,
		AddMaxCooldown,
		ReduceMaxCooldown,

	}


	public enum EntranceType {
		OnAbilityUsed,
		OnNormalAttack,
		OnShipGetHit,
		OnShipGetRevealed,
		OnShipBecomeVisible,
	}



	[System.Flags]
	public enum ActionKeyword : long {

		None = 0,

		// Tile
		Unrevealed = 1L << 0,
		Revealed = 1L << 1,
		Hit = 1L << 2,
		Sunk = 1L << 3,
		Stone = 1L << 4,
		Ship = 1L << 5,
		VisibleShip = 1L << 6,
		InvisibleShip = 1L << 7,

		// Action
		Self = 1L << 8,
		BreakIfFail = 1L << 9,
		BreakIfSuccess = 1L << 10,

	}


	[System.Flags]
	public enum EntranceKeyword : long {

		None = 0,

		HitShip = 1L << 0,
		RevealWater = 1L << 1,
		ReavealShip = 1L << 2,
		SonarReaveal = 1L << 3,

		Reaveal = RevealWater | ReavealShip | SonarReaveal,
		Miss = RevealWater | SonarReaveal,
		All = HitShip | RevealWater | ReavealShip | SonarReaveal,

	}





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



	[System.Serializable]
	public class Ship : ISerializationCallbackReceiver {


		// Ser-Api
		public string GlobalName = "";
		public string DisplayName = "";
		public string Discription = "";
		public int DefaultCooldown = 1;
		public int MaxCooldown = 1;
		public string Body = "1";

		// Data-Api
		[System.NonSerialized] public int GlobalCode = 0;
		[System.NonSerialized] public int FieldX = 0;
		[System.NonSerialized] public int FieldY = 0;
		[System.NonSerialized] public bool Flip = false;
		[System.NonSerialized] public Vector2Int[] BodyNodes = null;


		// MSG
		public void OnBeforeSerialize () { }


		public void OnAfterDeserialize () {
			GlobalCode = GlobalName.AngeHash();
			FieldX = 0;
			FieldY = 0;
			Flip = false;
			BodyNodes = GetBodyNode(Body);
		}


		// API
		public Vector2Int GetFieldNodePosition (int nodeIndex) {
			var node = BodyNodes[nodeIndex];
			return new(
				FieldX + (Flip ? node.y : node.x),
				FieldY + (Flip ? node.x : node.y)
			); ;
		}


		public Ship CreateDataCopy () => JsonUtility.FromJson<Ship>(
			JsonUtility.ToJson(this, false)
		);


		// LGC
		private static Vector2Int[] GetBodyNode (string body) {
			var result = new List<Vector2Int>();
			int x = 0;
			int y = 0;
			for (int i = 0; i < body.Length; i++) {
				char c = body[i];
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



	[System.Serializable]
	public class Map {

		public int this[int x, int y] => Content[y * Size + x];
		public int Size = 8;
		public int[] Content = new int[64]; // 0:Water 1:Stone

	}



}