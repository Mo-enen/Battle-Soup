using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace BattleSoup {
	public class RandomExplosionUI : Image {


		// SUB
		private class Node {
			public float StartTime;
			public float EndTime;
			public float X01;
			public float Y01;
		}

		// Ser
		[SerializeField] Vector2 m_Size = new(24, 24);
		[SerializeField] int m_Count = 4;
		[SerializeField] int m_Frame = 2;
		[SerializeField] float m_DurationA = 0.2f;
		[SerializeField] float m_DurationB = 0.6f;
		[SerializeField] float m_Gap = 0.2f;
		[SerializeField] BattleSoup Soup;
		[SerializeField] Sprite[] m_Sprites = null;

		// Data
		private static readonly UIVertex[] c_Vertex = new UIVertex[4] {
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
			new UIVertex(){ color = Color.white },
		};
		private readonly Queue<Node> Nodes = new();
		private int Index = 0;
		private AudioSource Audio = null;
		private float PrevAddTime = float.MinValue;

		// MSG
		protected override void OnEnable () {
			base.OnEnable();
			SetAllDirty();
			Nodes.Clear();
			if (Audio == null) Audio = GetComponent<AudioSource>();
			if (Audio != null && Soup.UseSound) {
				Audio.Play();
				Audio.volume = 1f;
			}
		}


		private void Update () {
			if (Nodes.Count < m_Count && Time.time > PrevAddTime + m_Gap) {
				PrevAddTime = Time.time;
				Nodes.Enqueue(new Node() {
					X01 = Random.value,
					Y01 = Random.value,
					StartTime = Time.time,
					EndTime = Time.time + Random.Range(m_DurationA, m_DurationB),
				});
			}
			if (Audio != null) {
				Audio.volume = Mathf.Clamp01(Audio.volume - Time.deltaTime / 2f);
			}
			while (Nodes.Count > 0) {
				if (Time.time > Nodes.Peek().EndTime) {
					Nodes.Dequeue();
				} else break;
			}
			SetVerticesDirty();
		}


		protected override void OnPopulateMesh (VertexHelper toFill) {
			toFill.Clear();
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
			if (m_Sprites == null || m_Sprites.Length == 0) return;
			c_Vertex[0].color = color;
			c_Vertex[1].color = color;
			c_Vertex[2].color = color;
			c_Vertex[3].color = color;
			if (sprite == null) {
				c_Vertex[0].uv0 = new Vector2(0, 0);
				c_Vertex[1].uv0 = new Vector2(0, 1);
				c_Vertex[2].uv0 = new Vector2(1, 1);
				c_Vertex[3].uv0 = new Vector2(1, 0);
			} else {
				var sp = m_Sprites[(Index / m_Frame).UMod(m_Sprites.Length)];
				Index++;
				c_Vertex[0].uv0 = sp.uv[0];
				c_Vertex[1].uv0 = sp.uv[1];
				c_Vertex[2].uv0 = sp.uv[3];
				c_Vertex[3].uv0 = sp.uv[2];
			}
			var rect = GetPixelAdjustedRect();
			var _rect = new Rect(0, 0, m_Size.x, m_Size.y);
			foreach (var node in Nodes) {
				_rect.x = Mathf.LerpUnclamped(rect.xMin, rect.xMax, node.X01) - m_Size.x / 2f;
				_rect.y = Mathf.LerpUnclamped(rect.yMin, rect.yMax, node.Y01) - m_Size.y / 2f;
				c_Vertex[0].position = new(_rect.xMin, _rect.yMin, 0f);
				c_Vertex[1].position = new(_rect.xMin, _rect.yMax, 0f);
				c_Vertex[2].position = new(_rect.xMax, _rect.yMax, 0f);
				c_Vertex[3].position = new(_rect.xMax, _rect.yMin, 0f);
				toFill.AddUIVertexQuad(c_Vertex);
			}

		}


	}
}
#if UNITY_EDITOR
namespace BattleSoup.Editor {
	using UnityEngine;
	using UnityEditor;
	[CustomEditor(typeof(RandomExplosionUI))]
	public class RandomExplosionUI_Inspector : Editor {
		public override void OnInspectorGUI () {
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_Script", "m_OnCullStateChanged");
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif