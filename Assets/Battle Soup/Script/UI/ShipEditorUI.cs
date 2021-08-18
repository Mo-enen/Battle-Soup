using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UIGadget;
using UnityEngine.UI;
using Moenen.Standard;
using MonoFileBrowser;


namespace BattleSoup {
	public class ShipEditorUI : MonoBehaviour {




		#region --- SUB ---


		// Handler
		public delegate Dictionary<string, ShipData> ShipDataMapHandler ();
		public delegate void StringHandler (string str);
		public delegate void StringStringHandler (string strA, string strB);
		public delegate bool BoolStringStringHandler (string strA, string strB);

		// Data
		[System.Serializable]
		public struct InfoContentData {
			public Image Icon;
			public InputField GlobalID;
			public InputField DisplayName;
			public InputField Description;
			public InputField TerminateHP;
			public InputField Cooldown;
			public Toggle BreakOnSunk;
			public Toggle BreakOnMiss;
			public Toggle ResetCooldownOnHit;
			public Toggle CopyOpponentLastUsed;

		}


		#endregion




		#region --- VAR ---


		// Api
		public static ShipDataMapHandler GetShipDataMap { get; set; } = null;
		public static StringHandler CreateShipData { get; set; } = null;
		public static StringHandler DeleteShipData { get; set; } = null;
		public static StringStringHandler SetShipIcon { get; set; } = null;
		public static BoolStringStringHandler RenameShipData { get; set; } = null;
		public static StringHandler SaveAssetData { get; set; } = null;
		public static StringHandler OnSelectionChanged { get; set; } = null;
		public string SelectingShipID { get; private set; } = "";

		// Short
		private ShipData SelectingShipData {
			get {
				var map = GetShipDataMap();
				if (map.ContainsKey(SelectingShipID)) {
					return map[SelectingShipID];
				}
				return null;
			}
		}

		// Ser
		[SerializeField] RectTransform m_NavContent = null;
		[SerializeField] Grabber m_NavTemplate = null;
		[SerializeField] InfoContentData m_InfoContentData = default;
		[SerializeField] Toggle[] m_Titles = null;
		[SerializeField] RectTransform[] m_Panels = null;


		#endregion




		#region --- MSG ---


		private void Awake () {
			ReloadNav();
		}


		private void OnEnable () {
			RefreshContentUI();
		}


		#endregion




		#region --- API ---


		public void UI_NewShip () {

			// Get ID
			string basicName = "_New Ship";
			string id = basicName;
			var map = GetShipDataMap();
			for (int i = 1; map.ContainsKey(id); i++) {
				id = $"{basicName} ({i})";
			}

			// Create Asset
			CreateShipData(id);

			// Final
			ReloadNav();
			if (map.ContainsKey(id)) {
				SelectShip(id);
				RefreshNavUI();
				RefreshContentUI();
			}

		}


		public void UI_DeleteShip () {
			string deleteID = SelectingShipID;
			SelectFirstShip(out string secondID);
			if (string.IsNullOrEmpty(secondID)) { return; }
			if (SelectingShipID == deleteID) {
				SelectingShipID = secondID;
			}
			DeleteShipData(deleteID);
			ReloadNav();
			RefreshNavUI();
			RefreshContentUI();
		}


		// Info Content
		public void UI_RenameSelectingShip (string newName) {
			bool success = RenameShipData(SelectingShipID, newName);
			if (success) {
				ReloadNav();
				if (GetShipDataMap().ContainsKey(newName)) {
					SelectShip(newName);
					RefreshContentUI();
					RefreshNavUI();
				}
			}
		}


		public void UI_PickIcon () {
			string path = FileBrowserUtil.PickFileDialog("Select Icon", "image", "png");
			if (string.IsNullOrEmpty(path)) { return; }
			string oldSelection = SelectingShipID;
			SetShipIcon(SelectingShipID, path);
			// Final
			ReloadNav();
			if (GetShipDataMap().ContainsKey(oldSelection)) {
				SelectShip(oldSelection);
				RefreshNavUI();
				RefreshContentUI();
			}
		}


		public void UI_SetDisplayName (string str) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.DisplayName = str;
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetDescription (string str) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Description = str;
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetTerminateHP (string str) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			if (int.TryParse(str, out int value)) {
				sData.Ship.TerminateHP = Mathf.Max(value, 0);
			}
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetCooldown (string str) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			if (int.TryParse(str, out int value)) {
				sData.Ship.Ability.Cooldown = Mathf.Max(value, 1);
			}
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetBreakOnSunk (bool isOn) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.BreakOnSunk = isOn;
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetBreakOnMiss (bool isOn) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.BreakOnMiss = isOn;
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetResetCooldownOnHit (bool isOn) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.ResetCooldownOnHit = isOn;
			SaveData();
			RefreshContentUI();
		}


