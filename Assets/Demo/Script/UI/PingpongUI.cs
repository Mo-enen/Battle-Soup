using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSoupDemo {
	public class PingpongUI : MonoBehaviour {



		// Ser
		[SerializeField] Vector2 m_PosA = Vector2.zero;
		[SerializeField] Vector2 m_PosB = Vector2.zero;
		[SerializeField] float m_Loop = 1f;



		// MSG
		private void Update () {
			if (Mathf.Approximately(m_Loop, 0f)) { return; }
			(transform as RectTransform).anchoredPosition = Vector2.Lerp(
				m_PosA, m_PosB,
				Mathf.PingPong(Time.time, m_Loop) / m_Loop
			);
		}


	}
}
