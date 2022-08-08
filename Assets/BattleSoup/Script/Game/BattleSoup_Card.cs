using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AngeliaFramework;
using UnityEngine.UI;

namespace BattleSoup {
	public partial class BattleSoup {





		#region --- VAR ---


		// Data
		private readonly List<CardInfo> Stack = new();
		private readonly List<Map> CardMaps = new();
		private int PlayerHP = 10;

		// Saving
		private SavingInt s_CardLevel = new("BattleSoup.CardLevel", 1);


		#endregion




		#region --- MSG ---


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
			bool enemyWin = PlayerHP <= 0;
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


		#endregion




		#region --- API ---


		public void UI_Card_SurrenderAndQuit () {
			s_CardLevel.Value = Mathf.Max(s_CardLevel.Value - 1, 1);
			SwitchState(GameState.Title);
		}


		#endregion




		#region --- LGC ---


		private void Card_GotoLevel (int level) {

			level = level.Clamp(1, int.MaxValue);
			s_CardLevel.Value = level;
			PlayerHP = Card_GetPlayerMaxHP();

			// Stack
			Stack.Clear();
			Stack.AddRange(m_CardAssets.CardInfos);


			// Shuffle Stack
			for (int i = 0; i < Stack.Count; i++) {
				int random = Random.Range(0, Stack.Count);
				if (random != i) (Stack[i], Stack[random]) = (Stack[random], Stack[i]);
			}



			// Field
			FieldB.SetMap(CardMaps[(level / 3).Clamp(0, CardMaps.Count - 1)], false);





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

		}


		// Util
		private int Card_GetPlayerMaxHP () {




			return 10;
		}


		#endregion





	}
}