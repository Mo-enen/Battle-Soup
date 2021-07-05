using UnityEngine;
using UnityEngine.UI;
namespace BattleSoup {
	public class GreyImage : Image {
		[SerializeField] Material m_GreyMat = null;
		public void SetGrey (bool grey) => material = grey ? m_GreyMat : null;
	}
}


#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEditor;


	[CustomEditor(typeof(GreyImage), true)]
	public class GreyImage_Inspector : Editor {


		private static readonly string[] PROP_EXC = new string[] {
			"m_Script", "m_OnCullStateChanged",
			"m_RaycastPadding", 
			"m_FillMethod","m_FillAmount","m_FillClockwise", "m_FillOrigin",
			"m_UseSpriteMesh", 
		};


		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, PROP_EXC);
			serializedObject.ApplyModifiedProperties();
		}


	}
}
#endif