using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KatarinaMod.SkillStates.Katarina
{
    public class Voracity : GenericCharacterMain
    {
        public float duration;
        public bool attacked;
        protected float stopwatch;
        protected float baseDuration = 0.2f;
        protected float damageCoefficient = 6f;
        private Transform indicatorInstance;
        public override void OnEnter()
        {
            base.OnEnter();
            this.attacked = false;
            this.duration = this.baseDuration / this.attackSpeedStat;
            base.PlayAnimation("FullBody, Override", "Passive", "Passive.playbackRate", this.duration);
            Util.PlaySound("KatarinaVoracitySFX", base.gameObject);
            if (!this.indicatorInstance) this.CreateIndicator();
        }

        public override void OnExit()
        {
            base.PlayAnimation("FullBody, Override", "BufferEmpty");
            if (this.indicatorInstance) EntityState.Destroy(this.indicatorInstance.gameObject);
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (!this.indicatorInstance) this.CreateIndicator(); this.UpdateIndicator();
            if (!attacked)
            {
                this.Fire();
                this.attacked = true;
            }
            if (stopwatch >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        private void CreateIndicator()
        {
            if (EntityStates.Huntress.ArrowRain.areaIndicatorPrefab)
            {
                this.indicatorInstance = UnityEngine.Object.Instantiate<GameObject>(EntityStates.Huntress.ArrowRain.areaIndicatorPrefab).transform;
                this.indicatorInstance.localScale = Vector3.one * 10f;
                this.indicatorInstance.transform.position = base.gameObject.transform.position;
            }
        }
        private void Fire()
        {
            Ray aimRay = base.GetAimRay();
            foreach (Collider collider in Physics.OverlapSphere(this.indicatorInstance.transform.position, 10f))
            {
                HealthComponent component = collider.GetComponent<HealthComponent>();
                if (component)
                {
                    TeamComponent component2 = collider.GetComponent<TeamComponent>();
                    bool flag = false;
                    if (component2)
                    {
                        flag = (component2.teamIndex == base.GetTeam());
                    }
                    if (!flag)
                    {
                        DamageInfo damageInfo = new DamageInfo();
                        damageInfo.damage = this.damageStat * this.damageCoefficient;
                        damageInfo.attacker = base.gameObject;
                        damageInfo.inflictor = base.gameObject;
                        damageInfo.force = Vector3.zero;
                        damageInfo.crit = base.RollCrit();
                        damageInfo.procCoefficient = 1f;
                        damageInfo.position = component.transform.position;
                        damageInfo.damageType = DamageType.AOE;
                        component.TakeDamage(damageInfo);
                        GlobalEventManager.instance.OnHitEnemy(damageInfo, component.gameObject);
                        GlobalEventManager.instance.OnHitAll(damageInfo, component.gameObject);

                    }
                }
            }
        }


        private void UpdateIndicator()
        {
            if (indicatorInstance)
            {
                this.indicatorInstance.transform.position = base.gameObject.transform.position;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Any;
        }
    }
}
