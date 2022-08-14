using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AngeliaFramework;
using UnityEngine.UI;
using System.Text;


namespace BattleSoup {
	public partial class BattleSoup {





		#region --- SUB ---


		public enum Hero {
			Nerd,
			GG,
			Nessie,
			Hacker,
		}


		#endregion




		#region --- VAR ---


		// Short
		private int Card_PlayerDrawCardCount => CurrentHero == Hero.Nerd ? 4 : 3;
		private int Card_PlayerPerformCardCount => CurrentHero == Hero.Hacker ? 2 : 1;

		// Data
		private readonly Stack<CardInfo> Card_PlayerStack = new();
		private readonly Stack<int> Card_EnemyStack = new();
		private readonly List<Map> Card_Maps = new();
		private readonly Dictionary<int, EnemyCard> Card_EnemyCardPool = new();
		private readonly List<CardUI> Card_CacheCards = new();
		private Hero CurrentHero = Hero.Nerd;
		private int Card_PlayerHP = 10;
		private int Card_PlayerSP = 0;
		private int CardLevel = 0;
		private EnemyCard Card_PerformingEnemyCard = null;
		private bool LastShipExposed = false;
		private int Card_PlayerStun = 0;


		#endregion




		#region --- MSG ---


		private void Init_Card () {

			CurrentHero = Hero.Nerd;

			Card_Maps.Clear();
			foreach (var level in m_CardAssets.Levels) Card_Maps.Add(new Map(level.Map));

			var iconPool = new Dictionary<int, Sprite>();
			foreach (var sp in m_CardAssets.EnemyCardSprites) iconPool.TryAdd(sp.name.AngeHash(), sp);
			Card_EnemyCardPool.Clear();
			foreach (var type in typeof(EnemyCard).AllChildClass()) {
				int id = type.AngeHash();
				var card = System.Activator.CreateInstance(type) as EnemyCard;
				Card_EnemyCardPool.TryAdd(id, card);
				card.Icon = iconPool.TryGetValue(id, out var sp) ? sp : null;
			}

		}


		private void SwitchState_CardGame () {

			FieldA.Enable = false;

			FieldB.Enable = true;
			FieldB.AllowHoveringOnShip = false;
			FieldB.ShowShips = true;
			FieldB.DragToMoveShips = false;
			FieldB.RightClickToFlipShips = false;
			FieldB.DrawPickingArrow = false;
			FieldB.DrawCookedInfo = false;
			FieldB.DrawDevInfo = false;
			FieldB.ClickToAttack = false;

			FieldB.HideInvisibleShip = true;

			Cheating = false;
			PickingPosition = default;
			PickingDirection = default;
			GameOver = false;

			m_CardAssets.HeroAvatar.sprite = m_CardAssets.HeroIcons[(int)CurrentHero];
			for (int i = 0; i < m_CardAssets.FinalWinDialogHeroRoot.childCount; i++) {
				m_CardAssets.FinalWinDialogHeroRoot.GetChild(i).gameObject.SetActive(
					(int)CurrentHero == i
				);
			}

			m_CardAssets.DemonRoot.gameObject.SetActive(true);

			Card_GotoLevel(0);

		}


