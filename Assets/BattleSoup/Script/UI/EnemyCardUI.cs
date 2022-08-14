using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;


namespace BattleSoup {
	public class EnemyCardUI : CardUI {


		[SerializeField] Text m_TurnNumber;
		[SerializeField] Text m_Description;

		private bool ShowTurnNumber = false;

		public void SetInfo (EnemyCard card) {
			FrontIMG.sprite = card.Icon;
			m_TurnNumber.text = (card.CurrentTurn + 1).ToString();
			ShowTurnNumber = !card.Performed;
			m_TurnNumber.gameObject.SetActive(Front && ShowTurnNumber);
			m_Description.text = card.Description;
		}

		protected override void RefreshFrontBackUI () {
			base.RefreshFrontBackUI();
			m_TurnNumber.gameObject.SetActive(Front && ShowTurnNumber);
		}

	}
}