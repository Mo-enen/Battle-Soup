using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace BattleSoupDemo {
	public class BlackScreenUI : MonoBehaviour {


		[SerializeField] private float m_Speed = 0.5f;
		private Image IMG = null;

		private void Update () {
			if (IMG == null) {
				IMG = GetComponent<Image>();
				IMG.enabled = true;
			}
			var color = IMG.color;
			color.a = Mathf.Clamp(color.a - Time.deltaTime * m_Speed, 0f, 1f);
			IMG.color = color;
			if (Mathf.Approximately(color.a, 0f)) {
				Destroy(gameObject);
			}
		}
	}
}
