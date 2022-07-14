using AngeliaFramework;
namespace BattleSoup {
	public class sSwitchTurn : Step {
		public override StepResult FrameUpdate (Game game) {
			(game as BattleSoup).SwitchTurn();
			return StepResult.Over;
		}
	}
}