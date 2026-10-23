using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace KatarinaMod.Modules
{
    // Bouncing Blades: shows the owner where the dagger is going to land while it is still in the air.
    // Spawned by the server as a networked effect at the landing point; only Katarina's own player sees it,
    // matching the landed dagger and its ground circle.
    internal class DaggerLandingPreview : MonoBehaviour
    {
        internal static GameObject prefab;

        // Keep the preview a moment past the landing so it overlaps with the landed-dagger circle (no flicker).
        private const float Overlap = 0.25f;

        private static readonly List<DaggerLandingPreview> active = new List<DaggerLandingPreview>();
        private const float MatchDistance = 1f;

        private EffectComponent effectComponent;
        private Vector3 landingPoint;
        private GameObject indicator;
        private bool started;
        private float age;
        private float duration;

        internal static void CreatePrefab()
        {
            GameObject template = new GameObject("KatarinaDaggerLandingPreview");
            prefab = PrefabAPI.InstantiateClone(template, "KatarinaDaggerLandingPreview", false);
            Object.Destroy(template);

            prefab.AddComponent<DaggerLandingPreview>();

            EffectComponent effect = prefab.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.soundName = "";

            VFXAttributes attributes = prefab.AddComponent<VFXAttributes>();
            attributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            attributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;

            prefab.AddComponent<DestroyOnTimer>().duration = 5f;

            Assets.AddNewEffectDef(prefab);
        }

        // Server side.
        internal static void Spawn(Vector3 landingPoint, float radius, float flightTime, GameObject owner)
        {
            if (!prefab || !owner) return;
            EffectData data = new EffectData
            {
                origin = landingPoint,
                scale = radius,
                genericFloat = flightTime,
                rootObject = owner
            };
            EffectManager.SpawnEffect(prefab, data, true);
        }

        private void Awake()
        {
            this.effectComponent = base.GetComponent<EffectComponent>();
        }

        private void OnEnable()
        {
            this.started = false;
            this.age = 0f;
        }

        private void OnDisable()
        {
            this.Cleanup();
        }

        private void OnDestroy()
        {
            this.Cleanup();
        }

        private void Cleanup()
        {
            if (this.indicator) Object.Destroy(this.indicator);
            this.indicator = null;
            active.Remove(this);
        }

        // Server side: the dagger was picked up before it landed, so remove its preview for the owner.
        internal static void CancelForOwner(GameObject owner, Vector3 landingPoint)
        {
            if (!NetworkServer.active || !owner) return;
            NetworkIdentity identity = owner.GetComponent<NetworkIdentity>();
            if (!identity) return;

            if (Util.HasEffectiveAuthority(identity))
            {
                Cancel(landingPoint);
            }
            else if (identity.clientAuthorityOwner != null)
            {
                new CancelMessage(landingPoint).Send(identity.clientAuthorityOwner);
            }
        }

        private static void Cancel(Vector3 landingPoint)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                DaggerLandingPreview preview = active[i];
                if (!preview)
                {
                    active.RemoveAt(i);
                    continue;
                }
                if ((preview.landingPoint - landingPoint).sqrMagnitude <= MatchDistance * MatchDistance) preview.Cleanup();
            }
        }

        internal class CancelMessage : INetMessage
        {
            private Vector3 landingPoint;

            public CancelMessage() { }

            public CancelMessage(Vector3 landingPoint)
            {
                this.landingPoint = landingPoint;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(this.landingPoint);
            }

            public void Deserialize(NetworkReader reader)
            {
                this.landingPoint = reader.ReadVector3();
            }

            public void OnReceived()
            {
                Cancel(this.landingPoint);
            }
        }

        private void Update()
        {
            if (!this.started)
            {
                this.started = true;
                this.Begin();
            }

            this.age += Time.deltaTime;
            if (this.indicator && this.age >= this.duration + Overlap) this.Cleanup();
        }

        private void Begin()
        {
            this.Cleanup();
            EffectData data = this.effectComponent ? this.effectComponent.effectData : null;
            if (data == null || !data.rootObject) return;

            NetworkIdentity identity = data.rootObject.GetComponent<NetworkIdentity>();
            if (!identity || !Util.HasEffectiveAuthority(identity)) return;

            this.duration = data.genericFloat > 0.05f ? data.genericFloat : 1f;
            float radius = data.scale > 0.1f ? data.scale : 3f;
            this.landingPoint = data.origin;
            this.indicator = DaggerGroundIndicator.CreatePreview(data.origin, radius, this.duration);
            if (this.indicator) active.Add(this);
        }
    }
}
