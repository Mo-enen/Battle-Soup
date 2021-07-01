using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoup;


namespace BattleSoupDemo {
	[CreateAssetMenu(fileName = "New Map", menuName = "BattleSoup Map", order = 102)]
	public class MapData : ScriptableObject {


		// Api
		public int Size => m_Size;
		public Int2[] Stones => m_Stones;

		// Ser
		[SerializeField] int m_Size = 8;
		[SerializeField] Int2[] m_Stones = new Int2[0];


	}
}
