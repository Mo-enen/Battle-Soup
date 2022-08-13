using AngeliaFramework;
namespace BattleSoup {
	public class scDealEnemyCard : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).Card_DealForEnemy();
			return StepResult.Over;
		}
	}
}