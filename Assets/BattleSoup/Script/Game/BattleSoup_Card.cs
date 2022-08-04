using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public partial class BattleSoup {





		#region --- VAR ---




		#endregion




		#region --- MSG ---


		private void Update_Card () {

			var currentStep = CellStep.CurrentStep;
			bool noDialog = GlobalFrame > DialogFrame + 4;
			bool waitingForPlayer = !GameOver && !DevMode && currentStep == null;

			FieldB.ShowShips = true;
			FieldB.AllowHoveringOnWater = noDialog && waitingForPlayer;
			FieldB.HideInvisibleShip = true;
			FieldB.ClickToAttack = noDialog && waitingForPlayer;
			FieldB.SetPickingDirection(PickingDirection);

			StopAbilityOnShipSunk();


		}


		private void SwitchState_CardGame () {

			FieldA.Enable = false;

			FieldB.Enable = true;
			FieldB.AllowHoveringOnShip = false;
			FieldB.ShowShips = false;
			FieldB.DragToMoveShips = false;
			FieldB.RightClickToFlipShips = false;
			FieldB.DrawPickingArrow = false;
			FieldB.HideInvisibleShip = true;
			FieldB.DrawCookedInfo = false;
			FieldB.DrawDevInfo = false;
			FieldB.GameStart();

			Cheating = false;
			PickingPosition = default;
			PickingDirection = default;
			GameOver = false;


		}


		#endregion




		#region --- API ---




		#endregion




		#region --- LGC ---




		#endregion





	}
}