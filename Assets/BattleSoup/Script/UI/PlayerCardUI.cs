using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;
using UnityEngine.EventSystems;


namespace BattleSoup {
	public class PlayerCardUI : CardUI, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {


		// Api
		public bool Interactable { get; set; } = true;

		// Ser
		[SerializeField] Image m_Icon;
		[SerializeField] RectTransform m_ClickedMark;
		[SerializeField] RectTransform m_Root;
		[SerializeField] TooltipUI m_Hint;

		// Data
		private bool ClickedOnce = false;
		private bool Hovering = false;


		// MSG
		protected override void OnEnable () {
			base.OnEnable();
			Hovering = false;
			ClickedOnce = false;
			Interactable = true;
		}


		protected override void Update () {
			base.Update();
			if (m_ClickedMark.gameObject.activeSelf != ClickedOnce) {
				m_ClickedMark.gameObject.SetActive(ClickedOnce);
			}
			m_Root.anchoredPosition3D = new Vector3(
				0f,
				Mathf.Lerp(m_Root.anchoredPosition3D.y, Hovering ? 36f : 0f, Time.deltaTime * 20f),
				0f
			);
		}


		public void OnPointerEnter (PointerEventData eventData) {
			Hovering = Interactable;
		}


		public void OnPointerExit (PointerEventData eventData) {
			Hovering = false;
			ClickedOnce = false;
		}


		public void OnPointerClick (PointerEventData eventData) {
			if (eventData.button != PointerEventData.InputButton.Left) return;
			if (!Interactable) return;
			if (ClickedOnce) {
				ClickedOnce = false;
				OnTriggered?.Invoke();
			} else {
				ClickedOnce = true;
			}
		}


		// API
		public void SetInfo (CardInfo info) {
			if (!info.IsShip) {
				// Built-in
				m_Icon.sprite = Soup.CardAssets.TypeIcons[(int)info.Type];
				m_Hint.Tooltip = info.Type switch {
					CardType.Attack => "Pick a cell to attack",
					CardType.Shield => "Add 2 shields",
					CardType.Heart => "Add 2 hearts",
					CardType.Card => "Draw 2 cards",
					_ => "",
				};
			} else {
				// Ship
				int id = info.GlobalName.AngeHash();
				if (Soup.TryGetShip(id, out var ship)) {
					m_Icon.sprite = ship.Icon;
					m_Hint.Tooltip = ship.Description;
				} else {
					m_Icon.sprite = null;
					m_Hint.Tooltip = "";
				}
			}
		}


		protected override void RefreshFrontBackUI () {
			base.RefreshFrontBackUI();
			m_Icon.enabled = Front;
		}



	}
}