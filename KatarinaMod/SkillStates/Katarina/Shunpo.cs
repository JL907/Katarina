using EntityStates;
using EntityStates.Huntress;
using KatarinaMod.Modules;
using RoR2;
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
        public float speedCoefficient = 8f;
        public string beginSoundString;
        public string endSoundString;
        private CharacterModel characterModel;
        private HurtBoxGroup hurtboxGroup;
        private GameObject weaponInstance;
        private Vector3 startLoc;
        private bool daggerThrown;

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(this.beginSoundString, base.gameObject);
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
            this.blinkVector = this.GetBlinkVector();
            this.startLoc = Util.GetCorePosition(base.gameObject);
            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
        }

        private void TossDagger()
        {
            if (NetworkServer.active)
            {
                if (!weaponInstance)
                {
                    weaponInstance = UnityEngine.Object.Instantiate<GameObject>(Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon"));
                    weaponInstance.AddComponent<DaggerPickup>().baseObject = weaponInstance;
                    weaponInstance.AddComponent<DestroyOnTimer>().duration = 6f;
                    weaponInstance.AddComponent<NetworkIdentity>();
                }
                Vector3 position = startLoc + Vector3.up * 1.5f;
                Vector3 upVector = Vector3.up * 20;
                weaponInstance.transform.position = position;
                Rigidbody component2 = weaponInstance.GetComponent<Rigidbody>();
                component2.velocity = upVector;
                component2.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);
                daggerThrown = true;
                NetworkServer.Spawn(weaponInstance);
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
                    TemporaryOverlay temporaryOverlay = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay.duration = 0.6f;
                    temporaryOverlay.animateShaderAlpha = true;
                    temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay.destroyComponentOnEnd = true;
                    temporaryOverlay.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
                    temporaryOverlay.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
                    TemporaryOverlay temporaryOverlay2 = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay2.duration = 0.7f;
                    temporaryOverlay2.animateShaderAlpha = true;
                    temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay2.destroyComponentOnEnd = true;
                    temporaryOverlay2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
                    temporaryOverlay2.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
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

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (this.stopwatch >= 0.3f && !this.daggerThrown)
            {
                TossDagger();
            }
            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity = Vector3.zero;
                base.characterMotor.rootMotion += this.blinkVector * (7f * this.speedCoefficient * Time.fixedDeltaTime);
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
