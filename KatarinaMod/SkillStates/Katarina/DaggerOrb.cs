using KatarinaMod.Modules;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Orb
{
    public class DaggerOrb : LightningOrb
    {
        private GameObject weaponInstance;
        public override void Begin()
        {
            base.Begin();
        }

		public override void OnArrival()
		{
			this.canBounceOnSameTarget = false;
			if (this.target)
			{
				HealthComponent healthComponent = this.target.healthComponent;
				if (healthComponent)
				{
					DamageInfo damageInfo = new DamageInfo();
					damageInfo.damage = this.damageValue;
					damageInfo.attacker = this.attacker;
					damageInfo.inflictor = this.inflictor;
					damageInfo.force = Vector3.zero;
					damageInfo.crit = this.isCrit;
					damageInfo.procChainMask = this.procChainMask;
					damageInfo.procCoefficient = this.procCoefficient;
					damageInfo.position = this.target.transform.position;
					damageInfo.damageColorIndex = this.damageColorIndex;
					damageInfo.damageType = this.damageType;
					healthComponent.TakeDamage(damageInfo);
					GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
					GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
					TossDagger();
				}
				this.failedToKill |= (!healthComponent || healthComponent.alive);
				if (this.bouncesRemaining > 0)
				{
					for (int i = 0; i < this.targetsToFindPerBounce; i++)
					{
						if (this.bouncedObjects != null)
						{
							if (this.canBounceOnSameTarget)
							{
								this.bouncedObjects.Clear();
							}
							this.bouncedObjects.Add(this.target.healthComponent);
						}
						HurtBox hurtBox = this.PickNextTarget(this.target.transform.position);
						if (hurtBox)
						{
							DaggerOrb lightningOrb = new DaggerOrb();
							lightningOrb.search = this.search;
							lightningOrb.origin = this.target.transform.position;
							lightningOrb.target = hurtBox;
							lightningOrb.attacker = this.attacker;
							lightningOrb.inflictor = this.inflictor;
							lightningOrb.teamIndex = this.teamIndex;
							lightningOrb.damageValue = this.damageValue * this.damageCoefficientPerBounce;
							lightningOrb.bouncesRemaining = this.bouncesRemaining - 1;
							lightningOrb.isCrit = this.isCrit;
							lightningOrb.bouncedObjects = this.bouncedObjects;
							lightningOrb.lightningType = this.lightningType;
							lightningOrb.procChainMask = this.procChainMask;
							lightningOrb.procCoefficient = this.procCoefficient;
							lightningOrb.damageColorIndex = this.damageColorIndex;
							lightningOrb.damageCoefficientPerBounce = this.damageCoefficientPerBounce;
							lightningOrb.speed = this.speed;
							lightningOrb.range = this.range;
							lightningOrb.damageType = this.damageType;
							lightningOrb.failedToKill = this.failedToKill;
							OrbManager.instance.AddOrb(lightningOrb);
						}
					}
					return;
				}
			}
		}
		private void TossDagger()
        {
            if(NetworkServer.active)
            {
                if (!weaponInstance)
                {
                    weaponInstance = UnityEngine.Object.Instantiate<GameObject>(Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon"));
                    weaponInstance.AddComponent<DaggerPickup>().baseObject = weaponInstance;
                    weaponInstance.AddComponent<DestroyOnTimer>().duration = 4f;
                    weaponInstance.AddComponent<NetworkIdentity>();
                }
                Vector3 position = this.target.transform.position + Vector3.up * 1.5f;
                Vector3 toTarget = (this.target.transform.position - this.attacker.gameObject.transform.position).normalized;
                Vector3 upVector = Vector3.up * 20 + toTarget * 2f;
				weaponInstance.transform.position = position;
                Rigidbody component2 = weaponInstance.GetComponent<Rigidbody>();
                component2.velocity = upVector;
                component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
                NetworkServer.Spawn(weaponInstance);
            }
        }
    }
}
