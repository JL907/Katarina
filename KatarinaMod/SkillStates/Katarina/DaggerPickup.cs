using KatarinaMod.SkillStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod
{
    public class DaggerPickup : MonoBehaviour
    {
		public GameObject baseObject;
		public GameObject ownerGameObject;
		public TeamFilter teamFilter;
		public GameObject pickupEffect;
		private bool alive = true;

		private void OnTriggerStay(Collider other)
		{
			if (NetworkServer.active && this.alive && other.gameObject == ownerGameObject)
			{
				this.alive = false;
				UnityEngine.Object.Destroy(this.baseObject);
				EntityStateMachine component = other.gameObject.transform.GetComponent<EntityStateMachine>();
				component.SetNextState(new SlashCombo { });
			}
		}
	}
}
