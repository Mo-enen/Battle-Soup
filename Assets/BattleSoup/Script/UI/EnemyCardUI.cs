using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AngeliaFramework;


namespace BattleSoup {
	public class EnemyCardUI : CardUI {


		[SerializeField] Text m_TurnNumber;


		public void SetInfo (EnemyCard card) {
			FrontIMG.sprite = card.Icon;
			m_TurnNumber.text = (card.CurrentTurn + 1).ToString();
			m_TurnNumber.gameObject.SetActive(!card.Performed);
		}


	}
}