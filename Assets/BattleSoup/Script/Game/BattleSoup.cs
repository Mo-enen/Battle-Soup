using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class BattleSoup : Game {




		#region --- VAR ---


		// Api
		public GameState State { get; private set; } = GameState.Title;
		public GameMode Mode { get; private set; } = GameMode.PvA;


		#endregion




		#region --- MSG ---


		protected override void Initialize () {
			base.Initialize();
			State = GameState.Title;
			Mode = GameMode.PvA;
		}


		protected override void FrameUpdate () {
			base.FrameUpdate();
			switch (State) {
				case GameState.Title:
					Update_Title();
					break;
				case GameState.Prepare:
					Update_Prepare();
					break;
				case GameState.Playing:
					Update_Playing();
					break;
			}
		}


		private void Update_Title () {



		}


		private void Update_Prepare () {



		}


		private void Update_Playing () {



		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}