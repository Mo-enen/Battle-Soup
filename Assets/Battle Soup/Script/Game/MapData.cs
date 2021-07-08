using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSoupAI;


namespace BattleSoup {
	[CreateAssetMenu(fileName = "New Map", menuName = "BattleSoup Map", order = 102)]
	public class MapData : ScriptableObject {


		// Api
		public int Size => m_Size;
		public Int2[] Stones { get => m_Stones; set => m_Stones = value; }

		// Ser
		[SerializeField] int m_Size = 8;
		[SerializeField] Int2[] m_Stones = new Int2[0];


		// API
		public bool HasStone (int x, int y) {
			for (int i = 0; i < m_Stones.Length; i++) {
				var stone = m_Stones[i];
				if (stone.x == x && stone.y == y) {
					return true;
				}
			}
			return false;
		}


		public bool GetRandomTile (Tile target, Tile[,] tiles, out int x, out int y, System.Func<int, int, bool> check = null) {
			x = Random.Range(0, m_Size);
			y = Random.Range(0, m_Size);
			for (int j = 0; j < m_Size; j++) {
				for (int i = 0; i < m_Size; i++) {
					int _x = (x + i) % m_Size;
					int _y = (y + j) % m_Size;
					if (
						target.HasFlag(tiles[_x, _y]) &&
						(check == null || check(_x, _y))
					) {
						x = _x;
						y = _y;
						return true;
					}
				}
			}
			return false;
		}


	}
}



#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEditor;


	[CustomEditor(typeof(MapData), true)]
	public class MapData_Inspector : Editor {



		// VAR
		private static GUIStyle CenterLabel => _CenterLabel ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, };
		private static GUIStyle _CenterLabel = null;
		private bool[,] Stones = new bool[0, 0];


		// MSG
		private void OnEnable () {
			RefreshStones();
		}


		private void OnDisable () {
			var map = target as MapData;
			var stones = new List<Int2>();
			int sizeX = Stones.GetLength(0);
			int sizeY = Stones.GetLength(1);
			for (int x = 0; x < sizeX; x++) {
				for (int y = 0; y < sizeY; y++) {
					if (Stones[x, y]) {
						stones.Add(new Int2(x, y));
					}
				}
			}
			map.Stones = stones.ToArray();
			EditorUtility.SetDirty(map);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}


		public override void OnInspectorGUI () {

			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script", "m_Stones");
			serializedObject.ApplyModifiedProperties();

			// Stone Editor
			GUILayout.Space(2);
			GUI.Label(GUIRect(0, 18), "Stones");
			GUILayout.Space(4);
			var rect = GUIRect(0, 200);
			rect.x += (rect.width - rect.height) / 2f;
			rect.width = rect.height;
			var map = target as MapData;
			int size = map.Size;
			float stoneSize = rect.width / size;
			for (int x = 0; x < size; x++) {
				for (int y = 0; y < size; y++) {
					var _rect = new Rect(
						rect.x + x * stoneSize,
						rect.y + y * stoneSize,
						stoneSize, stoneSize
					);
					Stones[x, y] = EditorGUI.Toggle(_rect, GUIContent.none, Stones[x, y], GUI.skin.button);
					if (Stones[x, y]) {
						GUI.Label(_rect, "¡ñ", CenterLabel);
					}
				}
			}
		}


		// LGC
		private void RefreshStones () {
			var map = target as MapData;
			Stones = new bool[map.Size, map.Size];
			foreach (var stone in map.Stones) {
				if (stone.x >= 0 && stone.x < map.Size && stone.y >= 0 && stone.y < map.Size) {
					Stones[stone.x, stone.y] = true;
				}
			}
		}


		private Rect GUIRect (int w, int h) => GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(w == 0), GUILayout.ExpandHeight(h == 0));


	}
}
#endif