		public void UI_SetCopyOppLastUsed (bool isOn) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.CopyOpponentLastUsed = isOn;
			SaveData();
			RefreshContentUI();
		}


		public void SelectFirstShip (out string secondShip) {
			secondShip = "";
			var map = GetShipDataMap();
			var keys = new List<string>(map.Keys);
			if (keys.Count > 0) {
				keys.Sort((a, b) => a.CompareTo(b));
				SelectingShipID = keys[0];
				if (keys.Count > 1) {
					secondShip = keys[1];
				}
			}
		}


		#endregion




		#region --- LGC ---


		private void SelectShip (string id) {
			SelectingShipID = id;
			OnSelectionChanged(id);
		}


		private void SaveData () => SaveAssetData(SelectingShipID);


		// Reload UI
		private void ReloadNav () {
			string selectingID = SelectingShipID;
			m_NavContent.gameObject.SetActive(false);
			m_NavContent.DestroyAllChirldrenImmediate();
			m_NavContent.gameObject.SetActive(true);
			var template = m_NavTemplate;
			var map = GetShipDataMap();
			var keyList = new List<string>();
			foreach (var pair in map) {
				keyList.Add(pair.Key);
			}
			keyList.Sort((a, b) => a.CompareTo(b));
			foreach (var key in keyList) {
				string globalID = key;
				var shipData = map[key];
				var grab = Instantiate(template, m_NavContent);
				var rt = grab.transform as RectTransform;
				rt.name = globalID;
				rt.gameObject.SetActive(true);
				rt.anchoredPosition3D = rt.anchoredPosition;
				rt.localRotation = Quaternion.identity;
				rt.localScale = Vector3.one;
				rt.SetAsLastSibling();
				grab.Grab<Toggle>().SetIsOnWithoutNotify(globalID == SelectingShipID);
				grab.Grab<Text>("Label").text = key;
				grab.Grab<Image>("Icon").sprite = shipData.Sprite;
			}
			foreach (RectTransform rt in m_NavContent) {
				var grab = rt.GetComponent<Grabber>();
				var tg = grab.Grab<Toggle>();
				tg.onValueChanged.AddListener((isOn) => {
					if (isOn) {
						SelectShip(rt.name);
						RefreshNavUI();
						RefreshContentUI();
					}
				});
			}
			SelectingShipID = selectingID;
		}


		// Refresh UI
		private void RefreshNavUI () {
			foreach (RectTransform rt in m_NavContent) {
				var grab = rt.GetComponent<Grabber>();
				if (grab == null) { continue; }
				grab.Grab<Toggle>().SetIsOnWithoutNotify(
					rt.name == SelectingShipID
				);
			}
		}


		private void RefreshContentUI () {

			var sData = SelectingShipData;
			if (sData == null) { return; }

			m_InfoContentData.GlobalID.SetTextWithoutNotify(SelectingShipID);
			m_InfoContentData.Icon.sprite = sData.Sprite;
			m_InfoContentData.DisplayName.SetTextWithoutNotify(sData.DisplayName);
			m_InfoContentData.Description.SetTextWithoutNotify(sData.Description);
			m_InfoContentData.TerminateHP.SetTextWithoutNotify(sData.Ship.TerminateHP.ToString());
			m_InfoContentData.Cooldown.SetTextWithoutNotify(sData.Ship.Ability.Cooldown.ToString());
			m_InfoContentData.BreakOnSunk.SetIsOnWithoutNotify(sData.Ship.Ability.BreakOnSunk);
			m_InfoContentData.BreakOnMiss.SetIsOnWithoutNotify(sData.Ship.Ability.BreakOnMiss);
			m_InfoContentData.ResetCooldownOnHit.SetIsOnWithoutNotify(sData.Ship.Ability.ResetCooldownOnHit);
			m_InfoContentData.CopyOpponentLastUsed.SetIsOnWithoutNotify(sData.Ship.Ability.CopyOpponentLastUsed);

			m_InfoContentData.BreakOnSunk.transform.parent.gameObject.SetActive(!sData.Ship.Ability.CopyOpponentLastUsed);
			m_InfoContentData.BreakOnMiss.transform.parent.gameObject.SetActive(!sData.Ship.Ability.CopyOpponentLastUsed);
			m_InfoContentData.ResetCooldownOnHit.transform.parent.gameObject.SetActive(!sData.Ship.Ability.CopyOpponentLastUsed);

			m_Titles[0].gameObject.SetActive(true);
			m_Titles[1].gameObject.SetActive(!sData.Ship.Ability.CopyOpponentLastUsed);
			m_Titles[2].gameObject.SetActive(!sData.Ship.Ability.CopyOpponentLastUsed);
			if (sData.Ship.Ability.CopyOpponentLastUsed) {
				m_Titles[0].SetIsOnWithoutNotify(true);
				m_Titles[1].SetIsOnWithoutNotify(false);
				m_Titles[2].SetIsOnWithoutNotify(false);
				m_Panels[0].gameObject.SetActive(true);
				m_Panels[1].gameObject.SetActive(false);
				m_Panels[2].gameObject.SetActive(false);
			}



		}


		#endregion




	}
}
