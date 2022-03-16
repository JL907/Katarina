using EntityStates;
using KatarinaMod.Modules;
using KatarinaMod.SkillStates;
using KatarinaMod.SkillStates.Katarina;
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
		private void OnTriggerStay(Collider other)
		{
			if (NetworkServer.active && this.alive && TeamComponent.GetObjectTeam(other.gameObject) == TeamIndex.Player)
			{
				EntityStateMachine component = other.GetComponent<EntityStateMachine>();
				if (component)
				{
					this.alive = false;
					component.SetInterruptState(new Voracity(), InterruptPriority.Any);
					//EffectManager.SimpleEffect(this.pickupEffect, base.transform.position, Quaternion.identity, true);
					UnityEngine.Object.Destroy(this.baseObject);
				}
			}
		}
		[Tooltip("The base object to destroy when this pickup is consumed.")]
		public GameObject baseObject;
		public GameObject pickupEffect;
		private bool alive = true;
	}
}
