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
		private bool used;
		private SphereCollider sphereCollider;
		Vector3 eulerAngleVelocity;

		private void Awake()
        {
			this.rigidbody = this.GetComponent<Rigidbody>();
			this.sphereCollider = this.GetComponent<SphereCollider>();
			this.sphereCollider.radius = 3f;
			this.sphereCollider.isTrigger = true;
			this.sphereCollider.center = Vector3.zero;
			this.used = false;
			if (this.sphereCollider) this.sphereCollider.enabled = false;
			this.stopwatch = 0f;
			this.rigidbody.velocity = Vector3.zero;
			eulerAngleVelocity = new Vector3(95f, 0, 0);

		}

		private void FindPlayer()
		{
			if (used) return;
			Collider[] array = Physics.OverlapSphere(base.transform.position, 5f);
			for (int i = 0; i < array.Length; i++)
			{
				if (used) return;
				CharacterBody characterBody = array[i].gameObject.GetComponent<CharacterBody>();
				BodyIndex bodyIndex = BodyCatalog.FindBodyIndex("Katarina");
				if (characterBody && characterBody.bodyIndex == bodyIndex && TeamComponent.GetObjectTeam(array[i].gameObject) == TeamIndex.Player)
				{
					EntityStateMachine component = array[i].gameObject.GetComponent<EntityStateMachine>();
					if (component)
					{
						used = true;
						this.alive = false;
						if (Util.HasEffectiveAuthority(component.networkIdentity)) component.SetNextState(new Voracity());
						UnityEngine.Object.Destroy(base.gameObject);
						return;
					}
				}
			}
		}

		private void FixedUpdate()
        {
			this.stopwatch += Time.fixedDeltaTime;
			if (!used && this.stopwatch >= 0.5f) FindPlayer();
			if (NetworkServer.active)
            {
				if (this.stopwatch < 0.5f && alive)
				{
					this.rigidbody.velocity = Vector3.up * 15f;
					Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity * Time.fixedDeltaTime);
					this.rigidbody.MoveRotation(this.rigidbody.rotation * deltaRotation);
				}
				if (this.stopwatch > 0.5f && alive)
				{
					this.sphereCollider.enabled = true;
					if (!collided)
					{
						this.rigidbody.AddForce(Vector3.down * 200f);
					}
					this.rigidbody.rotation = Quaternion.identity;
				}
			}
        }
		private void OnCollisionEnter()
        {
			if (NetworkServer.active)
            {
				if (stopwatch > 0.5f && !collided)
				{
					this.rigidbody.isKinematic = true;
					this.collided = true;
				}
			}
        }
		private void OnCollisionExit()
		{
			if (NetworkServer.active)
            {
				if (stopwatch > 0.5f && collided)
				{
					this.rigidbody.isKinematic = false;
					this.collided = false;
				}
			}
		}
	}
}
