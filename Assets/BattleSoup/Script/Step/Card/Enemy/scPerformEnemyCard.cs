using AngeliaFramework;
namespace BattleSoup {
	public class scPerformEnemyCard : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).Card_PerformEnemyCard();
			return StepResult.Over;
		}
	}
}