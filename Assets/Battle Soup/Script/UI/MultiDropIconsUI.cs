using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Moenen.Standard;


namespace BattleSoup {
	public class MultiDropIconsUI : MonoBehaviour {


		[SerializeField] DropdownEx m_Drop = null;

		public void RefreshUI () {
			if (m_Drop == null) { return; }
			for (int i = 0; i < m_Drop.options.Count && i < transform.childCount; i++) {
				transform.GetChild(i).gameObject.SetActive(m_Drop.options[i].selected);
			}
		}


	}
}
