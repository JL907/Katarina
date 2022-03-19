using EntityStates;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KatarinaMod.SkillStates
{
    public class Deathlotus : BaseState
    {
        private float baseDuration = 2.5f;
        private float duration;
        private float daggerTimer;
        private float baseDaggerThrottle = 0.166f;
        private float daggerThrottle;
        private bool throwing;
        private static float damageCoefficient = 2f;
        private float stopwatch;
        private Animator animator;
        public uint activeSFXPlayID;

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            int layerIndex = animator.GetLayerIndex("FullBody, Override");
            this.animator.PlayInFixedTime("Ultimate", layerIndex, this.stopwatch);
            this.animator.Update(0f);
            float length = animator.GetCurrentAnimatorStateInfo(layerIndex).length;
            animator.SetFloat("Ultimate.playbackRate", length / duration);
            if (daggerTimer < daggerThrottle)
            {
                throwing = false;
                daggerTimer += Time.fixedDeltaTime;
            }
            if (daggerTimer >= daggerThrottle && !throwing)
            {
                daggerTimer = 0f;
                throwing = true;
                ThrowDagger();
            }
            if (this.stopwatch >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
        protected virtual GenericDamageOrb CreateArrowOrb()
        {
            return new HuntressArrowOrb();
        }

        private void ThrowDagger()
        {
            foreach (Collider collider in Physics.OverlapSphere(base.gameObject.transform.position, 20f))
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
                        KnifeDamageOrb genericDamageOrb = new KnifeDamageOrb();
                        genericDamageOrb.damageValue = base.characterBody.damage * damageCoefficient;
                        genericDamageOrb.isCrit = base.RollCrit();
                        genericDamageOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
                        genericDamageOrb.attacker = base.gameObject;
                        genericDamageOrb.procCoefficient = 1f;
                        HurtBox hurtBox = component.body.mainHurtBox;
                        if (hurtBox)
                        {
                            genericDamageOrb.origin = base.transform.position + base.transform.up * 0.8f;
                            genericDamageOrb.target = hurtBox;
                            OrbManager.instance.AddOrb(genericDamageOrb);

                        }
                    }
                }
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.animator = base.GetModelAnimator();
            this.stopwatch = 0f;
            this.duration = this.baseDuration;
            this.daggerThrottle = this.baseDaggerThrottle / this.attackSpeedStat;
            activeSFXPlayID = Util.PlaySound("KatarinaRSFX", base.gameObject);
            Util.PlaySound("KatarinaRVO", base.gameObject);
            this.animator.SetFloat("Ultimate.playbackRate", 1f);
            //base.PlayAnimation("FullBody, Override", "Ultimate", "Ultimate.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            KatarinaMod.KatarinaPlugin.instance.Logger.LogMessage("Deathlotus onexit()");
            if (this.activeSFXPlayID != 0) AkSoundEngine.StopPlayingID(this.activeSFXPlayID);
            base.PlayAnimation("FullBody, Override", "BufferEmpty");
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
