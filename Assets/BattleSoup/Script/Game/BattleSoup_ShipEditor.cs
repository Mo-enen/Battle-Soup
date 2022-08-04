using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;
using MonoFileBrowser;


namespace BattleSoup {
	public partial class BattleSoup {




		#region --- VAR ---


		// Data
		private int EditingID = 0;
		private int AllowTestingFrame = int.MinValue;
		private int EditingBodyNodeIndex = -1;


		#endregion




		#region --- UI ---


		public void UI_OpenCustomShipFolder () => Application.OpenURL(Util.GetUrl(CustomShipRoot));


		public void UI_ReloadShipEditorFileUI () {
			ReloadShipDataFromDisk();
			ReloadShipEditorUI();
			OnFleetChanged();
		}


		public void UI_OnBodyChanged () {
			ReloadShipDataFromDisk();
			ReloadShipEditorUI();
			OnFleetChanged(false, true);
			ReloadWorkBenchArtworkEditorUI();
		}


		public void UI_ShipEditor_DeleteCurrentShip () {
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			var folder = Util.CombinePaths(CustomShipRoot, ship.GlobalName);
			if (!Util.FolderExists(folder)) return;
			Util.DeleteFolder(folder);
			EditingID = 0;
			ReloadShipDataFromDisk();
			ReloadShipEditorUI();
			OnFleetChanged();
		}


