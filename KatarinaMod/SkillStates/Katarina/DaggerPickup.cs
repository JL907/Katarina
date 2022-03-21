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
		private bool alive = true;
		private SphereCollider sphereCollider;
		Vector3 eulerAngleVelocity;

		private void Start()
        {
			this.rigidbody = this.GetComponent<Rigidbody>();
			this.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			this.sphereCollider = this.GetComponent<SphereCollider>();
			if (this.sphereCollider) this.sphereCollider.enabled = false;
			this.stopwatch = 0f;
			this.rigidbody.velocity = Vector3.zero;
			eulerAngleVelocity = new Vector3(0.1f, 0, 0);
			rigidbody.maxAngularVelocity = float.MaxValue;
			rigidbody.rotation = Quaternion.identity;

		}


		private void OnTriggerStay(Collider other)
		{
			if (NetworkServer.active && this.alive && TeamComponent.GetObjectTeam(other.gameObject) == TeamIndex.Player)
			{
				CharacterBody characterBody = other.gameObject.GetComponent<CharacterBody>();
				BodyIndex bodyIndex = BodyCatalog.FindBodyIndex("Katarina");
				if (characterBody && characterBody.bodyIndex == bodyIndex)
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
			this.stopwatch += Time.fixedDeltaTime;
			if (this.stopwatch > 0.5 && sphereCollider) this.sphereCollider.enabled = true;
			if (this.stopwatch < 0.5f && alive)
            {
				this.rigidbody.velocity = Vector3.up * 15f;
				Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity);
				this.rigidbody.MoveRotation(this.rigidbody.rotation * deltaRotation);
			}
			if (this.stopwatch > 0.5f && alive && sphereCollider)
			{
				if(!collided)
                {
					this.rigidbody.AddForce(Vector3.down * 200f); 
				}
				this.rigidbody.rotation = Quaternion.identity;
			}
        }
		private void OnCollisionEnter()
        {
			if (stopwatch > 0.5f && !collided)
            {
				this.rigidbody.isKinematic = true;
				this.collided = true;
			}
        }
		private void OnCollisionExit()
		{
			if (stopwatch > 0.5f && collided)
			{
				this.rigidbody.isKinematic = false;
				this.collided = false;
			}
		}
	}
}
