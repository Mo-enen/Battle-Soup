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



			const int MapSize = 8;

			var renderer = AddEntity(typeof(BattleRenderer).AngeHash(), 0, 0) as BattleRenderer;
			renderer.Battle = new Battle(MapSize);
			renderer.LocalShift = new Vector2Int(0, 0);
			for (int i = 0; i < MapSize; i++) {
				for (int j = 0; j < MapSize; j++) {
					var cell = renderer.Battle.Cells[i, j];
					cell.Type = CellType.Water;
					if (Random.value < 0.2f) cell.Type = CellType.Stone;
				}
			}


			renderer = AddEntity(typeof(BattleRenderer).AngeHash(), 0, 0) as BattleRenderer;
			renderer.Battle = new Battle(MapSize);
			renderer.LocalShift = new Vector2Int(0, MapSize + 2);
			for (int i = 0; i < MapSize; i++) {
				for (int j = 0; j < MapSize; j++) {
					var cell = renderer.Battle.Cells[i, j];
					cell.Type = CellType.Water;
					if (Random.value < 1f) cell.Type = CellType.Stone;
				}
			}


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