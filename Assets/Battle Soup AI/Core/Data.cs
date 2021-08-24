using System.Collections;
using System.Collections.Generic;



namespace BattleSoupAI {






	[System.Serializable]
	public struct Int2 {
		public static readonly Int2 Zero = new Int2(0, 0);
		public int x;
		public int y;
		public Int2 (int x, int y) {
			this.x = x;
			this.y = y;
		}
		public static Int2 operator + (Int2 a, Int2 b) {
			a.x += b.x;
			a.y += b.y;
			return a;
		}
		public static Int2 operator - (Int2 a, Int2 b) {
			a.x -= b.x;
			a.y -= b.y;
			return a;
		}
		public static Int2 operator * (Int2 a, Int2 b) {
			a.x *= b.x;
			a.y *= b.y;
			return a;
		}
		public static Int2 operator / (Int2 a, Int2 b) {
			a.x /= b.x;
			a.y /= b.y;
			return a;
		}
		public static Int2 operator * (Int2 a, int b) {
			a.x *= b;
			a.y *= b;
			return a;
		}
		public static Int2 operator / (Int2 a, int b) {
			a.x /= b;
			a.y /= b;
			return a;
		}
		public override string ToString () => $"({x},{y})";
	}


	public struct Float2 {
		public float x;
		public float y;
		public Float2 (float x, float y) {
			this.x = x;
			this.y = y;
		}
		public static Float2 operator + (Float2 a, Float2 b) {
			a.x += b.x;
			a.y += b.y;
			return a;
		}
		public static Float2 operator - (Float2 a, Float2 b) {
			a.x -= b.x;
			a.y -= b.y;
			return a;
		}
		public static Float2 operator * (Float2 a, Float2 b) {
			a.x *= b.x;
			a.y *= b.y;
			return a;
		}
		public static Float2 operator / (Float2 a, Float2 b) {
			a.x /= b.x;
			a.y /= b.y;
			return a;
		}
		public static Float2 operator * (Float2 a, float b) {
			a.x *= b;
			a.y *= b;
			return a;
		}
		public static Float2 operator / (Float2 a, float b) {
			a.x /= b;
			a.y /= b;
			return a;
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
		SunkShip = 1 << 6,
		All = GeneralWater | GeneralStone | RevealedWater | RevealedStone | RevealedShip | HittedShip | SunkShip,
	}





	#region --- Ability ---



	public enum AttackType {
		HitTile = 0,
		RevealTile = 1,
		HitWholeShip = 2,
		RevealWholeShip = 3,
		Sonar = 4,
		RevealOwnUnoccupiedTile = 5,
		RevealSelf = 6,
		DoNothing = 7,
	}



	public enum AttackTrigger {
		Picked = 0,
		TiedUp = 1,
		Random = 2,
		Break = 3,

	}


	public enum SoupEvent {

		CurrentShip_PerformAbility = 0,     // Finished
		CurrentShip_GetHit = 1,             // Finished
		CurrentShip_GetReveal = 2,          // Finished
		CurrentShip_Sunk = 3,               // Finished

		Own_PerformAbility = 4,             // Finished
		Own_NormalAttack = 5,               // Finished
		Own_TurnStart = 6,                  // Finished

		Opponent_PerformAbility = 7,        // Finished
		Opponent_NormalAttack = 8,          // Finished
		Opponent_TurnStart = 9,             // Finished

	}


	public enum EventCondition {
		None = 0,
		AliveShipCount = 1,
		SunkShipCount = 2,
		CurrentShip_HiddenTileCount = 3,
		CurrentShip_HitTileCount = 4,
		CurrentShip_RevealTileCount = 5,

	}


	public enum EventConditionCompare {
		Greater = 0,
		GreaterOrEqual = 1,
		Less = 2,
		LessOrEqual = 3,
		Equal = 4,
		NotEqual = 5,

	}



	public enum EventAction {
		PerformAttack = 0,

	}



	[System.Flags]
	public enum AttackResult {
		None = 0,
		Miss = 1 << 0,
		Hit = 1 << 1,
		Reveal = 1 << 2,
		Sunk = 1 << 3,
		Keep = 1 << 4,
	}



	[System.Serializable]
	public class Attack {

		// Api
		public bool IsHitOpponent => Type == AttackType.HitTile || Type == AttackType.HitWholeShip;
		public bool IsRevealOpponent => Type == AttackType.RevealTile || Type == AttackType.RevealWholeShip;

		// Ser-Api
		public int X;
		public int Y;
		public AttackType Type;
		public AttackTrigger Trigger;
		public Tile AvailableTarget;
		public AttackResult BreakingResult;

		// API
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

