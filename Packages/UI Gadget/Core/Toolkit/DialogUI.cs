using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace UIGadget {
	public class DialogUI : MonoBehaviour {




		#region --- SUB ---




		#endregion




		#region --- VAR ---


		// Ser
		[SerializeField] Text m_Title = null;
		[SerializeField] Text m_Message = null;
		[SerializeField] RectTransform m_ButtonContainer = null;
		[SerializeField] Button[] m_ButtonTemplates = null;

		// Data



		#endregion




		#region --- MSG ---




		#endregion




		#region --- API ---


		public void SetContent (string title, string message) {
			m_Title.text = title;
			m_Message.text = message;
		}


		public void AddOption (string label, System.Action action) => AddOption(label, 0, action);
		public void AddOption (string label, int templateID, System.Action action) {
			if (m_ButtonTemplates == null || m_ButtonTemplates.Length == 0) {
				Debug.LogWarning($"No button template for {name}");
			}
			if (templateID < 0 || templateID >= m_ButtonTemplates.Length) {
				templateID = 0;
			}
			var button = Instantiate(m_ButtonTemplates[templateID], m_ButtonContainer);
			var rt = button.transform as RectTransform;
			rt.anchoredPosition3D = rt.anchoredPosition;
			rt.localRotation = Quaternion.identity;
			rt.localScale = Vector3.one;
			rt.SetAsLastSibling();
			button.onClick.AddListener(() => action());
			var text = rt.GetComponentInChildren<Text>(true);
			if (text != null) {
				text.text = label;
			}
			rt.gameObject.SetActive(true);
		}


		public void Close () => DestroyImmediate(gameObject, false);


		#endregion




		#region --- LGC ---




		#endregion




	}
}
