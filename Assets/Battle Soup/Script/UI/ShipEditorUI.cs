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

		}


		#endregion




		#region --- VAR ---


		// Api
		public static ShipDataMapHandler GetShipDataMap { get; set; } = null;
		public static StringHandler CreateShipData { get; set; } = null;
		public static StringStringHandler SetShipIcon { get; set; } = null;
		public static BoolStringStringHandler RenameShipData { get; set; } = null;
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
				RefreshContentUI();
			}

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


		#endregion




		#region --- LGC ---


		public void SelectFirstShip () {
			var map = GetShipDataMap();
			foreach (var pair in map) {
				SelectingShipID = pair.Key;
				break;
			}
			RefreshContentUI();
			RefreshNavUI();
		}


		public void SelectShip (string id) {
			SelectingShipID = id;
			OnSelectionChanged(id);
		}


		// Reload UI
		private void ReloadNav () {
			m_NavContent.DestroyAllChirldrenImmediate();
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
				var tg = grab.Grab<Toggle>();
				tg.onValueChanged.AddListener((isOn) => {
					if (isOn) {
						SelectShip(globalID);
						RefreshNavUI();
						RefreshContentUI();
					}
				});
				tg.SetIsOnWithoutNotify(globalID == SelectingShipID);
				grab.Grab<Text>("Label").text = key;
				grab.Grab<Image>("Icon").sprite = shipData.Sprite;
			}
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




		}


		#endregion




	}
}
