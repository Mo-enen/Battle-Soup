using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AngeliaFramework;

namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }


namespace BattleSoup {




	public enum ActionType {

		None,

		// Position Buffer
		Pick,

		// Tile
		Attack,
		Reveal,
		Unreveal,
		Sonar,
		Expand,
		Shrink,

		// Ship
		SunkShip,
		RevealShip,
		ExposeShip,

		// Cooldown
		AddCooldown,
		ReduceCooldown,
		AddMaxCooldown,
		ReduceMaxCooldown,

		// Other
		PerformSelfLastUsedAbility,
		PerformOpponentLastUsedAbility,

	}



	public enum EntranceType {

		OnAbilityUsed,
		OnAbilityUsedOvercharged,

		OnSelfGetAttack,
		OnOpponentGetAttack,

		OnSelfShipGetHit,
		OnOpponentShipGetHit,
		OnCurrentShipGetHit,

		OnSelfShipGetSunk,
		OnOpponentShipGetSunk,
		OnCurrentShipGetSunk,

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

		Hittable = 1L << 6,

		ExposedShip = 1L << 7,
		UnexposedShip = 1L << 8,

		RevealedShip = 1L << 9,
		UnrevealedShip = 1L << 10,

		HitShip = 1L << 11,
		SunkShip = 1L << 12,

		// Target
		This = 1L << 13,
		Self = 1L << 14,
		Opponent = 1L << 15,

		// Break
		BreakIfMiss = 1L << 16,
		BreakIfHit = 1L << 17,
		BreakIfReveal = 1L << 18,
		BreakIfSunk = 1L << 19,
		BreakIfIsLastShip = 1L << 20,
		BreakIfIsNotLastShip = 1L << 21,

		// Trigger
		TriggerIfMiss = 1L << 22,
		TriggerIfHit = 1L << 23,
		TriggerIfReveal = 1L << 24,
		TriggerIfSunk = 1L << 25,
		TriggerIfIsLastShip = 1L << 26,
		TriggerIfIsNotLastShip = 1L << 27,

	}



	[System.Flags]
	public enum ActionResult : long {

		None = 0,

		Hit = 1L << 0,
		Sunk = 1L << 1,
		RevealWater = 1L << 2,
		UnrevealWater = 1L << 3,
		RevealShip = 1L << 4,
		UnrevealShip = 1L << 5,
		ExposeShip = 1L << 6,

	}


	public static class SoupConst {

		public const int ISO_WIDTH = 32 * 7;
		public const int ISO_HEIGHT = 16 * 7;
		public const int ISO_SIZE = 64 * 7;

	}


	public static class SoupUtil {

		public static Vector2Int GetPickedPosition (Vector2Int pickingPos, Direction4 pickingDir, int localX, int localY) =>
			pickingPos + pickingDir switch {
				Direction4.Down => new(-localX, -localY),
				Direction4.Left => new(-localY, localX),
				Direction4.Right => new(localY, -localX),
				_ or Direction4.Up => new(localX, localY),
			};

		public static BattleSoup.Turn Opposite (this BattleSoup.Turn turn) => 1 - turn;

		public static bool Check (this ActionKeyword keyword, Cell cell) {

			if (keyword == ActionKeyword.None) return true;

			// One-Vote-Off
			if (keyword.HasFlag(ActionKeyword.Stone)) {
				if (!cell.HasStone) return false;
			}
			if (keyword.HasFlag(ActionKeyword.NoStone)) {
				if (cell.HasStone) return false;
			}

			if (keyword.HasFlag(ActionKeyword.Ship)) {
				if (cell.ShipIndex < 0) return false;
			}
			if (keyword.HasFlag(ActionKeyword.NoShip)) {
				if (cell.ShipIndex >= 0) return false;
			}

			if (keyword.HasFlag(ActionKeyword.ExposedShip)) {
				if (!cell.HasExposedShip) return false;
			}
			if (keyword.HasFlag(ActionKeyword.UnexposedShip)) {
				if (cell.ShipIndex < 0 || cell.HasExposedShip) return false;
			}

			// One-Vote-In
			bool hasKeyword = false;
			if (keyword.HasFlag(ActionKeyword.NormalWater)) {
				if (cell.State == CellState.Normal) return true;
				hasKeyword = true;
			}
			if (keyword.HasFlag(ActionKeyword.RevealedWater)) {
				if (cell.State == CellState.Revealed && cell.ShipIndex < 0) return true;
				hasKeyword = true;
			}
			if (keyword.HasFlag(ActionKeyword.RevealedShip)) {
				if (cell.State == CellState.Revealed && cell.ShipIndex >= 0) return true;
				hasKeyword = true;
			}
			if (keyword.HasFlag(ActionKeyword.UnrevealedShip)) {
				if (cell.State == CellState.Normal && cell.ShipIndex >= 0) return true;
				hasKeyword = true;
			}
			if (keyword.HasFlag(ActionKeyword.HitShip)) {
				if (cell.State == CellState.Hit && cell.ShipIndex >= 0) return true;
				hasKeyword = true;
			}
			if (keyword.HasFlag(ActionKeyword.SunkShip)) {
				if (cell.State == CellState.Sunk && cell.ShipIndex >= 0) return true;
				hasKeyword = true;
			}
			if (keyword.HasFlag(ActionKeyword.Hittable)) {
				if (!cell.HasStone && cell.State == CellState.Normal) return true;
				if (!cell.HasStone && cell.State == CellState.Revealed) return true;
				hasKeyword = true;
			}

			return !hasKeyword;
		}

