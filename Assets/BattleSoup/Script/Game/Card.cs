using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class Card : MonoBehaviour {




		#region --- VAR ---


		// Api
		public bool Front {
			get {
				float a = Mathf.Abs(RT.localRotation.eulerAngles.y % 360f);
				return a < 90f || a > 270f;
			}
		}
		public bool InDock { get; set; } = true;
		public bool DynamicSlot { get; set; } = false;
		public bool Hovering { get; set; } = false;

		// Short
		protected Image Back => m_Back;
		protected Image FrontIMG => m_Front;
		protected TooltipUI Hint => m_Hint;
		protected RectTransform RT => _RT != null ? _RT : (_RT = transform as RectTransform);
		private RectTransform _RT = null;
		protected RectTransform Container => _Container != null ? _Container : (_Container = transform.parent as RectTransform);
		private RectTransform _Container = null;
		protected static BattleSoup Soup => _Soup != null ? _Soup : (_Soup = FindObjectOfType<BattleSoup>());
		private static BattleSoup _Soup = null;
		protected System.Action OnTriggered { get; private set; } = null;

		// Ser
		[SerializeField] Image m_Back;
		[SerializeField] Image m_Front;
		[SerializeField] TooltipUI m_Hint;
		[SerializeField] Text m_Number;

		// Data
		private Coroutine FlipCor = null;


		#endregion




		#region --- MSG ---


		protected virtual void OnEnable () => RefreshFrontBackUI();


		protected virtual void Update () {
			if (InDock) {
				if (DynamicSlot) {
					Update_DockInside();
				} else {
					Update_DockContainer();
				}
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
			var rot = RT.localRotation;
			rot.x = 0f;
			rot.z = 0f;
			RT.localRotation = rot;
		}


		private void Update_DockContainer () {
			RT.anchoredPosition3D = Vector2.Lerp(RT.anchoredPosition, Vector2.zero, Time.deltaTime * 10f);
			RT.localScale = Vector3.one;
			var rot = RT.localRotation;
			rot.x = 0f;
			rot.z = 0f;
			RT.localRotation = rot;
			if (!DynamicSlot && Mathf.Abs(RT.anchoredPosition3D.x) < 0.5f && Mathf.Abs(RT.anchoredPosition3D.y) < 0.5f) {
				Destroy(gameObject);
			}
		}


		#endregion




		#region --- API ---


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
				float duration = Soup.CardAssets.FlipCurve.Duration();
				for (float time = 0f; time < duration; time += Time.deltaTime) {
					float y01 = Soup.CardAssets.FlipCurve.Evaluate(time);
					if (_front) y01 = 1f - y01;
					RT.localRotation = Quaternion.Euler(0f, y01 * 180f, 0f);
					RefreshFrontBackUI();
					yield return new WaitForEndOfFrame();
				}
				RT.localRotation = Quaternion.Euler(0f, _front ? 0f : 180f, 0f);
				RefreshFrontBackUI();
			}
		}


		public void SetContainer (RectTransform container) {
			RT.SetParent(container, true);
			RT.localScale = Vector3.one;
			RT.anchoredPosition3D = Vector3.zero;
		}


		public void SetTrigger (System.Action trigger) => OnTriggered = trigger;


		public void Invoke () => OnTriggered?.Invoke();


		public void SetNumber (int value) => m_Number.text = value.ToString();


		#endregion




		#region --- LGC ---


		protected virtual void RefreshFrontBackUI () {
			bool front = Front;
			m_Back.gameObject.SetActive(!front);
			m_Front.gameObject.SetActive(front);
		}


		#endregion




	}
}