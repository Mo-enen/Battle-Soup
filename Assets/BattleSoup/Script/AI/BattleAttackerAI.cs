using System.Collections;
using System.Collections.Generic;
using AngeliaFramework;
using UnityEngine;


namespace BattleSoup {
	public class BattleAttackerEasyAI : SoupAI {


		public override string DisplayName => "Battle Attacker (Easy)";
		public override string Description => "Battle Attacker AI created by Moenen.";
		public override string Fleet => "Sailboat,SeaMonster,Longboat,MiniSub";



		public override bool Perform (
			in eField ownField, int usingAbilityIndex,
			out Vector2Int attackPosition, out int abilityIndex, out Direction4 abilityDirection
		) {
			attackPosition = default;
			abilityIndex = -1;
			abilityDirection = default;
			int mapSize = OpponentMapSize;



			/////////////////// TEMP /////////////////////

			int offsetX = Random.Range(0, mapSize);
			int offsetY = Random.Range(0, mapSize);
			for (int i = 0; i < mapSize; i++) {
				for (int j = 0; j < mapSize; j++) {
					int x = (offsetX + i) % mapSize;
					int y = (offsetY + j) % mapSize;
					var cell = OpponentCells[x, y];
					if (
						cell.HasStone ||
						(cell.ShipIndex < 0 && cell.State == CellState.Revealed) ||
						cell.State == CellState.Sunk ||
						cell.State == CellState.Hit
					) continue;
					attackPosition.x = x;
					attackPosition.y = y;
					goto EndLoop;
				}
			}
			EndLoop:;


			/////////////////// TEMP /////////////////////


			return true;
		}


	}
}