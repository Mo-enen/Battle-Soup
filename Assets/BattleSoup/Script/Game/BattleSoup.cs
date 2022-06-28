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
		private readonly Dictionary<int, Ship> ShipPool = new();
		private readonly Dictionary<int, Ability> AbilityPool = new();
		private readonly List<Map> AllMaps = new();
		private eFieldRenderer RendererA = null;
		private eFieldRenderer RendererB = null;


		#endregion




		#region --- MSG ---


		// Init
		protected override void Initialize () {

			base.Initialize();

			Init_Pools();

			State = GameState.Title;
			Mode = GameMode.PvA;
			RendererA = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RendererB = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RefreshCameraView(true);



			//////////////////  TEMP  ////////////////////////////////////////////////

			State = GameState.Playing;
			RendererA.Field = SetupBattleFields(1, 0);
			RendererB.Field = SetupBattleFields(3, RendererA.Field.MapSize + 2);

			///////////////////  TEMP  ///////////////////////////////////////////////////



		}


		private void Init_Pools () {

			// Ship Data
			try {
				ShipPool.Clear();
				foreach (var folder in Util.GetFoldersIn(ShipRoot, true)) {
					try {
						int globalID = 0;
						// Info
						string infoPath = Util.CombinePaths(folder.FullName, "Info.json");
						if (Util.FileExists(infoPath)) {
							var data = JsonUtility.FromJson<Ship>(Util.FileToText(infoPath));
							if (data == null) continue;
							globalID = data.GlobalCode;
							ShipPool.TryAdd(data.GlobalCode, data);
						} else continue;
						// Ability
						string abPath = Util.CombinePaths(folder.FullName, "Ability.txt");
						if (Util.FileExists(abPath)) {
							string code = Util.FileToText(abPath);
							var exe = AbilityCompiler.Compile(code, out string error);
							if (exe != null) {
								AbilityPool.TryAdd(globalID, exe);
							} else {
								Debug.LogError(folder.FullName + "\n" + error);
							}
						}
					} catch (System.Exception ex) { Debug.LogWarning(folder.Name); Debug.LogException(ex); }
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
						var map = new Map() {
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


		// Update
		protected override void FrameUpdate () {
			base.FrameUpdate();

			// View
			RefreshCameraView();

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


		private void RefreshCameraView (bool immediately = false) {

			if (RendererA.Field == null || RendererB.Field == null) return;

			int sizeA = RendererA.Field.MapSize;
			int sizeB = RendererB.Field.MapSize;

			var (l0, _) = RendererA.Field.Local_to_Global(0, sizeA - 1);
			var (r0, _) = RendererA.Field.Local_to_Global(sizeA, 0);
			var (_, d0) = RendererA.Field.Local_to_Global(0, 0);
			var (_, u0) = RendererA.Field.Local_to_Global(sizeA, sizeA, 1);

			var (l1, _) = RendererB.Field.Local_to_Global(0, sizeB - 1);
			var (r1, _) = RendererB.Field.Local_to_Global(sizeB, 0);
			var (_, d1) = RendererB.Field.Local_to_Global(0, 0);
			var (_, u1) = RendererB.Field.Local_to_Global(sizeB, sizeB, 1);

			int minX = Mathf.Min(l0, l1);
			int maxX = Mathf.Max(r0, r1) + SoupConst.ISO_SIZE / 2;
			int minY = Mathf.Min(d0, d1);
			int maxY = Mathf.Max(u0, u1);

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


		private Field SetupBattleFields (int mapIndex, int localShiftY) {

			var field = new Field(
				new Ship[] {
					ShipPool["Longboat".AngeHash()].CreateDataCopy(),
					ShipPool["Mutineers".AngeHash()].CreateDataCopy(),
					ShipPool["Coracle".AngeHash()].CreateDataCopy(),
				},
				AllMaps[mapIndex],
				new(0, localShiftY)
			);


			return field;
		}


		#endregion





	}
}