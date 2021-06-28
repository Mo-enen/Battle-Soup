using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



namespace BattleSoupDemo {
	public class DraglessScrollview : ScrollRect {
		public override void OnBeginDrag (PointerEventData eventData) { }
		public override void OnDrag (PointerEventData eventData) { }
		public override void OnEndDrag (PointerEventData eventData) { }
	}
}