		private void Update_Card () {

			RefreshDialogFrame();
			bool noDialog = GlobalFrame > DialogFrame + 4;
			bool playerPicking = !GameOver && CellStep.CurrentStep is sPick;
			bool picking = !GameOver && CellStep.CurrentStep is sPick;

			FieldB.AllowHoveringOnWater = noDialog && playerPicking;
			FieldB.SetPickingDirection(PickingDirection);

			// Picking Hint
			if (m_CardAssets.PickingHint.gameObject.activeSelf != picking) {
				m_CardAssets.PickingHint.gameObject.SetActive(picking);
			}

			// Fill up Steps for a whole turn
			if (!GameOver && CellStep.CurrentStep == null) {

				Card_RefreshFleetUI();

				// Deal for Player
				int count = Card_PlayerDrawCardCount;
				if (Card_PlayerStun > 0) count -= 2;
				for (int i = 0; i < count; i++) {
					CellStep.AddToLast(new scDealPlayerCard(8));
				}

				// Player Turn
				CellStep.AddToLast(new scWaitForPlayer());
				if (CurrentHero == Hero.Hacker) CellStep.AddToLast(new scWaitForPlayer());

				// Stun
				CellStep.AddToLast(new scReducePlayerStun());

				// End Player Turn
				CellStep.AddToLast(new scClearPlayerCards());

				// Enemy Turn
				if (Card_PerformingEnemyCard == null) {
					CellStep.AddToLast(new scDealEnemyCard());
				}
				CellStep.AddToLast(new scPerformEnemyCard());
				CellStep.AddToLast(new scClearEnemyPerformedCard());

			}

			// Waiting for Player to Pick a Card
			if (!GameOver && CellStep.CurrentStep is scWaitForPlayer) {
				// Clear Performing UI
				if (m_CardAssets.PlayerSlot_Performing.childCount > 0) {
					Card_ClearPlayerCards_Performing();
				}
				// GG Guy
				if (!GameOver && CurrentHero == Hero.GG && !LastShipExposed && FieldB.AliveShipCount <= 1) {
					LastShipExposed = true;
					foreach (var ship in FieldB.Ships) {
						if (!ship.Exposed && ship.Alive) FieldB.Expose(ship.FieldX, ship.FieldY);
					}
				}
			}

			// Slot BG UI Refresh
			if (!m_CardAssets.PlayerSlotBackground_Out.gameObject.activeSelf) {
				bool show = m_CardAssets.PlayerSlot_Out.childCount > 0;
				if (show) m_CardAssets.PlayerSlotBackground_Out.gameObject.SetActive(true);
			}
			if (!m_CardAssets.EnemySlotBackground_Out.gameObject.activeSelf) {
				bool show = m_CardAssets.EnemySlot_Out.childCount > 0;
				if (show) m_CardAssets.EnemySlotBackground_Out.gameObject.SetActive(true);
			}

			// Gameover Check
			if (!GameOver) {
				bool playerWin = FieldB.AllShipsSunk();
				bool enemyWin = Card_PlayerHP <= 0;
				if (playerWin || enemyWin) {
					// Game Over
					GameOver = true;
					CellStep.Clear();
					Card_ClearPlayerCards_Performing();
					Card_ClearPlayerCards_Dock();
					Card_PerformingEnemyCard = null;
					Card_ClearEnemyCards_Performing();
					Card_RefreshFleetUI();
					if (playerWin) {
						if (CardLevel >= m_CardAssets.Levels.Length - 1) {
							// Final Win
							CellStep.AddToLast(new scWait(24));
							CellStep.AddToLast(new scDemonExplosion());
							CellStep.AddToLast(new scFinalWin());
							Card_SetFinalWin((int)CurrentHero);
						} else {
							// Win
							CellStep.AddToLast(new scWait(24));
							CellStep.AddToLast(new scDemonExplosion());
							CellStep.AddToLast(new scGotoNextLevel());
							CellStep.AddToLast(new scDemonBack());
							CellStep.AddToLast(new scWait(24));
						}
					} else {
						// Lose
						CellStep.AddToLast(new scWait(24));
						CellStep.AddToLast(new scFinalLose());
					}
				}
			}

		}


		#endregion




		#region --- API ---


		// Deal
		public void Card_DealForPlayer () {
			if (Card_PlayerStack.Count == 0) Card_FillPlayerStack(CardLevel);
			if (Card_PlayerStack.Count == 0) return;
			var info = Card_PlayerStack.Pop();
			var card = Instantiate(m_CardAssets.PlayerCard, m_CardAssets.PlayerSlot_From);
			card.gameObject.SetActive(true);
			card.Flip(false, false);
			card.SetContainer(m_CardAssets.PlayerSlot_From, true);
			card.DynamicSlot = true;
			card.DestoryWhenReady = false;
			card.SetInfo(info);
			card.Flip(true);
			card.SetContainer(m_CardAssets.PlayerSlot_Dock);
			card.SetTrigger(() => Card_TriggerPlayerCard(info, card));
		}


