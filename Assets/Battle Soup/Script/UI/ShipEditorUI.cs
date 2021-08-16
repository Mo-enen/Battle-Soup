using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UIGadget;
using UnityEngine.UI;

namespace BattleSoup {
	public class ShipEditorUI : MonoBehaviour {




		#region --- SUB ---


		// Handler
		public delegate IEnumerator<KeyValuePair<string, ShipData>> ShipDataEnuHandler ();
		public delegate void StringHandler (string str);


		#endregion




		#region --- VAR ---


		// Api
		public static ShipDataEnuHandler GetShipDataEnu { get; set; } = null;
		public static StringHandler OnSelectionChanged { get; set; } = null;
		public string SelectingShipID { get; private set; } = "";

		// Ser
		[SerializeField] RectTransform m_NavContent = null;

		// Data


		#endregion




		#region --- MSG ---


		private void Awake () {
			Awake_Nav();

		}


		private void Awake_Nav () {
			var template = m_NavContent.GetChild(0).GetComponent<Grabber>();
			var enu = GetShipDataEnu();
			while (enu.MoveNext()) {
				var pair = enu.Current;
				string globalID = pair.Key;
				var shipData = pair.Value;
				var grab = Instantiate(template, m_NavContent);
				var rt = grab.transform as RectTransform;
				rt.name = globalID;
				rt.gameObject.SetActive(true);
				rt.anchoredPosition3D = rt.anchoredPosition;
				rt.localRotation = Quaternion.identity;
				rt.localScale = Vector3.one;
				rt.SetAsLastSibling();
				grab.Grab<Toggle>().onValueChanged.AddListener((isOn) => {
					if (isOn) {
						SelectShip(globalID);
						RefreshNavUI();
						RefreshContentUI();
					}
				});
				grab.Grab<Text>("Label").text = shipData.DisplayName;
				grab.Grab<Image>("Icon").sprite = shipData.Sprite;
			}
			DestroyImmediate(template.gameObject, false);
			m_NavContent.GetChild(0).GetComponent<Grabber>().Grab<Toggle>().SetIsOnWithoutNotify(true);
		}


		#endregion




		#region --- API ---




		#endregion




		#region --- LGC ---


		public void SelectFirstShip () {
			var enu = GetShipDataEnu();
			while (enu.MoveNext()) {
				SelectingShipID = enu.Current.Key;
				break;
			}
		}


		public void SelectShip (string id) {
			SelectingShipID = id;
			OnSelectionChanged(id);
		}


		// Refresh UI
		private void RefreshNavUI () {
			foreach (RectTransform rt in m_NavContent) {
				rt.GetComponent<Grabber>().Grab<Toggle>().SetIsOnWithoutNotify(
					rt.name == SelectingShipID
				);
			}
		}


		private void RefreshContentUI () {



		}


		#endregion




	}
}
