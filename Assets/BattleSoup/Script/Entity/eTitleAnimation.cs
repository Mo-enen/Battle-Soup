using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	[EntityCapacity(1)]
	[ForceUpdate]
	[ExcludeInMapEditor]
	[DontDepawnWhenOutOfRange]
	public class eRainningCoracleAnimation : Entity {




		#region --- VAR ---


		// Const
		private static readonly int CORACLE_CODE = "Coracle Title".AngeHash();

		// Data
		private Vector2Int CoracleSize = default;
		private readonly Vector3Int?[] CoraclePositions = new Vector3Int?[128];
		private BattleSoup Game = null;
		private int CurrentCount = 0;


		#endregion




		#region --- MSG ---



		public override void OnInitialize (Game game) {
			base.OnInitialize(game);
			Game = game as BattleSoup;
		}


		public override void OnActived () {
			base.OnActived();
			if (CellRenderer.TryGetSprite(CORACLE_CODE, out var sprite)) {
				CoracleSize = new(sprite.GlobalWidth, sprite.GlobalHeight);
			} else {
				CoracleSize = default;
			}
			for (int i = 0; i < CoraclePositions.Length; i++) CoraclePositions[i] = null;
			CurrentCount = 0;
		}


		public override void FrameUpdate () {

			base.FrameUpdate();

			int frame = AngeliaFramework.Game.GlobalFrame;
			const int FREQ = 40;
			const int SPEED_Y = 6;
			const int SPEED_ROT = 1;
			const int WIDTH = SoupConst.ISO_SIZE;
			int HEIGHT = WIDTH * CoracleSize.y / CoracleSize.x;

			var cameraRect = CellRenderer.CameraRect;

			// Spawn
			if (Game.State != BattleSoup.GameState.Playing && frame % FREQ == 0) {
				for (int i = 0; i < CoraclePositions.Length; i++) {
					if (!CoraclePositions[i].HasValue) {
						CoraclePositions[i] = new Vector3Int(
							Random.Range(0, 1000),
							CellRenderer.CameraRect.yMax + HEIGHT,
							Random.Range(0, 360)
						);
						CurrentCount++;
						break;
					}
				}
			}

			// Update
			int currentCount = CurrentCount;
			int index = 0;
			for (int i = 0; i < CoraclePositions.Length && index < CurrentCount; i++) {
				var pos = CoraclePositions[i];
				if (!pos.HasValue) continue;
				var _pos = pos.Value;
				float _pos01Y = Mathf.InverseLerp(cameraRect.yMin, cameraRect.yMax, _pos.y);
				var skyTint = Color32.Lerp(CellRenderer.SkyTintBottom, CellRenderer.SkyTintTop, _pos01Y);

				int scl = ((i * 199405) % 1000) + 1000;
				int speedAdd = ((i * 040471) % 10) - 5;
				CellRenderer.Draw(
					CORACLE_CODE,
					(int)Mathf.LerpUnclamped(
						cameraRect.xMin + WIDTH / 2,
						cameraRect.xMax - WIDTH / 2,
						_pos.x / 1000f
					),
					_pos.y,
					500, 500, _pos.z,
					WIDTH * scl / 1000, HEIGHT * scl / 1000,
					Color32.Lerp(Const.WHITE, skyTint, 0.85f)
				);

				_pos.y -= SPEED_Y + speedAdd;
				_pos.z += i % 2 == 0 ? SPEED_ROT : -SPEED_ROT;

				if (_pos.y < cameraRect.yMin - HEIGHT) {
					CoraclePositions[i] = null;
					currentCount--;
				} else {
					CoraclePositions[i] = _pos;
				}
				index++;
			}
			CurrentCount = currentCount;


		}


		#endregion




		#region --- API ---




		#endregion




		#region --- LGC ---




		#endregion




	}
}
