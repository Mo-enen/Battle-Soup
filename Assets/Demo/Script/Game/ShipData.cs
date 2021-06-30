using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoup;



namespace BattleSoupDemo {
	[CreateAssetMenu(fileName = "New Ship", menuName = "BattleSoup Ship", order = 101)]
	public class ShipData : ScriptableObject {


		// Api
		public Sprite Sprite => m_Sprite;
		public Ability Ability => m_Ability;
		public Int2[] Body => m_Body;

		// Ser
		[SerializeField] Sprite m_Sprite = null;
		[SerializeField] Ability m_Ability = default;
		[SerializeField] Int2[] m_Body = new Int2[0];

	}
}



#if UNITY_EDITOR
namespace BattleSoupDemo.Editor {
	using UnityEditor;
	[CustomEditor(typeof(ShipData))]
	public class Ship_Inspector : Editor {
		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script");
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif