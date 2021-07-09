using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Moenen.Standard;
using UnityEngine.UI;

namespace BattleSoup {
	public class SoupHighlightUI : MonoBehaviour, IPointerEnterHandler {




		// Ser
		[SerializeField] MapRenderer m_Map = null;
		[SerializeField] RectTransform m_Highlight = null;

		// Data
		private Coroutine MouseCor = null;


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
			MouseCor = StartCoroutine(MouseInside(m_Map.GridCountX));
			// Func
			IEnumerator MouseInside (int mapSize) {
				m_Highlight.gameObject.SetActive(true);
				while (true) {
					var pos = (transform as RectTransform).Get01Position(Input.mousePosition, Camera.main);
					if (pos.x <= 0f || pos.y <= 0f || pos.x >= 1f || pos.y >= 1f) { break; }

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


		public void Blink (int x, int y, Color color, Sprite sprite) {
			const float DURATION = 3f;
			const int COUNT = 8;
			StartCoroutine(Blinking(x, y, m_Map.GridCountX));
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
				Destroy(rt.gameObject, DURATION + 1f);
				var img = rt.GetComponent<Image>();
				img.color = color;
				img.sprite = sprite;
				for (float time = 0f; time < DURATION; time += Time.deltaTime) {
					img.enabled = (time * COUNT / DURATION) % 1f > 0.5f;
					yield return new WaitForEndOfFrame();
				}
				img.enabled = false;
			}
		}


	}
}
