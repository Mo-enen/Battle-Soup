using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Moenen.Standard;
using UnityEngine.UI;
using UIGadget;


namespace BattleSoup {
	public class SoupHighlightUI : MonoBehaviour, IPointerEnterHandler {


		// Short
		private int Size {
			get {
				if (m_Map != null) {
					return m_Map.GridCountX;
				}
				if (m_Grid != null) {
					return m_Grid.X;
				}
				return 0;
			}
		}

		// Ser
		[SerializeField] MapRenderer m_Map = null;
		[SerializeField] VectorGrid m_Grid = null;
		[SerializeField] RectTransform m_Highlight = null;

		// Data
		private Coroutine MouseCor = null;
		private float BlinkTime = 0f;


		// MSG
		private void OnDisable () {
			if (MouseCor != null) {
				StopCoroutine(MouseCor);
			}
			StopAllCoroutines();
		}


		public void OnPointerEnter (PointerEventData eData) {
			if (MouseCor != null) {
				StopCoroutine(MouseCor);
			}
			MouseCor = StartCoroutine(MouseInside());
			// Func
			IEnumerator MouseInside () {
				m_Highlight.gameObject.SetActive(true);
				while (true) {
					var pos = (transform as RectTransform).Get01Position(Input.mousePosition, Camera.main);
					if (pos.x <= 0f || pos.y <= 0f || pos.x >= 1f || pos.y >= 1f) { break; }

					int mapSize = Size;

					pos.x = Mathf.Floor(pos.x * mapSize) / mapSize;
					pos.y = Mathf.Floor(pos.y * mapSize) / mapSize;

					m_Highlight.anchorMin = pos;
					m_Highlight.anchorMax = new Vector2(
						pos.x + 1f / mapSize,
						pos.y + 1f / mapSize
					);
					m_Highlight.offsetMin = Vector2.zero;
					m_Highlight.offsetMax = Vector2.zero;

					yield return new WaitForEndOfFrame();
				}
				m_Highlight.gameObject.SetActive(false);
				MouseCor = null;
			}
		}


		public void Blink (int x, int y, Color color, Sprite sprite, float alpha = 0f, float duration = 0.618f, int count = 4) {
			StartCoroutine(Blinking(x, y, Size));
			IEnumerator Blinking (int _x, int _y, int _size) {
				var rt = new GameObject("Blinking", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform as RectTransform;
				rt.SetParent(transform);
				rt.localScale = Vector3.one;
				rt.localRotation = Quaternion.identity;
				rt.anchoredPosition3D = rt.anchoredPosition;
				rt.pivot = Vector2.zero;
				rt.anchorMin = new Vector2((float)_x / _size, (float)_y / _size);
				rt.anchorMax = new Vector2((_x + 1f) / _size, (_y + 1f) / _size);
				rt.offsetMin = Vector2.zero;
				rt.offsetMax = Vector2.zero;
				rt.SetAsLastSibling();
				Destroy(rt.gameObject, duration + 1f);
				var img = rt.GetComponent<Image>();
				float alphaA = color.a;
				img.color = color;
				img.sprite = sprite;
				float blinkTime = Time.time;
				img.enabled = true;
				for (float time = 0f; time < duration && blinkTime > BlinkTime; time += Time.deltaTime) {
					color.a = (time * count / duration) % 1f > 0.5f ? alpha : alphaA;
					img.color = color;
					yield return new WaitForEndOfFrame();
				}
				img.enabled = false;
			}
		}


		public void ClearAllBlinks () => BlinkTime = Time.time - 0.01f;


	}
}
