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


		// Data
		private readonly Stack<CardInfo> Card_PlayerStack = new();
		private readonly Stack<int> Card_EnemyStack = new();
		private readonly List<Map> Card_Maps = new();
		private readonly Dictionary<int, EnemyCard> Card_EnemyCardPool = new();
		private int Card_PlayerHP = 10;
		private int Card_PlayerSP = 0;
		private int CardLevel = 0;
		private Hero CurrentHero = Hero.Nerd;


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
			FieldB.HideInvisibleShip = true;
			FieldB.DrawCookedInfo = false;
			FieldB.DrawDevInfo = false;
			FieldB.ClickToAttack = false;

			Cheating = false;
			PickingPosition = default;
			PickingDirection = default;
			GameOver = false;

			Card_GotoLevel(0);

		}


		private void Update_Card () {

			RefreshDialogFrame();
			bool noDialog = GlobalFrame > DialogFrame + 4;
			bool waitingForPlayer = !GameOver && CellStep.CurrentStep == null;
			bool picking = CellStep.CurrentStep is sPick;

			FieldB.AllowHoveringOnWater = noDialog && waitingForPlayer;
			FieldB.SetPickingDirection(PickingDirection);

			StopAbilityOnShipSunk();

			// Picking Hint
			if (m_CardAssets.PickingHint.gameObject.activeSelf != picking) {
				m_CardAssets.PickingHint.gameObject.SetActive(picking);
			}

			// Fill Stack when Empty
			if (Card_PlayerStack.Count == 0) {
				FillPlayerStack(CardLevel);
			}
			if (Card_EnemyStack.Count == 0) {
				FillEnemyStack(CardLevel);
			}

			// Fill up Steps for a whole turn
			if (CellStep.CurrentStep == null) {
				// Deal for Player


				// Enemy Turn
				//sCard_EnemyTurn



			}

			// Gameover Check
			bool playerWin = FieldB.AllShipsSunk();
			bool enemyWin = Card_PlayerHP <= 0;
			if (playerWin || enemyWin) {
				// Game Over
				CellStep.Clear();
				if (playerWin) {
					Card_GotoLevel(CardLevel + 1);
				} else if (enemyWin) {
					// Lose



				}
			}

		}


		#endregion




		#region --- API ---


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


		public void UI_Card_SurrenderAndQuit () => SwitchState(GameState.Title);


		#endregion




		#region --- LGC ---


		private void Card_GotoLevel (int level) {

			level = level.Clamp(0, m_CardAssets.Levels.Length - 1);
			CardLevel = level;
			Card_SetPlayerHP(Card_GetPlayerMaxHP());
			Card_SetPlayerSP(0);

			// Stack
			FillPlayerStack(level);
			FillEnemyStack(level);

			// Map
			FieldB.SetMap(Card_Maps[(level / 3).Clamp(0, Card_Maps.Count - 1)], false);

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
			m_CardAssets.LevelNumber.text = level.ToString("00");
			m_CardAssets.PlayerSlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.EnemySlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.LevelNumberPop.Pop();

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


		// Util
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


		private void FillPlayerStack (int level) {
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


		private void FillEnemyStack (int level) {
			var eList = new List<int>(m_CardAssets.Levels[level].Cards.AngeHash());
			Card_ShuffleEnemyStack(eList);
			Card_EnemyStack.Clear();
			Card_EnemyStack.PushRange(eList);
		}


		private void Card_ShufflePlayerStack (List<CardInfo> list) {
			for (int i = 0; i < Card_PlayerStack.Count; i++) {
				int random = Random.Range(0, Card_PlayerStack.Count);
				if (random != i) (list[i], list[random]) = (list[random], list[i]);
			}
		}


		private void Card_ShuffleEnemyStack (List<int> list) {
			for (int i = 0; i < Card_EnemyStack.Count; i++) {
				int random = Random.Range(0, Card_EnemyStack.Count);
				if (random != i) (list[i], list[random]) = (list[random], list[i]);
			}
		}


		#endregion





	}
}