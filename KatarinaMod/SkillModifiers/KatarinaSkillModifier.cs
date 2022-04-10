using KatarinaMod.SkillStates.Katarina.Weapon;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;
using System;
using System.Collections.Generic;
using System.Text;

namespace KatarinaMod.SkillModifiers
{
    [SkillLevelModifier("LagannDrillRush", typeof(ThrowDagger))]
    class LagannDrillRushSkillModifier : SimpleSkillModifier<ThrowDagger>
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
            var capedLevel = Math.Min(25, level);
            ThrowDagger.damageCoefficient = AdditiveScaling(Modules.Config.bouncingBlades_damageCoefficient.Value, 0.30f, capedLevel);// increase damage by 10% every level (linear)
        }
    }
}