		public void UI_ShipEditor_CreateNewCustomShip () {

			string rootPath = CustomShipRoot;
			string globalName = System.Guid.NewGuid().ToString();

			// Create Folder
			string path = Util.CombinePaths(rootPath, globalName);
			for (int safe = 0; safe < 1024 && Util.FolderExists(path); safe++) {
				globalName = System.Guid.NewGuid().ToString();
				path = Util.CombinePaths(rootPath, globalName);
			}
			if (Util.FolderExists(path)) return;
			Util.CreateFolder(path);

			// Create Info
			var info = new StringBuilder();
			info.AppendLine(@"{");
			info.AppendLine(@$"""GlobalName"": ""{globalName}"",");
			info.AppendLine(@"""DisplayName"": ""New Ship"",");
			info.AppendLine(@"""Description"": """",");
			info.AppendLine(@"""DefaultCooldown"": 1,");
			info.AppendLine(@"""MaxCooldown"": 1,");
			info.AppendLine(@"""Body"": ""44,44""");
			info.AppendLine(@"}");
			Util.TextToFile(info.ToString(), Util.CombinePaths(path, "Info.json"));

			// Create Ability
			var ability = new StringBuilder();
			ability.AppendLine("");
			ability.AppendLine("pick");
			ability.AppendLine("attack (0,0)");
			ability.AppendLine("attack (0,1)");
			ability.AppendLine("attack (1,0)");
			ability.AppendLine("attack (1,1)");
			ability.AppendLine("");
			Util.TextToFile(ability.ToString(), Util.CombinePaths(path, "Ability.txt"));

			// Create Icon
			Util.ByteToFile(
				m_Assets.DefaultShipIcon.texture.EncodeToPNG(),
				Util.CombinePaths(path, "Icon.png")
			);

			// Final
			ReloadShipDataFromDisk();
			OnFleetChanged();
			SetEditingShip(globalName.AngeHash());
			ReloadShipEditorUI();
		}


		public void UI_ShipEditor_DuplicateCurrentShip () {

			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			var folder = Util.CombinePaths(ship.BuiltIn ? BuiltInShipRoot : CustomShipRoot, ship.GlobalName);
			if (!Util.FolderExists(folder)) return;

			// Create Folder
			string rootPath = CustomShipRoot;
			string globalName = System.Guid.NewGuid().ToString();
			string path = Util.CombinePaths(rootPath, globalName);
			for (int safe = 0; safe < 1024 && Util.FolderExists(path); safe++) {
				globalName = System.Guid.NewGuid().ToString();
				path = Util.CombinePaths(rootPath, globalName);
			}
			if (Util.FolderExists(path)) return;

			// Duplicate
			Util.CopyFolder(folder, path, true, true);
			var infoPath = Util.CombinePaths(path, "Info.json");
			if (Util.FileExists(infoPath)) {
				var newShip = JsonUtility.FromJson<Ship>(Util.FileToText(infoPath));
				newShip.GlobalName = globalName;
				newShip.GlobalCode = globalName.AngeHash();
				newShip.DisplayName += " (Duplicate)";
				Util.TextToFile(JsonUtility.ToJson(newShip, true), infoPath);
			}

			// Final
			ReloadShipDataFromDisk();
			OnFleetChanged();
			EditingID = globalName.AngeHash();
			ReloadShipEditorUI();
		}


		public void UI_ShipEditor_PickAvatar () {

			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			if (ship.BuiltIn) return;
			var folder = Util.CombinePaths(CustomShipRoot, ship.GlobalName);
			if (!Util.FolderExists(folder)) return;

			AllowTestingFrame = GlobalFrame + 30;
			string path = FileBrowserUtil.PickFileDialog("Pick a image as icon", "images", "png", "jpg", "jpeg");
			if (string.IsNullOrEmpty(path) || !Util.FileExists(path)) return;

			string iconPath = Util.CombinePaths(folder, "Icon" + Util.GetExtension(path));
			Util.DeleteFile(Util.CombinePaths(folder, "Icon.png"));
			Util.DeleteFile(Util.CombinePaths(folder, "Icon.jpg"));
			Util.DeleteFile(Util.CombinePaths(folder, "Icon.jpeg"));
			Util.CopyFile(path, iconPath);

			ReloadShipDataFromDisk();
			ReloadShipEditorUI();
			OnFleetChanged();

		}


		public void UI_ShipEditor_WorkBranchChanged () {

			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			if (ship.BuiltIn) return;

			// UI >> Ship in Pool
			var grab = m_Assets.Workbench;
			var dName = grab.Grab<InputField>("DisplayName");
			var dCooldown = grab.Grab<InputField>("DefaultCooldown");
			var mCooldown = grab.Grab<InputField>("MaxCooldown");
			var description = grab.Grab<InputField>("Description");
			var body = grab.Grab<ShipBodyEditorUI>("Body");

			ship.DisplayName = dName.text;
			ship.Description = description.text;
			ship.DefaultCooldown = int.TryParse(dCooldown.text, out int _dCooldown) ? _dCooldown : ship.DefaultCooldown;
			ship.MaxCooldown = int.TryParse(mCooldown.text, out int _mCooldown) ? _mCooldown : ship.MaxCooldown;
			ship.BodyNodes = body.Nodes.ToArray();

			// Ship >> File
			string infoPath = Util.CombinePaths(CustomShipRoot, ship.GlobalName, "Info.json");
			if (Util.FileExists(infoPath)) {
				Util.TextToFile(JsonUtility.ToJson(ship, true), infoPath);
			}

			// Final
			RefreshWorkBenchUI();
		}


		public void UI_ShipEditor_ResetField () {
			OnFleetChanged();
			FieldA.GameStart();
			FieldB.GameStart();
		}


		public void UI_ShipEditor_EditAbility () {
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			var aPath = Util.CombinePaths(ship.BuiltIn ? BuiltInShipRoot : CustomShipRoot, ship.GlobalName, "Ability.txt");
			if (!Util.FileExists(aPath)) return;
			Application.OpenURL(Util.GetUrl(aPath));
		}


		public void UI_ShipEditor_CopyAbility () {
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			var aPath = Util.CombinePaths(ship.BuiltIn ? BuiltInShipRoot : CustomShipRoot, ship.GlobalName, "Ability.txt");
			if (!Util.FileExists(aPath)) return;
			GUIUtility.systemCopyBuffer = Util.FileToText(aPath);
			m_Assets.Dialog_AbilityCopy.gameObject.SetActive(true);
		}


		public void UI_ShipEditor_ReloadAbility () {
			ReloadShipDataFromDisk();
			ReloadShipEditorUI();
			OnFleetChanged();
		}


		public void UI_ShipEditor_UseAbility () {
			if (CellStep.StepCount > 0) return;
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			if (!AbilityPool.TryGetValue(EditingID, out var ability)) return;
			PerformAbility(ability, ship, EntranceType.OnAbilityUsed, FieldA, FieldB, true);
		}


		public void UI_ShipEditor_UseReveal () {
			if (CellStep.StepCount > 0) return;
			CellStep.AddToLast(new sPick(FieldB, FieldA, null, null, ActionKeyword.None, true));
			CellStep.AddToLast(new sReveal() {
				RevealOnPickedPosition = true,
				Fast = true,
				Ship = null,
				Field = FieldB,
			});
		}


		public void UI_ShipEditor_SwitchPanel (int index) => SwitchShipEditorPanel(index);


		public void UI_ShipEditor_OpenCurrentShipFolder () {
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			Application.OpenURL(Util.GetUrl(Util.CombinePaths(CustomShipRoot, ship.GlobalName)));
		}


		#endregion




		#region --- LGC ---


		private void SwitchState_ShipEditor () {

			Mode = GameMode.AvA;
			EditingID = 0;

			FieldA.Enable = false;
			FieldA.HideInvisibleShip = false;
			FieldA.DragToMoveShips = true;
			FieldA.RightClickToFlipShips = true;
			FieldA.DrawDevInfo = false;
			FieldA.ClickToAttack = false;
			FieldA.AllowHoveringOnWater = false;
			FieldA.AllowHoveringOnShip = true;
			FieldA.DrawPickingArrow = false;
			FieldA.GameStart();

			FieldB.Enable = true;
			FieldB.HideInvisibleShip = false;
			FieldB.DragToMoveShips = false;
			FieldB.RightClickToFlipShips = false;
			FieldB.DrawDevInfo = false;
			FieldB.ClickToAttack = true;
			FieldB.AllowHoveringOnWater = true;
			FieldB.AllowHoveringOnShip = false;
			FieldB.DrawPickingArrow = false;
			FieldB.GameStart();

			SwitchShipEditorPanel(0);
			SetEditingShip(0);
			ReloadShipEditorUI();
			OnMapChanged();
			OnFleetChanged();
		}


		private void Update_ShipEditor () {

			FieldB.ClickToAttack = CellStep.StepCount == 0 && GlobalFrame > AllowTestingFrame;
			FieldB.AllowHoveringOnWater = CellStep.StepCount == 0;
			m_Assets.ShipEditorUseAbilityButton.interactable = CellStep.StepCount == 0;







		}


		private void SetEditingShip (int id) {
			EditingID = id;
			FieldB.GameStart();
			OnFleetChanged();
			RefreshWorkBenchUI();
			ReloadWorkBenchArtworkEditorUI();
		}


		private void ReloadShipEditorUI () {
			var container = m_Assets.ShipEditorFileContainer;
			container.gameObject.SetActive(false);
			container.DestroyAllChirldrenImmediate();
			container.gameObject.SetActive(true);
			Toggle editingTG = null;
			for (int i = 0; i < ShipMetas.Count; i++) {
				var meta = ShipMetas[i];
				var grab = Instantiate(m_Assets.ShipEditorFileItem, container);
				grab.ReadyForInstantiate();

				var label = grab.Grab<Text>();
				label.text = meta.DisplayName;

				var tg = grab.Grab<Toggle>();
				tg.SetIsOnWithoutNotify(false);
				tg.onValueChanged.AddListener(isOn => {
					if (!isOn || !container.gameObject.activeSelf) return;
					SetEditingShip(meta.ID);
				});
				if (meta.ID == EditingID) editingTG = tg;

				var icon = grab.Grab<Image>();
				icon.sprite = meta.Icon;

			}
			if (editingTG != null) editingTG.SetIsOnWithoutNotify(true);
			RefreshWorkBenchUI();
		}


		private void RefreshWorkBenchUI () {
			if (ShipPool.TryGetValue(EditingID, out var ship)) {
				// Valid
				m_Assets.DeleteShipButton.interactable = !ship.BuiltIn;
				m_Assets.ShipEditorWorkbenchRoot.gameObject.SetActive(true);
				m_Assets.ShipEditorBuiltInMark.gameObject.SetActive(ship.BuiltIn);

				// Fill Data into UI
				var grab = m_Assets.Workbench;
				var icon = grab.Grab<Image>("Icon");
				var iconBtn = grab.Grab<Button>("Icon Button");
				var dName = grab.Grab<InputField>("DisplayName");
				var dCooldown = grab.Grab<InputField>("DefaultCooldown");
				var mCooldown = grab.Grab<InputField>("MaxCooldown");
				var description = grab.Grab<InputField>("Description");
				var body = grab.Grab<ShipBodyEditorUI>("Body");
				var resetBtn = grab.Grab<Button>("Reset Body");
				var aEditBtn = grab.Grab<Button>("Ability Edit");
				var aCopyBtn = grab.Grab<Button>("Ability Copy");
				var aReloadBtn = grab.Grab<Button>("Ability Reload");
				var aTestIcon = grab.Grab<Image>("Ability Test Icon");
				var editingBtn = grab.Grab<Button>("Editing Folder Button");

				icon.sprite = ship.Icon;
				aTestIcon.sprite = ship.Icon;
				iconBtn.interactable = !ship.BuiltIn;
				dName.SetTextWithoutNotify(ship.DisplayName);
				dName.interactable = !ship.BuiltIn;
				dCooldown.SetTextWithoutNotify(ship.DefaultCooldown.ToString());
				dCooldown.interactable = !ship.BuiltIn;
				mCooldown.SetTextWithoutNotify(ship.MaxCooldown.ToString());
				mCooldown.interactable = !ship.BuiltIn;
				description.SetTextWithoutNotify(ship.Description);
				description.interactable = !ship.BuiltIn;
				body.SetNodes(ship.BodyNodes);
				body.Interactable = !ship.BuiltIn;
				resetBtn.interactable = !ship.BuiltIn;
				aEditBtn.gameObject.SetActive(!ship.BuiltIn);
				aCopyBtn.gameObject.SetActive(ship.BuiltIn);
				aReloadBtn.gameObject.SetActive(!ship.BuiltIn);
				editingBtn.gameObject.SetActive(!ship.BuiltIn);

			} else {
				// Fail
				EditingID = 0;
				m_Assets.ShipEditorWorkbenchRoot.gameObject.SetActive(false);
			}
		}


		// Artwork
		private void ReloadShipEditorArtworkPopupUI () {
			var container = m_Assets.ShipEditorArtworkPopupContainer;
			container.DestroyAllChirldrenImmediate();
			for (int i = 1; i < CustomShipSprites.Count; i++) {

				int index = i;
				var grab = Instantiate(m_Assets.ShipEditorArtworkPopupItem, container);
				grab.ReadyForInstantiate();

				var icon = grab.Grab<Image>("Icon");
				icon.sprite = CustomShipSprites[index];

				var btn = grab.Grab<Button>();
				btn.onClick.AddListener(() => SetNode(index));

			}
			// Func
			void SetNode (int artIndex) {
				if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
				if (EditingBodyNodeIndex < 0 || EditingBodyNodeIndex >= ship.BodyNodes.Length) return;

				// Change Body
				ref var node = ref ship.BodyNodes[EditingBodyNodeIndex];
				node.z = artIndex;

				// Ship >> File
				string infoPath = Util.CombinePaths(CustomShipRoot, ship.GlobalName, "Info.json");
				if (Util.FileExists(infoPath)) {
					Util.TextToFile(JsonUtility.ToJson(ship, true), infoPath);
				}

				// Final
				m_Assets.ShipEditorArtworkPopupRoot.gameObject.SetActive(false);
				RefreshWorkBenchArtworkEditorUI();
				OnFleetChanged();
				RefreshWorkBenchUI();
			}
		}


		private void ReloadWorkBenchArtworkEditorUI () {
			var container = m_Assets.ShipEditorArtworkContainer;
			container.DestroyAllChirldrenImmediate();
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			float size = Mathf.Max(ship.BodySize.x, ship.BodySize.y);
			for (int i = 0; i < ship.BodyNodes.Length; i++) {

				int nodeIndex = i;
				var node = ship.BodyNodes[nodeIndex];
				var grab = Instantiate(m_Assets.ShipEditorArtworkItem, container);
				grab.ReadyForInstantiate();

				var rt = grab.transform as RectTransform;
				rt.anchorMin = new(node.x / size, node.y / size);
				rt.anchorMax = new((node.x + 1f) / size, (node.y + 1f) / size);
				rt.offsetMin = rt.offsetMax = Vector2.zero;
				rt.localScale = new Vector3(0.96f, 0.96f, 1f);

				var icon = grab.Grab<Image>("Icon");
				icon.sprite = node.z >= 0 && node.z < CustomShipSprites.Count ? CustomShipSprites[node.z] : null;

				var btn = grab.Grab<Button>();
				btn.onClick.AddListener(() => {
					EditingBodyNodeIndex = nodeIndex;
					m_Assets.ShipEditorArtworkPopupRoot.gameObject.SetActive(true);
				});

			}
		}


		private void RefreshWorkBenchArtworkEditorUI () {
			if (!ShipPool.TryGetValue(EditingID, out var ship)) return;
			var container = m_Assets.ShipEditorArtworkContainer;
			for (int i = 0; i < ship.BodyNodes.Length; i++) {
				var node = ship.BodyNodes[i];
				var grab = container.GetChild(i).GetComponent<Grabber>();
				var icon = grab.Grab<Image>("Icon");
				icon.sprite = node.z >= 0 && node.z < CustomShipSprites.Count ? CustomShipSprites[node.z] : null;
			}
		}


		private void SwitchShipEditorPanel (int index) {
			for (int i = 0; i < m_Assets.ShipEditorPanels.Length; i++) {
				m_Assets.ShipEditorPanels[i].gameObject.SetActive(i == index);
			}
			for (int i = 0; i < m_Assets.ShipEditorTabs.Length; i++) {
				m_Assets.ShipEditorTabs[i].targetGraphic.color = new Color(1f, 1f, 1f, i == index ? 1f : 0f);
			}
			bool artworkMode = m_Assets.ShipEditorPanels[1].gameObject.activeSelf;
			m_Assets.ShipEditorUseAbilityButton.gameObject.SetActive(!artworkMode);
			m_Assets.ShipEditorRevealButton.gameObject.SetActive(!artworkMode);
			m_Assets.ShipEditorArtworkPopupRoot.gameObject.SetActive(false);
			EditingBodyNodeIndex = -1;
			FieldA.Enable = artworkMode;
			FieldB.Enable = !artworkMode;
		}


		#endregion



	}
}