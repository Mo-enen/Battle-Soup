using AngeliaFramework;
namespace BattleSoup {
	public class scClearEnemyPerformedCard : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).Card_ClearEnemyPerformedCards();
			return StepResult.Over;
		}
	}
}