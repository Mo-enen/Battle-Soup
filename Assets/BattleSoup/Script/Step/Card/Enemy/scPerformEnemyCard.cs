using AngeliaFramework;
namespace BattleSoup {
	public class scPerformEnemyCard : Step {
		public override StepResult FrameUpdate (Game game) {
			if (LocalFrame == 0) {
				var soup = game as BattleSoup;
				bool performed = soup.Card_PerformEnemyCard();
				if (performed) {
					soup.CardAssets.EnemyAni.SetTrigger("Perform");
					AudioPlayer.PlaySound("Cackle".AngeHash());
				} else {
					AudioPlayer.PlaySound("Tik".AngeHash());
				}
			}
			return LocalFrame > 40 ? StepResult.Over : StepResult.Continue;
		}
	}
}