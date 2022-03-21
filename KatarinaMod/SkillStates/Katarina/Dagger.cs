using EntityStates;
using EntityStates.Huntress.HuntressWeapon;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.SkillStates.Katarina.Weapon
{
    public class ThrowDagger : BaseState
    {
        private static float damageCoefficient = 2f;
        private static float daggerProcCoefficient = 1f;
		public static int maxBounceCount = 4;
		public static float daggerTravelSpeed = 100f;
		public static float daggerBounceRange = 10f;
        private static float damageCoefficientPerBounce = 1.0f;
        public float baseDuration = 1f;
        private HuntressTracker huntressTracker;
        private float stopwatch;
        private float duration;
        private Transform modelTransform;
        private Animator animator;
        private bool hasTriedToThrowDagger;
        private HurtBox initialOrbTarget;
        private ChildLocator childLocator;
        private bool hasSuccessfullyThrownDagger;

        public override void OnEnter()
        {
            base.OnEnter();
            this.stopwatch = 0f;
            this.duration = this.baseDuration / this.attackSpeedStat;
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
			if (!this.hasTriedToThrowDagger)
			{
				this.FireOrbGlaive();
			}
			if (this.stopwatch >= this.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
				return;
			}
        }

		public override void OnExit()
        {
			base.OnExit();
			if (!this.hasTriedToThrowDagger)
			{
				this.FireOrbGlaive();
			}
			if (!this.hasSuccessfullyThrownDagger && NetworkServer.active)
			{
				base.skillLocator.secondary.AddOneStock(); 
			}
        }

		public void Animation()
        {
			Util.PlaySound("KatarinaQSFX", base.gameObject);
			Util.PlaySound("KatarinaQVO", base.gameObject);
			base.PlayAnimation("Gesture, Override", "ThrowDagger", "ThrowDagger.playbackRate", this.duration);
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}

		private void FireOrbGlaive()
		{
			if (!NetworkServer.active || this.hasTriedToThrowDagger)
			{
				return;
			}
			this.hasTriedToThrowDagger = true;
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
			HurtBox hurtBox = this.initialOrbTarget;
			if (hurtBox)
			{
				this.hasSuccessfullyThrownDagger = true;
				KatarinaMod.KatarinaPlugin.instance.Logger.LogMessage("Toss Dagger");
				Animation();
				Transform transform = this.childLocator.FindChild("R_Hand");
				EffectManager.SimpleMuzzleFlash(ThrowGlaive.muzzleFlashPrefab, base.gameObject, "R_Hand", true);
				lightningOrb.origin = transform.position;
				lightningOrb.target = hurtBox;
				OrbManager.instance.AddOrb(lightningOrb);
			}
		}
		public override void OnSerialize(NetworkWriter writer)
		{
			writer.Write(HurtBoxReference.FromHurtBox(this.initialOrbTarget));
		}

		// Token: 0x06000E6F RID: 3695 RVA: 0x0003E6A4 File Offset: 0x0003C8A4
		public override void OnDeserialize(NetworkReader reader)
		{
			this.initialOrbTarget = reader.ReadHurtBoxReference().ResolveHurtBox();
		}
	}
}
