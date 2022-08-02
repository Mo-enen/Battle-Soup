using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;


namespace BattleSoup {
	public partial class BattleSoup {


		public enum GameState {
			Title = 0,
			Prepare = 1,
			Playing = 2,
			CardGame = 3,
			ShipEditor = 4,
		}



		public enum GameMode {
			PvA = 0,
			AvA = 1,
		}



		public enum Turn {
			A = 0,
			B = 1,
		}



		[System.Serializable]
		private class GameAsset {

			public RectTransform PanelRoot = null;
			public RectTransform PreparePanel = null;
			public RectTransform PlacePanel = null;
			public Button MapShipSelectorNextButton = null;
			public CanvasScaler CanvasScaler = null;
			public RectTransform TopUI = null;
			public RectTransform BottomUI = null;
			public PostProcessVolume EffectVolume = null;

			[Header("Dialog")]
			public RectTransform DialogRoot = null;
			public RectTransform QuitBattleDialog = null;
			public RectTransform NoShipAlert = null;
			public RectTransform NoMapAlert = null;
			public RectTransform FailPlacingShipsDialog = null;
			public RectTransform RobotFailedToPlaceShipsDialog = null;
			public RectTransform Dialog_Win = null;
			public RectTransform Dialog_WinCheat = null;
			public RectTransform Dialog_Lose = null;
			public RectTransform Dialog_LoseCheat = null;
			public RectTransform Dialog_AbilityCopy = null;

			[Header("Map")]
			public RectTransform MapSelectorContentA = null;
			public Text MapSelectorLabelA = null;
			public RectTransform MapSelectorContentB = null;
			public Text MapSelectorLabelB = null;
			public Grabber MapSelectorItem = null;

			[Header("Fleet")]
			public RectTransform FleetSelectorPlayer = null;
			public RectTransform FleetSelectorPlayerContent = null;
			public RectTransform FleetSelectorRobotA = null;
			public Grabber FleetSelectorShipItem = null;
			public Text FleetSelectorLabelA = null;
			public Text FleetSelectorLabelB = null;
			public RectTransform FleetRendererA = null;
			public RectTransform FleetRendererB = null;
			public Grabber FleetRendererItem = null;
			public Dropdown RobotAiA = null;
			public Dropdown RobotAiB = null;

			[Header("Setting")]
			public Toggle SoundTG = null;
			public Toggle AutoPlayAvATG = null;
			public Toggle UseAnimationTG = null;
			public Toggle UseEffectTG = null;
			public Toggle[] UiScaleTGs = null;

			[Header("Playing")]
			public Toggle CheatTG = null;
			public Toggle DevTG = null;
			public Toggle DevHitTG = null;
			public Toggle DevCookTG = null;
			public Grabber ShipAbilityItem = null;
			public RectTransform AbilityContainerA = null;
			public RectTransform AbilityContainerB = null;
			public RectTransform PickingHint = null;
			public Image AvatarIconA = null;
			public Image AvatarIconB = null;
			public Text AvatarLabelA = null;
			public Text TurnLabel = null;
			public Button PlayAvA = null;
			public Button PauseAvA = null;
			public Button RestartAvA = null;
			public Text RobotDescriptionA = null;
			public Text RobotDescriptionB = null;
			public BlinkImage AbilityHintA = null;
			public BlinkImage AbilityHintB = null;

			[Header("Ship Editor")]
			public RectTransform ShipEditorWorkbenchRoot = null;
			public RectTransform ShipEditorBottomUI = null;
			public RectTransform ShipEditorFileContainer = null;
			public RectTransform ShipEditorArtworkContainer = null;
			public Grabber Workbench = null;
			public Grabber ShipEditorFileItem = null;
			public Grabber ShipEditorArtworkItem = null;
			public Button DeleteShipButton = null;
			public Button ShipEditorUseAbilityButton = null;
			public Button ShipEditorRevealButton = null;
			public Button[] ShipEditorTabs = null;
			public RectTransform[] ShipEditorPanels = null;

			[Header("Asset")]
			public Sprite DefaultShipIcon = null;
			public Sprite PlayerAvatarIcon = null;
			public Sprite RobotAvatarIcon = null;
			public Sprite AngryRobotAvatarIcon = null;
			public Sprite EmptyMirrorShipIcon = null;
		}



		public class ShipMeta {
			public int ID;
			public string DisplayName;
			public bool IsBuiltIn;
			public Sprite Icon;
		}


	}
}