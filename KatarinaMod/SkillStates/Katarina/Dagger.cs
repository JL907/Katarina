using EntityStates;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.SkillStates.Katarina.Weapon
{
    public class ThrowDagger : BaseSkillState
    {
        public static float damageCoefficient = Modules.Config.bouncingBlades_damageCoefficient.Value;
        public static float daggerProcCoefficient = Modules.Config.bouncingBlades_procCoefficient.Value;
        public static int maxBounceCount = Modules.Config.bouncingBlades_maxBounceCount.Value;
        public static float daggerTravelSpeed = Modules.Config.bouncingBlades_travelSpeed.Value;
        public static float daggerBounceRange = Modules.Config.bouncingBlades_bounceRange.Value;
        public static float damageCoefficientPerBounce = Modules.Config.bouncingBlades_damageCoefficientPerBounce.Value;
        public static float landingDistance = Modules.Config.bouncingBlades_landingDistance.Value;
        public float baseDuration = 0.5f;
        private HuntressTracker huntressTracker;
        private float stopwatch;
        private float duration;
        private Transform modelTransform;
        private Animator animator;
        private bool hasTriedToThrowDagger;
        private HurtBox initialOrbTarget;
        private ChildLocator childLocator;

        public override void OnEnter()
        {
            base.OnEnter();
            this.stopwatch = 0f;
            this.duration = this.baseDuration;
            this.modelTransform = base.GetModelTransform();
            this.animator = base.GetModelAnimator();
            this.huntressTracker = base.GetComponent<HuntressTracker>();

            if (this.huntressTracker && base.isAuthority)
            {
                this.initialOrbTarget = this.huntressTracker.GetTrackingTarget();
            }

            if (this.modelTransform)
            {
                this.childLocator = this.modelTransform.GetComponent<ChildLocator>();
            }

            if (base.characterBody)
            {
                base.characterBody.SetAimTimer(this.duration);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (this.stopwatch >= this.duration * 0.1f && this.stopwatch < this.duration)
            {
                this.AttemptDagger();
            }
            if (this.stopwatch >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        private void AttemptDagger()
        {
            if (this.hasTriedToThrowDagger) return;
            this.hasTriedToThrowDagger = true;

            if (this.initialOrbTarget)
            {
                this.Animation();
                if (NetworkServer.active) this.FireOrbGlaive();
            }
            else if (base.isAuthority)
            {
                base.activatorSkillSlot.rechargeStopwatch += base.activatorSkillSlot.CalculateFinalRechargeInterval() - this.duration;
            }
        }

        public override void OnExit()
        {
            AttemptDagger();
            base.OnExit();
        }

        public void Animation()
        {
            Util.PlaySound("KatarinaQSFX", base.gameObject);
            if(Modules.Config.voiceLines.Value) Util.PlaySound("KatarinaQVO", base.gameObject);
            base.PlayAnimation("Gesture, Override", "ThrowDagger", "ThrowDagger.playbackRate", this.duration);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        private void FireOrbGlaive()
        {
            DaggerOrb lightningOrb = new DaggerOrb();
            lightningOrb.damageValue = base.characterBody.damage * ThrowDagger.damageCoefficient;
            lightningOrb.isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
            lightningOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
            lightningOrb.attacker = base.gameObject;
            lightningOrb.procCoefficient = ThrowDagger.daggerProcCoefficient;
            lightningOrb.bouncesRemaining = ThrowDagger.maxBounceCount;
            lightningOrb.speed = ThrowDagger.daggerTravelSpeed;
            lightningOrb.bouncedObjects = new List<HealthComponent>();
            lightningOrb.range = ThrowDagger.daggerBounceRange;
            lightningOrb.damageCoefficientPerBounce = ThrowDagger.damageCoefficientPerBounce;
            lightningOrb.tossDaggerOnArrival = true;
            lightningOrb.throwOrigin = base.characterBody.corePosition;
            lightningOrb.landingDistance = ThrowDagger.landingDistance;
            Transform hand = this.childLocator ? this.childLocator.FindChild("R_Hand") : null;
            lightningOrb.origin = hand ? hand.position : base.characterBody.corePosition;
            lightningOrb.target = this.initialOrbTarget;
            OrbManager.instance.AddOrb(lightningOrb);
        }
        public override void OnSerialize(NetworkWriter writer)
        {
            writer.Write(HurtBoxReference.FromHurtBox(this.initialOrbTarget));
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            this.initialOrbTarget = reader.ReadHurtBoxReference().ResolveHurtBox();
        }
    }
}
