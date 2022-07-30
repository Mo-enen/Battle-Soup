using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace BattleSoup {
	public class VersionLabel : Text {
		protected override void Start () {
			base.Start();
			text = "v" + Application.version;
		}
	}
}
