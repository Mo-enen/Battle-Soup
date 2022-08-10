using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



namespace BattleSoup {
	public class TooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
		[SerializeField] Text m_Tooltip = null;
		[TextArea] public string Tooltip = "";
		public void OnPointerEnter (PointerEventData eventData) {
			if (m_Tooltip == null) return;
			m_Tooltip.text = Tooltip;
		}
		public void OnPointerExit (PointerEventData eventData) {
			if (m_Tooltip == null) return;
			m_Tooltip.text = "";
		}
	}
}