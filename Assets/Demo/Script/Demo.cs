using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moenen.Standard;
using UIGadget;


namespace BattleSoupDemo {
	public class Demo : MonoBehaviour {







		#region --- SUB ---




		#endregion




		#region --- VAR ---


		// Ser
		[Header("Component")]
		[SerializeField] RectTransform m_BattleZone = null;
		[SerializeField] VectorGrid m_GridA = null;
		[SerializeField] VectorGrid m_GridB = null;
		[SerializeField] BlocksRenderer m_BlocksRendererA = null;
		[SerializeField] BlocksRenderer m_BlocksRendererB = null;


		#endregion




		#region --- MSG ---





		#endregion




		#region --- LGC ---


		// Soup
		private void RefreshSoups () {



		}


		private void SetSoupSize (int width, int height) {
			m_BattleZone.SetSizeWithCurrentAnchors(
				RectTransform.Axis.Vertical,
				m_BlocksRendererA.rectTransform.rect.width * height / width
			);
			m_GridA.X = width;
			m_GridA.Y = height;
			m_GridB.X = width;
			m_GridB.Y = height;
			m_BlocksRendererA.GridCountX = width;
			m_BlocksRendererA.GridCountY = height;
			m_BlocksRendererB.GridCountX = width;
			m_BlocksRendererB.GridCountY = height;
			m_BattleZone.gameObject.SetActive(false);
			m_BattleZone.gameObject.SetActive(true);
		}


		#endregion




	}
}