		public Attack Duplicate () => new Attack() {
			X = X,
			Y = Y,
			Type = Type,
			Trigger = Trigger,
			AvailableTarget = AvailableTarget,
			BreakingResult = BreakingResult,
		};

	}


	[System.Serializable]
	public class Event {
		public SoupEvent Type = SoupEvent.Own_NormalAttack;
		public EventCondition Condition = EventCondition.None;
		public EventConditionCompare ConditionCompare = EventConditionCompare.Equal;
		public EventAction Action = EventAction.PerformAttack;
		public bool ApplyConditionOnOpponent = false;
		public bool BreakAfterPerform = false;
		public int IntParam = 0;
		public int ActionParam = 0;
		public Event Duplicate () => new Event() {
			Type = this.Type,
			Condition = this.Condition,
			ConditionCompare = this.ConditionCompare,
			Action = this.Action,
			ApplyConditionOnOpponent = this.ApplyConditionOnOpponent,
			BreakAfterPerform = this.BreakAfterPerform,
			IntParam = this.IntParam,
			ActionParam = this.ActionParam,
		};
	}



	[System.Serializable]
	public class Ability {

		// Api
		public bool CanBeTrigger => Attacks != null && Attacks.Count > 0 && Attacks[0].Trigger != AttackTrigger.Break;
		public bool NeedAim {
			get {
				if (!_NeedAim.HasValue) {
					if (CopyOpponentLastUsed) {
						_NeedAim = true;
					} else {
						_NeedAim = false;
						int count = 0;
						foreach (var att in Attacks) {
							if (
								(att.Trigger == AttackTrigger.Picked || att.Trigger == AttackTrigger.TiedUp) &&
								(att.IsHitOpponent || att.Type == AttackType.Sonar)
							) {
								count++;
								if (count > 1) {
									_NeedAim = true;
									break;
								}
							}
						}
					}
				}
				return _NeedAim.Value;
			}
		}

		// Api-Ser
		public List<Event> Events = new List<Event>();
		public List<Attack> Attacks = new List<Attack>();
		public int Cooldown = 1;
		public bool ResetCooldownOnHit = false;
		public bool CopyOpponentLastUsed = false;

		// Data
		private bool? _NeedAim;


		// API
		public void ValidAttacks () {
			var validBreakingResult = AttackResult.Hit | AttackResult.Miss | AttackResult.Sunk | AttackResult.Reveal;
			for (int i = 0; i < Attacks.Capacity; i++) {
				var att = Attacks[i];
				att.AvailableTarget = Tile.All & att.AvailableTarget;
				att.BreakingResult = validBreakingResult & att.BreakingResult;
			}
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


		// Api
		public string GlobalID { get; set; } = "";
		public bool Symmetry {
			get {
				if (!_Symmetry.HasValue) {
					int bCount = Body.Length;
					for (int i = 0; i < bCount; i++) {
						var v0 = Body[i];
						if (v0.x == v0.y) { continue; }
						bool sFlag = false;
						for (int j = 0; j < bCount; j++) {
							if (i == j) { continue; }
							var v1 = Body[j];
							if (v0.x == v1.y && v0.y == v1.x) { sFlag = true; break; }
						}
						if (!sFlag) {
							_Symmetry = false;
							return false;
						}
					}
					_Symmetry = true;
				}
				return _Symmetry.Value;
			}
		}

		// Api-Ser
		public int TerminateHP = 0;
		public Int2[] Body = new Int2[0];
		public Ability Ability = new Ability();

		// Data
		private bool? _Symmetry = null;


		// API
		public (Int2 min, Int2 max) GetBounds (bool flip) {
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
			return flip ?
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


		public void GroundBodyToZero () {
			var (min, _) = GetBounds(false);
			if (min.x != 0 || min.y != 0) {
				for (int i = 0; i < Body.Length; i++) {
					Body[i] -= min;
				}
			}
		}


	}



	[System.Serializable]
	public struct ShipPosition {
		public Int2 Pivot;
		public bool Flip;
		public ShipPosition (int x, int y, bool flip) {
			Pivot = new Int2(x, y);
			Flip = flip;
		}
		public ShipPosition (Int2 pivot, bool flip) {
			Pivot = pivot;
			Flip = flip;
		}
		public Int2 GetPosition (int bodyX, int bodyY) => new Int2(
			Flip ? Pivot.x + bodyY : Pivot.x + bodyX,
			Flip ? Pivot.y + bodyX : Pivot.y + bodyY
		);
		public Int2 GetPosition (Int2 bodyPos) => new Int2(
			Flip ? Pivot.x + bodyPos.y : Pivot.x + bodyPos.x,
			Flip ? Pivot.y + bodyPos.x : Pivot.y + bodyPos.y
		);
	}



	#endregion




}
