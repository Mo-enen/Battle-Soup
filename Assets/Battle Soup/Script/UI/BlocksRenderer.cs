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
		public int BlockSpriteCount => m_Blocks.Length;

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
				SetCachePos(block.X, block.Y, block.Scale * m_BlockScale);
				SetCacheUV(block.ID >= 0 && block.ID < m_Blocks.Length ? m_Blocks[block.ID] : null);
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
					var _rect = sprite.rect;
					float _width = sprite.texture.width;
					float _height = sprite.texture.height;
					_rect.x /= _width;
					_rect.y /= _height;
					_rect.width /= _width;
					_rect.height /= _height;
					CacheVertices[0].uv0 = new Vector2(_rect.xMin, _rect.yMin);
					CacheVertices[1].uv0 = new Vector2(_rect.xMin, _rect.yMax);
					CacheVertices[2].uv0 = new Vector2(_rect.xMax, _rect.yMax);
					CacheVertices[3].uv0 = new Vector2(_rect.xMax, _rect.yMin);
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


		public void AddBody (ShipData shipData) {
			var (bodyMin, bodyMax) = shipData.Ship.GetBounds(false);
			var bodySize = bodyMax - bodyMin;
			bool flip = false;
			if (bodySize.x > bodySize.y) {
				(bodyMin, bodyMax) = shipData.Ship.GetBounds(true);
				bodySize = bodyMax - bodyMin;
				flip = true;
			}
			GridCountX = bodySize.x + 1;
			GridCountY = bodySize.y + 1;
			rectTransform.SetSizeWithCurrentAnchors(
				RectTransform.Axis.Horizontal,
				GridCountX * 12
			);
			rectTransform.SetSizeWithCurrentAnchors(
				RectTransform.Axis.Vertical,
				GridCountY * 12
			);
			ClearBlock();
			foreach (var v in shipData.Ship.Body) {
				AddBlock(flip ? v.y : v.x, flip ? v.x : v.y, 0);
			}
		}


		public void ClearBlock () {
			if (Blocks.Count > 0) {
				Blocks.Clear();
			}
		}


		public void SetSprites (Sprite[] sprites) => m_Blocks = sprites;


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