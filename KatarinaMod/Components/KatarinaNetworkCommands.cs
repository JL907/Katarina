using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using RoR2.Projectile;
using KatarinaMod.SkillStates.Katarina;

namespace KatarinaMod.Components
{
    public class KatarinaNetworkCommands : NetworkBehaviour
    {
        [ClientRpc]
        public void RpcResetSecondaryCooldown(float duration)
        {
            skillLocator.secondary.stock = skillLocator.secondary.maxStock;
            skillLocator.secondary.rechargeStopwatch = 0f;
        }

        [ClientRpc]
        public void RpcDaggerToss(GameObject owner, Vector3 position)
        {
            if (this.hasAuthority)
            {
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = Modules.Projectiles.knifePrefab,
                    position = position,
                    rotation = Quaternion.identity,
                    owner = owner,
                    damage = 0,
                    force = 0,
                    crit = false,
                    speedOverride = 0f
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
        }

        [ClientRpc]
        public void RpcVoracity()
        {
            {
                entityStateMachine.SetNextState(new Voracity());
            }
        }

        private void Awake()
        {
            characterBody = base.GetComponent<CharacterBody>();
            skillLocator = base.GetComponent<SkillLocator>();
            entityStateMachine = base.GetComponent<EntityStateMachine>();
        }

        private SkillLocator skillLocator;
        private EntityStateMachine entityStateMachine;
        private CharacterBody characterBody;
    }
}