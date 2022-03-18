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
		private float duration;
		public GameObject baseObject;
		public Rigidbody rigidbody;
		public GameObject pickupEffect;
		public bool collided = false;
		private bool alive = true;

		private void Awake()
        {
			this.rigidbody = this.GetComponent<Rigidbody>();
        }
		private void OnTriggerStay(Collider other)
		{
			if (NetworkServer.active && this.alive && TeamComponent.GetObjectTeam(other.gameObject) == TeamIndex.Player)
			{
				EntityStateMachine component = other.GetComponent<EntityStateMachine>();
				SkillLocator component2 = other.GetComponent<SkillLocator>();
				if (component && component2)
				{
					this.alive = false;
					component.SetNextState(new Voracity());
					component2.utility.AddOneStock();
					//EffectManager.SimpleEffect(this.pickupEffect, base.transform.position, Quaternion.identity, true);
					UnityEngine.Object.Destroy(base.gameObject);
				}
			}
		}

		private void FixedUpdate()
        {
			this.duration += Time.deltaTime;
			if (this.duration < 1f && alive)
            {
				this.rigidbody.AddTorque(25000, 0, 0);
            }
			if (this.duration > 1f && alive)
			{
				if(!collided)
                {
					this.rigidbody.AddForce(Vector3.down * 100f); ;
				}
				this.rigidbody.rotation = Quaternion.identity;
			}
        }
		private void OnCollisionEnter()
        {
			if (duration > 1f)
            {
				this.rigidbody.isKinematic = true;
				this.collided = true;
			}
        }
	}
}
