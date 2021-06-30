using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSoupDemo {
	[CreateAssetMenu(fileName = "New Map", menuName = "BattleSoup Map", order = 102)]
	public class MapData : ScriptableObject {


		// Api
		public Vector2Int Size => m_Size;
		public Vector2Int[] Stones => m_Stones;

		// Ser
		[SerializeField] Vector2Int m_Size = new Vector2Int(8, 8);
		[SerializeField] Vector2Int[] m_Stones = new Vector2Int[0];


	}
}
