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
            Transform transform = knifePrefab.GetComponent<Transform>();
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(2, 2, 2);

            GameObject.Destroy(knifePrefab.GetComponent<ProjectileImpactExplosion>());

            knifePrefab.AddComponent<CapsuleCollider>();
            CapsuleCollider capsuleCollider = knifePrefab.GetComponent<CapsuleCollider>();
            capsuleCollider.radius = 0.1584539f;
            capsuleCollider.height = 1.1f;
            capsuleCollider.center = Vector3.zero;
            capsuleCollider.isTrigger = false;

            Rigidbody rigidBody = knifePrefab.GetComponent<Rigidbody>();
            rigidBody.mass = 10;
            rigidBody.drag = 0;
            rigidBody.angularDrag = 0.05f;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidBody.useGravity = true;
            rigidBody.constraints = RigidbodyConstraints.None;

            knifePrefab.layer = LayerIndex.defaultLayer.intVal;

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