		public static bool CheckTrigger (this ActionKeyword keyword, ActionResult result, int shipCount) {
			bool hasTrigger = false;
			if (keyword.HasFlag(ActionKeyword.TriggerIfHit)) {
				if (result == ActionResult.Hit) return true;
				hasTrigger = true;
			}
			if (keyword.HasFlag(ActionKeyword.TriggerIfMiss)) {
				if (result == ActionResult.RevealWater) return true;
				hasTrigger = true;
			}
			if (keyword.HasFlag(ActionKeyword.TriggerIfReveal)) {
				if (result == ActionResult.RevealShip) return true;
				hasTrigger = true;
			}
			if (keyword.HasFlag(ActionKeyword.TriggerIfSunk)) {
				if (result == ActionResult.Sunk) return true;
				hasTrigger = true;
			}
			if (keyword.HasFlag(ActionKeyword.TriggerIfIsLastShip)) {
				if (shipCount == 1) return true;
				hasTrigger = true;
			}
			if (keyword.HasFlag(ActionKeyword.TriggerIfIsNotLastShip)) {
				if (shipCount != 1) return true;
				hasTrigger = true;
			}
			return !hasTrigger;
		}

		public static bool CheckBreak (this ActionKeyword keyword, ActionResult result, int shipCount) {
			bool _break = false;
			if (keyword.HasFlag(ActionKeyword.BreakIfHit)) {
				_break = _break || result == ActionResult.Hit;
			}
			if (keyword.HasFlag(ActionKeyword.BreakIfMiss)) {
				_break = _break || result == ActionResult.RevealWater;
			}
			if (keyword.HasFlag(ActionKeyword.BreakIfReveal)) {
				_break = _break || result == ActionResult.RevealShip;
			}
			if (keyword.HasFlag(ActionKeyword.BreakIfSunk)) {
				_break = _break || result == ActionResult.Sunk;
			}
			if (keyword.HasFlag(ActionKeyword.BreakIfIsLastShip)) {
				_break = _break || shipCount == 1;
			}
			if (keyword.HasFlag(ActionKeyword.BreakIfIsNotLastShip)) {
				_break = _break || shipCount != 1;
			}
			return _break;
		}

		public static bool IsManualEntrance (this EntranceType entrance) => entrance == EntranceType.OnAbilityUsed || entrance == EntranceType.OnAbilityUsedOvercharged;

	}



	[System.Serializable]
	public class Ship : ISerializationCallbackReceiver {


		// Ser-Api
		public string GlobalName = "";
		public string DisplayName = "";
		public string Description = "";
		public int DefaultCooldown = 1;
		public int MaxCooldown = 1;
		public string Body = "1";

		// Data-Api
		[System.NonSerialized] public int GlobalCode = 0;
		[System.NonSerialized] public int FieldX = 0;
		[System.NonSerialized] public int FieldY = 0;
		[System.NonSerialized] public int CurrentCooldown = 1;
		[System.NonSerialized] public bool BuiltIn = true;
		[System.NonSerialized] public bool Flip = false;
		[System.NonSerialized] public bool Exposed = false;
		[System.NonSerialized] public bool Valid = true;
		[System.NonSerialized] public bool Alive = true;
		[System.NonSerialized] public bool IsSymmetric = false;
		[System.NonSerialized] public Vector3Int[] BodyNodes = null;
		[System.NonSerialized] public Sprite Icon = null;
		[System.NonSerialized] public Vector2Int BodySize = new(1, 1);

		// Cache
		private static readonly HashSet<Vector2Int> c_Symmetric = new();



		// MSG
		public void OnBeforeSerialize () {
			Body = Nodes_to_BodyString(BodyNodes);
		}


		public void OnAfterDeserialize () {
			GlobalCode = GlobalName.AngeHash();
			FieldX = 0;
			FieldY = 0;
			Flip = false;
			BodyNodes = BodyString_to_Nodes(Body);
			IsSymmetric = GetIsSymmetric();
			BodySize = new(1, 1);
			foreach (var node in BodyNodes) {
				BodySize.x = Mathf.Max(BodySize.x, node.x + 1);
				BodySize.y = Mathf.Max(BodySize.y, node.y + 1);
			}
		}


