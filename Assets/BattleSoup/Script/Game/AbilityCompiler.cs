using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BattleSoup {



	// Unit
	public abstract class ExecuteUnit {
		public int AbilityID = 0;
		public int LineIndex = 0;
	}



	public class ActionUnit : ExecuteUnit {


		public int PositionCount => Positions.Length;
		public int KeywordCount => Keywords.Length;

		public ActionType Type = ActionType.None;
		public int RandomCount = 0;
		public Vector2Int[] Positions = new Vector2Int[0];
		public ActionKeyword?[] Keywords = new ActionKeyword?[0];


		public bool TryGetKeyword (int index, out ActionKeyword keyword) {
			keyword = ActionKeyword.None;
			if (index < 0 || index >= Keywords.Length) return false;
			var _k = Keywords[index];
			if (_k.HasValue) {
				keyword = _k.Value;
				return true;
			} else {
				return false;
			}
		}


		public ActionKeyword GetMergedKeyword () {
			var result = ActionKeyword.None;
			foreach (var k in Keywords) {
				if (!k.HasValue) continue;
				result |= k.Value;
			}
			return result;
		}


	}



	public class EntranceUnit : ExecuteUnit {
		public EntranceType Type = EntranceType.OnAbilityUsed;
	}



	// Compiler
	public static class AbilityCompiler {




		#region --- VAR ---


		// Short
		private static string[] AllActionNames {
			get {
				if (_AllActionNames == null) {
					var names = new List<string>(System.Enum.GetNames(typeof(ActionType)));
					names.Sort((a, b) => b.Length.CompareTo(a.Length));
					_AllActionNames = names.ToArray();
				}
				return _AllActionNames;
			}
		}
		private static string[] _AllActionNames = null;
		private static string[] AllEntranceNames {
			get {
				if (_AllEntranceNames == null) {
					var names = new List<string>(System.Enum.GetNames(typeof(EntranceType)));
					names.Sort((a, b) => b.Length.CompareTo(a.Length));
					_AllEntranceNames = names.ToArray();
				}
				return _AllEntranceNames;
			}
		}
		private static string[] _AllEntranceNames = null;


		#endregion




		#region --- API ---


		public static Ability Compile (int id, string code, out string error) {

			error = "";
			var units = new List<ExecuteUnit>();

			// Get Units from all Lines
			var cookedEntrance = new HashSet<EntranceType>();
			var lines = GetCookedLines(code);
			for (int i = 0; i < lines.Count; i++) {
				string line = lines[i];
				var unit = GetUnitFromCookedLine(line, out error);
				if (unit != null) {
					if (unit is EntranceUnit eUnit) {
						// Check for Duplicate Entrance
						if (cookedEntrance.Contains(eUnit.Type)) {
							error = $"Duplicate entrance for {eUnit.Type}";
							return null;
						} else {
							cookedEntrance.Add(eUnit.Type);
						}
					}
					// Add Unit
					unit.AbilityID = id;
					unit.LineIndex = units.Count;
					units.Add(unit);
				} else {
					error = $"Compile Error\n" + error;
					return null;
				}
			}

			// Add OnAbilityUsed if Ability Not Start with an Entrance
			if (
				units.Count == 0 ||
				units[0] is not EntranceUnit
			) {
				// Check if Already Have OnAbilityUsed
				if (cookedEntrance.Contains(EntranceType.OnAbilityUsed)) {
					error = "Already have \"OnAbilityUsed\" entrance in code but still have no entrance at start.";
					return null;
				}
				// Add OnAbilityUsed to Start
				units.Insert(0, new EntranceUnit() {
					Type = EntranceType.OnAbilityUsed,
				});
			}

			// Result
			var result = new Ability() {
				Units = units.ToArray(),
				HasManuallyEntrance = false,
				HasPassiveEntrance = false,
				HasSolidAction = false,
				HasCopySelfAction = false,
				HasCopyOpponentAction = false,
			};
			for (int i = 0; i < units.Count; i++) {
				var unit = units[i];
				switch (unit) {
					case EntranceUnit eUnit:
						result.EntrancePool.TryAdd(eUnit.Type, i);
						switch (eUnit.Type) {
							case EntranceType.OnAbilityUsed:
							case EntranceType.OnAbilityUsedOvercharged:
								result.HasManuallyEntrance = true;
								break;
							case EntranceType.OnOpponentGetAttack:
							case EntranceType.OnSelfGetAttack:
							case EntranceType.OnOpponentShipGetHit:
							case EntranceType.OnSelfShipGetHit:
							case EntranceType.OnCurrentShipGetHit:
								result.HasPassiveEntrance = true;
								break;
						}
						break;
					case ActionUnit aUnit:
						switch (aUnit.Type) {
							case ActionType.Attack:
							case ActionType.Reveal:
							case ActionType.Unreveal:
							case ActionType.Sonar:
							case ActionType.Expand:
							case ActionType.Shrink:
							case ActionType.SunkShip:
							case ActionType.RevealShip:
							case ActionType.ExposeShip:
							case ActionType.AddCooldown:
							case ActionType.ReduceCooldown:
							case ActionType.AddMaxCooldown:
							case ActionType.ReduceMaxCooldown:
								result.HasSolidAction = true;
								break;
							case ActionType.PerformSelfLastUsedAbility:
								result.HasCopySelfAction = true;
								break;
							case ActionType.PerformOpponentLastUsedAbility:
								result.HasCopyOpponentAction = true;
								break;
						}
						break;
				}
			}
			return result;
		}


		public static List<string> GetCookedLines (string rawCode) {

			// Remove Whitespace
			rawCode = rawCode.RemoveWhitespace('\n');

			// Lines
			var lines = new List<string>(rawCode.Split('\n'));

			// Remove Empty Line
			lines.RemoveAll((str) => string.IsNullOrWhiteSpace(str));

			// Remove All Comments Line
			lines.RemoveAll((str) => str.StartsWith("//"));

			// Remove Partically Comment
			for (int i = 0; i < lines.Count; i++) {
				var str = lines[i];
				int index = str.IndexOf("//");
				if (index <= 0) continue;
				lines[i] = str[..index];
			}

			return lines;
		}


		public static ExecuteUnit GetUnitFromCookedLine (string line, out string error) {

			var entrance = CheckForEntrance(line, out error);
			if (entrance != null) return entrance;
			if (entrance == null && !string.IsNullOrEmpty(error)) return null;

			var action = CheckForAction(line, out error);
			if (action != null) return action;
			if (action == null && !string.IsNullOrEmpty(error)) return null;

			error = "Unknown keyword";
			return null;
		}


		#endregion




		#region --- LGC ---


		private static ActionUnit CheckForAction (string line, out string _error) {

			_error = "";
			var action = new ActionUnit();

			// Type
			string targetName = AllActionNames.FirstOrDefault(
				name => line.StartsWith(name, System.StringComparison.OrdinalIgnoreCase)
			);
			if (string.IsNullOrEmpty(targetName)) return null;
			if (System.Enum.TryParse<ActionType>(targetName, out var aType)) {
				action.Type = aType;
			} else {
				_error = $"Unknown Function Type for \"{line}\"";
				return null;
			}
			line = line[targetName.Length..];

			// Check for "?"
			action.RandomCount = 0;
			for (
				int i = line.IndexOf('?');
				i >= 0 && i < line.Length && line[i] == '?';
				i++
			) {
				action.RandomCount++;
			}

			// All Operations ()[]...()[]...()()()...
			var positions = new List<Vector2Int?>();
			var keywords = new List<ActionKeyword?>();
			bool hasPos = false;
			bool hasKeyword = false;
			while (!string.IsNullOrWhiteSpace(line)) {
				int oldLength = line.Length;
				line = GetPositionAndKeyword(line, out var pos, out var keyword, out _error);
				if (!string.IsNullOrEmpty(_error)) return null;
				if (line.Length >= oldLength) break;
				if (keyword.HasValue) hasKeyword = true;
				if (pos.HasValue) hasPos = true;
				positions.Add(pos);
				keywords.Add(keyword);
			}
			// Positions >> Final Positions
			var finalPositions = new List<Vector2Int>();
			if (hasPos) {
				foreach (var _pos in positions) {
					finalPositions.Add(_pos ?? Vector2Int.zero);
				}
			} else if (action.RandomCount == 0) {
				finalPositions.Add(Vector2Int.zero);
			}
			// Final
			action.Positions = finalPositions.ToArray();
			if (hasKeyword) action.Keywords = keywords.ToArray();
			return action;
		}


		private static EntranceUnit CheckForEntrance (string line, out string _error) {
			_error = "";
			var entrance = new EntranceUnit();
			string targetName = AllEntranceNames.FirstOrDefault(
				name => line.StartsWith(name, System.StringComparison.OrdinalIgnoreCase)
			);
			if (string.IsNullOrEmpty(targetName)) return null;
			line = line[targetName.Length..];
			if (System.Enum.TryParse<EntranceType>(targetName, out var eType)) {
				entrance.Type = eType;
			} else {
				_error = $"Unknown Entrance Type for \"{line}\"";
				return null;
			}
			return entrance;
		}


		private static string GetPositionAndKeyword (string line, out Vector2Int? position, out ActionKeyword? keyword, out string error) {

			error = "";

			// Get Position in ()
			position = null;
			int indexL = line.IndexOf('(');
			int indexR = line.IndexOf(')');
			if (indexL >= 0 && indexR >= 0 && indexL <= indexR) {
				int indexMid = line.IndexOf(',', indexL, indexR - indexL);
				if (
					indexMid >= 0 &&
					int.TryParse(line[(indexL + 1)..indexMid], out int x) &&
					int.TryParse(line[(indexMid + 1)..indexR], out int y)
				) {
					position = new(x, y);
				}
			}

			// Get Keywords in [,,,]
			keyword = null;
			int lineEnd = indexR >= 0 ? indexR + 1 : 0;
			int startIndex = line.IndexOf('[', indexR + 1);
			if (startIndex >= 0) {
				int endIndex = line.IndexOf(']', startIndex);
				if (endIndex >= 0) {
					lineEnd = endIndex + 1;
					var k = GetKeyword(line[(startIndex + 1)..endIndex], out error);
					if (k != ActionKeyword.None) keyword = k;
				}
			}

			return line[lineEnd.Clamp(0, line.Length - 1)..];
		}


		private static ActionKeyword GetKeyword (string line, out string error) {
			error = "";
			var result = ActionKeyword.None;
			if (string.IsNullOrWhiteSpace(line)) return ActionKeyword.None;
			var keywordStrs = line.Split(',');
			foreach (var keywordStr in keywordStrs) {
				if (System.Enum.TryParse<ActionKeyword>(keywordStr, true, out var keyword)) {
					result |= keyword;
				} else {
					error = $"Unknown keyword \"{keywordStr}\"";
				}
			}
			return result;
		}


		#endregion




	}
}
