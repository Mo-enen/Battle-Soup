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

		// Data
		private List<ShipPosition>[] HiddenPositions = new List<ShipPosition>[0];
		private List<ShipPosition>[] ExposedPositions = new List<ShipPosition>[0];
		private int[,,] Values = new int[0, 0, 0];
		private int MinValue = 0;
		private int MaxValue = 0;


		#endregion




		#region --- API ---


		public bool LoadData (Game.GameData data) {

			if (!SoupStrategy.CalculatePotentialPositions(
				data.Ships, data.Tiles, data.KnownPositions,
				ref HiddenPositions, ref ExposedPositions
			)) { return false; }

			if (!SoupStrategy.CalculatePotentialValues(
				data.Ships, data.Tiles, HiddenPositions, ExposedPositions,
				ref Values, out MinValue, out MaxValue
			)) { return false; }

			m_PotentialValueRenderer.GridCountX = m_PotentialValueRenderer.GridCountY = data.Tiles.GetLength(1);

			return true;
		}


		public void RefreshRenderer (int shipIndex) {

			m_PotentialValueRenderer.ClearBlock();

			// Values
			if (Values != null && shipIndex >= 0 && shipIndex < Values.GetLength(0)) {
				int size = Values.GetLength(1);
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						int value = Values[shipIndex, x, y];
						if (value <= 0) { continue; }
						float _vLerp = MinValue != MaxValue ? (float)(value - MinValue) / (MaxValue - MinValue) : 0f;
						m_PotentialValueRenderer.AddBlock(
							x, y, -1,
							Color.Lerp(m_MinValueTint, m_MaxValueTint, _vLerp),
							1f
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
			MinValue = MaxValue = 0;
		}


		#endregion




		#region --- LGC ---




		#endregion




	}
}
