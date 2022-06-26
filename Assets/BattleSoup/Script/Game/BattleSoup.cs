using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public class BattleSoup : Game {




		#region --- SUB ---


		public enum GameState {
			Title = 0,
			Prepare = 1,
			Playing = 2,
		}


		public enum GameMode {
			PvA = 0,
			AvA = 1,
			Card = 2,
		}


		#endregion




		#region --- VAR ---


		// Api
		public GameState State { get; private set; } = GameState.Title;
		public GameMode Mode { get; private set; } = GameMode.PvA;
		public string ShipRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Ships");
		public string MapRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Maps");

		// Data
		private readonly Dictionary<int, ShipData> ShipPool = new();
		private readonly List<MapData> AllMaps = new();
		private eFieldRenderer RendererA = null;
		private eFieldRenderer RendererB = null;


		#endregion




		#region --- MSG ---


		protected override void Initialize () {

			base.Initialize();

			Init_Pools();

			State = GameState.Title;
			Mode = GameMode.PvA;
			RendererA = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RendererB = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RefreshView(true);



			/////////////////////////////////////////////////////////////////////////

			State = GameState.Playing;

			string shipName = "Mutineers";
			RendererA.Field = new Field(
				new Ship[] {
					new Ship() {
						GlobalName = shipName,
						GlobalID = shipName.AngeHash(),
						Flip = false,
						FieldY = 2,
						FieldX = 4,
						Data = ShipPool[shipName.AngeHash()],
						Body = ShipPool[shipName.AngeHash()].GetBodyArray(),
					}
				},
				AllMaps[2], new(0, 0)
			);
			



			RendererB.Field = new Field(new Ship[] {

			}, AllMaps[1], new(0, AllMaps[1].Size + 2));

			/////////////////////////////////////////////////////////////////////////



		}


		private void Init_Pools () {

			// Ship Data
			try {
				ShipPool.Clear();
				foreach (var file in Util.GetFilesIn(ShipRoot, false, "*.json")) {
					try {
						var data = JsonUtility.FromJson<ShipData>(Util.FileToText(file.FullName));
						if (data == null) continue;
						ShipPool.TryAdd(Util.GetNameWithoutExtension(file.Name).AngeHash(), data);
					} catch (System.Exception ex) { Debug.LogException(ex); }
				}
			} catch (System.Exception ex) { Debug.LogException(ex); }

			// All Maps
			try {
				AllMaps.Clear();
				foreach (var file in Util.GetFilesIn(MapRoot, false, "*.png")) {
					try {
						var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false) {
							filterMode = FilterMode.Point,
							anisoLevel = 0,
							wrapMode = TextureWrapMode.Clamp,
						};
						texture.LoadImage(Util.FileToByte(file.FullName));
						if (texture.width != texture.height) {
							Debug.LogWarning($"Map texture \"{file.Name}\" have differect width and height.");
							continue;
						}
						if (texture.width == 0 || texture.height == 0) continue;
						var pixels = texture.GetPixels32();
						var map = new MapData() {
							Size = texture.width,
							Content = new int[pixels.Length],
						};
						for (int i = 0; i < pixels.Length; i++) {
							map.Content[i] = pixels[i].r < 128 ? 1 : 0;
						}
						AllMaps.Add(map);
					} catch (System.Exception ex) { Debug.LogException(ex); }
				}
			} catch (System.Exception ex) { Debug.LogException(ex); }

		}


		protected override void FrameUpdate () {
			base.FrameUpdate();

			// View
			RefreshView();

			// Game
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




		#region --- API ---


		public void UI_OpenURL (string url) => Application.OpenURL(url);


		#endregion



		#region --- LGC ---


		private void RefreshView (bool immediately = false) {
			if (RendererA.Field == null || RendererB.Field == null) return;
			int sizeA = RendererA.Field.MapSize;

			var (minX0, _) = RendererA.Field.Local_to_Global(0, sizeA);
			var (maxX0, _) = RendererA.Field.Local_to_Global(sizeA, 0);
			var (_, minY0) = RendererA.Field.Local_to_Global(0, 0);
			var (_, maxY0) = RendererA.Field.Local_to_Global(sizeA, sizeA, 2);

			var (minX1, _) = RendererB.Field.Local_to_Global(0, sizeA);
			var (maxX1, _) = RendererB.Field.Local_to_Global(sizeA, 0);
			var (_, minY1) = RendererB.Field.Local_to_Global(0, 0);
			var (_, maxY1) = RendererB.Field.Local_to_Global(sizeA, sizeA, 2);

			int minX = Mathf.Min(minX0, minX1) - SoupConst.ISO_SIZE / 2;
			int minY = Mathf.Min(minY0, minY1);
			int maxX = Mathf.Max(maxX0, maxX1) + SoupConst.ISO_SIZE / 2;
			int maxY = Mathf.Max(maxY0, maxY1) + SoupConst.ISO_SIZE;
			var rect = new RectInt(minX, minY, maxX - minX, maxY - minY);
			rect = rect.Expand(
				SoupConst.ISO_SIZE / 2, SoupConst.ISO_SIZE / 2,
				SoupConst.ISO_SIZE / 2, SoupConst.ISO_SIZE
			);

			var targetCameraRect = rect.ToRect().Envelope((float)Screen.width / Screen.height);

			SetViewPositionDely(
				(int)(targetCameraRect.x + targetCameraRect.width / 2) - ViewRect.width / 2,
				(int)targetCameraRect.y, immediately ? 1000 : 220
			);
			SetViewSizeDely((int)targetCameraRect.height, immediately ? 1000 : 220);
		}


		#endregion





	}
}