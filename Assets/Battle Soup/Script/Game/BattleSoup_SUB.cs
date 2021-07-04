using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSoup {
	public partial class BattleSoup {




		[System.Serializable]
		public struct CursorData {
			public Texture2D Cursor;
			public Vector2 Offset;
		}



		public enum GameState {
			BattleMode = 0,
			Ship = 1,
			Map = 2,
			PositionShip = 3,
			Playing = 4,
		}



		public enum BattleMode {
			PvA = 0,
			AvA = 1,
		}



		[System.Serializable]
		public enum Group {
			A = 0,
			B = 1,
		}



		// Data
		[System.Serializable]
		public struct PanelChest {
			public RectTransform LogoPanel;
			public RectTransform BattlePanel;
			public RectTransform ShipPanel;
			public RectTransform MapPanel;
			public RectTransform ShipPositionPanel;
			public RectTransform BattleZonePanel;
		}


		[System.Serializable]
		public struct GameChest {
			public ShipPositionUI ShipPositionUI;
			public BattleSoupUI BattleSoupUIA;
			public BattleSoupUI BattleSoupUIB;
		}


		[System.Serializable]
		public struct UIChest {
			public RectTransform ShipsToggleContainerA;
			public RectTransform ShipsToggleContainerB;
			public RectTransform MapsToggleContainerA;
			public RectTransform MapsToggleContainerB;
			public Text ShipLabelA;
			public Text ShipLabelB;
			public Text MapLabelA;
			public Text MapLabelB;
			public Button StartButton_Ship;
			public Button StartButton_Map;
			public Button StartButton_ShipPos;
			public Text StartMessage_Ship;
			public Text StartMessage_Map;
			public Text StartMessage_ShipPos;
			public Image[] BattleAvatarA;
			public Image[] BattleAvatarB;
		}


		[System.Serializable]
		public struct ResourceChest {
			public Sprite[] BattleAvatars;
			public ShipData[] Ships;
			public MapData[] Maps;
			public CursorData[] Cursors;
		}


	}
}
