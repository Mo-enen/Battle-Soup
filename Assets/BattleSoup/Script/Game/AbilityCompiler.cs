using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BattleSoup {



	// Unit
	public abstract class ExecuteUnit { }



	public class ActionUnit : ExecuteUnit {
		public class Operation {
			public int X = 0;
			public int Y = 0;
			public ActionKeyword Keyword = ActionKeyword.None;
		}
		public ActionType Type = ActionType.Clear;
		public int RandomCount = 0;
		public Operation[] Operations = new Operation[0];
	}



	public class EntranceUnit : ExecuteUnit {
		public EntranceType Type = EntranceType.OnAbilityUsed;
		public ActionResult Keyword = ActionResult.None;
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


		public static Ability Compile (string code, out string error) {

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
					Keyword = ActionResult.None,
				});
			}

			// Result
			var result = new Ability() { Units = units.ToArray() };
			for (int i = 0; i < units.Count; i++) {
				var unit = units[i];
				if (unit is EntranceUnit eUnit) {
					result.EntrancePool.TryAdd(eUnit.Type, i);
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
			var operations = new List<ActionUnit.Operation>();
			while (!string.IsNullOrWhiteSpace(line)) {
				int oldLength = line.Length;
				line = GetOperation(line, out var operation);
				if (line.Length >= oldLength) break;
				operations.Add(operation);
			}
			action.Operations = operations.ToArray();
			return action;
		}


		private static EntranceUnit CheckForEntrance (string line, out string _error) {

			_error = "";
			var entrance = new EntranceUnit();

			// Type
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

			// Keywords Inside []
			var word = ActionResult.None;
			if (line.Length >= 2 && line[0] == '[') {
				int closeIndex = line.IndexOf(']');
				if (closeIndex < 0) {
					_error = "\"]\" not found";
					return null;
				}
				string keywordStr = line[1..closeIndex];
				if (!string.IsNullOrWhiteSpace(keywordStr)) {
					var keywords = keywordStr.Split(',');
					for (int i = 0; i < keywords.Length; i++) {
						string keyword = keywords[i];
						if (System.Enum.TryParse<ActionResult>(keyword, true, out var _word)) {
							word |= _word;
						}
					}
				}
			}

			// Final
			entrance.Keyword = word;
			return entrance;
		}


		private static string GetOperation (string line, out ActionUnit.Operation operation) {

			operation = new();

			// Get Position in ()
			int indexL = line.IndexOf('(');
			int indexR = line.IndexOf(')');
			if (indexL >= 0 && indexR >= 0 && indexL <= indexR) {
				int indexMid = line.IndexOf(',', indexL, indexR - indexL);
				if (
					indexMid >= 0 &&
					int.TryParse(line[(indexL + 1)..indexMid], out int x) &&
					int.TryParse(line[(indexMid + 1)..indexR], out int y)
				) {
					operation.X = x;
					operation.Y = y;
				}
			}

			// Get Keywords in [,,,]
			int lineEnd = indexR >= 0 ? indexR + 1 : 0;
			int startIndex = line.IndexOf('[', indexR + 1);
			if (startIndex >= 0) {
				int endIndex = line.IndexOf(']', startIndex);
				if (endIndex >= 0) {
					lineEnd = endIndex + 1;
					operation.Keyword = GetKeyword(line[(startIndex + 1)..endIndex]);
				}
			}

			return line[lineEnd.Clamp(0, line.Length - 1)..];
		}


		private static ActionKeyword GetKeyword (string line) {
			var result = ActionKeyword.None;
			if (string.IsNullOrWhiteSpace(line)) return ActionKeyword.None;
			var keywordStrs = line.Split(',');
			foreach (var keywordStr in keywordStrs) {
				if (System.Enum.TryParse<ActionKeyword>(keywordStr, true, out var keyword)) {
					result |= keyword;
				}
			}
			//Debug.Log(line + "\n" + result);
			return result;
		}


		#endregion




	}
}
