using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSoup {
	public partial class BattleSoup {


		public void UI_OpenURL (string url) => Application.OpenURL(url);


		public void UI_SwitchState (string state) {
			if (System.Enum.TryParse<GameState>(state, true, out var result)) {
				SwitchState(result);
			}
		}


		public void UI_SwitchMode (string mode) {
			if (System.Enum.TryParse<GameMode>(mode, true, out var result)) {
				SwitchMode(result);
			}
		}


		public void UI_RefreshSettingUI () {
			m_Assets.SoundTG.SetIsOnWithoutNotify(s_UseSound.Value);
			m_Assets.AutoPlayAvATG.SetIsOnWithoutNotify(s_AutoPlayForAvA.Value);
			m_Assets.UseAnimationTG.SetIsOnWithoutNotify(s_UseAnimation.Value);
		}


		public void UI_OpenReloadDialog () {
			ReloadShipDataFromDisk();
			ReloadMapDataFromDisk();
		}


		public void UI_SelectingMapChanged () {
			s_MapIndexA.Value = GetMapIndexFromUI(m_Assets.MapSelectorContentA).Clamp(0, AllMaps.Count);
			s_MapIndexB.Value = GetMapIndexFromUI(m_Assets.MapSelectorContentB).Clamp(0, AllMaps.Count);
			OnMapChanged();
			// Func
			int GetMapIndexFromUI (RectTransform container) {
				int childCount = container.childCount;
				for (int i = 0; i < childCount; i++) {
					var tg = container.GetChild(i).GetComponent<Toggle>();
					if (tg != null && tg.isOn) return i;
				}
				return 0;
			}
		}


		public void UI_SelectingFleetChanged () {
			ReloadFleetRendererUI();
			OnFleetChanged();
		}


		public void UI_ClearPlayerFleetSelector () {
			s_PlayerFleet.Value = "";
			ReloadFleetRendererUI();
			OnFleetChanged();
		}


		public void UI_ResetPlayerShipPositions () => FieldA.RandomPlaceShips(256);


		public void UI_AiSelectorChanged () {
			s_SelectingAiA.Value = m_Assets.RobotAiA.value.Clamp(0, AllAi.Count - 1);
			s_SelectingAiB.Value = m_Assets.RobotAiB.value.Clamp(0, AllAi.Count - 1);
			OnFleetChanged();
			ReloadFleetRendererUI();
		}


		public void UI_TryQuitBattle () {
			if (State != GameState.Playing) return;
			if (GameOver) {
				SwitchState(GameState.Prepare);
			} else {
				m_Assets.QuitBattleDialog.gameObject.SetActive(true);
			}
		}


		public void UI_SetUiScale (int id) => SetUiScale(id);


		public void UI_SetAvAPlay (bool play) {
			bool PvA = Mode == GameMode.PvA;
			AvAPlaying = play;
			m_Assets.PlayAvA.gameObject.SetActive(!GameOver && !PvA && !play);
			m_Assets.PauseAvA.gameObject.SetActive(!GameOver && !PvA && play);
			m_Assets.RestartAvA.gameObject.SetActive(GameOver && !PvA);
		}


		public void UI_ShowDevMode (bool devMode) => SetDevMode(devMode);


		public void UI_SetDrawHitInfo (bool hitInfo) {
			FieldA.DrawHitInfo = hitInfo;
			FieldB.DrawHitInfo = hitInfo;
		}


		public void UI_SetDrawCookInfo (bool cookInfo) {
			FieldA.DrawCookedInfo = cookInfo;
			FieldB.DrawCookedInfo = cookInfo;
			RefreshShipAbilityUI(m_Assets.AbilityContainerA, FieldA, Mode == GameMode.PvA);
			RefreshShipAbilityUI(m_Assets.AbilityContainerB, FieldB, false);
		}


		public void UI_UseScreenEffect (bool use) => SetUseScreenEffect(use);


	}
}