using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AngeliaFramework;


namespace BattleSoup {
	public abstract class SoupAI {


		public abstract string DisplayName { get; }
		public abstract string Description { get; }
		public abstract string Fleet { get; }


		protected Cell[,] TargetCells { get; private set; } = new Cell[0, 0];
		protected Ship[] TargetShips { get; private set; } = new Ship[0];


		public abstract bool Perform (
			in eField ownField, int usingAbilityIndex,
			out Vector2Int attackPosition, out int abilityIndex, out Direction4 abilityDirection
		);


		public void SyncTargetShips (in Ship[] sourceShips) {
			if (TargetShips.Length != sourceShips.Length) {
				TargetShips = new Ship[sourceShips.Length];
			}
			for (int i = 0; i < TargetShips.Length; i++) {
				var source = sourceShips[i];
				var target = TargetShips[i];
				if (target == null) {
					TargetShips[i] = target = new Ship();
				}
				target.DefaultCooldown = source.DefaultCooldown;
				target.MaxCooldown = source.MaxCooldown;
				target.GlobalCode = source.GlobalCode;
				target.Visible = source.Visible;
				target.BodyNodes = source.BodyNodes;
				target.CurrentCooldown = source.CurrentCooldown;
			}
		}


		public void SyncTargetCells (in eField source) {
			int size = source.MapSize;
			if (TargetCells.GetLength(0) != size || TargetCells.GetLength(1) != size) {
				TargetCells = new Cell[size, size];
			}
			for (int j = 0; j < size; j++) {
				for (int i = 0; i < size; i++) {
					var sourceCell = source[i, j];
					var targetCell = TargetCells[i, j];
					targetCell.HasStone = sourceCell.HasStone;
					targetCell.State = sourceCell.State;
					targetCell.Sonar = sourceCell.Sonar;
					if (sourceCell.ShipIndex >= 0 && source.Ships[sourceCell.ShipIndex].Visible) {
						if (targetCell.ShipIndexs.Count == 0) {
							targetCell.ShipIndexs.Add(sourceCell.ShipIndex);
						} else {
							targetCell.ShipIndexs[0] = sourceCell.ShipIndex;
						}
					}
				}
			}

		}


	}
}