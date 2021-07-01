using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BattleSoup {




	#region --- Ability ---



	public enum AbilityType {
		None = 0,
		Active = 1,
		Passive = 2,
		CopyOwnLastUsed = 3,
		CopyOpponentLastUsed = 4,
	}



	public enum AttackType {
		DoNothing = -1,
		HitTile = 0,
		RevealTile = 1,
		HitWholeShip = 2,
		RevealWholeShip = 3,
		Sonar = 4,
	}



	public enum AttackTrigger {
		Picked = 0,
		TiedUp = 1,
		Random = 2,
	}



	[System.Serializable]
	public struct Attack {
		public int X;
		public int Y;
		public AttackType Type;
		public AttackTrigger Trigger;
		public Tile AvailableTarget;
	}



	[System.Serializable]
	public struct Ability {
		public List<Attack> Attacks;
		public AbilityType Type;
		public int Cooldown;
		public bool BreakOnMiss;
	}



	#endregion




	#region --- Tile ---



	[System.Serializable]
	public struct Int2 {
		public int X;
		public int Y;
		public Int2 (int x, int y) {
			X = x;
			Y = y;
		}
	}



	[System.Flags]
	public enum Tile {

		None = 0,

		GeneralWater = 1 << 0,
		GeneralStone = 1 << 1,
		RevealedWater = 1 << 2,
		RevealedStone = 1 << 3,
		RevealedShip = 1 << 4,
		HittedShip = 1 << 5,

		Water = RevealedWater | GeneralWater,
		Stone = RevealedStone | GeneralStone,
		Ship = RevealedShip | HittedShip,

		Revealed = RevealedWater | RevealedStone | RevealedShip,
		General = GeneralWater | GeneralStone,
		Hit = HittedShip,

		All = GeneralWater | GeneralStone | RevealedWater | RevealedStone | RevealedShip | HittedShip,

	}



	#endregion




	#region --- Ship ---



	[System.Serializable]
	public struct Ship {
		public Int2[] Body;
		public Ability Ability;
	}



	[System.Serializable]
	public struct ShipPosition {
		public Int2 Pivot;
		public bool Flip;
	}



	#endregion




}
