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
		public Rigidbody rigidbody;
		public GameObject pickupEffect;
		public bool collided = false;
		public GameObject owner;
		private bool alive = true;
		private void Awake()
        {
			this.rigidbody = this.GetComponent<Rigidbody>();
            RoR2.GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
		{
			if (damageReport is null) return;
			if (damageReport.victimBody is null) return;
			if (damageReport.attackerBody is null) return;

			if (damageReport.victimTeamIndex != TeamIndex.Player && damageReport.attackerBody.baseNameToken == "Lemonlust_KATARINA_BODY_NAME")
			{
				SkillLocator component = damageReport.attackerBody.GetComponent<SkillLocator>();
				if (component.primary)
				{
					component.primary.RunRecharge(2f);
				}
				if (component.secondary)
				{
					component.secondary.RunRecharge(2f);
				}
				if (component.utility)
				{
					component.utility.RunRecharge(2f);
				}
				if (component.special)
				{
					component.special.RunRecharge(2f);
				}
			}
		}

		private void OnTriggerStay(Collider other)
		{
			if (NetworkServer.active && this.alive && TeamComponent.GetObjectTeam(other.gameObject) == TeamIndex.Player && owner == other.gameObject)
			{
				EntityStateMachine component = other.GetComponent<EntityStateMachine>();
				SkillLocator component2 = other.GetComponent<SkillLocator>();
				if (component && component2 && component.state.isAuthority)
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
				this.rigidbody.AddTorque(2500000, 0, 0);
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
			if (duration > 1f && !collided)
            {
				this.rigidbody.isKinematic = true;
				this.collided = true;
			}
        }
		private void OnCollisionExit()
		{
			if (duration > 1f && collided)
			{
				this.rigidbody.isKinematic = false;
				this.collided = false;
			}
		}
    }
}
