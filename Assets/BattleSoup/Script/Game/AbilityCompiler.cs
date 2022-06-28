using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BattleSoup {


	// Unit
	public abstract class ExecuteUnit { }


	public class ActionUnit : ExecuteUnit {
		public ActionType Type = ActionType.None;
		public ActionKeyword[] Keywords = new ActionKeyword[0];
		public int X = 0;
		public int Y = 0;
		public int RandomCount = 0;
	}


	public class EntranceUnit : ExecuteUnit {
		public EntranceType Type = EntranceType.OnAbilityUsed;
		public EntranceKeyword Keyword = EntranceKeyword.All;
	}



	// Compiler
	public static class AbilityCompiler {




		#region --- VAR ---


		// Short
		private static string[] AllActionNames => _AllActionNames ??= System.Enum.GetNames(typeof(ActionType));
		private static string[] _AllActionNames = null;
		private static string[] AllEntranceNames => _AllEntranceNames ??= System.Enum.GetNames(typeof(EntranceType));
		private static string[] _AllEntranceNames = null;
		private static ActionKeyword A_KEYWORD_ALL {
			get {
				if (_A_KEYWORD_ALL == ActionKeyword.None) {
					foreach (var value in System.Enum.GetValues(typeof(ActionKeyword))) {
						_A_KEYWORD_ALL |= (ActionKeyword)value;
					}
				}
				return _A_KEYWORD_ALL;
			}
		}
		private static ActionKeyword _A_KEYWORD_ALL = ActionKeyword.None;
		private static EntranceKeyword E_KEYWORD_ALL {
			get {
				if (_E_KEYWORD_ALL == EntranceKeyword.None) {
					foreach (var value in System.Enum.GetValues(typeof(EntranceKeyword))) {
						_E_KEYWORD_ALL |= (EntranceKeyword)value;
					}
				}
				return _E_KEYWORD_ALL;
			}
		}
		private static EntranceKeyword _E_KEYWORD_ALL = EntranceKeyword.None;


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
							error = $"Duplicate entrance at Line:{i}.";
							return null;
						} else {
							cookedEntrance.Add(eUnit.Type);
						}
					}
					// Add Unit
					units.Add(unit);
				} else {
					error = $"Compile Error at Line:{i}\n" + error;
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
					Keyword = EntranceKeyword.None,
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

			var lines = new List<string>(
				rawCode.RemoveWhitespace('\n').Split('\n')
			);

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

			var action = CheckForAction(line, out error);
			if (action != null) return action;
			if (action == null && !string.IsNullOrEmpty(error)) return null;

			var entrance = CheckForEntrance(line, out error);
			if (entrance != null) return entrance;
			if (entrance == null && !string.IsNullOrEmpty(error)) return null;

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

			// Position Inside ()
			if (line.Length > 2 && line[0] == '(') {
				int closeIndex = line.IndexOf(')');
				if (closeIndex < 0) {
					_error = "\")\" not found";
					return null;
				}
				string posStr = line[1..closeIndex];
				if (!string.IsNullOrWhiteSpace(posStr)) {
					int cIndex = posStr.IndexOf(',');
					if (
						cIndex >= 0 &&
						int.TryParse(posStr[0..cIndex], out int _x) &&
						int.TryParse(posStr[(cIndex + 1)..], out int _y)
					) {
						// Number
						action.X = _x;
						action.Y = _y;
						action.RandomCount = 0;
					} else if (posStr.All(c => c == '?')) {
						// ???
						action.X = 0;
						action.Y = 0;
						action.RandomCount = posStr.Length;
					}
				}
			}

			// Keywords Inside []
			var words = new List<ActionKeyword>();
			while (line.Length >= 2 && line[0] == '[') {
				int closeIndex = line.IndexOf(']');
				if (closeIndex < 0) {
					_error = "\"]\" not found";
					return null;
				}
				var word = ActionKeyword.None;
				string keywordStr = line[1..closeIndex];
				if (!string.IsNullOrWhiteSpace(keywordStr)) {
					var keywords = keywordStr.Split(',');
					for (int i = 0; i < keywords.Length; i++) {
						string keyword = keywords[i];
						bool opposite = false;
						if (keyword.StartsWith('!')) {
							keyword = keyword[1..];
							opposite = true;
						}
						if (System.Enum.TryParse<ActionKeyword>(keyword, true, out var _word)) {
							// Tile Keywords
							if (opposite) _word = ~A_KEYWORD_ALL ^ ~_word;
							word |= _word;
						}
					}
				}
				if (word != ActionKeyword.None) words.Add(word);
				line = line[(closeIndex + 1)..];
				if (string.IsNullOrWhiteSpace(line)) break;
			}
			action.Keywords = words.ToArray();
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
			var word = EntranceKeyword.None;
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
						bool opposite = false;
						if (keyword.StartsWith('!')) {
							keyword = keyword[1..];
							opposite = true;
						}
						if (System.Enum.TryParse<EntranceKeyword>(keyword, true, out var _word)) {
							// Tile Keywords
							if (opposite) _word = ~E_KEYWORD_ALL ^ ~_word;
							word |= _word;
						}
					}
				}
			}
			if (word == EntranceKeyword.None) word = EntranceKeyword.All;

			// Final
			entrance.Keyword = word;
			return entrance;
		}


		#endregion




	}
}
