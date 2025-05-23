﻿using EntityStates;
using EntityStates.Huntress;
using KatarinaMod.Modules;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.SkillStates
{
    public class Shunpo : BaseSkillState
    {
        private Transform modelTransform;
        public static GameObject blinkPrefab;
        private float stopwatch;
        private Vector3 blinkVector = Vector3.zero;
        public float duration = 0.3f;
        public float speedCoefficient = 12f;
        public string beginSoundString = "KatarinaESFX";
        public string endSoundString;
        private CharacterModel characterModel;
        private HurtBoxGroup hurtboxGroup;
        private Vector3 startLoc;
        private HuntressTracker huntressTracker;
        private HurtBox initialTarget;
        private float damageCoefficient = Modules.Config.shunpo_damageCoefficient.Value;
        private float procCoefficient = Modules.Config.shunpo_procCoefficient.Value;
        private bool damaged = false;

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(this.beginSoundString, base.gameObject);
            this.huntressTracker = base.GetComponent<HuntressTracker>();
            if (this.huntressTracker && base.isAuthority)
			{
				this.initialTarget = this.huntressTracker.GetTrackingTarget();
			}
            this.modelTransform = base.GetModelTransform();
            if (this.modelTransform)
            {
                this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
                this.hurtboxGroup = this.modelTransform.GetComponent<HurtBoxGroup>();
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount++;
            }
            if (this.hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = this.hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }
            this.startLoc = base.transform.position;
            this.blinkVector = this.GetBlinkVector();
            TossDagger(base.gameObject.transform.position);
            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
        }

        private void TossDagger(Vector3 location)
        {
            if (base.isAuthority)
            {
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = Modules.Projectiles.knifePrefab,
                    position = location + Vector3.up * 2f,
                    rotation = Quaternion.identity,
                    owner = base.gameObject,
                    damage = 0,
                    force = 0,
                    crit = false,
                    speedOverride = 0f
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
        }

        public override void OnExit()
        {
            if (!this.outer.destroying)
            {
                Util.PlaySound(this.endSoundString, base.gameObject);
                this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
                this.modelTransform = base.GetModelTransform();
                if (this.modelTransform)
                {
                    TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
                    temporaryOverlayInstance.duration = 0.6f;
                    temporaryOverlayInstance.animateShaderAlpha = true;
                    temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlayInstance.destroyComponentOnEnd = true;
                    temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
                    temporaryOverlayInstance.AddToCharacterModel(this.modelTransform.GetComponent<CharacterModel>());
                    TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
                    temporaryOverlayInstance2.duration = 0.7f;
                    temporaryOverlayInstance2.animateShaderAlpha = true;
                    temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlayInstance2.destroyComponentOnEnd = true;
                    temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
                    temporaryOverlayInstance2.AddToCharacterModel(this.modelTransform.GetComponent<CharacterModel>());
                }
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount--;
            }
            if (this.hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = this.hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }
            if (base.characterMotor)
            {
                base.characterMotor.disableAirControlUntilCollision = false;
            }
            base.OnExit();
        }

        private void ShunpoDamage(HurtBox hurtbox)
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.damage = this.damageStat * this.damageCoefficient;
            damageInfo.attacker = base.gameObject;
            damageInfo.inflictor = base.gameObject;
            damageInfo.force = Vector3.zero;
            damageInfo.crit = base.RollCrit();
            damageInfo.procCoefficient = this.procCoefficient;
            damageInfo.position = hurtbox.gameObject.transform.position;
            damageInfo.damageType = DamageType.Generic;
            hurtbox.healthComponent.TakeDamage(damageInfo);
            GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtbox.healthComponent.gameObject);
            GlobalEventManager.instance.OnHitAll(damageInfo, hurtbox.healthComponent.gameObject);
            Util.PlaySound("KatarinaImpactSFX", base.gameObject);
            damaged = true; 
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (base.characterMotor && base.characterDirection)
            {
                if (initialTarget && base.inputBank.skill1.down)
                {
                    Vector3 lerpLoc = Vector3.Lerp(this.startLoc, this.initialTarget.transform.position + Vector3.up * 2f, this.stopwatch / duration);
                    base.characterMotor.Motor.SetPositionAndRotation(lerpLoc, Quaternion.LookRotation(this.initialTarget.gameObject.transform.forward, this.initialTarget.gameObject.transform.up));
                }
                else
                {
                    base.characterMotor.velocity = Vector3.zero;
                    base.characterMotor.rootMotion += this.blinkVector * (8f * this.speedCoefficient * Time.fixedDeltaTime);
                }
            }
            if (this.stopwatch >= this.duration && initialTarget && base.inputBank.skill1.down)
            {
                if (!damaged) ShunpoDamage(initialTarget);
            }
            if (this.stopwatch >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        protected virtual Vector3 GetBlinkVector()
        {
            return base.inputBank.aimDirection;
        }

        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(this.blinkVector);
            effectData.origin = origin;
            EffectManager.SpawnEffect(BlinkState.blinkPrefab, effectData, false);
        }
    }
}
