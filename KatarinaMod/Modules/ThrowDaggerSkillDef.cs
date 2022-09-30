using JetBrains.Annotations;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace KatarinaMod.Skills
{
    public class ThrowDaggerSkillDef : SkillDef
    {
		public override SkillDef.BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
		{
			return new ThrowDaggerSkillDef.InstanceData
			{
				huntressTracker = skillSlot.GetComponent<HuntressTracker>()
			};
		}
		private static bool HasTarget([NotNull] GenericSkill skillSlot)
		{
			HuntressTracker huntressTracker = ((ThrowDaggerSkillDef.InstanceData)skillSlot.skillInstanceData).huntressTracker;
			return (huntressTracker != null) ? huntressTracker.GetTrackingTarget() : null;
		}
		public override bool CanExecute([NotNull] GenericSkill skillSlot)
		{
			return ThrowDaggerSkillDef.HasTarget(skillSlot) && base.CanExecute(skillSlot);
		}
		public override bool IsReady([NotNull] GenericSkill skillSlot)
		{
			return base.IsReady(skillSlot) && ThrowDaggerSkillDef.HasTarget(skillSlot);
		}
		protected class InstanceData : SkillDef.BaseSkillInstanceData
		{
			public HuntressTracker huntressTracker;
		}
	}
}
