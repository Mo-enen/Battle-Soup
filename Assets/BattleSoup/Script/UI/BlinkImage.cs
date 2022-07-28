using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BattleSoup {
	[RequireComponent(typeof(Image))]
	public class BlinkImage : MonoBehaviour {



		public float BlinkRate { get => m_BlinkRate; set => m_BlinkRate = value; }
		public Image Image => _Img != null ? _Img : (_Img = GetComponent<Image>());


		private Image _Img = null;


		[SerializeField] float m_BlinkRate = 1f;
		[SerializeField] Color m_ColorA = Color.white;
		[SerializeField] Color m_ColorB = Color.white;


		private float StopTime = float.MaxValue;
		private bool IsDirty = true;


		private void OnEnable () => IsDirty = true;


		private void Update () {
			if (m_BlinkRate == 0 || Time.time > StopTime) {
				if (IsDirty) Image.color = m_ColorA;
				IsDirty = false;
				return;
			}
			Image.color = Color.Lerp(
				m_ColorA,
				m_ColorB,
				Mathf.PingPong(Time.time, m_BlinkRate) / m_BlinkRate
			);
			IsDirty = true;
		}


		public void Blink (float duration) => StopTime = Time.time + duration;


	}
}
