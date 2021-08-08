using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public class BattleAttacker : SoupStrategy_Advanced {



		// Const
		private const string TASK_SEARCH = "S";
		private const string TASK_ATTACK = "A";
		private const int SAILBOAT_INDEX = 0;
		private const int SEAMONSTER_INDEX = 1;
		private const int LONGBOAT_INDEX = 2;
		private const int MINISUB_INDEX = 3;

		// Api
		public override string DisplayName => "Battle Attacker";
		public override string Description => "Standard Battle Attacker strategy created by Moenen.";
		public override string[] FleetID => new string[] { "Sailboat", "SeaMonster", "Longboat", "MiniSub", };
		protected int SailBoatCooldown => Cooldowns[SAILBOAT_INDEX];
		protected int SeaMonsterCooldown => Cooldowns[SEAMONSTER_INDEX];
		protected int LongBoatCooldown => Cooldowns[LONGBOAT_INDEX];
		protected int MiniSubCooldown => Cooldowns[MINISUB_INDEX];



		// API
		protected override string GetTask (BattleInfo info) {
			if (UsingAbilityIndex >= 0) {
				// Longboat Attacking
				return TASK_ATTACK;
			} else if (ExposedShipCount == 0) {
				// Ships All Hidden
				return TASK_SEARCH;
			} else {
				// Has Ship Exposed
				return TASK_ATTACK;
			}
		}


		protected override AnalyseResult PerformTask (BattleInfo oppInfo, string taskID) => taskID switch {
			TASK_SEARCH => PerformTask_Search(oppInfo),
			TASK_ATTACK => PerformTask_Attack(oppInfo),
			_ => AnalyseResult.NotPerformed,
		};


		// LGC
		private AnalyseResult PerformTask_Search (BattleInfo info) {
			var result = AnalyseResult.None;




			return result;
		}


		private AnalyseResult PerformTask_Attack (BattleInfo info) {
			var result = AnalyseResult.None;




			return result;
		}


	}
}