		public void Card_DealForEnemy () {
			if (Card_PerformingEnemyCard != null) return;
			if (Card_EnemyStack.Count == 0) Card_FillEnemyStack(CardLevel);
			if (Card_EnemyStack.Count == 0) return;
			int id = Card_EnemyStack.Pop();
			if (!Card_EnemyCardPool.TryGetValue(id, out var card)) return;
			Card_PerformingEnemyCard = card;
			card.Start();
			// Clear UI
			Card_ClearEnemyCards_Performing();
			// Deal
			var cardUI = Instantiate(m_CardAssets.EnemyCard, m_CardAssets.EnemySlot_From);
			cardUI.gameObject.SetActive(true);
			cardUI.Flip(false, false);
			cardUI.SetContainer(m_CardAssets.EnemySlot_From, true);
			cardUI.DynamicSlot = false;
			cardUI.DestoryWhenReady = false;
			cardUI.SetInfo(card);
			cardUI.Flip(true);
			cardUI.SetContainer(m_CardAssets.EnemySlot_Performing);
		}


		// Perform
		public bool Card_PerformEnemyCard () {
			var card = Card_PerformingEnemyCard;
			if (card == null) return false;
			bool performed = card.Turn(this);
			var cardUI = m_CardAssets.EnemySlot_Performing.GetComponentInChildren<EnemyCardUI>(true);
			if (cardUI != null) cardUI.SetInfo(card);
			return performed;
		}


		// Clear
		public void Card_ClearEnemyPerformedCards () {
			var card = Card_PerformingEnemyCard;
			if (card != null && card.Performed) {
				Card_PerformingEnemyCard = null;
				Card_ClearEnemyCards_Performing();
			}
		}


		public void Card_ClearPlayerCards_Performing (bool immediately = false) => Card_ClearCardsLogic(m_CardAssets.PlayerSlot_Performing, m_CardAssets.PlayerSlot_Out, immediately);
		public void Card_ClearPlayerCards_Dock (bool immediately = false) => Card_ClearCardsLogic(m_CardAssets.PlayerSlot_Dock, m_CardAssets.PlayerSlot_Out, immediately);
		public void Card_ClearEnemyCards_Performing (bool immediately = false) => Card_ClearCardsLogic(m_CardAssets.EnemySlot_Performing, m_CardAssets.EnemySlot_Out, immediately);


		// Deck
		public void Card_DamagePlayer (int damage) {
			if (damage <= 0) return;
			int damageShied = Mathf.Min(Card_PlayerSP.Clamp(0, int.MaxValue), damage);
			int damageHealth = damage - damageShied;
			if (damageShied > 0) {
				Card_SetPlayerSP(Card_PlayerSP - damageShied);
				AudioPlayer.PlaySound("DamageShield".AngeHash());
			}
			if (damageHealth > 0) {
				Card_SetPlayerHP(Card_PlayerHP - damageHealth);
				AudioPlayer.PlaySound("Damage".AngeHash());
			}
		}


		public void Card_HealPlayer (int heal) {
			if (heal <= 0) return;
			Card_SetPlayerHP(Card_PlayerHP + heal);
		}


		public void Card_ShieldPlayer (int shield) {
			if (shield < 0) return;
			Card_SetPlayerSP(Card_PlayerSP + shield);
		}


		public void Card_StunPlayer (int stun) {
			stun = stun.Clamp(0, int.MaxValue);
			Card_PlayerStun += stun;
			if (stun > 0) {
				AudioPlayer.PlaySound("Stun".AngeHash());
			}
			Card_RefreshStunUI();
		}


		public void Card_ReducePlayerStun () {
			Card_PlayerStun = (Card_PlayerStun - 1).Clamp(0, int.MaxValue);
			Card_RefreshStunUI();
		}


		// Workflow
		public void Card_GotoNextLevel () => Card_GotoLevel(CardLevel + 1);


		public void Card_FinalWin () => m_Assets.CardFinalWinDialog.gameObject.SetActive(true);


		public void Card_FinalLose () => m_Assets.CardFinalLoseDialog.gameObject.SetActive(true);


		// UI
		public void UI_SetHero_Nerd (bool isOn) {
			if (isOn) CurrentHero = Hero.Nerd;
		}
		public void UI_SetHero_GG (bool isOn) {
			if (isOn) CurrentHero = Hero.GG;
		}
		public void UI_SetHero_Nessie (bool isOn) {
			if (isOn) CurrentHero = Hero.Nessie;
		}
		public void UI_SetHero_Hacker (bool isOn) {
			if (isOn) CurrentHero = Hero.Hacker;
		}


		#endregion




		#region --- LGC ---


