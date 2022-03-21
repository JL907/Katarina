using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Modules
{
    internal static class Projectiles
    {
        internal static GameObject knifePrefab;

        internal static void AddProjectile(GameObject projectileToAdd)
        {
            Modules.Prefabs.projectilePrefabs.Add(projectileToAdd);
        }

        internal static void RegisterProjectiles()
        {
            // only separating into separate methods for my sanity
            CreateKnife();
            AddProjectile(knifePrefab);
        }

        private static GameObject CloneProjectilePrefab(string prefabName, string newPrefabName)
        {
            GameObject newPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/" + prefabName), newPrefabName);
            return newPrefab;
        }
        private static void CreateKnife()
        {
            knifePrefab = CloneProjectilePrefab("CommandoGrenadeProjectile", "KatarinaKnifeProjectile");

            ProjectileImpactExplosion impactExplosion = knifePrefab.GetComponent<ProjectileImpactExplosion>();
            impactExplosion.blastDamageCoefficient = 1f;
            impactExplosion.blastProcCoefficient = 1f;
            impactExplosion.bonusBlastForce = Vector3.zero;
            impactExplosion.childrenCount = 0;
            impactExplosion.childrenDamageCoefficient = 0f;
            impactExplosion.childrenProjectilePrefab = null;
            impactExplosion.destroyOnWorld = false;
            impactExplosion.destroyOnEnemy = false;
            impactExplosion.falloffModel = RoR2.BlastAttack.FalloffModel.None;
            impactExplosion.fireChildren = false;
            impactExplosion.lifetimeRandomOffset = 0f;
            impactExplosion.offsetForLifetimeExpiredSound = 0f;
            impactExplosion.blastRadius = 0f;
            impactExplosion.lifetime = 6f;
            impactExplosion.timerAfterImpact = false;
            impactExplosion.impactEffect = null;
            impactExplosion.explosionSoundString = "";

            ProjectileController bombController = knifePrefab.GetComponent<ProjectileController>();
            knifePrefab.AddComponent<DaggerPickup>();
            knifePrefab.AddComponent<DestroyOnTimer>().duration = 6f;
            if (Modules.Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon") != null) bombController.ghostPrefab = CreateGhostPrefab("KatarinaWeapon");
        }
 

        private static GameObject CreateGhostPrefab(string ghostName)
        {
            GameObject ghostPrefab = Modules.Assets.mainAssetBundle.LoadAsset<GameObject>(ghostName);
            if (!ghostPrefab.GetComponent<NetworkIdentity>()) ghostPrefab.AddComponent<NetworkIdentity>();
            if (!ghostPrefab.GetComponent<ProjectileGhostController>()) ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Assets.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }
    }
}