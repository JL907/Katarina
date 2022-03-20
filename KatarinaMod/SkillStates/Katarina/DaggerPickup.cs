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
		private float stopwatch;
		public Rigidbody rigidbody;
		public GameObject pickupEffect;
		public bool collided = false;
		public GameObject owner;
		private bool alive = true;
		private SphereCollider sphereCollider;
		private void Awake()
        {
			this.rigidbody = this.GetComponent<Rigidbody>();
			this.sphereCollider = this.GetComponent<SphereCollider>();
			if (this.sphereCollider) this.sphereCollider.enabled = false;
        }


		private void OnTriggerStay(Collider other)
		{
			if (this.alive && TeamComponent.GetObjectTeam(other.gameObject) == TeamIndex.Player)
			{
				if (other.gameObject.GetComponent<CharacterBody>().baseNameToken == "Lemonlust_KATARINA_BODY_NAME")
                {
					EntityStateMachine component = other.gameObject.GetComponent<EntityStateMachine>();
					if (component)
					{
						this.alive = false;
						if (Util.HasEffectiveAuthority(component.networkIdentity))
                        {
							component.SetNextState(new Voracity());
						}
						UnityEngine.Object.Destroy(base.gameObject);
					}
				}
			}
		}

		private void FixedUpdate()
        {
			this.stopwatch += Time.deltaTime;
			if (this.stopwatch > 0.5f && sphereCollider) this.sphereCollider.enabled = true;
			if (this.stopwatch < 1f && alive)
            {
				this.rigidbody.AddTorque(250000, 0, 0);
            }
			if (this.stopwatch > 1f && alive)
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
			if (stopwatch > 1f && !collided)
            {
				this.rigidbody.isKinematic = true;
				this.collided = true;
			}
        }
		private void OnCollisionExit()
		{
			if (stopwatch > 1f && collided)
			{
				this.rigidbody.isKinematic = false;
				this.collided = false;
			}
		}
	}
}
