using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;


// Start Remake at 2022/6/23
namespace BattleSoup {
	public class BattleSoup : Game {




		#region --- SUB ---



		public enum GameState {

			Title = 0,

			SelectMap = 1,
			SelectShip = 2,
			PlaceShip = 3,
			Playing = 4,

			CardGame = 5,

			ShipEditor = 6,

		}



		public enum GameMode {
			PvA = 0,
			AvA = 1,
			Card = 2,
		}



		[System.Serializable]
		private class GameAsset {
			public RectTransform PanelRoot = null;
			public RectTransform CornerSoup = null;
			public Toggle SoundTG = null;
			public Toggle AutoPlayAvATG = null;
		}



		#endregion




		#region --- VAR ---


		// Api
		public GameState State { get; private set; } = GameState.Title;
		public GameMode Mode { get; private set; } = GameMode.PvA;
		public string ShipRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Ships");
		public string MapRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Maps");
		public bool UseSound { get => s_UseSound.Value; set => s_UseSound.Value = value; }
		public bool AutoPlayForAvA { get => s_AutoPlayForAvA.Value; set => s_AutoPlayForAvA.Value = value; }

		// Ser
		[SerializeField] GameAsset m_Assets = null;

		// Data
		private readonly Dictionary<int, Ship> ShipPool = new();
		private readonly Dictionary<int, Ability> AbilityPool = new();
		private readonly List<Map> AllMaps = new();
		private eFieldRenderer RendererA = null;
		private eFieldRenderer RendererB = null;

		// Saving
		private readonly SavingBool s_UseSound = new("BattleSoup.UseSound", true);
		private readonly SavingBool s_AutoPlayForAvA = new("BattleSoup.AutoPlayForAvA", false);


		#endregion




		#region --- MSG ---


		// Init
		protected override void Initialize () {

			base.Initialize();

			ReloadShipPoolFromDisk();
			ReloadMapPoolFromDisk();

			SwitchState(GameState.Title);
			SwitchMode(GameMode.PvA);

			AddEntity(typeof(eRainningCoracleAnimation).AngeHash(), 0, 0);
			RendererA = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RendererB = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RendererA.HideInvisibleShip = false;
			RendererB.HideInvisibleShip = true;

			RefreshCameraView(true);
		}


		// Update
		protected override void FrameUpdate () {
			base.FrameUpdate();
			RefreshCameraView();
		}


		#endregion




		#region --- API ---


		public void UI_OpenURL (string url) => Application.OpenURL(url);


		public void UI_SwitchState (int state) => SwitchState((GameState)state);


		public void UI_SwitchMode (int mode) => SwitchMode((GameMode)mode);


		public void UI_RefreshSettingUI () {
			m_Assets.SoundTG.SetIsOnWithoutNotify(s_UseSound.Value);
			m_Assets.AutoPlayAvATG.SetIsOnWithoutNotify(s_AutoPlayForAvA.Value);
		}


		public void UI_OpenReloadDialog () {
			ReloadShipPoolFromDisk();
			ReloadMapPoolFromDisk();
		}


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

			for (int i = 0; i < field.MapSize; i++) {
				for (int j = 0; j < field.MapSize; j++) {
					//var cell = field[i, j];



				}
			}

			return field;
		}


		// Setting 
		private void SwitchState (GameState state) {
			State = state;
			int count = m_Assets.PanelRoot.childCount;
			for (int i = 0; i < count; i++) {
				m_Assets.PanelRoot.GetChild(i).gameObject.SetActive(i == (int)state);
			}
			m_Assets.CornerSoup.gameObject.SetActive(state != GameState.Title);
		}


		private void SwitchMode (GameMode mode) => Mode = mode;


		// Load Data
		private void ReloadShipPoolFromDisk () {
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
		}


		private void ReloadMapPoolFromDisk () {
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


		#endregion





	}
}