		private void Card_GotoLevel (int level) {

			GameOver = false;
			level = level.Clamp(0, m_CardAssets.Levels.Length - 1);
			CardLevel = level;
			Card_SetPlayerHP(Card_GetPlayerMaxHP());
			Card_SetPlayerSP(Card_GetPlayerDefaultSP());
			LastShipExposed = false;
			FieldB.Enable = true;
			Card_PlayerStun = 0;
			Card_RefreshStunUI();

			// Stack
			Card_FillPlayerStack(level);
			Card_FillEnemyStack(level);

			// Map
			FieldB.SetMap(Card_Maps[level.Clamp(0, Card_Maps.Count - 1)], false);

			// Level
			var levelAsset = m_CardAssets.Levels[level];

			// Fleet
			var ships = GetShipsFromFleetString(levelAsset.Fleet);
			if (ships == null || ships.Length == 0) ships = GetShipsFromFleetString("Sailboat,SeaMonster,Longboat,MiniSub");
			FieldB.SetShips(ships);
			FieldB.RandomPlaceShips(128);
			FieldB.GameStart();

			// Final
			Card_ReloadFleetUI();
			m_CardAssets.LevelNumber.text = (level + 1).ToString("00");
			m_CardAssets.PlayerSlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.EnemySlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.LevelNumberPop.Pop();
			Card_ClearPlayerCards_Performing();
			Card_ClearPlayerCards_Dock();
			m_Assets.PanelRoot.pivot = Vector2.one * 0.5f;
			m_Assets.PanelRoot.localRotation = Quaternion.identity;
			m_CardAssets.DemonExplosion.gameObject.SetActive(false);
			m_CardAssets.EnemyAni.SetBool("Lose", false);
			m_CardAssets.DemonRoot.anchoredPosition3D = Vector3.zero;
			m_CardAssets.DemonRoot.localScale = Vector3.one;

		}


		private void Card_SetPlayerHP (int newHP) {
			newHP = newHP.Clamp(0, Card_GetPlayerMaxHP());
			if (newHP != Card_PlayerHP) {
				Card_PlayerHP = newHP;
				m_CardAssets.PlayerHpPop.Pop();
			}
			m_CardAssets.PlayerHp.text = Card_PlayerHP.ToString("00");
		}


		private void Card_SetPlayerSP (int newSP) {
			newSP = newSP.Clamp(0, int.MaxValue);
			if (newSP != Card_PlayerSP) {
				Card_PlayerSP = newSP;
				m_CardAssets.PlayerSpPop.Pop();
			}
			m_CardAssets.PlayerSpPop.gameObject.SetActive(Card_PlayerSP > 0);
			m_CardAssets.PlayerSp.text = Card_PlayerSP.ToString("00");
		}


		private void Card_ReloadFleetUI () {
			m_CardAssets.EnemyShipContainer.DestroyAllChirldrenImmediate();
			foreach (var ship in FieldB.Ships) {
				var grab = Instantiate(m_CardAssets.ShipItem, m_CardAssets.EnemyShipContainer);
				grab.ReadyForInstantiate();
				var icon = grab.Grab<Image>("Icon");
				var tip = grab.Grab<TooltipUI>();
				var shape = grab.Grab<ShipShapeUI>();
				icon.sprite = ship.Icon;
				tip.Tooltip = $"Enemy Ship {ship.DisplayName}";
				shape.Ship = ship;
			}
		}


		private void Card_TriggerPlayerCard (CardInfo info, PlayerCardUI card) {
			if (CellStep.CurrentStep is not scWaitForPlayer wait) return;
			if (info.Type == CardType.Heart && Card_PlayerHP >= Card_GetPlayerMaxHP()) return;
			wait.StopWaiting();
			// Perform
			if (!info.IsShip) {
				// Built In
				switch (info.Type) {
					case CardType.Attack:
						CellStep.AddToFirst(new sAttack() {
							AimToPickedPosition = true,
							Fast = false,
							Field = FieldB,
							Ship = null,
						});
						CellStep.AddToFirst(new sPick(FieldB, FieldA, null, null, ActionKeyword.Hittable, true));
						break;
					case CardType.Shield:
						CellStep.AddToFirst(new scShieldPlayer(2, 22));
						break;
					case CardType.Heart:
						CellStep.AddToFirst(new scHealPlayer(2, 22));
						break;
					case CardType.Card:
						CellStep.AddToFirst(new scWaitForPlayer());
						CellStep.AddToFirst(new scDealPlayerCard());
						CellStep.AddToFirst(new scDealPlayerCard());
						break;
				}
			} else {
				// Ship
				int id = info.GlobalName.AngeHash();
				if (TryGetShip(id, out var ship) && TryGetAbility(id, out var ability)) {
					PerformAbility(ability, ship, EntranceType.OnAbilityUsed, FieldA, FieldB, true);
				}
			}
			// Final
			card.Flip(true);
			card.SetContainer(m_CardAssets.PlayerSlot_Performing);
			card.DynamicSlot = false;
			card.Interactable = false;
		}