		// API
		public Vector2Int GetFieldNodePosition (int nodeIndex) {
			var node = BodyNodes[nodeIndex];
			return new(
				FieldX + (Flip ? node.y : node.x),
				FieldY + (Flip ? node.x : node.y)
			);
		}


		public Ship CreateDataCopy () {
			var newShip = JsonUtility.FromJson<Ship>(JsonUtility.ToJson(this, false));
			newShip.Icon = Icon;
			newShip.BuiltIn = BuiltIn;
			return newShip;
		}


		// LGC
		private static Vector3Int[] BodyString_to_Nodes (string body) {
			var result = new List<Vector3Int>();
			int x = 0;
			int y = 0;
			for (int i = 0; i < body.Length; i++) {
				char c = body[i];
				switch (c) {
					case '0':
						x++;
						break;
					default:
						result.Add(new(x, y, Char2Int(c)));
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


		private static string Nodes_to_BodyString (Vector3Int[] nodes) {
			if (nodes == null || nodes.Length == 0) return "";
			var hash = new Dictionary<Vector2Int, int>();
			var min = new Vector2Int(int.MaxValue, int.MaxValue);
			var max = new Vector2Int(int.MinValue, int.MinValue);
			foreach (var node in nodes) {
				var node2 = new Vector2Int(node.x, node.y);
				min = Vector2Int.Min(min, node2);
				max = Vector2Int.Max(max, node2);
				hash.TryAdd(node2, node.z);
			}
			var size = max - min + Vector2Int.one;
			var builder = new StringBuilder();
			for (int y = 0; y < size.y; y++) {
				for (int x = 0; x < size.x; x++) {
					builder.Append(
						hash.TryGetValue(min + new Vector2Int(x, y), out int value) ? Int2Char(value) : '0'
					);
				}
				if (y != size.y - 1) builder.Append(',');
			}
			return builder.ToString();
		}


		private bool GetIsSymmetric () {
			c_Symmetric.Clear();
			foreach (var node in BodyNodes) c_Symmetric.TryAdd(new(node.x, node.y));
			foreach (var node in BodyNodes) if (!c_Symmetric.Contains(new(node.y, node.x))) return false;
			return true;
		}


		private static int Char2Int (char c) {
			if (c >= '0' && c <= '9') {
				return c - '0';
			} else if (c >= 'a' && c <= 'z') {
				return c - 'a' + 10;
			} else if (c >= 'A' && c <= 'Z') {
				return c - 'A' + 36;
			}
			return 0;
		}


		private static char Int2Char (int i) {
			if (i >= 0 && i < 10) {
				return (char)(i + '0');
			} else if (i >= 10 && i < 36) {
				return (char)((i - 10) + 'a');
			} else if (i >= 36 && i < 36 + 26) {
				return (char)((i - 36) + 'A');
			}
			return '0';
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
		public bool HasExposedShip = false;
		public int Sonar = 0;
		public readonly List<int> ShipIndexs = new(16);
		public readonly List<int> ShipRenderIDs = new(16);
		public readonly List<int> ShipRenderIDsAdd = new(16);
		public readonly List<int> ShipRenderIDsSunk = new(16);

		public void AddShip (int shipIndex, Ship ship, int bodyX, int bodyY, int bodyZ) {
			ShipIndexs.Add(shipIndex);

			string baseName = ship.BuiltIn ? ship.GlobalName : "Custom Ship";
			string subName = ship.BuiltIn ? $"{bodyX}.{bodyY}" : $"{bodyZ}";

			int rID = $"{baseName} {subName}".AngeHash();
			if (!CellRenderer.TryGetSprite(rID, out _)) rID = $"DefaultShip".AngeHash();
			ShipRenderIDs.Add(rID);

			rID = $"{baseName}_Add {subName}".AngeHash();
			if (!CellRenderer.TryGetSprite(rID, out _)) rID = $"DefaultShip_Add".AngeHash();
			ShipRenderIDsAdd.Add(rID);

			rID = $"{baseName}_Sunk {subName}".AngeHash();
			if (!CellRenderer.TryGetSprite(rID, out _)) rID = $"DefaultShip_Sunk".AngeHash();
			ShipRenderIDsSunk.Add(rID);
		}

		public void ClearShip () {
			ShipIndexs.Clear();
			ShipRenderIDs.Clear();
			ShipRenderIDsAdd.Clear();
			ShipRenderIDsSunk.Clear();
		}


	}


	public struct PerformResult {
		public static readonly PerformResult ATTACK = new(-1);
		public Vector2Int Position;
		public int AbilityIndex;
		public Direction4 Direction;
		public PerformResult (int abilityIndex) {
			AbilityIndex = abilityIndex;
			Position = default;
			Direction = default;
		}
	}


}