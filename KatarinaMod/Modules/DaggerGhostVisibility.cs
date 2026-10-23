using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace KatarinaMod.Modules
{
    internal class DaggerGhostVisibility : MonoBehaviour
    {
        private static readonly Color markerTint = new Color(0.9f, 0.15f, 0.2f, 1f);
        private static GameObject indicatorPrefab;

        private ProjectileGhostController ghostController;
        private Renderer[] renderers;
        private GameObject indicatorInstance;
        private GameObject groundIndicatorInstance;
        private GameObject lootBeamInstance;
        private ParticleSystem[] particleSystems;
        private Transform projectileTransform;
        private bool resolved;

        private const float minAirTime = 0.5f;
        private const float settleTime = 0.1f;
        private const float settleDistance = 0.02f;
        private bool waitingForLanding;
        private float aliveTime;
        private float stillTime;
        private Vector3 lastPosition;

        private void Awake()
        {
            this.ghostController = base.GetComponent<ProjectileGhostController>();
            this.renderers = base.GetComponentsInChildren<Renderer>(true);
            this.particleSystems = base.GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnEnable()
        {
            this.resolved = false;
            this.waitingForLanding = false;
            this.aliveTime = 0f;
            this.stillTime = 0f;
            this.lastPosition = base.transform.position;
            this.SetVisible(false);
            this.SetParticles(true);
        }

        private void OnDisable()
        {
            if (this.indicatorInstance) Object.Destroy(this.indicatorInstance);
            this.indicatorInstance = null;
            if (this.groundIndicatorInstance) Object.Destroy(this.groundIndicatorInstance);
            this.groundIndicatorInstance = null;
            if (this.lootBeamInstance) Object.Destroy(this.lootBeamInstance);
            this.lootBeamInstance = null;
            this.projectileTransform = null;
        }

        private void LateUpdate()
        {
            if (this.waitingForLanding) this.CheckLanded();
            if (this.resolved || !this.ghostController) return;

            Transform projectile = this.ghostController.authorityTransform ? this.ghostController.authorityTransform : this.ghostController.predictionTransform;
            if (!projectile) return;

            ProjectileController projectileController = projectile.GetComponent<ProjectileController>();
            if (!projectileController || !projectileController.owner) return;

            this.resolved = true;
            this.projectileTransform = projectile;
            bool mine = Util.HasEffectiveAuthority(projectileController.owner.GetComponent<NetworkIdentity>());
            this.SetVisible(mine);
            this.waitingForLanding = mine;
        }

        private void CheckLanded()
        {
            this.aliveTime += Time.deltaTime;
            Vector3 position = base.transform.position;
            bool still = (position - this.lastPosition).sqrMagnitude < settleDistance * settleDistance;
            this.lastPosition = position;

            if (this.aliveTime < minAirTime || !still)
            {
                this.stillTime = 0f;
                return;
            }

            this.stillTime += Time.deltaTime;
            if (this.stillTime >= settleTime)
            {
                this.waitingForLanding = false;
                this.CreateIndicator();
                this.groundIndicatorInstance = DaggerGroundIndicator.Create(base.transform.position, this.GetPickupRadius());
                this.SetParticles(false);
                this.lootBeamInstance = LeagueVfx.DaggerLootBeam.Create(base.transform.position);
            }
        }

        private float GetPickupRadius()
        {
            SphereCollider sphere = this.projectileTransform ? this.projectileTransform.GetComponent<SphereCollider>() : null;
            if (!sphere) return 3f;
            Vector3 scale = sphere.transform.lossyScale;
            return sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private void SetParticles(bool on)
        {
            if (this.particleSystems == null) return;
            foreach (ParticleSystem system in this.particleSystems)
            {
                if (!system) continue;
                if (on)
                {
                    system.Clear(false);
                    system.Play(false);
                }
                else
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    Renderer renderer = system.GetComponent<Renderer>();
                    if (renderer) renderer.enabled = false;
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (this.renderers == null) return;
            foreach (Renderer r in this.renderers)
            {
                if (r) r.enabled = visible;
            }
        }

        private void CreateIndicator()
        {
            if (!indicatorPrefab) indicatorPrefab = BuildIndicatorPrefab();
            if (!indicatorPrefab) return;

            this.indicatorInstance = Object.Instantiate(indicatorPrefab, base.transform.position, Quaternion.identity);
            PositionIndicator positionIndicator = this.indicatorInstance.GetComponent<PositionIndicator>();
            if (positionIndicator) positionIndicator.targetTransform = base.transform;
        }

        private static GameObject BuildIndicatorPrefab()
        {
            GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/PositionIndicators/PoiPositionIndicator");
            if (!original) return null;

            GameObject prefab = PrefabAPI.InstantiateClone(original, "KatarinaDaggerIndicator", false);
            PositionIndicator positionIndicator = prefab.GetComponent<PositionIndicator>();
            Sprite icon = Assets.mainAssetBundle ? Assets.mainAssetBundle.LoadAsset<Sprite>("KatarinaQ") : null;

            if (positionIndicator && positionIndicator.insideViewObject && icon)
            {
                foreach (SpriteRenderer sr in positionIndicator.insideViewObject.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite) sr.transform.localScale *= sr.sprite.bounds.size.x / icon.bounds.size.x;
                    sr.sprite = icon;
                    sr.color = Color.white;
                }
                foreach (Image image in positionIndicator.insideViewObject.GetComponentsInChildren<Image>(true))
                {
                    image.sprite = icon;
                    image.color = Color.white;
                    image.preserveAspect = true;
                }
            }

            if (positionIndicator)
            {
                Tint(positionIndicator.outsideViewObject);
                Tint(positionIndicator.alwaysVisibleObject);
            }

            return prefab;
        }

        private static void Tint(GameObject root)
        {
            if (!root) return;
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true)) sr.color = markerTint;
            foreach (Image image in root.GetComponentsInChildren<Image>(true)) image.color = markerTint;
        }
    }
}
