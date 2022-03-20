using EntityStates;
using RoR2;
using UnityEngine;

namespace KatarinaMod.SkillStates.BaseStates
{
    internal class Death : GenericCharacterDeath
    {
        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound("KatarinaDeath", base.gameObject);
            Animator modelAnimator = base.GetModelAnimator();
            if (modelAnimator)
            {
                modelAnimator.CrossFadeInFixedTime("Death", 0.1f);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}