using EntityStates;
using KatarinaMod.Components;
using KatarinaMod.Modules;
using KatarinaMod.SkillStates;
using KatarinaMod.SkillStates.Katarina;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod
{
	public class DaggerPickup : NetworkBehaviour
    {
		private float stopwatch;
		public Rigidbody rigidbody;
		public GameObject pickupEffect;
		public bool collided = false;
		private bool alive = true;
		private SphereCollider sphereCollider;
        private ProjectileController projectileController;
        Vector3 eulerAngleVelocity;

		private void Awake()
        {
			this.rigidbody = this.GetComponent<Rigidbody>();
			this.sphereCollider = this.GetComponent<SphereCollider>();
			this.sphereCollider.radius = 3f;
			this.sphereCollider.isTrigger = true;
			this.sphereCollider.center = Vector3.zero;
			this.projectileController = base.GetComponent<ProjectileController>();
			if (this.sphereCollider) this.sphereCollider.enabled = false;
			this.stopwatch = 0f;
			this.rigidbody.velocity = Vector3.zero;
			eulerAngleVelocity = new Vector3(15f, 0, 0);
		}


		private void OnTriggerEnter(Collider other)
        {
			CharacterBody characterBody = other.GetComponent<CharacterBody>();
			if (characterBody && characterBody?.bodyIndex == BodyCatalog.FindBodyIndex("Katarina"))
			{
				this.alive = false;
				KatarinaNetworkCommands knc = characterBody.GetComponent<KatarinaNetworkCommands>();
				if (knc)
				{
					knc.RpcVoracity();
				}
				UnityEngine.Object.Destroy(base.gameObject);
			}
        }

		private void FixedUpdate()
        {
			this.stopwatch += Time.fixedDeltaTime;
			if (this.stopwatch < 0.5f && alive)
			{
				this.sphereCollider.enabled = false;
				this.rigidbody.velocity = Vector3.up * 15f;
				Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity * 1.5f);
				this.rigidbody.MoveRotation(this.rigidbody.rotation * deltaRotation);
			}
			if (this.stopwatch > 0.5f && alive)
			{
				this.sphereCollider.enabled = true;
				if (!collided)
				{
					this.rigidbody.AddForce(Vector3.down * 10f, ForceMode.VelocityChange);
				}
				this.rigidbody.rotation = Quaternion.Lerp(this.rigidbody.rotation, Quaternion.identity, this.stopwatch / 1f);
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
