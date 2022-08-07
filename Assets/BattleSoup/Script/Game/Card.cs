using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AngeliaFramework;


namespace BattleSoup {
	public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {




		#region --- VAR ---


		// Api
		public bool Front {
			get {
				float a = Mathf.Abs(RT.localRotation.eulerAngles.y % 360f);
				return a < 90f || a > 270f;
			}
		}
		public bool InDock { get; set; } = true;
		public bool InSlot => transform.parent == m_Slot;
		public bool RequireDoubleClick { get; set; } = true;

		// Ser
		[SerializeField] Image m_Back;
		[SerializeField] Image m_Front;
		[SerializeField] Image m_Icon;
		[SerializeField] Image m_TypeIcon;
		[SerializeField] RectTransform m_ClickedMark;
		[SerializeField] RectTransform m_SlotFrom;
		[SerializeField] RectTransform m_Slot;
		[SerializeField] RectTransform m_SlotOutside;
		[SerializeField] CardConfig m_Config;

		// Short
		private RectTransform RT => _RT != null ? _RT : (_RT = transform as RectTransform);
		private RectTransform _RT = null;
		private RectTransform Container => _Container != null ? _Container : (_Container = transform.parent as RectTransform);
		private RectTransform _Container = null;
		private static BattleSoup Soup => _Soup != null ? _Soup : (_Soup = FindObjectOfType<BattleSoup>());
		private static BattleSoup _Soup = null;

		// Data
		private Coroutine FlipCor = null;
		private bool Hovering = false;
		private bool ClickedOnce = false;
		private System.Action OnClick = null;


		#endregion




		#region --- MSG ---


		private void OnEnable () {
			Hovering = false;
			ClickedOnce = false;
			RefreshFrontBackUI();
		}


		private void Update () {

			// Position
			if (InDock) {
				if (InSlot) {
					Update_DockInside();
				} else {
					Update_DockContainer();
				}
			}

			// Misc
			if (m_ClickedMark.gameObject.activeSelf != ClickedOnce) {
				m_ClickedMark.gameObject.SetActive(ClickedOnce);
			}

		}


		private void OnDestroy () {
			if (FlipCor != null) {
				StopCoroutine(FlipCor);
				FlipCor = null;
			}
		}


		private void OnDisable () {
			if (FlipCor != null) {
				StopCoroutine(FlipCor);
				FlipCor = null;
			}
		}


		private void Update_DockInside () {
			int index = transform.GetSiblingIndex();
			int count = Container.childCount;
			var aimPosition = new Vector2(
				Mathf.Lerp(0f, Container.rect.width, count > 1 ? (index + 1f) / (count + 1f) : 0.5f),
				Hovering ? 40f : 11f
			);
			RT.anchoredPosition3D = Vector2.Lerp(RT.anchoredPosition, aimPosition, Time.deltaTime * 20f);
			RT.localScale = Vector3.one;
			RT.localRotation = Quaternion.identity;
		}


		private void Update_DockContainer () {
			RT.anchoredPosition3D = Vector2.Lerp(RT.anchoredPosition, Vector2.zero, Time.deltaTime * 10f);
			RT.localScale = Vector3.one;
			RT.localRotation = Quaternion.identity;
			if (transform.parent == m_SlotOutside && Mathf.Abs(RT.anchoredPosition3D.x) < 0.5f && Mathf.Abs(RT.anchoredPosition3D.y) < 0.5f) {
				Destroy(gameObject);
			}
		}


		#endregion




		#region --- API ---


		// Card Info
		public void Init (CardInfo info, System.Action action) {
			if (!info.IsShip) {
				// Built-in
				m_Icon.sprite = m_Config.TypeIcons[(int)info.Type];
				m_TypeIcon.sprite = null;
			} else {
				// Ship
				int id = info.GlobalName.AngeHash();
				m_Icon.sprite = Soup.TryGetShip(id, out var ship) ? ship.Icon : null;
				m_TypeIcon.sprite = m_Config.TypeIcons[(int)info.Type];
			}
			m_Icon.gameObject.SetActive(m_Icon.sprite != null);
			m_TypeIcon.gameObject.SetActive(m_TypeIcon.sprite != null);
			OnClick = action;
			// Make From
			if (Front) Flip(false, false);
			RT.SetParent(m_SlotFrom, true);
			RT.localScale = Vector3.one;
			RT.anchoredPosition3D = Vector3.zero;
		}


		public void Flip (bool front, bool useAnimation = true) {
			if (FlipCor != null) {
				StopCoroutine(FlipCor);
				FlipCor = null;
			}
			if (useAnimation) {
				FlipCor = StartCoroutine(Flipping(front));
			} else {
				RT.localRotation = Quaternion.Euler(0f, front ? 0f : 180f, 0f);
			}
			// Func
			IEnumerator Flipping (bool _front) {
				float duration = m_Config.Flip.Duration();
				for (float time = 0f; time < duration; time += Time.deltaTime) {
					float y01 = m_Config.Flip.Evaluate(time);
					if (_front) y01 = 1f - y01;
					RT.localRotation = Quaternion.Euler(0f, y01 * 180f, 0f);
					RefreshFrontBackUI();
					yield return new WaitForEndOfFrame();
				}
				RT.localRotation = Quaternion.Euler(0f, _front ? 0f : 180f, 0f);
				RefreshFrontBackUI();
			}
		}


		public void MakeOutside () {
			if (Front) Flip(false);
			RT.SetParent(m_SlotOutside, true);
			RT.localScale = Vector3.one;
		}


		public void MakeInside () {
			if (!Front) Flip(true);
			RT.SetParent(m_Slot, true);
			RT.SetAsFirstSibling();
			RT.localScale = Vector3.one;
		}


		// MSG
		public void OnPointerEnter (PointerEventData eventData) => Hovering = true;


		public void OnPointerExit (PointerEventData eventData) {
			Hovering = false;
			ClickedOnce = false;
		}


		public void OnPointerClick (PointerEventData eventData) {
			if (eventData.button != PointerEventData.InputButton.Left) return;
			if (!RequireDoubleClick || ClickedOnce) {
				ClickedOnce = false;
				OnClick?.Invoke();
			} else {
				ClickedOnce = true;
			}
		}


		#endregion




		#region --- LGC ---


		private void RefreshFrontBackUI () {
			bool front = Front;
			m_Back.gameObject.SetActive(!front);
			m_Front.gameObject.SetActive(front);
			m_Icon.gameObject.SetActive(front);
			m_TypeIcon.gameObject.SetActive(front);
			m_ClickedMark.gameObject.SetActive(front);

		}


		#endregion




	}
}



#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEngine;
	using UnityEditor;
	[CustomEditor(typeof(Card))]
	[CanEditMultipleObjects]
	public class Card_Inspector : Editor {
		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script");
			serializedObject.ApplyModifiedProperties();
			if (EditorApplication.isPlaying) {
				if (GUILayout.Button("Flip")) {
					foreach (var target in targets) {
						var card = target as Card;
						card.Flip(!card.Front);
					}
				}
				if (GUILayout.Button("Out")) {
					foreach (var target in targets) {
						var card = target as Card;
						card.MakeOutside();
					}
				}
			}
		}
	}
}
#endif
