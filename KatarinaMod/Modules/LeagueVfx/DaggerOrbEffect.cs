using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueVfx
{
    internal class DaggerOrbEffect : MonoBehaviour
    {
        internal static GameObject prefab;

        private const float SpinSpeed = 1260f;
        private const float ArcPerMeter = 0.12f;
        private const float MinArc = 0.3f;
        private const float MaxArc = 2.5f;

        private class Template
        {
            internal GameObject root;
            internal Vector3 bladeAxis = Vector3.up;
            internal Vector3 faceNormal = Vector3.forward;
            internal Vector3 center = Vector3.zero;
        }

        private static GameObject holder;
        private static readonly Dictionary<GameObject, Template> templates = new Dictionary<GameObject, Template>();

        private EffectComponent effectComponent;
        private GameObject visualSource;
        private GameObject visual;
        private Template template;
        private Renderer[] visualRenderers;
        private ParticleSystem[] visualParticles;
        private DaggerAfterimages afterimages;

        private bool started;
        private bool arrived;
        private float age;
        private float duration;
        private float spin;
        private Vector3 origin;
        private Vector3 lastTargetPosition;
        private Vector3 lastDirection = Vector3.forward;
        private HurtBox target;

        internal static void CreatePrefab()
        {
            GameObject template = new GameObject("KatarinaDaggerOrbEffect");
            prefab = PrefabAPI.InstantiateClone(template, "KatarinaDaggerOrbEffect", false);
            Object.Destroy(template);

            prefab.AddComponent<DaggerOrbEffect>();

            EffectComponent effect = prefab.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.soundName = "";

            VFXAttributes attributes = prefab.AddComponent<VFXAttributes>();
            attributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            attributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;

            prefab.AddComponent<DestroyOnTimer>().duration = 4f;

            Assets.AddNewEffectDef(prefab);
        }

        internal static void Spawn(Vector3 origin, HurtBox target, float duration, GameObject attacker)
        {
            if (!prefab || !target) return;
            EffectData data = new EffectData
            {
                origin = origin,
                genericFloat = duration,
                rootObject = attacker
            };
            data.SetHurtBoxReference(target);
            EffectManager.SpawnEffect(prefab, data, true);
        }

        private void Awake()
        {
            this.effectComponent = base.GetComponent<EffectComponent>();
        }

        private void OnEnable()
        {
            this.started = false;
            this.arrived = false;
            this.age = 0f;
            this.spin = Random.Range(0f, 360f);
        }

        private void OnDestroy()
        {
            if (this.visual) Object.Destroy(this.visual);
        }

        private void Update()
        {
            if (!this.started)
            {
                this.started = true;
                this.Begin();
            }
            if (this.arrived) return;

            float deltaTime = Time.deltaTime;
            this.age += deltaTime;

            if (this.target) this.lastTargetPosition = this.target.transform.position;

            float t = Mathf.Clamp01(this.age / this.duration);
            float distance = Vector3.Distance(this.origin, this.lastTargetPosition);
            float arc = Mathf.Clamp(distance * ArcPerMeter, MinArc, MaxArc);
            Vector3 position = Vector3.Lerp(this.origin, this.lastTargetPosition, t) + Vector3.up * (4f * arc * t * (1f - t));

            Vector3 step = position - base.transform.position;
            if (step.sqrMagnitude > 0.000001f) this.lastDirection = step.normalized;
            base.transform.position = position;

            this.spin += SpinSpeed * deltaTime;
            this.PoseVisual(position, this.lastDirection);

            if (t >= 1f)
            {
                this.arrived = true;
                this.HideVisual();
            }
        }

        private void Begin()
        {
            EffectData data = this.effectComponent ? this.effectComponent.effectData : null;
            this.origin = data != null ? data.origin : base.transform.position;
            this.duration = data != null && data.genericFloat > 0.01f ? data.genericFloat : 0.01f;
            GameObject targetObject = data != null ? data.ResolveHurtBoxReference() : null;
            this.target = targetObject ? targetObject.GetComponent<HurtBox>() : null;
            this.lastTargetPosition = this.target ? this.target.transform.position : this.origin;

            Vector3 direction = this.lastTargetPosition - this.origin;
            this.lastDirection = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.forward;

            base.transform.position = this.origin;
            base.transform.rotation = Quaternion.identity;

            this.SetupVisual(ResolveGhost(data != null ? data.rootObject : null));
            this.PoseVisual(this.origin, this.lastDirection);
        }

        private void PoseVisual(Vector3 position, Vector3 direction)
        {
            if (!this.visual || this.template == null) return;

            Vector3 side = Vector3.Cross(Vector3.up, direction);
            side = side.sqrMagnitude > 0.0001f ? side.normalized : Vector3.right;

            Transform t = this.visual.transform;
            Quaternion meshToFrame = Quaternion.Inverse(Quaternion.LookRotation(this.template.faceNormal, this.template.bladeAxis));
            Quaternion aligned = Quaternion.LookRotation(side, direction) * meshToFrame;
            t.rotation = Quaternion.AngleAxis(this.spin, side) * aligned;
            t.position = position - t.TransformVector(this.template.center);
        }

        private void SetupVisual(GameObject ghost)
        {
            if (!ghost)
            {
                if (this.visual) this.visual.SetActive(false);
                return;
            }

            if (ghost != this.visualSource || !this.visual)
            {
                if (this.visual) Object.Destroy(this.visual);
                this.template = GetTemplate(ghost);
                if (this.template == null) return;

                this.visualSource = ghost;
                this.visual = Object.Instantiate(this.template.root, base.transform);
                this.visual.name = ghost.name + "Flying";
                this.visualRenderers = this.visual.GetComponentsInChildren<Renderer>(true);
                this.visualParticles = this.visual.GetComponentsInChildren<ParticleSystem>(true);
                if (this.afterimages) Object.Destroy(this.afterimages);
                this.afterimages = base.gameObject.AddComponent<DaggerAfterimages>();
                this.afterimages.SetSource(this.visual.transform);
            }

            this.visual.SetActive(true);
            if (this.afterimages)
            {
                this.afterimages.Clear();
                this.afterimages.emitting = true;
            }
            foreach (Renderer r in this.visualRenderers) if (r) r.enabled = true;
            foreach (ParticleSystem p in this.visualParticles)
            {
                if (!p) continue;
                p.Clear(false);
                p.Play(false);
            }
        }

        private void HideVisual()
        {
            if (this.afterimages) this.afterimages.emitting = false;
            if (!this.visual) return;
            foreach (Renderer r in this.visualRenderers)
            {
                if (r && !(r is ParticleSystemRenderer)) r.enabled = false;
            }
            foreach (ParticleSystem p in this.visualParticles)
            {
                if (p) p.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private static GameObject DefaultGhost()
        {
            ProjectileController controller = Projectiles.knifePrefab ? Projectiles.knifePrefab.GetComponent<ProjectileController>() : null;
            return controller ? controller.ghostPrefab : null;
        }

        private static GameObject ResolveGhost(GameObject attacker)
        {
            GameObject fallback = DefaultGhost();
            if (!attacker) return fallback;

            CharacterBody body = attacker.GetComponent<CharacterBody>();
            ModelLocator locator = body ? body.modelLocator : attacker.GetComponent<ModelLocator>();
            Transform model = locator ? locator.modelTransform : null;
            ModelSkinController skinController = model ? model.GetComponent<ModelSkinController>() : null;
            if (!skinController || skinController.skins == null) return fallback;

            int index = skinController.currentSkinIndex;
            if (index < 0 || index >= skinController.skins.Length) return fallback;

            SkinDef skin = skinController.skins[index];
            if (!skin || skin.projectileGhostReplacements == null) return fallback;

            foreach (SkinDef.ProjectileGhostReplacement replacement in skin.projectileGhostReplacements)
            {
                if (replacement.projectilePrefab == Projectiles.knifePrefab && replacement.projectileGhostReplacementPrefab)
                {
                    return replacement.projectileGhostReplacementPrefab;
                }
            }
            return fallback;
        }

        private static Template GetTemplate(GameObject ghost)
        {
            if (templates.TryGetValue(ghost, out Template cached) && cached != null && cached.root) return cached;

            if (!holder)
            {
                holder = new GameObject("KatarinaDaggerOrbTemplates");
                holder.SetActive(false);
                Object.DontDestroyOnLoad(holder);
            }

            GameObject root = Object.Instantiate(ghost, holder.transform);
            root.name = ghost.name;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            StripComponents(root);

            Template template = new Template { root = root };

            MeshFilter filter = null;
            foreach (MeshFilter f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh) { filter = f; break; }
            }
            if (filter)
            {
                Bounds bounds = filter.sharedMesh.bounds;
                Matrix4x4 toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Vector3 x = toRoot.MultiplyVector(new Vector3(bounds.extents.x, 0f, 0f));
                Vector3 y = toRoot.MultiplyVector(new Vector3(0f, bounds.extents.y, 0f));
                Vector3 z = toRoot.MultiplyVector(new Vector3(0f, 0f, bounds.extents.z));
                Vector3 longest = x;
                if (y.sqrMagnitude > longest.sqrMagnitude) longest = y;
                if (z.sqrMagnitude > longest.sqrMagnitude) longest = z;
                Vector3 thinnest = x;
                if (y.sqrMagnitude < thinnest.sqrMagnitude) thinnest = y;
                if (z.sqrMagnitude < thinnest.sqrMagnitude) thinnest = z;
                if (longest.sqrMagnitude > 0.000001f) template.bladeAxis = longest.normalized;
                if (thinnest.sqrMagnitude > 0.000001f && thinnest != longest) template.faceNormal = thinnest.normalized;
                else template.faceNormal = Vector3.Cross(template.bladeAxis, Vector3.right).sqrMagnitude > 0.01f
                    ? Vector3.Cross(template.bladeAxis, Vector3.right).normalized
                    : Vector3.forward;
                template.center = toRoot.MultiplyPoint3x4(bounds.center);
            }

            templates[ghost] = template;
            return template;
        }

        private static void StripComponents(GameObject root)
        {
            List<Component> behaviours = new List<Component>();
            List<Component> colliders = new List<Component>();
            List<Component> bodies = new List<Component>();
            List<Component> others = new List<Component>();
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (!c || c is Transform || c is Renderer || c is MeshFilter || c is ParticleSystem) continue;
                if (c is Behaviour) behaviours.Add(c);
                else if (c is Collider) colliders.Add(c);
                else if (c is Rigidbody) bodies.Add(c);
                else others.Add(c);
            }
            foreach (Component c in behaviours) if (c) Object.DestroyImmediate(c);
            foreach (Component c in colliders) if (c) Object.DestroyImmediate(c);
            foreach (Component c in bodies) if (c) Object.DestroyImmediate(c);
            foreach (Component c in others) if (c) Object.DestroyImmediate(c);
        }
    }
}
