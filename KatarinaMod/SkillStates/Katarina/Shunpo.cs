using EntityStates;
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
        private Animator animator;
        private HuntressTracker huntressTracker;
        private HurtBox target;
        private CharacterMotor characterMotor;
        private float duration = 0.5f;
        public float stopwatch;
        private bool successfulTeleport;
        private bool hasTriedToTeleport;

        public override void OnEnter()
        {
            base.OnEnter();
            this.characterMotor = base.GetComponent<CharacterMotor>();
            this.modelTransform = base.GetModelTransform();
            this.animator = base.GetModelAnimator();
            this.huntressTracker = base.GetComponent<HuntressTracker>();
            if (this.huntressTracker && base.isAuthority)
            {
                this.target = this.huntressTracker.GetTrackingTarget();
            }
        }

        private void Teleport()
        {
            hasTriedToTeleport = true;
            HurtBox hurtBox = this.target;
            if (hurtBox)
            {
                if (this.characterMotor)
                {
                    base.PlayAnimation("FullBody, Override", "Shunpo");
                    this.characterMotor.velocity = Vector3.zero;
                    this.characterMotor.rootMotion = Vector3.zero;
                    this.characterMotor.Motor.SetPosition(GetBehindPosition(), true);
                    var lookPos = target.transform.position - base.transform.position;
                    lookPos.y = 0;
                    var rotation = Quaternion.LookRotation(lookPos);
                    this.characterMotor.Motor.SetRotation(rotation);
                    successfulTeleport = true;
                }
            }
        }

        private Vector3 GetBehindPosition()
        {
            return this.target.transform.TransformPoint(new Vector3(0, 3, -3));
        }

        public override void OnExit()
        {
            base.OnExit();
            base.PlayAnimation("FullBody, Override", "BufferEmpty");
            if (!this.hasTriedToTeleport)
            {
                Teleport();
            }
            if (!successfulTeleport && NetworkServer.active)
            {
                base.skillLocator.utility.AddOneStock();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (!hasTriedToTeleport)
            {
                Teleport();
            }
            if (successfulTeleport && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
            if (this.stopwatch >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }
    }
}
