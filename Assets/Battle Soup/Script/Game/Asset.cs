using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moenen.Standard;



namespace BattleSoup {
	public class Asset : MonoBehaviour {




		#region --- VAR ---


		// Api
		public List<ShipData> ShipDatas { get; } = new List<ShipData>();
		public List<MapData> MapDatas { get; } = new List<MapData>();

		// Ser
		[SerializeField] Material m_ShipMaterial = null;
		[SerializeField] Texture2D m_ShipBlockTexture = null;


		#endregion




		#region --- MSG ---


		private void Awake () {
			Awake_Ship();
			Awake_Map();
		}


		private void Awake_Ship () {
			ShipDatas.Clear();
			var iconList = new List<Texture2D>() { m_ShipBlockTexture };
			string root = Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Ships");
			var folders = Util.GetFoldersIn(root, true);
			for (int i = 0; i < folders.Length; i++) {
				try {
					var folder = folders[i];
					string json = Util.FileToText(Util.CombinePaths(folder.FullName, "Data.json"));
					if (string.IsNullOrEmpty(json)) { continue; }
					var shipData = JsonUtility.FromJson<ShipData>(json);
					if (shipData == null) { continue; }
					shipData.GlobalID = ShipDatas.Count;
					var iconByte = Util.FileToByte(Util.CombinePaths(folder.FullName, "Icon.png"));
					if (iconByte == null || iconByte.Length == 0) { continue; }
					var iconTexture = new Texture2D(1, 1);
					if (!iconTexture.LoadImage(iconByte, false)) { continue; }
					iconList.Add(iconTexture);
					ShipDatas.Add(shipData);
				} catch { }
			}
			// Pack Texture
			var shipTexture = new Texture2D(1, 1);
			var uvRects = shipTexture.PackTextures(iconList.ToArray(), 1);
			float tWidth = shipTexture.width;
			float tHeight = shipTexture.height;
			var sprites = new Sprite[ShipDatas.Count + 1];
			var uv0 = uvRects[0];
			sprites[0] = Sprite.Create(shipTexture, new Rect(uv0.x * tWidth, uv0.y * tHeight, uv0.width * tWidth, uv0.height * tHeight), Vector2.one * 0.5f);
			sprites[0].name = "(ship block)";
			for (int i = 0; i < ShipDatas.Count; i++) {
				var ship = ShipDatas[i];
				var uv = uvRects[i + 1];
				var rect = new Rect(uv.x * tWidth, uv.y * tHeight, uv.width * tWidth, uv.height * tHeight);
				ship.Sprite = Sprite.Create(shipTexture, rect, Vector2.one * 0.5f);
				ship.Sprite.name = ship.DisplayName;
				sprites[i + 1] = ship.Sprite;
			}
			m_ShipMaterial.mainTexture = shipTexture;
			// Sprites for Renderers
			var sRenderers = FindObjectsOfType<ShipRenderer>(true);
			foreach (var renderer in sRenderers) {
				renderer.SetSprites(sprites);
			}
		}


		private void Awake_Map () {
			MapDatas.Clear();
			string root = Util.CombinePaths(Util.GetRuntimeBuiltRootPath(), "Maps");
			var files = Util.GetFilesIn(root, true, "*.json");
			for (int i = 0; i < files.Length; i++) {
				try {
					var file = files[i];
					string json = Util.FileToText(file.FullName);
					if (string.IsNullOrEmpty(json)) { continue; }
					var map = JsonUtility.FromJson<MapData>(json);
					if (map == null) { continue; }
					MapDatas.Add(map);
				} catch { }
			}
		}


		#endregion




		#region --- API ---




		#endregion




		#region --- LGC ---




		#endregion




		#region --- UTL ---




		#endregion




	}
}
