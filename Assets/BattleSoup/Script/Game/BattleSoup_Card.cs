using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AngeliaFramework;
using UnityEngine.UI;
using System.Text;

namespace BattleSoup {
	public partial class BattleSoup {





		#region --- VAR ---


		// Data
		private readonly List<CardInfo> CardStack = new();
		private readonly List<Map> CardMaps = new();
		private int Card_PlayerHP = 10;
		private int Card_PlayerSP = 0;

		// Saving
		private SavingInt s_CardLevel = new("BattleSoup.CardLevel", 1);
		private SavingInt s_CardMaxLevel = new("BattleSoup.CardMaxLevel", 1);


		#endregion




		#region --- MSG ---


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

			Card_GotoLevel(s_CardLevel.Value);

		}


		private void Update_Card () {

			RefreshDialogFrame();
			bool noDialog = GlobalFrame > DialogFrame + 4;
			bool waitingForPlayer = !GameOver && CellStep.CurrentStep == null;

			FieldB.AllowHoveringOnWater = noDialog && waitingForPlayer;
			FieldB.SetPickingDirection(PickingDirection);

			StopAbilityOnShipSunk();

			// Fill up Steps
			if (CellStep.CurrentStep == null) {
				// Deal for Player


				// Enemy Attack



			}

			// Gameover Check
			bool playerWin = FieldB.AllShipsSunk();
			bool enemyWin = Card_PlayerHP <= 0;
			if (playerWin || enemyWin) {
				// Game Over
				CellStep.Clear();
				if (playerWin) {
					Card_GotoLevel(s_CardLevel.Value + 1);
				} else if (enemyWin) {
					Card_GotoLevel(s_CardLevel.Value - 1);
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
		public void UI_Card_SurrenderAndQuit () {
			s_CardLevel.Value = Mathf.Max(s_CardLevel.Value - 1, 1);
			SwitchState(GameState.Title);
		}


		#endregion




		#region --- LGC ---


		private void Card_GotoLevel (int level) {

			level = level.Clamp(1, int.MaxValue);
			s_CardLevel.Value = level;
			Card_SetPlayerHP(Card_GetPlayerMaxHP());
			Card_SetPlayerSP(0);

			// Fill Stack
			CardStack.Clear();
			CardStack.AddRange(m_CardAssets.BasicCards);
			int addLen = CardAssets.AdditionalCards.Length;
			if (level < addLen) {
				CardStack.AddRange(CardAssets.AdditionalCards[0..level]);
			} else {
				CardStack.AddRange(CardAssets.AdditionalCards);
				for (int i = addLen; i < level; i++) {
					CardStack.Add(CardAssets.AdditionalCards[(i % 3) + addLen - 3]);
				}
			}
			Card_ReloadStackInfoUI();

			// Shuffle Stack
			for (int i = 0; i < CardStack.Count; i++) {
				int random = Random.Range(0, CardStack.Count);
				if (random != i) (CardStack[i], CardStack[random]) = (CardStack[random], CardStack[i]);
			}

			// Field
			FieldB.SetMap(CardMaps[(level / 3).Clamp(0, CardMaps.Count - 1)], false);

			// Fleet
			string fleet;
			if (level - 1 < m_CardAssets.Fleets.Length) {
				// Built In
				int fIndex = level - 1;
				fleet = m_CardAssets.Fleets[fIndex.Clamp(0, m_CardAssets.Fleets.Length - 1)];
			} else {
				// Random
				int sCount = Random.Range(4, 7);
				var builder = new StringBuilder();
				for (int i = 0; i < sCount; i++) {
					string sName = "Sailboat";
					var meta = ShipMetas[Random.Range(0, ShipMetas.Count)];
					if (TryGetShip(meta.ID, out var ship)) sName = ship.GlobalName;
					builder.Append(sName);
					if (i != sCount - 1) builder.Append(',');
				}
				fleet = builder.ToString();
			}
			var ships = GetShipsFromFleetString(fleet);
			if (ships == null || ships.Length == 0) ships = GetShipsFromFleetString("Sailboat,SeaMonster,Longboat,MiniSub");
			FieldB.SetShips(ships);
			FieldB.RandomPlaceShips(128);
			FieldB.GameStart();

			// Reload Fleet UI
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

			// Final
			m_CardAssets.LevelNumber.text = level.ToString("00");
			m_CardAssets.PlayerSlotBackground_Out.gameObject.SetActive(false);
			m_CardAssets.EnemySlotBackground_Out.gameObject.SetActive(false);
			s_CardMaxLevel.Value = Mathf.Max(s_CardMaxLevel.Value, s_CardLevel.Value);
			m_CardAssets.MaxLevel.text = $"Best {s_CardMaxLevel.Value:00}";
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
			foreach (var info in CardStack) {
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


		// Util
		private int Card_GetPlayerMaxHP () {




			return 10;
		}


		#endregion





	}
}