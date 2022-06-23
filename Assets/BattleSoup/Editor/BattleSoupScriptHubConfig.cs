using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moenen.Standard;


namespace BattleSoup.Editor {
	public class BattleSoupScriptHubConfig : IScriptHubConfig {
		public string Title => "Battle Soup";
		public string[] Paths => new string[] { "Assets" };
		public string IgnoreFolders => "Editor";
		public string IgnoreFiles => "";
		public IScriptHubConfig.FileExtension[] FileExtensions => new IScriptHubConfig.FileExtension[] {
			new("cs", "", true)
		};
		public int Order => 1;
	}
	public class BattleSoupArtworkHubConfig : IScriptHubConfig {
		public string Title => "Battle Soup Artwork";
		public string[] Paths => new string[] { "Assets" };
		public string IgnoreFolders => "Editor";
		public string IgnoreFiles => "";
		public IScriptHubConfig.FileExtension[] FileExtensions => new IScriptHubConfig.FileExtension[] {
			new("aseprite", "", true), new("ase", "", true)
		};
		public int Order => 2;
	}
}
