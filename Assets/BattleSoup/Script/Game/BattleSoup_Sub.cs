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
			CardPrepare = 3,
			CardGame = 4,
			ShipEditor = 5,
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
		public class GameAsset {

			public RectTransform PanelRoot;
			public RectTransform PreparePanel;
			public RectTransform PlacePanel;
			public Button MapShipSelectorNextButton;
			public Canvas Canvas;
			public CanvasScaler CanvasScaler;
			public RectTransform TopUI;
			public RectTransform BottomUI;
			public PostProcessVolume EffectVolume;

			[Header("Dialog")]
			public RectTransform DialogRoot;
			public RectTransform QuitBattleDialog;
			public RectTransform NoShipAlert;
			public RectTransform NoMapAlert;
			public RectTransform FailPlacingShipsDialog;
			public RectTransform RobotFailedToPlaceShipsDialog;
			public RectTransform Dialog_Win;
			public RectTransform Dialog_WinCheat;
			public RectTransform Dialog_Lose;
			public RectTransform Dialog_LoseCheat;
			public RectTransform Dialog_AbilityCopy;
			public RectTransform CardFinalWinDialog;
			public RectTransform CardFinalLoseDialog;

			[Header("Map")]
			public RectTransform MapSelectorContentA;
			public Text MapSelectorLabelA;
			public RectTransform MapSelectorContentB;
			public Text MapSelectorLabelB;
			public Grabber MapSelectorItem;

			[Header("Fleet")]
			public RectTransform FleetSelectorPlayer;
			public RectTransform FleetSelectorPlayerContent;
			public RectTransform FleetSelectorRobotA;
			public Grabber FleetSelectorShipItem;
			public Text FleetSelectorLabelA;
			public Text FleetSelectorLabelB;
			public RectTransform FleetRendererA;
			public RectTransform FleetRendererB;
			public Grabber FleetRendererItem;
			public Dropdown RobotAiA;
			public Dropdown RobotAiB;

			[Header("Setting")]
			public Toggle SoundTG;
			public Toggle AutoPlayAvATG;
			public Toggle UseAnimationTG;
			public Toggle UseEffectTG;
			public Toggle[] UiScaleTGs;

			[Header("Playing")]
			public Toggle CheatTG;
			public Toggle DevTG;
			public Toggle DevHitTG;
			public Toggle DevCookTG;
			public Grabber ShipAbilityItem;
			public RectTransform AbilityContainerA;
			public RectTransform AbilityContainerB;
			public RectTransform PickingHint;
			public Image AvatarIconA;
			public Image AvatarIconB;
			public Text AvatarLabelA;
			public Text TurnLabel;
			public Button PlayAvA;
			public Button PauseAvA;
			public Button RestartAvA;
			public Text RobotDescriptionA;
			public Text RobotDescriptionB;
			public BlinkImage AbilityHintA;
			public BlinkImage AbilityHintB;

			[Header("Ship Editor")]
			public RectTransform ShipEditorWorkbenchRoot;
			public RectTransform ShipEditorBottomUI;
			public RectTransform ShipEditorFileContainer;
			public Grabber Workbench;
			public Grabber ShipEditorFileItem;
			public Grabber ShipEditorArtworkItem;
			public Grabber ShipEditorArtworkPopupItem;
			public Button DeleteShipButton;
			public Button ShipEditorUseAbilityButton;
			public Button ShipEditorRevealButton;
			public RectTransform ShipEditorArtworkContainer;
			public RectTransform ShipEditorArtworkPopupRoot;
			public RectTransform ShipEditorArtworkPopupContainer;
			public RectTransform ShipEditorBuiltInMark;
			public Button[] ShipEditorTabs;
			public RectTransform[] ShipEditorPanels;

			[Header("Asset")]
			public Sprite DefaultShipIcon;
			public Sprite PlayerAvatarIcon;
			public Sprite RobotAvatarIcon;
			public Sprite AngryRobotAvatarIcon;
			public Sprite EmptyMirrorShipIcon;
		}



		[System.Serializable]
		public class GameAsset_Card {

			[System.Serializable]
			public class Level {
				public string Fleet;
				public Texture2D Map;
				public string[] Cards;
			}

			public RectTransform LevelNumberRoot;
			public Text LevelNumber;
			public PopUI LevelNumberPop;
			public RectTransform EnemyShipContainer;
			public PopUI PlayerHpPop;
			public PopUI PlayerSpPop;
			public Text PlayerHp;
			public Text PlayerSp;
			public RectTransform PickingHint;
			public Image HeroAvatar;
			public Animator EnemyAni;
			public RectTransform DemonRoot;
			public RandomExplosionUI DemonExplosion;
			public RectTransform FinalWinDialogHeroRoot;
			public RectTransform[] PrepareCaps;
			[Header("Item")]
			public PlayerCardUI PlayerCard;
			public EnemyCardUI EnemyCard;
			public Grabber ShipItem;
			[Header("Slot")]
			public RectTransform PlayerSlot_From;
			public RectTransform PlayerSlot_Out;
			public RectTransform PlayerSlot_Dock;
			public RectTransform PlayerSlot_Performing;
			public RectTransform PlayerSlotBackground_Out;
			public RectTransform EnemySlot_From;
			public RectTransform EnemySlot_Out;
			public RectTransform EnemySlot_Performing;
			public RectTransform EnemySlotBackground_Out;
			[Header("Data")]
			public AnimationCurve FlipCurve;
			public Sprite[] TypeIcons;
			public Sprite[] EnemyCardSprites;
			public Sprite[] HeroIcons;
			public CardInfo[] BasicCards;
			public CardInfo[] AdditionalCards;
			public Level[] Levels;
		}



		public class ShipMeta {
			public int ID;
			public string DisplayName;
			public bool IsBuiltIn;
			public Sprite Icon;
		}


	}
}