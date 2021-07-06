using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace BattleSoup {
	public class BlocksRenderer : Image {




		#region --- SUB ---



		[System.Serializable]
		public class Block {
			public int X = 0;
			public int Y = 0;
			public int ID = -1;
			public Color Color = Color.white;
			public float Scale = 1f;
			public Block (int x, int y, int id) : this(x, y, id, Color.white) { }
			public Block (int x, int y, int id, float scale) : this(x, y, id, Color.white, scale) { }
			public Block (int x, int y, int id, Color color, float scale = 1f) {
				X = x;
				Y = y;
				ID = id;
				Color = color;
				Scale = scale;
			}
		}



		#endregion




		#region --- VAR ---


		// Api
		public int GridCountX {
			get => m_GridCountX;
			set {
				if (m_GridCountX != value) {
					m_GridCountX = value;
					SetVerticesDirty();
				}
			}
		}
		public int GridCountY {
			get => m_GridCountY;
			set {
				if (m_GridCountY != value) {
					m_GridCountY = value;
					SetVerticesDirty();
				}
			}
		}

		// Ser
		[SerializeField] int m_GridCountX = 8;
		[SerializeField] int m_GridCountY = 8;
		[SerializeField] float m_BlockScale = 1f;
		[SerializeField] Sprite[] m_Blocks = new Sprite[0];

		// Data
		private static readonly UIVertex[] CacheVertices = new UIVertex[4] {
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
		};
		private readonly List<Block> Blocks = new List<Block>();


		#endregion




		#region --- MSG ---


		protected override void Awake () {
			base.Awake();
			raycastPadding = new Vector4(0, 0, 0, 0);
			raycastTarget = false;
			maskable = false;
			sprite = null;
		}


		protected override void OnPopulateMesh (VertexHelper toFill) {
			toFill.Clear();
			var rect = GetPixelAdjustedRect();
			float gridSizeX = rect.width / m_GridCountX;
			float gridSizeY = rect.height / m_GridCountY;
			foreach (var block in Blocks) {
				if (block.ID < 0 || block.ID >= m_Blocks.Length) { continue; }
				SetCachePos(block.X, block.Y, block.Scale * m_BlockScale);
				SetCacheUV(m_Blocks[block.ID]);
				SetColor(block.Color * color);
				toFill.AddUIVertexQuad(CacheVertices);
			}
			// Func
			void SetCachePos (int x, int y, float scale) {
				float scaleGapX = (gridSizeX - gridSizeX * scale) / 2f;
				float scaleGapY = (gridSizeY - gridSizeY * scale) / 2f;
				CacheVertices[0].position = new Vector2(
					rect.xMin + x * gridSizeX + scaleGapX,
					rect.yMin + y * gridSizeY + scaleGapY
				);
				CacheVertices[1].position = new Vector2(
					rect.xMin + x * gridSizeX + scaleGapX,
					rect.yMin + (y + 1) * gridSizeY - scaleGapY
				);
				CacheVertices[2].position = new Vector2(
					rect.xMin + (x + 1) * gridSizeX - scaleGapX,
					rect.yMin + (y + 1) * gridSizeY - scaleGapY
				);
				CacheVertices[3].position = new Vector2(
					rect.xMin + (x + 1) * gridSizeX - scaleGapX,
					rect.yMin + y * gridSizeY + scaleGapY
				);
			}
			void SetCacheUV (Sprite sprite) {
				if (sprite != null) {
					CacheVertices[0].uv0 = sprite.uv[2];
					CacheVertices[1].uv0 = sprite.uv[0];
					CacheVertices[2].uv0 = sprite.uv[1];
					CacheVertices[3].uv0 = sprite.uv[3];
				} else {
					CacheVertices[0].uv0 = default;
					CacheVertices[1].uv0 = default;
					CacheVertices[2].uv0 = default;
					CacheVertices[3].uv0 = default;
				}
			}
			void SetColor (Color color) {
				CacheVertices[0].color = color;
				CacheVertices[1].color = color;
				CacheVertices[2].color = color;
				CacheVertices[3].color = color;
			}
		}


		#endregion




		#region --- API ---


		public void AddBlock (int x, int y, int id) => Blocks.Add(new Block(x, y, id));
		public void AddBlock (int x, int y, int id, Color color) => Blocks.Add(new Block(x, y, id, color));
		public void AddBlock (int x, int y, int id, float scale) => Blocks.Add(new Block(x, y, id, scale));
		public void AddBlock (int x, int y, int id, Color color, float scale) => Blocks.Add(new Block(x, y, id, color, scale));


		public void ClearBlock () => Blocks.Clear();


		#endregion




		#region --- LGC ---




		#endregion




		#region --- UTL ---




		#endregion





	}
}



#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEditor;


	[CustomEditor(typeof(BlocksRenderer), true)]
	public class BlocksRenderer_Inspector : Editor {


		private static readonly string[] PROP_EXC = new string[] {
			"m_Script", "m_RaycastTarget", "m_Maskable", "m_OnCullStateChanged",
			"m_RaycastPadding", "m_Sprite", "m_Type","m_PreserveAspect", "m_FillCenter",
			"m_FillMethod","m_FillAmount","m_FillClockwise",  "m_FillOrigin",
			"m_UseSpriteMesh", "m_PixelsPerUnitMultiplier",
		};


		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, PROP_EXC);
			serializedObject.ApplyModifiedProperties();
		}


	}
}
#endif