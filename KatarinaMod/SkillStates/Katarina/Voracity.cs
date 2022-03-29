using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.SkillStates.Katarina
{
    public class Voracity : GenericCharacterMain
    {
        public float duration;
        public bool attacked;
        protected float stopwatch;
        protected float baseDuration = 0.2f;
        protected float damageCoefficient = Modules.Config.voracity_damageCoefficient.Value;
        protected float procCoefficient = Modules.Config.voracity_procCoefficient.Value;
        public float damageRadius = Modules.Config.voracity_radius.Value;
        public float shunpoRecharge = Modules.Config.voracity_shunpoRecharge.Value;
        private Transform indicatorInstance;
        private Animator animator;
        public override void OnEnter()
        {
            base.OnEnter();
            this.animator = base.GetModelAnimator();
            this.attacked = false;
            this.duration = this.baseDuration / this.attackSpeedStat;
            Util.PlaySound("KatarinaVoracitySFX", base.gameObject);
            this.animator.SetFloat("Ultimate.playbackRate", 1f);
            base.skillLocator.utility.RunRecharge(this.shunpoRecharge);
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
            if (isAuthority)
            {
                int layerIndex = animator.GetLayerIndex("FullBody, Override");
                this.animator.PlayInFixedTime("Ultimate", layerIndex, this.stopwatch);
                this.animator.Update(0f);
                float length = animator.GetCurrentAnimatorStateInfo(layerIndex).length;
                animator.SetFloat("Ultimate.playbackRate", length / duration);
            }
            if (!this.indicatorInstance) this.CreateIndicator(); this.UpdateIndicator();
            if (!this.attacked) 
            {
                this.attacked = true;
                if (NetworkServer.active)
                {
                    this.Fire();
                }
            }
            if (stopwatch >= this.duration && base.isAuthority)
            {
                if (this.indicatorInstance) EntityState.Destroy(this.indicatorInstance.gameObject);
                this.outer.SetNextStateToMain();
                return;
            }
        }

        private void CreateIndicator()
        {
            if (EntityStates.Huntress.ArrowRain.areaIndicatorPrefab)
            {
                this.indicatorInstance = UnityEngine.Object.Instantiate<GameObject>(EntityStates.Huntress.ArrowRain.areaIndicatorPrefab).transform;
                this.indicatorInstance.localScale = Vector3.one * this.damageRadius;
                this.indicatorInstance.transform.position = base.gameObject.transform.position;
            }
        }
        private void Fire()
        {
            KatarinaMod.KatarinaPlugin.instance.Logger.LogMessage("Fired Voracity");
            List<HurtBox> HurtBoxes = new List<HurtBox>();
            HurtBoxes = new SphereSearch
            {
                radius = this.damageRadius,
                mask = LayerIndex.entityPrecise.mask,
                origin = base.transform.position
            }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(base.teamComponent.teamIndex)).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes().ToList();

            foreach (HurtBox hurtbox in HurtBoxes)
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.damage = this.damageStat * this.damageCoefficient;
                damageInfo.attacker = base.gameObject;
                damageInfo.inflictor = base.gameObject;
                damageInfo.force = Vector3.zero;
                damageInfo.crit = base.RollCrit();
                damageInfo.procCoefficient = this.procCoefficient;
                damageInfo.position = hurtbox.gameObject.transform.position;
                damageInfo.damageType = DamageType.AOE;
                hurtbox.healthComponent.TakeDamage(damageInfo);
                GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtbox.healthComponent.gameObject);
                GlobalEventManager.instance.OnHitAll(damageInfo, hurtbox.healthComponent.gameObject);
                Util.PlaySound("KatarinaImpactSFX", base.gameObject);
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
