using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Modules
{
    internal static class Projectiles
    {
        internal static GameObject bombPrefab;

        internal static GameObject knifePrefab;

        internal static void AddProjectile(GameObject projectileToAdd)
        {
            Modules.Prefabs.projectilePrefabs.Add(projectileToAdd);
        }

        internal static void RegisterProjectiles()
        {
            // only separating into separate methods for my sanity
            CreateBomb();
            CreateKnife();
            AddProjectile(bombPrefab);
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
            knifePrefab.AddComponent<NetworkIdentity>();
            if (Modules.Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaWeapon") != null) bombController.ghostPrefab = CreateGhostPrefab("KatarinaWeapon");
        }
        private static void CreateBomb()
        {
            bombPrefab = CloneProjectilePrefab("CommandoGrenadeProjectile", "KatarinaBombProjectile");

            ProjectileImpactExplosion bombImpactExplosion = bombPrefab.GetComponent<ProjectileImpactExplosion>();
            InitializeImpactExplosion(bombImpactExplosion);

            bombImpactExplosion.blastRadius = 16f;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = Modules.Assets.bombExplosionEffect;
            //bombImpactExplosion.lifetimeExpiredSound = Modules.Assets.CreateNetworkSoundEventDef("KatarinaBombExplosion");
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileController bombController = bombPrefab.GetComponent<ProjectileController>();
            if (Modules.Assets.mainAssetBundle.LoadAsset<GameObject>("KatarinaBombGhost") != null) bombController.ghostPrefab = CreateGhostPrefab("KatarinaBombGhost");
            bombController.startSound = "";
        }

        private static GameObject CreateGhostPrefab(string ghostName)
        {
            GameObject ghostPrefab = Modules.Assets.mainAssetBundle.LoadAsset<GameObject>(ghostName);
            if (!ghostPrefab.GetComponent<NetworkIdentity>()) ghostPrefab.AddComponent<NetworkIdentity>();
            if (!ghostPrefab.GetComponent<ProjectileGhostController>()) ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Assets.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        private static void InitializeImpactExplosion(ProjectileImpactExplosion projectileImpactExplosion)
        {
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            projectileImpactExplosion.blastRadius = 1f;
            projectileImpactExplosion.bonusBlastForce = Vector3.zero;
            projectileImpactExplosion.childrenCount = 0;
            projectileImpactExplosion.childrenDamageCoefficient = 0f;
            projectileImpactExplosion.childrenProjectilePrefab = null;
            projectileImpactExplosion.destroyOnEnemy = false;
            projectileImpactExplosion.destroyOnWorld = false;
            projectileImpactExplosion.explosionSoundString = "";
            projectileImpactExplosion.falloffModel = RoR2.BlastAttack.FalloffModel.None;
            projectileImpactExplosion.fireChildren = false;
            projectileImpactExplosion.impactEffect = null;
            projectileImpactExplosion.lifetime = 0f;
            projectileImpactExplosion.lifetimeAfterImpact = 0f;
            projectileImpactExplosion.lifetimeExpiredSoundString = "";
            projectileImpactExplosion.lifetimeRandomOffset = 0f;
            projectileImpactExplosion.offsetForLifetimeExpiredSound = 0f;
            projectileImpactExplosion.timerAfterImpact = false;

            projectileImpactExplosion.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
        }
    }
}