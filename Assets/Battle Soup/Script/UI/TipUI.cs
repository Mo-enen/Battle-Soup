using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace BattleSoup {
	public class TipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {


		// Api
		public string Content { get => m_Content; set => m_Content = value; }

		// Ser
		[SerializeField] Text m_Text = null;
		[SerializeField] string m_Content = "";


		// MSG
		private void OnDisable () {
			if (m_Text != null) {
				if (!string.IsNullOrEmpty(m_Content)) {
					m_Text.text = "";
				} else {
					m_Text.gameObject.SetActive(false);
				}
			}
		}


		public void OnPointerEnter (PointerEventData eData) {
			if (m_Text != null) {
				if (!string.IsNullOrEmpty(m_Content)) {
					m_Text.text = m_Content;
				} else {
					m_Text.gameObject.SetActive(true);
				}
			}
		}


		public void OnPointerExit (PointerEventData eData) {
			if (m_Text != null) {
				if (!string.IsNullOrEmpty(m_Content)) {
					m_Text.text = "";
				} else {
					m_Text.gameObject.SetActive(false);
				}
			}
		}



	}
}
