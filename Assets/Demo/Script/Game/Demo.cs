using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moenen.Standard;
using UIGadget;
using BattleSoup;
using UnityEngine.UI;

namespace BattleSoupDemo {
	public class Demo : MonoBehaviour {




		#region --- SUB ---




		#endregion




		#region --- VAR ---


		// Ser
		[Header("Config")]
		[SerializeField] MapData m_DefaultMap = null;
		[Header("UI")]
		[SerializeField] RectTransform m_ConfigPanel = null;
		[SerializeField] RectTransform m_BattleZone = null;
		[SerializeField] RectTransform m_BattleInfo = null;
		[Header("Component")]
		[SerializeField] VectorGrid m_GridA = null;
		[SerializeField] VectorGrid m_GridB = null;
		[SerializeField] BlocksRenderer m_ShipRendererA = null;
		[SerializeField] BlocksRenderer m_ShipRendererB = null;
		[SerializeField] BlocksRenderer m_SonarRendererA = null;
		[SerializeField] BlocksRenderer m_SonarRendererB = null;
		[SerializeField] BlocksRenderer m_MapRendererA = null;
		[SerializeField] BlocksRenderer m_MapRendererB = null;
		[SerializeField] MapThumbnailRenderer m_SelectingMapRenderer = null;
		[SerializeField] Text m_SelectingMapInfo = null;

		// Data
		private bool IsPlaying = false;
		private MapData SelectingMap = null;


		#endregion




		#region --- MSG ---


		private void Start () {
			SelectMapLogic(m_DefaultMap);
		}


		private void Update () {
			Update_State();

		}


		private void Update_State () {
			m_ConfigPanel.gameObject.SetActive(!IsPlaying);
			m_BattleZone.gameObject.SetActive(IsPlaying);
			m_BattleInfo.gameObject.SetActive(IsPlaying);

		}


		#endregion




		#region --- API ---


		public void UI_SelectMap (MapData map) => SelectMapLogic(map);


		#endregion




		#region --- LGC ---


		// Soup
		private void RefreshSoups () {



		}


		private void SetSoupSize (int width, int height) {
			m_BattleZone.SetSizeWithCurrentAnchors(
				RectTransform.Axis.Vertical,
				m_ShipRendererA.rectTransform.rect.width * height / width
			);
			m_GridA.X = width;
			m_GridA.Y = height;
			m_GridB.X = width;
			m_GridB.Y = height;
			m_ShipRendererA.GridCountX = width;
			m_ShipRendererA.GridCountY = height;
			m_ShipRendererB.GridCountX = width;
			m_ShipRendererB.GridCountY = height;
			m_SonarRendererA.GridCountX = width;
			m_SonarRendererA.GridCountY = height;
			m_SonarRendererB.GridCountX = width;
			m_SonarRendererB.GridCountY = height;
			m_MapRendererA.GridCountX = width;
			m_MapRendererA.GridCountY = height;
			m_MapRendererB.GridCountX = width;
			m_MapRendererB.GridCountY = height;
			m_BattleZone.gameObject.SetActive(false);
			m_BattleZone.gameObject.SetActive(true);
		}


		// Map
		private void SelectMapLogic (MapData map) {
			SelectingMap = map;
			m_SelectingMapRenderer.Map = map;
			if (map != null) {
				m_SelectingMapInfo.text = $"{map.name} ({map.Size.x}¡Á{map.Size.y})";
			} else {
				m_SelectingMapInfo.text = "(No map selected)";
			}
		}


		#endregion




	}
}