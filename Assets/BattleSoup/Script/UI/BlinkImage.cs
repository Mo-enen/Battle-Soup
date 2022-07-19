using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BattleSoup {
	[RequireComponent(typeof(Image))]
	public class BlinkImage : MonoBehaviour {



		private Image Img => _Img != null ? _Img : (_Img = GetComponent<Image>());
		private Image _Img = null;

		[SerializeField] float m_BlinkRate = 1f;
		[SerializeField] Color m_ColorA = Color.white;
		[SerializeField] Color m_ColorB = Color.white;


		private void Update () {
			if (m_BlinkRate == 0) {
				Img.color = m_ColorA;
				return;
			}
			Img.color = Color.Lerp(
				m_ColorA,
				m_ColorB,
				Mathf.PingPong(Time.time, m_BlinkRate) / m_BlinkRate
			);
		}


	}
}
