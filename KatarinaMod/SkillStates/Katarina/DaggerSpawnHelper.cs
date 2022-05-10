using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KatarinaMod
{
    public static class DaggerHelper
    {
        public static void SpawnDagger(this CharacterBody body, Vector3 location)
        {
			FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
			{
				projectilePrefab = Modules.Projectiles.knifePrefab,
				position = location + Vector3.up * 2f,
				rotation = Quaternion.identity,
				owner = body.gameObject,
				damage = 0,
				force = 0,
				crit = false,
				speedOverride = 0f
			};
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
    }
}
