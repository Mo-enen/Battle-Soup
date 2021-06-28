using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace BattleSoupDemo {
	[CreateAssetMenu(fileName = "New Ship", menuName = "BattleSoup Ship", order = 101)]
	public class ShipData : ScriptableObject {


		// Api
		public Sprite Sprite => m_Sprite;

		// Ser
		[SerializeField] Sprite m_Sprite = null;


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