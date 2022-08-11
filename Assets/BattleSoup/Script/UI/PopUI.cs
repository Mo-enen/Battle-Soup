using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace BattleSoup {
	[RequireComponent(typeof(Graphic))]
	public class PopUI : MonoBehaviour {


		[SerializeField] float m_Amount = 1.2f;
		[SerializeField] float m_Lerp = 20f;
		[SerializeField] Color m_Tint = Color.white;

		private Graphic IMG => _IMG != null ? _IMG : (_IMG = GetComponent<Graphic>()); Graphic _IMG = null;
		private Coroutine PopCor = null;
		private Vector3 Scale = Vector3.one;
		private Color Color = Color.white;

		private void Awake () {
			Scale = transform.localScale;
			Color = IMG.color;
		}

		private void OnEnable () {
			StopAllCoroutines();
		}

		private void OnDisable () {
			StopAllCoroutines();
		}

		public void Pop () {
			if (PopCor != null) StopCoroutine(PopCor);
			if (gameObject.activeInHierarchy) PopCor = StartCoroutine(Poping());
			IEnumerator Poping () {
				transform.localScale = Scale * m_Amount;
				IMG.color = m_Tint;
				while (true) {
					var newScl = Vector3.Lerp(transform.localScale, Scale, Time.deltaTime * m_Lerp);
					var newColor = Color.Lerp(IMG.color, Color, Time.deltaTime * m_Lerp);
					transform.localScale = newScl;
					IMG.color = newColor;
					if (newScl.StopLerp(Scale, 0.1f) && newColor.StopLerp(Color, 0.1f)) break;
					yield return new WaitForEndOfFrame();
				}
				transform.localScale = Scale;
				IMG.color = Color;
			}
		}

	}
}