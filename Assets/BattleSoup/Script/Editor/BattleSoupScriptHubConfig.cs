using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Moenen.Standard;
using AngeliaFramework.Editor;


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




	/*
	public class Test {
		[MenuItem("Test/Test")]
		public static void Invoke () {
			var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ShipSunk.png");
			var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/ShipSunk.png");
			var pixels = texture.GetPixels32();
			int width = texture.width;
			foreach (var sp in sprites) {
				var sprite = sp as Sprite;
				var rect = new RectInt(
					(int)sprite.rect.x, (int)sprite.rect.y,
					(int)sprite.rect.width, (int)sprite.rect.height
				);

				// Alpha
				for (int j = rect.yMin; j < rect.yMax; j++) {
					for (int i = rect.xMin; i < rect.xMax; i++) {
						var pixel = pixels[j * width + i];
						float a01 = pixel.a / 255f;
						a01 *= Mathf.Clamp01(Mathf.InverseLerp(rect.yMin + 6, rect.yMin + 33, j));
						pixel.a = (byte)Mathf.Clamp(a01 * 255, 0, 255);
						pixels[j * width + i] = pixel;
					}
				}

			}
			texture.SetPixels32(pixels);
			texture.Apply();
			AseUtil.ByteToFile(texture.EncodeToPNG(), "Assets/Result.png");
			AssetDatabase.Refresh();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
	//*/



}
