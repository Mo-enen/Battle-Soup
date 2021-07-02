using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;


namespace BattleSoup {
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



#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEditor;
	[CustomEditor(typeof(MapData), true)]
	public class MapData_Inspector : Editor {


		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script");
			serializedObject.ApplyModifiedProperties();
			// Stone Editor





		}


	}
}
#endif