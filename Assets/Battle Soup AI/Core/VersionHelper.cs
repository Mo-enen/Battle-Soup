using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {


	public static class VersionHelper {


		public static int GetJsonVersion (string json) {
			int vIndex = json.IndexOf("m_Version");
			if (vIndex == -1) {
				return 0;
			} else {
				int endIndex = json.IndexOf(',', vIndex);
				int mult = 1;
				int version = 0;
				for (int i = endIndex - 1; i >= 0; i--) {
					char c = json[i];
					if (c >= '0' && c <= '9') {
						version += mult * (c - '0');
						mult *= 10;
					} else if (c != ' ') {
						break;
					}
				}
				return version;
			}
		}












	}


	// Version 0
	[System.Serializable]
	public class Ability_0 {
		public List<Attack_0> Attacks = new List<Attack_0>();
		public int Cooldown = 1;
		public bool BreakOnSunk = false;
		public bool BreakOnMiss = false;
		public bool ResetCooldownOnHit = false;
		public bool CopyOpponentLastUsed = false;
	}


	[System.Serializable]
	public struct Attack_0 {
		public int X;
		public int Y;
		public AttackType Type;
		public AttackTrigger Trigger;
		public Tile AvailableTarget;
	}


}
