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
		private readonly List<PlayerCardUI> Card_CacheCards = new();
		private Hero CurrentHero = Hero.Nerd;
		private int Card_PlayerHP = 10;
		private int Card_PlayerSP = 0;
		private int CardLevel = 0;
		private bool LastShipExposed = false;


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
			if (CellStep.CurrentStep == null) {

				// Deal for Player
				int count = Card_PlayerDrawCardCount;
				for (int i = 0; i < count; i++) {
					CellStep.AddToLast(new scDealPlayerCard(8));
				}

				// Player Turn
				CellStep.AddToLast(new scWaitForPlayer());
				if (CurrentHero == Hero.Hacker) CellStep.AddToLast(new scWaitForPlayer());

				// End Player Turn
				CellStep.AddToLast(new scClearPlayerCards());

				// Enemy Turn
				//sCard_EnemyTurn



			}

			// Waiting for Player to Pick a Card
			if (CellStep.CurrentStep is scWaitForPlayer) {
				// Clear Performing UI
				if (m_CardAssets.PlayerSlot_Performing.childCount > 0) {
					Card_ClearPlayerCards(m_CardAssets.PlayerSlot_Performing);
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
					Card_ClearPlayerCards(m_CardAssets.PlayerSlot_Performing);
					Card_ClearPlayerCards(m_CardAssets.PlayerSlot_Dock);
					if (playerWin) {
						// Win
						CellStep.AddToLast(new scWait(24));
						CellStep.AddToLast(new scDemonExplosion());
						CellStep.AddToLast(new scGotoNextLevel());
						CellStep.AddToLast(new scDemonBack());
						CellStep.AddToLast(new scWait(24));
					} else {
						// Lose



					}
				}
			}

		}


		#endregion




		#region --- API ---


		// Deck
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


		public void Card_ClearPlayerCards (Transform container, bool immediately = false) {
			container.GetComponentsInChildren(true, Card_CacheCards);
			for (int i = 0; i < Card_CacheCards.Count; i++) {
				var card = Card_CacheCards[i];
				if (!immediately) {
					card.Flip(false);
					card.SetContainer(m_CardAssets.PlayerSlot_Out);
					card.DynamicSlot = false;
					card.DestoryWhenReady = true;
				} else {
					DestroyImmediate(card.gameObject);
				}
			}
		}


		public void Card_DealForEnemy () {
			if (Card_EnemyStack.Count == 0) Card_FillEnemyStack(CardLevel);




		}


		// Player Health
		public void Card_DamagePlayer (int damage) {
			if (damage <= 0) return;
			int damageShied = Mathf.Min(Card_PlayerSP.Clamp(0, int.MaxValue), damage);
			int damageHealth = damage - damageShied;
			if (damageShied > 0) Card_SetPlayerSP(Card_PlayerSP - damageShied);
			if (damageHealth > 0) Card_SetPlayerHP(Card_PlayerHP - damageHealth);
		}


		public void Card_HealPlayer (int heal) {
			if (heal <= 0) return;
			Card_SetPlayerHP(Card_PlayerHP + heal);
		}


		public void Card_ShieldPlayer (int shield) {
			if (shield < 0) return;
			Card_SetPlayerSP(Card_PlayerSP + shield);
		}


		// Workflow
		public void Card_GotoNextLevel () => Card_GotoLevel(CardLevel + 1);


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
			Card_ReloadStackInfoUI();
			m_CardAssets.LevelNumber.text = (level + 1).ToString("00");
			m_CardAssets.PlayerSlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.EnemySlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.LevelNumberPop.Pop();
			Card_ClearPlayerCards(m_CardAssets.PlayerSlot_Performing);
			Card_ClearPlayerCards(m_CardAssets.PlayerSlot_Dock);
			m_Assets.PanelRoot.pivot = Vector2.one * 0.5f;
			m_Assets.PanelRoot.localRotation = Quaternion.identity;
			CardAssets.DemonExplosion.gameObject.SetActive(false);
			CardAssets.EnemyAni.SetBool("Lose", false);
			CardAssets.DemonRoot.anchoredPosition3D = Vector3.zero;
			CardAssets.DemonRoot.localScale = Vector3.one;
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


		private void Card_ReloadStackInfoUI () {
			m_CardAssets.StackContainer.DestroyAllChirldrenImmediate();
			foreach (var info in Card_PlayerStack) {
				var rt = new GameObject("", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform as RectTransform;
				rt.SetParent(m_CardAssets.StackContainer);
				rt.localScale = Vector3.one;
				rt.localRotation = Quaternion.identity;
				var img = rt.GetComponent<Image>();
				img.preserveAspect = true;
				img.raycastTarget = false;
				if (!info.IsShip) {
					// Built-in
					img.sprite = CardAssets.TypeIcons[(int)info.Type];
				} else {
					// Ship
					int id = info.GlobalName.AngeHash();
					if (TryGetShip(id, out var ship)) {
						img.sprite = ship.Icon;
					} else {
						img.sprite = null;
						img.enabled = false;
					}
				}
			}
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
			int addLen = CardAssets.AdditionalCards.Length;
			if (level < addLen) {
				pList.AddRange(CardAssets.AdditionalCards[0..level]);
			} else {
				pList.AddRange(CardAssets.AdditionalCards);
				for (int i = addLen; i < level; i++) {
					pList.Add(CardAssets.AdditionalCards[(i % 3) + addLen - 3]);
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


		#endregion





	}
}