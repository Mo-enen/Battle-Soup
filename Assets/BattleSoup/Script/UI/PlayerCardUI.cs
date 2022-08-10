using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;
using UnityEngine.EventSystems;


namespace BattleSoup {
	public class PlayerCardUI : CardUI, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {


		// Ser
		[SerializeField] Image m_Icon;
		[SerializeField] Image m_TypeIcon;
		[SerializeField] RectTransform m_ClickedMark;

		// Data
		private bool ClickedOnce = false;



		// MSG
		protected override void OnEnable () {
			base.OnEnable();
			Hovering = false;
			ClickedOnce = false;
		}


		protected override void Update () {
			base.Update();
			if (m_ClickedMark.gameObject.activeSelf != ClickedOnce) {
				m_ClickedMark.gameObject.SetActive(ClickedOnce);
			}
		}


		public void OnPointerEnter (PointerEventData eventData) => Hovering = true;


		public void OnPointerExit (PointerEventData eventData) {
			Hovering = false;
			ClickedOnce = false;
		}


		public void OnPointerClick (PointerEventData eventData) {
			if (eventData.button != PointerEventData.InputButton.Left) return;
			if (ClickedOnce) {
				ClickedOnce = false;
				OnTriggered?.Invoke();
			} else {
				ClickedOnce = true;
			}
		}


		// API
		public void Init (CardInfo info) {
			if (!info.IsShip) {
				// Built-in
				m_Icon.sprite = Soup.CardAssets.TypeIcons[(int)info.Type];
				m_TypeIcon.sprite = null;
				Hint.Tooltip = info.Type switch {
					CardType.Attack => "Pick a cell to Attack",
					CardType.Reveal => "Pick a cell to Reveal",
					CardType.Sonar => "Pick a cell to Sonar",
					CardType.Shield => "Add a Shield",
					CardType.Heart => "Add a Heart",
					_ => "",
				};
			} else {
				// Ship
				int id = info.GlobalName.AngeHash();
				if (Soup.TryGetShip(id, out var ship)) {
					m_Icon.sprite = ship.Icon;
					Hint.Tooltip = ship.Description;
				} else {
					m_Icon.sprite = null;
					Hint.Tooltip = "";
				}
				m_TypeIcon.sprite = Soup.CardAssets.TypeIcons[(int)info.Type];
			}
			m_Icon.gameObject.SetActive(m_Icon.sprite != null);
			m_TypeIcon.gameObject.SetActive(m_TypeIcon.sprite != null);
		}


		protected override void RefreshFrontBackUI () {
			base.RefreshFrontBackUI();
			bool front = Front;
			m_Icon.gameObject.SetActive(front);
			m_TypeIcon.gameObject.SetActive(front);
			m_ClickedMark.gameObject.SetActive(front);
		}



	}
}