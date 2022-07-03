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
			Prepare = 1,
			Playing = 2,
			CardGame = 3,
			ShipEditor = 4,
		}



		public enum GameMode {
			PvA = 0,
			AvA = 1,
		}



		[System.Serializable]
		private class GameAsset {
			public RectTransform PanelRoot = null;
			public RectTransform CornerSoup = null;
			public Toggle SoundTG = null;
			public Toggle AutoPlayAvATG = null;
			public RectTransform NoShipAlert = null;
			public RectTransform NoMapAlert = null;
			public RectTransform PreparePanel = null;
			public RectTransform PlacePanel = null;
			public Sprite DefaultShipIcon = null;
			public Button MapShipSelectorNextButton = null;
			public Sprite PlusSprite = null;
			[Header("Map")]
			public RectTransform MapSelectorContentA = null;
			public Text MapSelectorLabelA = null;
			public RectTransform MapSelectorContentB = null;
			public Text MapSelectorLabelB = null;
			public Grabber MapSelectorItem = null;
			[Header("Fleet")]
			public RectTransform FleetSelectorPlayer = null;
			public RectTransform FleetSelectorPlayerContent = null;
			public RectTransform FleetSelectorRobotA = null;
			public Grabber FleetSelectorShipItem = null;
			public Text FleetSelectorLabelB = null;
			public RectTransform FleetRendererA = null;
			public RectTransform FleetRendererB = null;
			public Grabber FleetRendererItem = null;
		}



		#endregion




		#region --- VAR ---


		// Api
		public static BattleSoup Main => _Main != null ? _Main : (_Main = FindObjectOfType<BattleSoup>());
		public GameState State { get; private set; } = GameState.Title;
		public GameMode Mode { get; private set; } = GameMode.PvA;
		public string ShipRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Ships");
		public string MapRoot => Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Maps");
		public bool UseSound { get => s_UseSound.Value; set => s_UseSound.Value = value; }
		public bool AutoPlayForAvA { get => s_AutoPlayForAvA.Value; set => s_AutoPlayForAvA.Value = value; }

		// Ser
		[SerializeField] GameAsset m_Assets = null;

		// Data
		private static BattleSoup _Main = null;
		private readonly Dictionary<int, Ship> ShipPool = new();
		private readonly Dictionary<int, Ability> AbilityPool = new();
		private readonly List<Map> AllMaps = new();
		private eFieldRenderer RendererA = null;
		private eFieldRenderer RendererB = null;
		private int MapIndexA = 0;
		private int MapIndexB = 0;

		// Saving
		private readonly SavingBool s_UseSound = new("BattleSoup.UseSound", true);
		private readonly SavingBool s_AutoPlayForAvA = new("BattleSoup.AutoPlayForAvA", false);
		private readonly SavingString s_PlayerFleet = new("BattleSoup.PlayerFleet", "Sailboat,SeaMonster,Longboat,MiniSub");


		#endregion




		#region --- MSG ---


		// Init
		protected override void Initialize () {

			base.Initialize();

			ReloadShipDataFromDisk();
			ReloadMapDataFromDisk();

			SwitchState(GameState.Title);
			SwitchMode(GameMode.PvA);

			AddEntity(typeof(eRainningCoracleAnimation).AngeHash(), 0, 0);
			RendererA = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;
			RendererB = AddEntity(typeof(eFieldRenderer).AngeHash(), 0, 0) as eFieldRenderer;

			RefreshCameraView(true);
		}


		// Update
		protected override void FrameUpdate () {
			base.FrameUpdate();
			Update_StateRedirect();
			RefreshCameraView();
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
				case GameState.CardGame:

					break;
				case GameState.ShipEditor:

					break;
			}
		}


		private void Update_StateRedirect () {
			switch (State) {
				case GameState.Prepare:
				case GameState.Playing:
					if (ShipPool.Count == 0) {
						SwitchState(GameState.Title);
						m_Assets.NoShipAlert.gameObject.SetActive(true);
					}
					if (AllMaps.Count == 0) {
						SwitchState(GameState.Title);
						m_Assets.NoMapAlert.gameObject.SetActive(true);
					}
					break;
				case GameState.CardGame:

					break;
				case GameState.ShipEditor:
					if (AllMaps.Count == 0) {
						SwitchState(GameState.Title);
						m_Assets.NoMapAlert.gameObject.SetActive(true);
					}
					break;
			}
		}


		private void Update_Title () {
			if (RendererA.Field != null) RendererA.Field = null;
			if (RendererB.Field != null) RendererB.Field = null;
			RendererA.Enable = false;
			RendererB.Enable = false;
		}


		private void Update_Prepare () {

			bool preparing = m_Assets.PreparePanel.gameObject.activeSelf;
			m_Assets.PlacePanel.gameObject.SetActive(!preparing);

			// Map
			MapIndexA = Mathf.Clamp(MapIndexA, 0, AllMaps.Count);
			MapIndexB = Mathf.Clamp(MapIndexB, 0, AllMaps.Count);

			// Renderer
			RendererA.Enable = !preparing;
			RendererA.AllowHoveringOnShip = true;
			RendererA.AllowHoveringOnWater = false;
			RendererA.HideInvisibleShip = false;
			RendererA.ShowShips = !preparing;
			RendererA.DragToMoveShips = !preparing;

			RendererB.Enable = false;
			RendererB.AllowHoveringOnShip = false;
			RendererB.AllowHoveringOnWater = false;
			RendererB.HideInvisibleShip = false;
			RendererB.ShowShips = false;
			RendererB.DragToMoveShips = false;

			// Field
			if (RendererA.Field == null) {
				RendererA.Field = CreateField(
					MapIndexA,
					new(-AllMaps[MapIndexB].Size - 1, AllMaps[MapIndexB].Size + 1),
					Mode == GameMode.PvA ? s_PlayerFleet.Value : GetBotFleetA()
				);
				RendererA.Field.RandomPlaceShips(12);
			}

			if (RendererB.Field == null) {
				RendererB.Field = CreateField(
					MapIndexB,
					Vector2Int.zero,
					GetBotFleetB()
				);
				RendererB.Field.RandomPlaceShips(12);
			}

			// UI
			m_Assets.MapShipSelectorNextButton.interactable = RendererA.Field != null && RendererA.Field.Ships.Length > 0;

			// Switch State for AvA
			if (Mode == GameMode.AvA && !preparing) {
				SwitchState(GameState.Playing);
			}



		}


		private void Update_Playing () {




		}


		#endregion




		#region --- API ---


		public void UI_OpenURL (string url) => Application.OpenURL(url);


		public void UI_SwitchState (string state) {
			if (System.Enum.TryParse<GameState>(state, true, out var result)) {
				SwitchState(result);
			}
		}


		public void UI_SwitchMode (string mode) {
			if (System.Enum.TryParse<GameMode>(mode, true, out var result)) {
				SwitchMode(result);
			}
		}


		public void UI_RefreshSettingUI () {
			m_Assets.SoundTG.SetIsOnWithoutNotify(s_UseSound.Value);
			m_Assets.AutoPlayAvATG.SetIsOnWithoutNotify(s_AutoPlayForAvA.Value);
		}


		public void UI_OpenReloadDialog () {
			ReloadShipDataFromDisk();
			ReloadMapDataFromDisk();
		}


		public void UI_SelectingMapChanged () {
			RendererA.Field = null;
			RendererB.Field = null;
			MapIndexA = GetMapIndexFromUI(m_Assets.MapSelectorContentA);
			MapIndexB = GetMapIndexFromUI(m_Assets.MapSelectorContentB);
			// Func
			int GetMapIndexFromUI (RectTransform container) {
				int childCount = container.childCount;
				for (int i = 0; i < childCount; i++) {
					var tg = container.GetChild(i).GetComponent<Toggle>();
					if (tg != null && tg.isOn) return i;
				}
				return 0;
			}
		}


		public void UI_SelectingFleetChanged () => ReloadFleetRendererUI();


		public void UI_ClearPlayerFleetSelector () {
			s_PlayerFleet.Value = "";
			ReloadFleetRendererUI();
		}


		public void UI_ResetPlayerShipPositions () => RendererA.Field?.RandomPlaceShips(12);


		#endregion




		#region --- LGC ---


		private void RefreshCameraView (bool immediately = false) {

			bool availableA = RendererA.Field != null && RendererA.Enable;
			bool availableB = RendererB.Field != null && RendererB.Enable;

			if (!availableA && !availableB) return;

			const int MIN_WIDTH = SoupConst.ISO_SIZE * 10;
			const int MIN_HEIGHT = SoupConst.ISO_SIZE * 10;

			int sizeA = availableA ? RendererA.Field.MapSize : 0;
			int sizeB = availableB ? RendererB.Field.MapSize : 0;

			int l0 = int.MaxValue;
			int r0 = int.MinValue;
			int d0 = int.MaxValue;
			int u0 = int.MinValue;
			if (availableA) {
				(l0, _) = RendererA.Field.Local_to_Global(0, sizeA - 1);
				(r0, _) = RendererA.Field.Local_to_Global(sizeA, 0);
				(_, d0) = RendererA.Field.Local_to_Global(0, 0);
				(_, u0) = RendererA.Field.Local_to_Global(sizeA, sizeA, 1);
			}

			int l1 = int.MaxValue;
			int r1 = int.MinValue;
			int d1 = int.MaxValue;
			int u1 = int.MinValue;
			if (availableB) {
				(l1, _) = RendererB.Field.Local_to_Global(0, sizeB - 1);
				(r1, _) = RendererB.Field.Local_to_Global(sizeB, 0);
				(_, d1) = RendererB.Field.Local_to_Global(0, 0);
				(_, u1) = RendererB.Field.Local_to_Global(sizeB, sizeB, 1);
			}

			int minX = Mathf.Min(l0, l1);
			int maxX = Mathf.Max(r0, r1) + SoupConst.ISO_SIZE / 2;
			int minY = Mathf.Min(d0, d1);
			int maxY = Mathf.Max(u0, u1);

			var rect = new RectInt(minX, minY, maxX - minX, maxY - minY);
			rect = rect.Expand(
				SoupConst.ISO_SIZE / 2, SoupConst.ISO_SIZE / 2,
				SoupConst.ISO_SIZE / 2, SoupConst.ISO_SIZE
			);
			if (rect.width < MIN_WIDTH) {
				int exp = (MIN_WIDTH - rect.width) / 2;
				rect = rect.Expand(exp, exp, 0, 0);
			}
			if (rect.height < MIN_HEIGHT) {
				int exp = (MIN_HEIGHT - rect.height) / 2;
				rect = rect.Expand(0, 0, exp, exp);
			}
			var targetCameraRect = rect.ToRect().Envelope((float)Screen.width / Screen.height);

			SetViewPositionDely(
				(int)(targetCameraRect.x + targetCameraRect.width / 2) - ViewRect.width / 2,
				(int)targetCameraRect.y, immediately ? 1000 : 220
			);
			SetViewSizeDely((int)targetCameraRect.height, immediately ? 1000 : 220);
		}


		private Field CreateField (int mapIndex, Vector2Int localShift, string fleet) {
			var ships = new List<Ship>();
			var shipNames = fleet.Split(',');
			foreach (var name in shipNames) {
				if (ShipPool.TryGetValue(name.AngeHash(), out var ship)) {
					ships.Add(ship.CreateDataCopy());
				}
			}
			return new Field(
				ships.ToArray(),
				AllMaps[mapIndex],
				new(localShift.x, localShift.y)
			);
		}


		private void ReloadFleetRendererUI () {
			ReloadFleet(
				Mode == GameMode.PvA ? s_PlayerFleet : null,
				Mode == GameMode.PvA ? s_PlayerFleet.Value : GetBotFleetA(),
				m_Assets.FleetRendererA
			);
			ReloadFleet(
				null,
				GetBotFleetB(),
				m_Assets.FleetRendererB
			);
			// Func
			void ReloadFleet (SavingString fleet, string fleetStr, RectTransform container) {
				container.DestroyAllChirldrenImmediate();
				var ships = fleetStr.Split(',');
				var hori = container.GetComponent<HorizontalLayoutGroup>();
				float plusWidth = 24;
				float itemWidth = (container.rect.width - hori.padding.horizontal) / ships.Length - hori.spacing - plusWidth;
				itemWidth = itemWidth.Clamp(0, 64);
				for (int i = 0; i < ships.Length; i++) {
					int shipIndex = i;
					string shipName = ships[shipIndex];
					if (!ShipPool.TryGetValue(shipName.AngeHash(), out var ship) || ship.Icon == null) continue;
					// Spawn Item
					var grab = Instantiate(m_Assets.FleetRendererItem, container);
					grab.gameObject.SetActive(true);
					var rt = grab.transform as RectTransform;
					rt.SetAsLastSibling();
					rt.anchoredPosition3D = rt.anchoredPosition;
					rt.localRotation = Quaternion.identity;
					rt.localScale = Vector3.one;
					rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemWidth);
					var img = grab.Grab<Image>();
					var btn = grab.Grab<Button>();
					img.sprite = ship.Icon;
					btn.interactable = fleet != null;
					if (fleet != null) {
						btn.onClick.AddListener(() => {
							RemoveShipFromFleetString(fleet, shipIndex);
							ReloadFleetRendererUI();
							RendererA.Field = null;
							RendererB.Field = null;
						});
					}
					// Spawn Plus
					if (shipIndex < ships.Length - 1) {
						var plusG = new GameObject("Plus", typeof(RectTransform), typeof(Image));
						var pRt = plusG.transform as RectTransform;
						pRt.SetParent(container);
						pRt.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
						pRt.localScale = Vector3.one;
						pRt.SetAsLastSibling();
						pRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, plusWidth);
						var pImg = plusG.GetComponent<Image>();
						pImg.sprite = m_Assets.PlusSprite;
						pImg.preserveAspect = true;
					}
				}
			}
		}


		private void RemoveShipFromFleetString (SavingString sString, int index) {
			var ships = new List<string>(sString.Value.Split(','));
			if (index >= 0 && index < ships.Count) {
				ships.RemoveAt(index);
				sString.Value = ships.Count > 0 ? string.Join(',', ships) : "";
			}
		}


		// Setting 
		private void SwitchState (GameState state) {
			State = state;
			int count = m_Assets.PanelRoot.childCount;
			for (int i = 0; i < count; i++) {
				m_Assets.PanelRoot.GetChild(i).gameObject.SetActive(i == (int)state);
			}
			m_Assets.CornerSoup.gameObject.SetActive(state != GameState.Title);

			switch (state) {
				case GameState.Title:
					break;
				case GameState.Prepare:
					m_Assets.PreparePanel.gameObject.SetActive(true);
					m_Assets.PlacePanel.gameObject.SetActive(false);
					ReloadFleetRendererUI();
					break;

				case GameState.Playing:

					MapIndexA = MapIndexA.Clamp(0, AllMaps.Count - 1);
					MapIndexB = MapIndexB.Clamp(0, AllMaps.Count - 1);

					// A
					var shiftA = new Vector2Int(0, AllMaps[MapIndexB].Size + 2);
					RendererA.Enable = true;
					RendererA.ShowShips = true;
					RendererA.HideInvisibleShip = false;
					RendererA.AllowHoveringOnShip = true;
					RendererA.AllowHoveringOnWater = true;
					RendererA.DragToMoveShips = false;
					if (Mode == GameMode.PvA) {
						if (RendererA.Field == null) {
							RendererA.Field = CreateField(MapIndexA, shiftA, s_PlayerFleet.Value);
							RendererA.Field.RandomPlaceShips(12);
						}
					} else {
						RendererA.Field = CreateField(MapIndexA, shiftA, GetBotFleetA());
						RendererA.Field.RandomPlaceShips(12);
					}
					RendererA.Field.LocalShift = shiftA;

					// B
					RendererB.Enable = true;
					RendererB.ShowShips = true;
					RendererB.HideInvisibleShip = true;
					RendererB.AllowHoveringOnShip = false;
					RendererB.AllowHoveringOnWater = true;
					RendererB.DragToMoveShips = false;
					RendererB.Field = CreateField(
						MapIndexB,
						Vector2Int.zero,
						GetBotFleetB()
					);
					bool success = RendererB.Field.RandomPlaceShips(12);
					if (!success) {
						Debug.LogWarning("Robot B Failed to Place Ships");
						SwitchState(GameState.Title);
					}

					break;

				case GameState.CardGame:
					break;

				case GameState.ShipEditor:
					break;

			}
		}


		private void SwitchMode (GameMode mode) {
			Mode = mode;
			switch (mode) {
				case GameMode.PvA:
					m_Assets.MapSelectorLabelA.text = "Your Map";
					m_Assets.MapSelectorLabelB.text = "Opponent Map";
					m_Assets.FleetSelectorLabelB.text = "Opponent Fleet";
					m_Assets.FleetSelectorPlayer.gameObject.SetActive(true);
					m_Assets.FleetSelectorRobotA.gameObject.SetActive(false);
					ReloadFleetRendererUI();
					break;
				case GameMode.AvA:
					m_Assets.MapSelectorLabelA.text = "Robot A Map";
					m_Assets.MapSelectorLabelB.text = "Robot B Map";
					m_Assets.FleetSelectorLabelB.text = "Robot B Fleet";
					m_Assets.FleetSelectorPlayer.gameObject.SetActive(false);
					m_Assets.FleetSelectorRobotA.gameObject.SetActive(true);
					ReloadFleetRendererUI();
					break;
			}
		}


		// Load Data
		private void ReloadShipDataFromDisk () {

			// Ship Pool
			try {
				ShipPool.Clear();
				foreach (var folder in Util.GetFoldersIn(ShipRoot, true)) {
					try {
						int globalID = 0;
						Ship ship = null;
						// Info
						string infoPath = Util.CombinePaths(folder.FullName, "Info.json");
						if (Util.FileExists(infoPath)) {
							ship = JsonUtility.FromJson<Ship>(Util.FileToText(infoPath));
							if (ship == null) continue;
							globalID = ship.GlobalCode;
							ShipPool.TryAdd(ship.GlobalCode, ship);
						} else continue;
						// Icon
						if (ship != null) {
							string iconPath = Util.CombinePaths(folder.FullName, "Icon.png");
							if (Util.FileExists(iconPath)) {
								var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false) {
									filterMode = FilterMode.Bilinear,
									anisoLevel = 0,
									wrapMode = TextureWrapMode.Clamp,
								};
								texture.LoadImage(Util.FileToByte(iconPath));
								ship.Icon = Sprite.Create(
									texture,
									new Rect(0, 0, texture.width, texture.height),
									Vector2.one * 0.5f
								);
							}
							if (ship.Icon == null) ship.Icon = m_Assets.DefaultShipIcon;
						}
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

			// Ship UI
			m_Assets.FleetSelectorPlayerContent.DestroyAllChirldrenImmediate();
			foreach (var (_, ship) in ShipPool) {
				var grab = Instantiate(m_Assets.FleetSelectorShipItem, m_Assets.FleetSelectorPlayerContent);
				var rt = grab.transform as RectTransform;
				var img = grab.Grab<Image>();
				img.sprite = ship.Icon != null ? ship.Icon : m_Assets.DefaultShipIcon;
				var label = grab.Grab<Text>();
				label.text = ship.DisplayName;
				var btn = grab.Grab<Button>();
				btn.onClick.AddListener(() => {
					if (string.IsNullOrEmpty(s_PlayerFleet.Value) || s_PlayerFleet.Value.EndsWith(',')) {
						s_PlayerFleet.Value += $"{ship.GlobalName}";
					} else {
						s_PlayerFleet.Value += $",{ship.GlobalName}";
					}
					ReloadFleetRendererUI();
					RendererA.Field = null;
				});
			}
		}


		private void ReloadMapDataFromDisk () {

			// Load Data from Disk
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
				AllMaps.Sort((a, b) => a.Size.CompareTo(b.Size));
			} catch (System.Exception ex) { Debug.LogException(ex); }

			MapIndexA = MapIndexA.Clamp(0, AllMaps.Count - 1);
			MapIndexB = MapIndexB.Clamp(0, AllMaps.Count - 1);

			// Reload UI
			ReloadMapUI(m_Assets.MapSelectorItem, m_Assets.MapSelectorContentA, MapIndexA);
			ReloadMapUI(m_Assets.MapSelectorItem, m_Assets.MapSelectorContentB, MapIndexB);

			// Func
			void ReloadMapUI (Grabber itemSource, RectTransform content, int selectingIndex) {
				content.DestroyAllChirldrenImmediate();
				var group = content.GetComponent<ToggleGroup>();
				foreach (var map in AllMaps) {
					var item = Instantiate(itemSource, content);
					item.gameObject.SetActive(true);
					item.transform.SetAsLastSibling();
					var mapRenderer = item.Grab<MapRendererUI>();
					var tg = item.Grab<Toggle>();
					var label = item.Grab<Text>();
					mapRenderer.Map = map;
					tg.SetIsOnWithoutNotify(item.transform.GetSiblingIndex() == selectingIndex);
					tg.group = group;
					label.text = $"{map.Size}¡Á{map.Size}";
				}
			}

		}


		private string GetBotFleetA () => "Sailboat,SeaMonster,Longboat,MiniSub";
		private string GetBotFleetB () => "Sailboat,SeaMonster,Longboat,MiniSub";


		#endregion




	}
}