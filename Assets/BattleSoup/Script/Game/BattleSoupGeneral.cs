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
		OnAbilityUsedWithOverCooldown,
		OnNormalAttack,
		OnShipGetHit,
		OnShipGetRevealed,
		OnShipBecomeVisible,
	}



	[System.Flags]
	public enum ActionKeyword : long {

		None = 0,

		// Tile
		NormalWater = 1L << 0,
		RevealedWater = 1L << 1,

		Stone = 1L << 2,
		NoStone = 1L << 3,

		Ship = 1L << 4,
		NoShip = 1L << 5,

		RevealedShip = 1L << 6,
		HitShip = 1L << 7,
		SunkShip = 1L << 8,

		VisibleShip = 1L << 9,
		InvisibleShip = 1L << 10,

		// Action
		Self = 1L << 11,
		BreakIfMiss = 1L << 12,
		BreakIfHit = 1L << 13,
		breakIfSunk = 1L << 14,

	}



	[System.Flags]
	public enum ActionResult : long {

		None = 0,

		HitShip = 1L << 0,
		SunkShip = 1L << 1,
		RevealWater = 1L << 2,
		RevealShip = 1L << 3,
		SonarWater = 1L << 4,

		Reaveal = RevealWater | RevealShip | SonarWater,
		Hit = HitShip | SunkShip,
		Miss = RevealWater | SonarWater,
		All = HitShip | SunkShip | RevealWater | RevealShip | SonarWater,

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

		public static BattleSoup.Turn Opponent (this BattleSoup.Turn turn) => 1 - turn;

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
		[System.NonSerialized] public int CurrentCooldown = 1;
		[System.NonSerialized] public bool Visible = false;
		[System.NonSerialized] public bool Valid = true;
		[System.NonSerialized] public bool Alive = true;
		[System.NonSerialized] public Vector2Int[] BodyNodes = null;
		[System.NonSerialized] public Sprite Icon = null;



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


		public Ship CreateDataCopy () {
			var newShip = JsonUtility.FromJson<Ship>(JsonUtility.ToJson(this, false));
			newShip.Icon = Icon;
			return newShip;
		}


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



	public enum CellState {
		Normal = 0,
		Revealed = 1,
		Hit = 2,
		Sunk = 3,
	}



	public class Cell {

		public int ShipIndex => ShipIndexs.Count > 0 ? ShipIndexs[0] : -1;

		public CellState State = CellState.Normal;
		public bool HasStone = false;
		public int Sonar = 0;
		public readonly List<int> ShipIndexs = new(16);
		public readonly List<int> ShipRenderIDs = new(16);
		public readonly List<int> ShipRenderIDsAdd = new(16);
		public readonly List<int> ShipRenderIDsSunk = new(16);

		public void AddShip (int shipIndex, Ship ship, int bodyX, int bodyY) {
			ShipIndexs.Add(shipIndex);
			ShipRenderIDs.Add($"{ship.GlobalName} {bodyX}.{bodyY}".AngeHash());
			ShipRenderIDsAdd.Add($"{ship.GlobalName}_Add {bodyX}.{bodyY}".AngeHash());
			ShipRenderIDsSunk.Add($"{ship.GlobalName}_Sunk {bodyX}.{bodyY}".AngeHash());
		}

		public void ClearShip () {
			ShipIndexs.Clear();
			ShipRenderIDs.Clear();
			ShipRenderIDsAdd.Clear();
			ShipRenderIDsSunk.Clear();
		}


	}


}