		private void Card_RefreshFleetUI () {
			int count = m_CardAssets.EnemyShipContainer.childCount;
			for (int i = 0; i < count && i < FieldB.Ships.Length; i++) {
				var grab = m_CardAssets.EnemyShipContainer.GetChild(i).GetComponent<Grabber>();
				var img = grab.Grab<Image>(grab.name);
				img.color = FieldB.Ships[i].Alive ? Color.white : new Color32(242, 76, 46, 255);
			}
		}


		private void Card_RefreshStunUI () {
			m_CardAssets.PlayerStun.gameObject.SetActive(Card_PlayerStun > 0);
			m_CardAssets.PlayerStunLabel.text = Card_PlayerStun.ToString();
		}


		// Config
		private int Card_GetPlayerMaxHP () => CurrentHero switch {
			Hero.Nerd => 10,
			Hero.GG => 10,
			Hero.Nessie => 15,
			Hero.Hacker => 7,
			_ => 10,
		};


		private int Card_GetPlayerDefaultSP () => CurrentHero switch {
			Hero.Nerd => 0,
			Hero.GG => 0,
			Hero.Nessie => 2,
			Hero.Hacker => 0,
			_ => 0,
		};


		// LGC
		private void Card_FillPlayerStack (int level) {
			var pList = new List<CardInfo>();
			pList.AddRange(m_CardAssets.BasicCards);
			int addLen = m_CardAssets.AdditionalCards.Length;
			if (level < addLen) {
				pList.AddRange(m_CardAssets.AdditionalCards[0..level]);
			} else {
				pList.AddRange(m_CardAssets.AdditionalCards);
				for (int i = addLen; i < level; i++) {
					pList.Add(m_CardAssets.AdditionalCards[(i % 3) + addLen - 3]);
				}
			}
			Card_ShufflePlayerStack(pList);
			Card_PlayerStack.Clear();
			Card_PlayerStack.PushRange(pList);
		}


		private void Card_FillEnemyStack (int level) {
			var eList = new List<int>(m_CardAssets.Levels[level].Cards.AngeHash());
			Card_ShuffleEnemyStack(eList);
			Card_EnemyStack.Clear();
			Card_EnemyStack.PushRange(eList);
		}


		private void Card_ShufflePlayerStack (List<CardInfo> list) {
			for (int i = 0; i < list.Count; i++) {
				int random = Random.Range(0, list.Count);
				if (random != i) (list[i], list[random]) = (list[random], list[i]);
			}
		}


		private void Card_ShuffleEnemyStack (List<int> list) {
			for (int i = 0; i < list.Count; i++) {
				int random = Random.Range(0, list.Count);
				if (random != i) (list[i], list[random]) = (list[random], list[i]);
			}
		}


		private bool Card_GetFinalWin (int heroIndex) => s_CardFinalWin.Value[heroIndex] == '1';


		private void Card_SetFinalWin (int heroIndex) {
			string result = "";
			for (int i = 0; i < 4; i++) {
				result += i == heroIndex ? "1" : s_CardFinalWin.Value[i];
			}
			s_CardFinalWin.Value = result;
		}


		private void Card_ClearCardsLogic (RectTransform container, RectTransform outContainer, bool immediately = false) {
			container.GetComponentsInChildren(true, Card_CacheCards);
			for (int i = 0; i < Card_CacheCards.Count; i++) {
				var card = Card_CacheCards[i];
				if (!immediately) {
					card.Flip(false);
					card.SetContainer(outContainer);
					card.DynamicSlot = false;
					card.DestoryWhenReady = true;
				} else {
					DestroyImmediate(card.gameObject);
				}
			}
		}


		#endregion





	}
}