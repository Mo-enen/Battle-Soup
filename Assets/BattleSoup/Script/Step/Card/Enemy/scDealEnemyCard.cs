using AngeliaFramework;
namespace BattleSoup {
	public class scDealEnemyCard : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).Card_DealForEnemy();
			AudioPlayer.PlaySound("Warning".AngeHash(), 0.6f);
			AudioPlayer.PlaySound("Iron".AngeHash(), 0.6f);
			return StepResult.Over;
		}
	}
}