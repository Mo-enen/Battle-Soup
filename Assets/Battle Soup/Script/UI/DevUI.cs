using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;



namespace BattleSoup {
	public class DevUI : MonoBehaviour {




		#region --- VAR ---


		// Ser
		[SerializeField] BlocksRenderer m_PotentialValueRenderer = null;
		[SerializeField] Color m_MinValueTint_Hidden = default;
		[SerializeField] Color m_MaxValueTint_Hidden = default;
		[SerializeField] Color m_MinValueTint_Exposed = default;
		[SerializeField] Color m_MaxValueTint_Exposed = default;
		[SerializeField] Color m_NumberTint = default;

		// Data
		private List<ShipPosition>[] HiddenPositions = new List<ShipPosition>[0];
		private List<ShipPosition>[] ExposedPositions = new List<ShipPosition>[0];
		private (float[,,] values, float min, float max) HiddenValues = (new float[0, 0, 0], 0, 0);
		private (float[,,] values, float min, float max) ExposedValues = (new float[0, 0, 0], 0, 0);


		#endregion




		#region --- API ---


		public bool LoadData (SoupStrategy strategy, BattleInfo info) {

			// Calculate Potential Positions
			if (!strategy.CalculatePotentialPositions(
				info,
				Tile.GeneralWater, Tile.GeneralWater,
				ref HiddenPositions
			)) { return false; }

			if (!strategy.CalculatePotentialPositions(
				info,
				Tile.HittedShip | Tile.RevealedShip,
				Tile.GeneralWater | Tile.HittedShip | Tile.RevealedShip,
				ref ExposedPositions
			)) { return false; }

			// Remove Impossible Positions
			strategy.RemoveImpossiblePositions(
				info,
				ref HiddenPositions, ref ExposedPositions
			);

			// Calculate Potential Values
			if (!strategy.CalculatePotentialValues(
				info, HiddenPositions,
				ref HiddenValues.values, out HiddenValues.min, out HiddenValues.max
			)) { return false; }

			if (!strategy.CalculatePotentialValues(
				info, ExposedPositions,
				ref ExposedValues.values, out ExposedValues.min, out ExposedValues.max
			)) { return false; }

			// Grid Size
			m_PotentialValueRenderer.GridCountX = m_PotentialValueRenderer.GridCountY = info.MapSize;

			return true;
		}


		public void RefreshRenderer (int shipIndex) {
			m_PotentialValueRenderer.ClearBlock();
			if (HiddenValues.values == null || shipIndex < 0 || shipIndex >= HiddenValues.values.GetLength(0)) { return; }
			int size = HiddenValues.values.GetLength(1);
			int blockSpriteCount = m_PotentialValueRenderer.BlockSpriteCount;
			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					bool exposed = true;
					float value = ExposedValues.values[shipIndex, x, y];
					if (value == 0) {
						exposed = false;
						value = HiddenValues.values[shipIndex, x, y];
					}
					if (value == 0) { continue; }
					float _vLerp = HiddenValues.min != HiddenValues.max ? (value - HiddenValues.min) / (HiddenValues.max - HiddenValues.min) : 0f;
					var _color = Color.Lerp(
						exposed ? m_MinValueTint_Exposed : m_MinValueTint_Hidden,
						exposed ? m_MaxValueTint_Exposed : m_MaxValueTint_Hidden,
						_vLerp
					);
					m_PotentialValueRenderer.AddBlock(x, y, 0, _color);
					m_PotentialValueRenderer.AddBlock(
						x, y, Mathf.Clamp(Mathf.RoundToInt(value), 1, blockSpriteCount - 1),
						m_NumberTint, 0.618f
					);
				}
			}
			m_PotentialValueRenderer.SetVerticesDirty();
		}


		public void Clear () {
			m_PotentialValueRenderer.ClearBlock();
			HiddenPositions = null;
			ExposedPositions = null;
			HiddenValues = (null, 0, 0);
			ExposedValues = (null, 0, 0);
		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}
