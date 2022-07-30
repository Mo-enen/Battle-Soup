using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AngeliaFramework;
using AngeliaFramework.Editor;
using UnityEditor.SceneManagement;


namespace BattleSoup.Editor {
	public class BattleSoupArtworkEvent : IArtworkEvent {



		public string Message => "Battle Soup";


		public void OnArtworkSynced () {
		}


	}
}