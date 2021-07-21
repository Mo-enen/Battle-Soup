using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;



namespace BattleSoup {
	public class DevUI : MonoBehaviour {




		#region --- VAR ---


		// Ser
		[SerializeField] BlocksRenderer m_PotentialValueRenderer = null;
		[SerializeField] Color m_MinValueTint = default;
		[SerializeField] Color m_MaxValueTint = default;
		[SerializeField] Color m_NumberTint = default;

		// Data
		private List<ShipPosition>[] HiddenPositions = new List<ShipPosition>[0];
		private List<ShipPosition>[] ExposedPositions = new List<ShipPosition>[0];
		private Dictionary<Int2, int>[] ExposedMap = new Dictionary<Int2, int>[0];
		private int[,,] Values = new int[0, 0, 0];
		private int MinValue = 0;
		private int MaxValue = 0;


		#endregion




		#region --- API ---


		public bool LoadData (Game.GameData data) {

			// Calculate Potential Positions
			if (!data.Strategy.CalculatePotentialPositions(
				data.Ships, data.Tiles, data.KnownPositions,
				ref HiddenPositions, ref ExposedPositions
			)) { return false; }

			// Calculate Exposed Map
			if (!data.Strategy.CalculateExposedMap(
				data.Ships, data.Tiles,
				ref ExposedMap
			)) { return false; }

			// Smart Fix Exposed Positions
			if (!data.Strategy.SmartFixExposedPositions(
				data.Ships, data.Tiles, ExposedMap, ref ExposedPositions
			)) { return false; }

			// Calculate Potential Values
			if (!data.Strategy.CalculatePotentialValues(
				data.Ships, data.Map.Size, HiddenPositions, true,
				ref Values, out int _minValue0, out int _maxValue0
			)) { return false; }

			if (!data.Strategy.CalculatePotentialValues(
				data.Ships, data.Map.Size, ExposedPositions, false,
				ref Values, out int _minValue1, out int _maxValue1
			)) { return false; }

			MinValue = Mathf.Min(_minValue0, _minValue1);
			MaxValue = Mathf.Max(_maxValue0, _maxValue1);

			// Grid Size
			m_PotentialValueRenderer.GridCountX = m_PotentialValueRenderer.GridCountY = data.Map.Size;

			return true;
		}


		public void RefreshRenderer (int shipIndex) {
			m_PotentialValueRenderer.ClearBlock();
			if (Values != null && shipIndex >= 0 && shipIndex < Values.GetLength(0)) {
				int size = Values.GetLength(1);
				int blockSpriteCount = m_PotentialValueRenderer.BlockSpriteCount;
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						int value = Values[shipIndex, x, y];
						if (value <= 0) { continue; }
						float _vLerp = MinValue != MaxValue ? (float)(value - MinValue) / (MaxValue - MinValue) : 0f;
						m_PotentialValueRenderer.AddBlock(
							x, y, 0,
							Color.Lerp(m_MinValueTint, m_MaxValueTint, _vLerp)
						);
						m_PotentialValueRenderer.AddBlock(
							x, y, Mathf.Clamp(value, 0, blockSpriteCount - 1),
							m_NumberTint, 0.618f
						);
					}
				}
			}
			m_PotentialValueRenderer.SetVerticesDirty();
		}


		public void Clear () {
			m_PotentialValueRenderer.ClearBlock();
			HiddenPositions = null;
			ExposedPositions = null;
			Values = null;
			MinValue = 0;
			MaxValue = 0;
		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}
