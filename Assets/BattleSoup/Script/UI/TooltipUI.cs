using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



namespace BattleSoup {
	public class TooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
		public string Tooltip = "";
		[SerializeField] Text m_Tooltip = null;
		public void OnPointerEnter (PointerEventData eventData) {
			if (m_Tooltip == null) return;
			m_Tooltip.transform.gameObject.SetActive(true);
			m_Tooltip.text = Tooltip;
		}
		public void OnPointerExit (PointerEventData eventData) {
			if (m_Tooltip == null) return;
			m_Tooltip.transform.gameObject.SetActive(false);
			m_Tooltip.text = "";
		}
	}
}