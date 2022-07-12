using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	[ForceUpdate]
	[EntityCapacity(16)]
	[ExcludeInMapEditor]
	[DontDespawnWhenOutOfRange]
	public class eTag : Entity {


		// Const
		private static readonly int HIT = "Hit".AngeHash();
		private static readonly int Sunk = "Sunk".AngeHash();
		private static readonly int REVEAL = "Reveal".AngeHash();
		private static readonly int MISS = "Miss".AngeHash();
		private const int DURATION = 48;

		// Api
		public int LocalFrame { get; set; } = 0;
		public ActionResult Result { get; set; } = ActionResult.None;

		// Short
		private static int TagTypeID => _TagTypeID != 0 ? _TagTypeID : (_TagTypeID = typeof(eTag).AngeHash());
		private static int _TagTypeID = 0;

		// Data
		private static Game Game = null;
		private int ID = -1;


		// MSG
		public override void OnInitialize (Game game) {
			base.OnInitialize(game);
			Game = game;
		}


		public override void OnInactived () {
			base.OnInactived();
			ID = -1;
			LocalFrame = 0;
		}


		public override void FrameUpdate () {
			base.FrameUpdate();

			if (ID == -1) {
				ID = Result switch {
					ActionResult.Hit => HIT,
					ActionResult.Sunk => Sunk,
					ActionResult.RevealWater => MISS,
					ActionResult.RevealShip => REVEAL,
					_ => 0,
				};
				LocalFrame = 0;
				if (ID == 0) {
					Active = false;
					return;
				}
			}

			if (LocalFrame > DURATION) {
				Active = false;
				return;
			}
			if (CellRenderer.TryGetSprite(ID, out var sp)) {
				if (LocalFrame < DURATION / 2 || LocalFrame % 2 == 0) {
					const int SIZE = 48 * 7;
					CellRenderer.Draw(
						ID,
						X, Y + LocalFrame * 2,
						SIZE, SIZE * sp.GlobalHeight / sp.GlobalWidth,
						new Color(1f, 1f, 1f, Util.Remap(0, DURATION, 2f, 0f, LocalFrame).Clamp01())
					);
				}
				LocalFrame++;
			} else {
				Active = false;
				return;
			}
		}


		public static void SpawnTag (int x, int y, ActionResult result) {
			if (Game.AddEntity(TagTypeID, x, y) is eTag tag) tag.Result = result;
		}


	}
}
