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
			if (NetworkServer.active && this.alive && TeamComponent.GetObjectTeam(other.gameObject) == TeamIndex.Player && owner == other.gameObject)
			{
				EntityStateMachine component = other.GetComponent<EntityStateMachine>();
				SkillLocator component2 = other.GetComponent<SkillLocator>();
				if (component && component2)
				{
					this.alive = false;
					component.SetNextState(new Voracity());
					if (component2.utility)
					{
						component2.utility.RunRecharge(8f);
					}
					//EffectManager.SimpleEffect(this.pickupEffect, base.transform.position, Quaternion.identity, true);
					UnityEngine.Object.Destroy(base.gameObject);
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
		internal static GameObject CreateDagger(GameObject daggerOwner)
		{
			var newDagger = UnityEngine.Object.Instantiate<GameObject>(Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon"));
			newDagger.AddComponent<DaggerPickup>();
			newDagger.AddComponent<DestroyOnTimer>().duration = 6f;
			newDagger.AddComponent<NetworkIdentity>();
			newDagger.GetComponent<DaggerPickup>().owner = daggerOwner;
			return newDagger;
		}

	}
}
