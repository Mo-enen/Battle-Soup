using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSoup {
	public class SpinUI : MonoBehaviour {
		[SerializeField] float m_Speed = 360f;
		private void Update () {
			transform.localRotation = Quaternion.Euler(0f, 0f, (Time.time * m_Speed) % 360f);
		}
	}
}
