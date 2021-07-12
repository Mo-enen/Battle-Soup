using System.Collections;
using System.Collections.Generic;



namespace BattleSoupAI {





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



	[System.Serializable]
	public struct SonarPosition {
		public int x;
		public int y;
		public int number;
		public SonarPosition (int x, int y, int number) {
			this.x = x;
			this.y = y;
			this.number = number;
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
		public (int x, int y) GetPosition (int targetX, int targetY, AbilityDirection direction) {
			switch (direction) {
				case AbilityDirection.Up:
					targetX += X;
					targetY += Y;
					break;
				case AbilityDirection.Right:
					targetX += Y;
					targetY -= X;
					break;
				case AbilityDirection.Down:
					targetX -= X;
					targetY -= Y;
					break;
				case AbilityDirection.Left:
					targetX -= Y;
					targetY += X;
					break;
			}
			return (targetX, targetY);
		}
	}



	[System.Serializable]
	public class Ability {

		// Api
		public bool HasActive {
			get {
				if (!_HasActive.HasValue) {
					_HasActive = false;
					foreach (var att in Attacks) {
						if (att.Trigger == AttackTrigger.Picked || att.Trigger == AttackTrigger.Random) {
							_HasActive = true;
							break;
						}
					}
				}
				return _HasActive.Value;
			}
		}
		public bool HasPassive {
			get {
				if (!_HasPassive.HasValue) {
					_HasPassive = false;
					foreach (var att in Attacks) {
						if (att.Trigger == AttackTrigger.PassiveRandom) {
							_HasPassive = true;
							break;
						}
					}
				}
				return _HasPassive.Value;
			}
		}
		public bool NeedAim {
			get {
				if (!_NeedAim.HasValue) {
					_NeedAim = false;
					int count = 0;
					foreach (var att in Attacks) {
						if (
							(att.Trigger == AttackTrigger.Picked || att.Trigger == AttackTrigger.TiedUp) &&
							(att.Type == AttackType.HitTile || att.Type == AttackType.HitWholeShip || att.Type == AttackType.Sonar)
						) {
							count++;
							if (count > 1) {
								_NeedAim = true;
								break;
							}
						}
					}
				}
				return _NeedAim.Value;
			}
		}

		// Api-Ser
		public List<Attack> Attacks = new List<Attack>();
		public int Cooldown = 1;
		public bool BreakOnSunk = false;
		public bool BreakOnMiss = false;
		public bool ResetCooldownOnHit = false;
		public bool CopyOpponentLastUsed = false;

		// Data
		private bool? _HasActive;
		private bool? _HasPassive;
		private bool? _NeedAim;

		public void CopyFrom (Ability target) {
			Attacks.Clear();
			Attacks.AddRange(target.Attacks);
			BreakOnSunk = target.BreakOnSunk;
			BreakOnMiss = target.BreakOnMiss;
		}

		public void CopyFromNull () {
			Attacks.Clear();
			BreakOnSunk = false;
			BreakOnMiss = false;
		}

	}



	public enum AbilityDirection {
		Up = 0,
		Right = 1,
		Down = 2,
		Left = 3,
	}



	#endregion




	#region --- Ship ---



	[System.Serializable]
	public class Ship {

		public int TerminateHP = 0;
		public Int2[] Body = new Int2[0];
		public Ability Ability = new Ability();

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
