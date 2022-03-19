using RoR2.Audio;
using System;
using UnityEngine;

namespace RoR2.Orbs
{
	public class KnifeDamageOrb : GenericDamageOrb
	{
		public override void Begin()
		{
			this.speed = 120f;
			base.duration = base.distanceToTarget / this.speed;
			if (this.GetOrbEffect())
			{
				EffectData effectData = new EffectData
				{
					scale = this.scale,
					origin = this.origin,
					genericFloat = base.duration,
					networkSoundEventIndex = NetworkSoundEventIndex.Invalid,
				};
				effectData.SetHurtBoxReference(this.target);
				EffectManager.SpawnEffect(this.GetOrbEffect(), effectData, true);
			}
		}

		public override GameObject GetOrbEffect()
		{
			return LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/ArrowOrbEffect");
		}


	}
}
