using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UIGadget;
using UnityEngine.UI;
using Moenen.Standard;
using MonoFileBrowser;
using BattleSoupAI;


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
			public Toggle ResetCooldownOnHit;
			public Toggle CopyOpponentLastUsed;
			public ShipBodyEditorUI ShipBodyEditor;
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
		[SerializeField] Game m_Game = null;
		[SerializeField] RectTransform m_NavContent = null;
		[SerializeField] RectTransform m_AbilityHighlight = null;
		[SerializeField] Grabber m_NavTemplate = null;
		[SerializeField] Grabber m_EventTemplate = null;
		[SerializeField] Grabber m_AttackTemplate = null;
		[SerializeField] RectTransform m_EventContainer = null;
		[SerializeField] RectTransform m_AttackContainer = null;
		[SerializeField] Toggle[] m_Titles = null;
		[SerializeField] RectTransform[] m_Panels = null;
		[SerializeField] InfoContentData m_InfoContentData = default;

		// Data
		private readonly List<Dropdown.OptionData> EventTypeOptions = new List<Dropdown.OptionData>();
		private readonly List<Dropdown.OptionData> EventConditionOptions0 = new List<Dropdown.OptionData>();
		private readonly List<Dropdown.OptionData> EventConditionOptions1 = new List<Dropdown.OptionData>();
		private readonly List<Dropdown.OptionData> EventConditionOptions2 = new List<Dropdown.OptionData>();
		private readonly List<Dropdown.OptionData> EventActionOptions = new List<Dropdown.OptionData>();
		private readonly List<Dropdown.OptionData> AttackTypeOptions = new List<Dropdown.OptionData>();
		private readonly List<Dropdown.OptionData> AttackTriggerOptions = new List<Dropdown.OptionData>();
		private readonly List<DropdownEx.OptionData> AvailableTargetOptions = new List<DropdownEx.OptionData>();


		#endregion




		#region --- MSG ---


		private void Awake () {
			ReloadNav();
			RefreshEventPanel();
			RefreshAttackPanel();
			Awake_Options();
			m_Panels[0].gameObject.SetActive(true);
			m_Panels[1].gameObject.SetActive(false);
			m_Panels[2].gameObject.SetActive(false);
		}


		private void OnEnable () {
			RefreshContentUI();
			RefreshEventPanel();
			RefreshAttackPanel();
		}


		private void Update () {
			// Ability Highlight
			bool highlight = m_Game.AbilityShipIndex >= 0;
			if (m_AbilityHighlight.gameObject.activeSelf != highlight) {
				m_AbilityHighlight.gameObject.SetActive(highlight);
			}
		}


		private void Awake_Options () {

			// Event Type
			EventTypeOptions.Clear();
			foreach (var codeName in System.Enum.GetNames(typeof(SoupEvent))) {
				EventTypeOptions.Add(new Dropdown.OptionData(Util.GetDisplayName(codeName)));
			}

			// Con0
			EventConditionOptions0.Clear();
			EventConditionOptions0.Add(new Dropdown.OptionData("Own"));
			EventConditionOptions0.Add(new Dropdown.OptionData("Opponent"));

			// Con1
			EventConditionOptions1.Clear();
			foreach (var codeName in System.Enum.GetNames(typeof(EventCondition))) {
				EventConditionOptions1.Add(new Dropdown.OptionData(Util.GetDisplayName(codeName)));
			}

			// Con2
			EventConditionOptions2.Clear();
			int con2Len = System.Enum.GetNames(typeof(EventConditionCompare)).Length;
			for (int i = 0; i < con2Len; i++) {
				switch ((EventConditionCompare)i) {
					case EventConditionCompare.Greater:
						EventConditionOptions2.Add(new Dropdown.OptionData("£¾"));
						break;
					case EventConditionCompare.GreaterOrEqual:
						EventConditionOptions2.Add(new Dropdown.OptionData("¡Ý"));
						break;
					case EventConditionCompare.Less:
						EventConditionOptions2.Add(new Dropdown.OptionData("£¼"));
						break;
					case EventConditionCompare.LessOrEqual:
						EventConditionOptions2.Add(new Dropdown.OptionData("¡Ü"));
						break;
					case EventConditionCompare.Equal:
						EventConditionOptions2.Add(new Dropdown.OptionData("="));
						break;
					case EventConditionCompare.NotEqual:
						EventConditionOptions2.Add(new Dropdown.OptionData("¡Ù"));
						break;
				}
			}

			// Action Type
			EventActionOptions.Clear();
			foreach (var codeName in System.Enum.GetNames(typeof(EventAction))) {
				EventActionOptions.Add(new Dropdown.OptionData(Util.GetDisplayName(codeName)));
			}

			// Attack Type
			AttackTypeOptions.Clear();
			foreach (var codeName in System.Enum.GetNames(typeof(AttackType))) {
				AttackTypeOptions.Add(new Dropdown.OptionData(Util.GetDisplayName(codeName)));
			}

			// Attack Trigger
			AttackTriggerOptions.Clear();
			foreach (var codeName in System.Enum.GetNames(typeof(AttackTrigger))) {
				AttackTriggerOptions.Add(new Dropdown.OptionData(Util.GetDisplayName(codeName)));
			}

			// Available Target
			AvailableTargetOptions.Clear();
			foreach (var codeName in System.Enum.GetNames(typeof(Tile))) {
				if (codeName == "None" || codeName == "All") { continue; }
				AvailableTargetOptions.Add(new DropdownEx.OptionData(Util.GetDisplayName(codeName)));
			}


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
				RefreshEventPanel();
				RefreshAttackPanel();
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
			RefreshEventPanel();
			RefreshAttackPanel();
		}


		public void UI_UseAbility () {
			m_Game.OnAbilityClick(Group.A, 0);
		}


		// Misc
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


		// Info Panel
		public void UI_RenameSelectingShip (string newName) {
			bool success = RenameShipData(SelectingShipID, newName);
			if (success) {
				ReloadNav();
				if (GetShipDataMap().ContainsKey(newName)) {
					SelectShip(newName);
					RefreshContentUI();
					RefreshNavUI();
					RefreshEventPanel();
					RefreshAttackPanel();
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


		public void UI_BodyChanged (Int2[] body) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Body = body;
			SaveData();
			RefreshContentUI();
		}


		// Event Panel
		public void UI_NewEvent () {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events.Add(new BattleSoupAI.Event() {
				Type = SoupEvent.Own_NormalAttack,
				Condition = EventCondition.None,
				Action = EventAction.PerformAttack,
				IntParam = 0,
				ActionParam = 0,
				BreakAfterPerform = false,
				ApplyConditionOnOpponent = false,
			});
			SaveData();
			RefreshEventPanel();
		}


		private void SetEventType (int index, SoupEvent type) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events[index].Type = type;
			SaveData();
			RefreshEventPanel();
		}


		private void SetEventCondition0 (int index, bool forOwn) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events[index].ApplyConditionOnOpponent = !forOwn;
			SaveData();
			RefreshEventPanel();
		}


		private void SetEventCondition1 (int index, EventCondition condition) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events[index].Condition = condition;
			SaveData();
			RefreshEventPanel();
		}


		private void SetEventCondition2 (int index, EventConditionCompare compare) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events[index].ConditionCompare = compare;
			SaveData();
			RefreshEventPanel();
		}


		private void SetEventCondition3 (int index, string str) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			if (int.TryParse(str, out int value)) {
				sData.Ship.Ability.Events[index].IntParam = value;
				SaveData();
				RefreshEventPanel();
			}
		}


		private void SetEventActionType (int index, EventAction action) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events[index].Action = action;
			SaveData();
			RefreshEventPanel();
		}


		private void SetEventActionParam (int index, string str) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			if (int.TryParse(str, out int value)) {
				sData.Ship.Ability.Events[index].ActionParam = value;
				SaveData();
				RefreshEventPanel();
			}
		}


		private void SetBreakAfterPerform (int index, bool breakAfterPerform) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Events[index].BreakAfterPerform = breakAfterPerform;
			SaveData();
			RefreshEventPanel();
		}


		private void MoveEventUI (int index, bool up) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			var events = sData.Ship.Ability.Events;
			int newIndex = up ? index - 1 : index + 1;
			if (index >= 0 && index < events.Count && newIndex >= 0 && newIndex < events.Count) {
				(events[index], events[newIndex]) = (events[newIndex], events[index]);
				SaveData();
				RefreshEventPanel();
			}
		}


		private void DeleteEvent (int index) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			var events = sData.Ship.Ability.Events;
			if (index >= 0 && index < events.Count) {
				events.RemoveAt(index);
				SaveData();
				RefreshEventPanel();
			}
		}


		private void DuplicateEvent (int index) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			var events = sData.Ship.Ability.Events;
			if (index >= 0 && index < events.Count) {
				events.Insert(index, events[index].Duplicate());
				SaveData();
				RefreshEventPanel();
			}
		}


		// Attack Panel
		public void UI_NewAttack () {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Attacks.Add(new Attack() {
				Trigger = AttackTrigger.Picked,
				Type = AttackType.HitTile,
				AvailableTarget = Tile.GeneralWater,
				X = 0,
				Y = 0,
			});
			SaveData();
			RefreshAttackPanel();
		}


		private void SetAttackType (int index, AttackType type) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Attacks[index].Type = type;
			SaveData();
			RefreshAttackPanel();
		}


		private void SetAttackTrigger (int index, AttackTrigger trigger) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Attacks[index].Trigger = trigger;
			SaveData();
			RefreshAttackPanel();
		}


		private void SetAttackAvailableTarget (int index, uint targetBit) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Attacks[index].AvailableTarget = (Tile)targetBit;
			SaveData();
			RefreshAttackPanel();
		}


		private void SetBreakResult (int index, uint targetBit) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			sData.Ship.Ability.Attacks[index].BreakingResult = (AttackResult)targetBit;
			SaveData();
			RefreshAttackPanel();
		}


		public void SetAttackLocalPositionX (int index, string x) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			if (int.TryParse(x, out int intX)) {
				sData.Ship.Ability.Attacks[index].X = intX;
				SaveData();
				RefreshAttackPanel();
			}
		}


		public void SetAttackLocalPositionY (int index, string y) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			if (int.TryParse(y, out int intY)) {
				sData.Ship.Ability.Attacks[index].Y = intY;
				SaveData();
				RefreshAttackPanel();
			}
		}


		private void MoveAttackUI (int index, bool up) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			var attacks = sData.Ship.Ability.Attacks;
			int newIndex = up ? index - 1 : index + 1;
			if (index >= 0 && index < attacks.Count && newIndex >= 0 && newIndex < attacks.Count) {
				(attacks[index], attacks[newIndex]) = (attacks[newIndex], attacks[index]);
				SaveData();
				RefreshAttackPanel();
			}
		}


		private void DeleteAttack (int index) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			var attacks = sData.Ship.Ability.Attacks;
			if (index >= 0 && index < attacks.Count) {
				attacks.RemoveAt(index);
				SaveData();
				RefreshAttackPanel();
			}
		}


		private void DuplicateAttack (int index) {
			var sData = SelectingShipData;
			if (sData == null) { return; }
			var attacks = sData.Ship.Ability.Attacks;
			if (index >= 0 && index < attacks.Count) {
				attacks.Insert(index, attacks[index].Duplicate());
				SaveData();
				RefreshAttackPanel();
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
						RefreshEventPanel();
						RefreshAttackPanel();
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
			m_InfoContentData.ResetCooldownOnHit.SetIsOnWithoutNotify(sData.Ship.Ability.ResetCooldownOnHit);
			m_InfoContentData.ShipBodyEditor.RefreshUI(sData.Ship);
			m_InfoContentData.CopyOpponentLastUsed.SetIsOnWithoutNotify(sData.Ship.Ability.CopyOpponentLastUsed);
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


		private void RefreshEventPanel () {

			var sData = SelectingShipData;
			if (sData == null) { return; }
			var container = m_EventContainer;
			var events = sData.Ship.Ability.Events;

			// Fix Item Count
			if (container.childCount < events.Count) {
				int count = events.Count - container.childCount;
				for (int i = 0; i < count; i++) {
					SpawnNewEventItem();
				}
			} else if (container.childCount > events.Count) {
				int count = container.childCount - events.Count;
				for (int i = 0; i < count; i++) {
					DestroyImmediate(container.GetChild(events.Count).gameObject, false);
				}
			}

			// Refresh UI
			int childCount = container.childCount;
			for (int i = 0; i < childCount; i++) {
				var ev = events[i];
				var grab = container.GetChild(i).GetComponent<Grabber>();
				var indexTxt = grab.Grab<Text>("Index");
				var eType = grab.Grab<Dropdown>("Event Type");
				var con0 = grab.Grab<Dropdown>("Condition 0");
				var con1 = grab.Grab<Dropdown>("Condition 1");
				var con2 = grab.Grab<Dropdown>("Condition 2");
				var con3 = grab.Grab<InputField>("Condition 3");
				var aType = grab.Grab<Dropdown>("Action Type");
				var aParam = grab.Grab<InputField>("Action Param");
				var bap = grab.Grab<Toggle>("BreakAfterPerform");
				indexTxt.text = i.ToString();
				eType.SetValueWithoutNotify((int)ev.Type);
				con0.SetValueWithoutNotify(ev.ApplyConditionOnOpponent ? 1 : 0);
				con1.SetValueWithoutNotify((int)ev.Condition);
				con2.SetValueWithoutNotify((int)ev.ConditionCompare);
				con3.SetTextWithoutNotify(ev.IntParam.ToString());
				aType.SetValueWithoutNotify((int)ev.Action);
				aParam.SetTextWithoutNotify(ev.ActionParam.ToString());
				bap.SetIsOnWithoutNotify(ev.BreakAfterPerform);
				con0.gameObject.SetActive(
					ev.Condition != EventCondition.None &&
					ev.Condition != EventCondition.CurrentShip_HiddenTileCount &&
					ev.Condition != EventCondition.CurrentShip_HitTileCount &&
					ev.Condition != EventCondition.CurrentShip_RevealTileCount
				);
				con2.gameObject.SetActive(ev.Condition != EventCondition.None);
				con3.gameObject.SetActive(ev.Condition != EventCondition.None);
			}
		}


		private void SpawnNewEventItem () {

			var container = m_EventContainer;
			var grab = Instantiate(m_EventTemplate, container);
			var rt = grab.transform as RectTransform;
			rt.anchoredPosition3D = rt.anchoredPosition;
			rt.localRotation = Quaternion.identity;
			rt.localScale = Vector3.one;
			rt.SetAsLastSibling();

			// Event Type
			var eType = grab.Grab<Dropdown>("Event Type");
			eType.AddOptions(EventTypeOptions);
			eType.onValueChanged.AddListener((value) => SetEventType(
				rt.GetSiblingIndex(), (SoupEvent)value
			));

			// Condition 0
			var cDrop0 = grab.Grab<Dropdown>("Condition 0");
			cDrop0.AddOptions(EventConditionOptions0);
			cDrop0.onValueChanged.AddListener((value) => SetEventCondition0(
				rt.GetSiblingIndex(), cDrop0.value == 0
			));

			// Condition 1
			var cDrop1 = grab.Grab<Dropdown>("Condition 1");
			cDrop1.AddOptions(EventConditionOptions1);
			cDrop1.onValueChanged.AddListener((value) => SetEventCondition1(
				rt.GetSiblingIndex(), (EventCondition)cDrop1.value
			));

			// Condition 2
			var cDrop2 = grab.Grab<Dropdown>("Condition 2");
			cDrop2.AddOptions(EventConditionOptions2);
			cDrop2.onValueChanged.AddListener((value) => SetEventCondition2(
				rt.GetSiblingIndex(), (EventConditionCompare)cDrop2.value
			));

			// Condition 3
			var cInput3 = grab.Grab<InputField>("Condition 3");
			cInput3.onEndEdit.AddListener((text) => SetEventCondition3(rt.GetSiblingIndex(), text));

			// Action Type
			var aType = grab.Grab<Dropdown>("Action Type");
			aType.AddOptions(EventActionOptions);
			aType.onValueChanged.AddListener((value) => SetEventActionType(
				rt.GetSiblingIndex(), (EventAction)aType.value
			));

			// Action Param
			var aParam = grab.Grab<InputField>("Action Param");
			aParam.onEndEdit.AddListener((text) => SetEventActionParam(rt.GetSiblingIndex(), text));

			// Break After Perform
			var bap = grab.Grab<Toggle>("BreakAfterPerform");
			bap.onValueChanged.AddListener((isOn) => SetBreakAfterPerform(rt.GetSiblingIndex(), isOn));

			// Misc Buttons
			grab.Grab<Button>("Delete Event Real").onClick.AddListener(
				() => DeleteEvent(rt.GetSiblingIndex())
			);
			grab.Grab<Button>("Move Up").onClick.AddListener(
				() => MoveEventUI(rt.GetSiblingIndex(), true)
			);
			grab.Grab<Button>("Move Down").onClick.AddListener(
				() => MoveEventUI(rt.GetSiblingIndex(), false)
			);
			grab.Grab<Button>("Duplicate").onClick.AddListener(
				() => DuplicateEvent(rt.GetSiblingIndex())
			);
		}


		private void RefreshAttackPanel () {

			var sData = SelectingShipData;
			if (sData == null) { return; }
			var container = m_AttackContainer;
			var attacks = sData.Ship.Ability.Attacks;

			// Fix Item Count
			if (container.childCount < attacks.Count) {
				int count = attacks.Count - container.childCount;
				for (int i = 0; i < count; i++) {
					SpawnNewAttackItem();
				}
			} else if (container.childCount > attacks.Count) {
				int count = container.childCount - attacks.Count;
				for (int i = 0; i < count; i++) {
					DestroyImmediate(container.GetChild(attacks.Count).gameObject, false);
				}
			}

			// Refresh UI
			int childCount = container.childCount;
			for (int i = 0; i < childCount; i++) {

				var att = attacks[i];
				var grab = container.GetChild(i).GetComponent<Grabber>();

				var indexTxt = grab.Grab<Text>("Index");
				var aTrigger = grab.Grab<Dropdown>("Attack Trigger");
				var aType = grab.Grab<Dropdown>("Attack Type");
				var aTarget = grab.Grab<DropdownEx>("Available Target");
				var bResult = grab.Grab<DropdownEx>("Breaking Result");
				var mIcons0 = grab.Grab<MultiDropIconsUI>("Available Target Icons");
				var mIcons1 = grab.Grab<MultiDropIconsUI>("Breaking Result Icons");
				var xField = grab.Grab<InputField>("X");
				var yField = grab.Grab<InputField>("Y");

				indexTxt.text = i.ToString();
				aTrigger.SetValueWithoutNotify((int)att.Trigger);
				aType.SetValueWithoutNotify((int)att.Type);
				aTarget.SetValueWithoutNotify((uint)att.AvailableTarget);
				bResult.SetValueWithoutNotify((uint)att.BreakingResult);
				mIcons0.RefreshUI();
				mIcons1.RefreshUI();
				xField.SetTextWithoutNotify(att.X.ToString());
				yField.SetTextWithoutNotify(att.Y.ToString());

				aType.transform.parent.gameObject.SetActive(att.Trigger != AttackTrigger.Break);
				aTarget.transform.parent.gameObject.SetActive(
					att.Trigger != AttackTrigger.Break &&
					att.Type != AttackType.RevealSelf
				);
				xField.transform.parent.gameObject.SetActive(
					(att.Trigger == AttackTrigger.Picked || att.Trigger == AttackTrigger.TiedUp) &&
					att.Type != AttackType.RevealSelf
				);
				yField.transform.parent.gameObject.SetActive(
					(att.Trigger == AttackTrigger.Picked || att.Trigger == AttackTrigger.TiedUp) &&
					att.Type != AttackType.RevealSelf
				);
				bResult.transform.parent.gameObject.SetActive(
					att.Trigger != AttackTrigger.Break
				);

			}

		}


		private void SpawnNewAttackItem () {

			var container = m_AttackContainer;
			var grab = Instantiate(m_AttackTemplate, container);
			var rt = grab.transform as RectTransform;
			rt.anchoredPosition3D = rt.anchoredPosition;
			rt.localRotation = Quaternion.identity;
			rt.localScale = Vector3.one;
			rt.SetAsLastSibling();

			// Attack Type
			var aType = grab.Grab<Dropdown>("Attack Type");
			aType.AddOptions(AttackTypeOptions);
			aType.onValueChanged.AddListener((value) => SetAttackType(
				rt.GetSiblingIndex(), (AttackType)value
			));

			// Attack Trigger
			var aTrigger = grab.Grab<Dropdown>("Attack Trigger");
			aTrigger.AddOptions(AttackTriggerOptions);
			aTrigger.onValueChanged.AddListener((value) => SetAttackTrigger(
				rt.GetSiblingIndex(), (AttackTrigger)value
			));

			// Available Target
			var aTarget = grab.Grab<DropdownEx>("Available Target");
			aTarget.AddOptions(AvailableTargetOptions);
			aTarget.onValueChanged.AddListener((value) => SetAttackAvailableTarget(
				rt.GetSiblingIndex(), value
			));

			// Breaking Result
			var bResult = grab.Grab<DropdownEx>("Breaking Result");
			bResult.onValueChanged.AddListener((value) => SetBreakResult(
				rt.GetSiblingIndex(), value
			));

			// Position
			var xInput = grab.Grab<InputField>("X");
			var yInput = grab.Grab<InputField>("Y");
			xInput.onEndEdit.AddListener((text) => SetAttackLocalPositionX(
				rt.GetSiblingIndex(), text
			));
			yInput.onEndEdit.AddListener((text) => SetAttackLocalPositionY(
				rt.GetSiblingIndex(), text
			));

			// Misc Buttons
			grab.Grab<Button>("Delete Attack Real").onClick.AddListener(
				() => DeleteAttack(rt.GetSiblingIndex())
			);
			grab.Grab<Button>("Move Up").onClick.AddListener(
				() => MoveAttackUI(rt.GetSiblingIndex(), true)
			);
			grab.Grab<Button>("Move Down").onClick.AddListener(
				() => MoveAttackUI(rt.GetSiblingIndex(), false)
			);
			grab.Grab<Button>("Duplicate").onClick.AddListener(
				() => DuplicateAttack(rt.GetSiblingIndex())
			);
		}


		#endregion




	}
}
