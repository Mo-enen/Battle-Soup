using System.Collections;
using System.Collections.Generic;
using UnityEngine;




namespace Moenen.Standard {
	public static class EditorExtension {



		// Number
		public static bool GetBit (this ulong value, int index) => (value & (1UL << index)) != 0;
		public static bool GetBit (this int value, int index) => (value & (1 << index)) != 0;


		public static ulong SettedBitValue (this ulong value, int index, bool bitValue) {
			if (index < 0 || index > 63) { return value; }
			var val = 1UL << index;
			return bitValue ? (value | val) : (value & ~val);
		}
		public static int SettedBitValue (this int value, int index, bool bitValue) {
			if (index < 0 || index > 31) { return value; }
			var val = 1 << index;
			return bitValue ? (value | val) : (value & ~val);
		}
		public static void SetBitValue (this ref ulong value, int index, bool bitValue) => value = value.SettedBitValue(index, bitValue);
		public static void SetBitValue (this ref int value, int index, bool bitValue) => value = value.SettedBitValue(index, bitValue);


		// Vector
		public static void Clamp (this ref Vector2Int v, int minX, int minY, int maxX = int.MaxValue, int maxY = int.MaxValue) {
			v.x = Mathf.Clamp(v.x, minX, maxX);
			v.y = Mathf.Clamp(v.y, minY, maxY);
		}



		public static Vector2Int Clamped (this Vector2Int v, int minX, int minY, int maxX = int.MaxValue, int maxY = int.MaxValue) {
			v.Clamp(minX, minY, maxX, maxY);
			return v;
		}



		public static bool SimilarWith (this Vector3 a, Vector3 b) => Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);



		// Transform
		public static void DestroyAllChirldrenImmediate (this Transform target) {
			int childCount = target.childCount;
			for (int i = 0; i < childCount; i++) {
				Object.DestroyImmediate(target.GetChild(0).gameObject, false);
			}
		}



		public static bool IsChildOf (this Transform tf, Transform root) {
			while (tf != null && tf != root) {
				tf = tf.parent;
			}
			return tf == root;
		}



		public static void SetHideFlagForAllChildren (this Transform target, HideFlags flag) {
			target.gameObject.hideFlags = flag;
			foreach (Transform t in target) {
				SetHideFlagForAllChildren(t, flag);
			}
		}



		public static void ClampInsideParent (this RectTransform target) {
			target.anchoredPosition = VectorClamp2(
				target.anchoredPosition,
				target.pivot * target.rect.size - target.anchorMin * ((RectTransform)target.parent).rect.size,
				(Vector2.one - target.anchorMin) * ((RectTransform)target.parent).rect.size - (Vector2.one - target.pivot) * target.rect.size
			);
			static Vector2 VectorClamp2 (Vector2 v, Vector2 min, Vector2 max) => new Vector2(
				Mathf.Clamp(v.x, min.x, max.x),
				Mathf.Clamp(v.y, min.y, max.y)
			);
		}


		// Coroutine
		public static Coroutine StartBetterCoroutine (this MonoBehaviour beh, IEnumerator routine, System.Action onFinished = null) => beh.StartBetterCoroutine(routine, (ex) => onFinished?.Invoke());
		public static Coroutine StartBetterCoroutine (this MonoBehaviour beh, IEnumerator routine, System.Action<System.Exception> onFinished = null) {
			var cor = beh.StartCoroutine(Coroutine());
			return cor;
			// Func
			IEnumerator Coroutine () {
				while (true) {
					bool done;
					try {
						done = routine.MoveNext();
					} catch (System.Exception ex) {
						onFinished?.Invoke(ex);
						yield break;
					}
					if (done) {
						yield return routine.Current;
					} else { break; }
				}
				onFinished?.Invoke(null);
			}
		}



	}
}
