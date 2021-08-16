using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace BattleSoup {
	public class Effect : MonoBehaviour {


		// Ser
		[SerializeField] ParticleSystem[] m_Particles = null;



		// API
		public void UI_SpawnParticle_WaterReveal (Vector3 pos) => SpawnParticle(0, pos);
		public void UI_SpawnParticle_ShipReveal (Vector3 pos) => SpawnParticle(1, pos);
		public void UI_SpawnParticle_ShipHit (Vector3 pos) => SpawnParticle(2, pos);
		public void UI_SpawnParticle_ShipSunk (Vector3 pos) => SpawnParticle(3, pos);


		public void SpawnParticle (int index, Vector3 pos) {
			Instantiate(m_Particles[index], null).transform.position = pos;
		}


	}
}
