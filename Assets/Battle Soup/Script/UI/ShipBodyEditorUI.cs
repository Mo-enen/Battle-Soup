using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using BattleSoupAI;
using UIGadget;
using Moenen.Standard;


namespace BattleSoup {
	public class ShipBodyEditorUI : MonoBehaviour, IPointerDownHandler {



		// SUB
		[System.Serializable] public class Int2Event : UnityEvent<Int2[]> { }

		// Ser
		[SerializeField] BlocksRenderer m_BlockRenderer = null;
		[SerializeField] VectorGrid m_Grid = null;
		[SerializeField] Int2Event m_OnValueChanged = null;

		// Data
		private readonly List<Int2> Body = new List<Int2>();


		// MSG
		public void OnPointerDown (PointerEventData eData) {
			if (eData.button != PointerEventData.InputButton.Left) { return; }
			var rt = transform as RectTransform;
			var pos01 = rt.Get01Position(eData.position, eData.pressEventCamera);
			if (pos01.x < 0f || pos01.x > 1f || pos01.y < 0f || pos01.y > 1f) { return; }
			int size = m_Grid.X;
			int localX = Mathf.FloorToInt(pos01.x * size);
			int localY = Mathf.FloorToInt(pos01.y * size);
			bool found = false;
			for (int i = 0; i < Body.Count; i++) {
				if (Body[i].x == localX && Body[i].y == localY) {
					if (Body.Count > 1) {
						Body.RemoveAt(i);
						i--;
					}
					found = true;
				}
			}
			if (!found) {
				Body.Add(new Int2(localX, localY));
			}
			m_OnValueChanged.Invoke(Body.ToArray());
		}


		// API
		public void RefreshUI (Ship ship) {
			ship.GroundBodyToZero();
			Body.Clear();
			Body.AddRange(ship.Body);
			var (min, max) = ship.GetBounds(false);
			int size = Mathf.Max(max.x - min.x + 1, max.y - min.y + 1);
			if (size < 16) {
				size++;
			}
			m_Grid.X = size;
			m_Grid.Y = size;
			m_Grid.SetVerticesDirty();
			m_BlockRenderer.GridCountX = size;
			m_BlockRenderer.GridCountY = size;
			m_BlockRenderer.ClearBlock();
			foreach (var block in ship.Body) {
				int x = block.x - min.x;
				int y = block.y - min.y;
				m_BlockRenderer.AddBlock(x, y, 0, Color.white);
			}
			m_BlockRenderer.SetVerticesDirty();
		}


	}
}
