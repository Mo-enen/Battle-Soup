using AngeliaFramework;
namespace BattleSoup {
	public class scWait : Step {
		public int Duration = 0;
		public scWait (int duration) => Duration = duration;
		public override StepResult FrameUpdate (Game game) => LocalFrame > Duration ? StepResult.Over : StepResult.Continue;
	}
}