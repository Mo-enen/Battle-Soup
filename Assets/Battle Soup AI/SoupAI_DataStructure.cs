using System.Collections;
using System.Collections.Generic;



namespace BattleSoupAI {




	#region --- Ability ---



	public enum AttackType {
		HitTile = 0,
		RevealTile = 1,
		HitWholeShip = 2,
		RevealWholeShip = 3,
		Sonar = 4,
		RevealOwnUnoccupiedTile = 5,
		RevealSelf = 6,
	}


	 
	public enum AttackTrigger {
		Picked = 0,
		TiedUp = 1,
		Random = 2,
		PassiveRandom = 3,
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
		public int Cooldown;
		public bool HasActive;
		public bool HasPassive;
		public bool BreakOnMiss;
		public bool ResetCooldownOnHit;
		public bool CopyOpponentLastUsed;
	}



	#endregion




	#region --- Tile ---



	[System.Serializable]
	public struct Int2 {
		public int x;
		public int y;
		public Int2 (int x, int y) {
			this.x = x;
			this.y = y;
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
	}



	#endregion




	#region --- Ship ---



	[System.Serializable]
	public struct Ship {


		public Int2[] Body;
		public Ability Ability;
		public int TerminateHP;


		public (Int2 min, Int2 max) GetBounds (ShipPosition pos) {
			int minX = int.MaxValue;
			int minY = int.MaxValue;
			int maxX = int.MinValue;
			int maxY = int.MinValue;
			foreach (var v in Body) {
				minX = System.Math.Min(minX, v.x);
				minY = System.Math.Min(minY, v.y);
				maxX = System.Math.Max(maxX, v.x);
				maxY = System.Math.Max(maxY, v.y);
			}
			return pos.Flip ?
				(new Int2(minY, minX), new Int2(maxY, maxX)) :
				(new Int2(minX, minY), new Int2(maxX, maxY));
		}


		public bool Contains (int x, int y, ShipPosition pos) {
			foreach (var v in Body) {
				if (
					x == (pos.Flip ? v.y : v.x) + pos.Pivot.x &&
					y == (pos.Flip ? v.x : v.y) + pos.Pivot.y
				) { return true; }
			}
			return false;
		}


	}



	[System.Serializable]
	public struct ShipPosition {
		public Int2 Pivot;
		public bool Flip;
	}



	#endregion




}
