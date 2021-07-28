using System.Collections;
using System.Collections.Generic;
using UIGadget;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSoup {



	[System.Serializable]
	public enum Group {
		A = 0,
		B = 1,
	}


	public enum BattleMode {
		PvA = 0,
		AvA = 1,
	}



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
			ShipEditor = 5,
		}




		// Data
		[System.Serializable]
		public struct PanelData {
			public RectTransform LogoPanel;
			public RectTransform BattlePanel;
			public RectTransform ShipPanel;
			public RectTransform MapPanel;
			public RectTransform ShipPositionPanel;
			public RectTransform BattleZonePanel;
			public RectTransform ShipEditorPanel;
			public RectTransform QuitGameWindow;
		}



		[System.Serializable]
		public struct GameData {
			public Game Game;
			public Asset Asset;
			public ShipPositionUI ShipPositionUI;
			public BattleSoupUI BattleSoupUIA;
			public BattleSoupUI BattleSoupUIB;
			public Grabber AbilityShip;
		}



		[System.Serializable]
		public struct UIData {
			public Grabber ShipSelectionItem;
			public RectTransform ShipSelectionContainer;
			public RectTransform ShipsButtonContainer;
			public RectTransform MapsToggleContainerA;
			public RectTransform MapsToggleContainerB;
			public RectTransform AbilityContainerA;
			public RectTransform AbilityContainerB;
			public RectTransform MessageRoot;
			public Text MapLabelA;
			public Text MapLabelB;
			public Button StartButton_Ship;
			public Button StartButton_Map;
			public Button StartButton_ShipPos;
			public Text StartMessage_Ship;
			public Text StartMessage_Map;
			public Text StartMessage_ShipPos;
			public Text MessageText;
			public RectTransform TurnArrowA;
			public RectTransform TurnArrowB;
			public Toggle SoundTG;
			public Toggle AutoPlayAvATG;
			public Dropdown StrategyDropA;
			public Dropdown StrategyDropB;
			public Text StrategyDescriptionA;
			public Text StrategyDescriptionB;
			public Button PositionShipResetButton;
			public Sprite DevValueIterIcon;
			public Image[] BattleAvatarA;
			public Image[] BattleAvatarB;
		}



		[System.Serializable]
		public struct ResourceData {
			public Sprite[] BattleAvatars;
			public CursorData[] Cursors;
		}



	}
}
