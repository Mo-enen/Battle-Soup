using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;



namespace BattleSoup {
	[CreateAssetMenu(fileName = "New Ship", menuName = "BattleSoup Ship", order = 101)]
	public class ShipData : ScriptableObject {


		// Api
		public string DisplayName => m_DisplayName;
		public int GlobalID => m_GlobalID;
		public Sprite Sprite => m_Sprite;
		public Ship Ship => m_Ship;



		// Ser
		[SerializeField] string m_DisplayName = "";
		[SerializeField] int m_GlobalID = 0;
		[SerializeField] Sprite m_Sprite = null;
		[SerializeField] Ship m_Ship = default;



		// API
		public static bool Contains (int x, int y, ShipData[] ships, List<ShipPosition> positions, out int index) {
			for (int i = ships.Length - 1; i >= 0; i--) {
				var ship = ships[i];
				var sPos = positions[i];
				if (ship.Ship.Contains(x, y, sPos)) {
					index = i;
					return true;
				}
			}
			index = -1;
			return false;
		}



	}
}



#if UNITY_EDITOR
namespace BattleSoup.Editor {
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