using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueVfx
{
    internal class DaggerPickupHitEffect : MonoBehaviour
    {
        internal static GameObject prefab;

        private const float LeagueUnit = 0.012f;
        private const float DurationScale = 1.5f;
        private const float Brightness = 1.5f;
        private const int ErosionFrames = 8;
        private const float ErosionFeather = 0.3f;

        private enum Facing { Billboard, Flat, Ray }

        private class Particle
        {
            internal GameObject gameObject;
            internal MeshRenderer renderer;
            internal MaterialPropertyBlock block = new MaterialPropertyBlock();
            internal Material[] frames;
            internal Facing facing;
            internal float start;
            internal float life;
            internal float[] colorTimes;
            internal Color[] colors;
            internal Color birthColor = Color.white;
            internal float brightness = 1f;

            internal Vector3 position;
            internal Vector3 velocity;
            internal float drag;
            internal Vector3 direction = Vector3.forward;
            internal float roll;

            internal Vector2 size;
            internal float[] scaleTimes;
            internal float[] scaleX;
            internal float[] scaleY;
        }

        private static bool assetsBuilt;
        private static bool assetsFailed;
        private static Mesh quadMesh;
        private static Mesh[] atlasCells;
        private static Material[] slashArcMaterials;
        private static Material starMaterial;
        private static Material glowMaterial;
        private static Material cutMaterial;
        private static Material shardMaterial;
        private static Material[] spikeBlendMaterials;
        private static Material[] spikeAddMaterials;

        private readonly List<Particle> particles = new List<Particle>();
        private readonly List<GameObject> spawned = new List<GameObject>();
        private EffectComponent effectComponent;
        private bool started;
        private float age;

        internal static void CreatePrefab()
        {
            GameObject template = new GameObject("KatarinaDaggerPickupHitEffect");
            prefab = PrefabAPI.InstantiateClone(template, "KatarinaDaggerPickupHitEffect", false);
            UnityEngine.Object.Destroy(template);

            prefab.AddComponent<DaggerPickupHitEffect>();

            EffectComponent effect = prefab.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.soundName = "";

            VFXAttributes attributes = prefab.AddComponent<VFXAttributes>();
            attributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            attributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            prefab.AddComponent<DestroyOnTimer>().duration = 1.3f * DurationScale;

            Assets.AddNewEffectDef(prefab);
        }

        internal static void Spawn(HurtBox target, Vector3 attackerPosition)
        {
            if (!prefab || !target) return;

            CharacterBody body = target.healthComponent ? target.healthComponent.body : null;
            Vector3 position = body ? body.corePosition : target.transform.position;

            Vector3 away = position - attackerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                Vector2 random = UnityEngine.Random.insideUnitCircle;
                away = new Vector3(random.x, 0f, random.y);
            }
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;

            float size = body ? Mathf.Clamp(body.bestFitRadius, 0.8f, 3f) : 1f;

            EffectData data = new EffectData
            {
                origin = position,
                rotation = Quaternion.LookRotation(away.normalized, Vector3.up),
                scale = size
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

        private void Update()
        {
            if (!this.started)
            {
                this.started = true;
                if (!EnsureAssets()) return;
                this.Begin();
            }

            float deltaTime = Time.deltaTime;
            this.age += deltaTime;
            Camera camera = LeagueVfxAssets.BillboardCamera();
            for (int i = 0; i < this.particles.Count; i++) this.Tick(this.particles[i], deltaTime, camera);
        }

        private void Begin()
        {
            this.Cleanup();

            EffectData data = this.effectComponent ? this.effectComponent.effectData : null;
            float size = (data != null && data.scale > 0f) ? data.scale : 1f;
            float unit = LeagueUnit * size;
            Vector3 center = base.transform.position;
            Vector3 away = base.transform.rotation * Vector3.forward;
            away.y = 0f;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward;

            Particle arc = this.Add("Flash_Add", slashArcMaterials, quadMesh, Facing.Flat);
            arc.position = center + Vector3.up * (20f * unit);
            arc.direction = away;
            arc.life = 0.3f;
            arc.size = new Vector2(180f, 180f) * unit;
            arc.scaleTimes = new float[] { 0f, 0.1f, 1f };
            arc.scaleX = new float[] { 0.2f, 1.4f, 0.7f };
            arc.scaleY = arc.scaleX;
            arc.colorTimes = new float[] { 0f, 0.3f, 1f };
            arc.colors = new Color[] { new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.7255f), new Color(1f, 0.0118f, 0.0118f, 0.5882f) };
            arc.birthColor = new Color(1f, 0.55f, 0.5f, 1f);

            Particle star = this.Add("flash1", new Material[] { starMaterial }, quadMesh, Facing.Billboard);
            star.position = center;
            star.roll = UnityEngine.Random.Range(0f, 360f);
            star.life = 0.35f;
            star.size = new Vector2(400f, 400f) * unit;
            star.scaleTimes = new float[] { 0f, 0.3f, 1f };
            star.scaleX = new float[] { 0.3f, 1f, 0f };
            star.scaleY = star.scaleX;
            star.birthColor = new Color(1f, 0.149f, 0.3059f, 0.8235f);
            star.colorTimes = new float[] { 0f, 0.5f, 1f };
            star.colors = new Color[] { new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.4235f), new Color(1f, 1f, 1f, 0f) };

            Particle glow = this.Add("flash3", new Material[] { glowMaterial }, quadMesh, Facing.Billboard);
            glow.position = center;
            glow.roll = UnityEngine.Random.Range(0f, 360f);
            glow.life = 0.35f;
            glow.size = new Vector2(400f, 400f) * unit;
            glow.scaleTimes = new float[] { 0f, 0.2f, 1f };
            glow.scaleX = new float[] { 0.3f, 1f, 0f };
            glow.scaleY = glow.scaleX;
            glow.birthColor = new Color(0.3333f, 0f, 0.498f, 0.3686f);
            glow.colorTimes = new float[] { 0f, 1f };
            glow.colors = new Color[] { new Color(1f, 0.9882f, 0.5451f, 1f), new Color(0.1647f, 0f, 1f, 0f) };

            Particle cut = this.Add("Cuts", new Material[] { cutMaterial }, quadMesh, Facing.Billboard);
            cut.position = center;
            cut.roll = 180f * UnityEngine.Random.Range(-1f, 1f);
            cut.start = 0.03f;
            cut.life = 0.3f;
            cut.size = new Vector2(220f * UnityEngine.Random.Range(0.8f, 1f), 140f * UnityEngine.Random.Range(1f, 2f)) * unit;
            cut.scaleTimes = new float[] { 0f, 0.35f, 0.65f, 1f };
            cut.scaleX = new float[] { 0.5f, 0.75f, 1f, 1.25f };
            cut.scaleY = new float[] { 2f, 1f, 0.3f, 0f };
            cut.birthColor = new Color(1f, 0.8f, 0.45f, 1f);
            cut.colorTimes = new float[] { 0f, 1f };
            cut.colors = new Color[] { new Color(1f, 0.7176f, 0.7176f, 1f), new Color(0.4863f, 0f, 0.0471f, 0f) };

            for (int i = 0; i < 3; i++)
            {
                Particle shard = this.Add("Alpha_Spikes", new Material[] { shardMaterial }, RandomCell(), Facing.Ray);
                float speed = 2500f * UnityEngine.Random.Range(0.67f, 1f) * unit;
                Vector3 direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-50f, 50f), Vector3.up) * away;
                shard.start = i / 30f;
                shard.position = center + Vector3.up * (50f * unit) + away * (20f * unit);
                shard.velocity = direction * speed;
                shard.drag = 4f * UnityEngine.Random.Range(1f, 3f);
                shard.direction = direction;
                shard.life = 0.5f * UnityEngine.Random.Range(0.6f, 1.2f);
                shard.size = new Vector2(160f * UnityEngine.Random.Range(0.7f, 1f), 160f * UnityEngine.Random.Range(0.7f, 1.5f)) * unit;
                shard.scaleTimes = new float[] { 0f, 1f };
                shard.scaleX = new float[] { 0.5f, 0.2f };
                shard.scaleY = shard.scaleX;
                shard.birthColor = new Color(0.4392f, 0.4392f, 0.4392f, 1f);
                shard.colorTimes = new float[] { 0f, 0.1f, 0.8f, 0.9f, 1f };
                shard.colors = new Color[]
                {
                    new Color(0.5804f, 0.0157f, 0.0157f, 0f),
                    new Color(0.302f, 0f, 0f, 1f),
                    new Color(0.2275f, 0.0078f, 0.0078f, 0.6f),
                    new Color(0.2118f, 0.0235f, 0.0275f, 0.3647f),
                    new Color(0.051f, 0f, 0.0314f, 0f)
                };
            }

            for (int i = 0; i < 5; i++)
            {
                bool additive = i >= 3;
                Particle spike = this.Add(additive ? "Spikes_Add" : "Spikes_Blend", additive ? spikeAddMaterials : spikeBlendMaterials, RandomCell(), Facing.Ray);
                spike.start = additive ? (i - 3) * 0.05f : 0.05f + i * 0.05f;
                spike.position = center + Vector3.up * (40f * unit);
                spike.direction = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * Vector3.forward;
                spike.life = 0.3f * UnityEngine.Random.Range(0.5f, 1f);
                spike.size = new Vector2(110f * UnityEngine.Random.Range(0.5f, 1.5f), 180f * UnityEngine.Random.Range(0.5f, 1.5f)) * unit;
                spike.scaleTimes = new float[] { 0f, 0.2f, 1f };
                spike.scaleX = new float[] { 0.5f, 2f, 2f };
                spike.scaleY = new float[] { 0.5f, 1.2f, 1.7f };
                spike.colorTimes = new float[] { 0f, 0.3f, 0.8f, 0.9f, 1f };
                spike.colors = additive
                    ? new Color[]
                    {
                        new Color(0.9765f, 0.3412f, 0.0902f, 1f),
                        new Color(0.9961f, 0.1373f, 0.1373f, 1f),
                        new Color(0.8078f, 0.0471f, 0.0471f, 0.6f),
                        new Color(0.2824f, 0f, 0f, 0.1608f),
                        new Color(0.2549f, 0f, 0f, 0f)
                    }
                    : new Color[]
                    {
                        new Color(0.9765f, 0.3647f, 0.1804f, 1f),
                        new Color(0.9961f, 0.1373f, 0.1373f, 1f),
                        new Color(0.5098f, 0.0275f, 0.0275f, 0.6f),
                        new Color(0.1294f, 0f, 0f, 0.1608f),
                        new Color(0.0235f, 0f, 0f, 0f)
                    };
            }

            for (int i = 0; i < this.particles.Count; i++)
            {
                Particle p = this.particles[i];
                p.start *= DurationScale;
                p.life *= DurationScale;
                p.drag /= DurationScale;
                p.velocity /= DurationScale;
                p.brightness = Brightness;
            }

            Camera camera = LeagueVfxAssets.BillboardCamera();
            for (int i = 0; i < this.particles.Count; i++) this.Tick(this.particles[i], 0f, camera);
        }

        private Particle Add(string name, Material[] frames, Mesh mesh, Facing facing)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(base.transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = frames[0];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.enabled = false;
            this.spawned.Add(obj);

            Particle particle = new Particle { gameObject = obj, renderer = renderer, frames = frames, facing = facing, life = 1f };
            this.particles.Add(particle);
            return particle;
        }

        private void Tick(Particle p, float deltaTime, Camera camera)
        {
            if (!p.gameObject) return;

            float t = (this.age - p.start) / p.life;
            bool visible = t >= 0f && t < 1f;
            if (p.renderer.enabled != visible) p.renderer.enabled = visible;
            if (!visible) return;

            if (p.drag > 0f) p.velocity *= Mathf.Exp(-p.drag * deltaTime);
            p.position += p.velocity * deltaTime;

            if (p.frames.Length > 1)
            {
                int frame = Mathf.Clamp((int)(t * p.frames.Length), 0, p.frames.Length - 1);
                p.renderer.sharedMaterial = p.frames[frame];
            }

            Color color = LeagueVfxAssets.Evaluate(t, p.colorTimes, p.colors) * p.birthColor;
            color.r *= p.brightness;
            color.g *= p.brightness;
            color.b *= p.brightness;
            color.a = Mathf.Clamp01(color.a);
            LeagueVfxAssets.SetColor(p.renderer, p.block, color);

            float sx = LeagueVfxAssets.Evaluate(t, p.scaleTimes, p.scaleX);
            float sy = LeagueVfxAssets.Evaluate(t, p.scaleTimes, p.scaleY);
            Transform tr = p.gameObject.transform;
            tr.position = p.position;
            tr.localScale = new Vector3(Mathf.Max(0.0001f, p.size.x * sx), Mathf.Max(0.0001f, p.size.y * sy), 1f);

            Vector3 toCamera = camera ? (camera.transform.position - p.position) : Vector3.up;
            if (toCamera.sqrMagnitude < 0.0001f) toCamera = Vector3.up;
            toCamera.Normalize();

            switch (p.facing)
            {
                case Facing.Billboard:
                    {
                        Vector3 up = camera ? camera.transform.up : Vector3.forward;
                        tr.rotation = Quaternion.LookRotation(-toCamera, up) * Quaternion.Euler(0f, 0f, p.roll);
                        break;
                    }
                case Facing.Flat:
                    tr.rotation = Quaternion.LookRotation(Vector3.down, p.direction);
                    break;
                case Facing.Ray:
                    {
                        Vector3 axis = p.direction;
                        Vector3 normal = toCamera - axis * Vector3.Dot(toCamera, axis);
                        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.Cross(axis, Vector3.right);
                        tr.rotation = Quaternion.LookRotation(-normal.normalized, axis);
                        break;
                    }
            }
        }

        private static Mesh RandomCell()
        {
            return atlasCells[UnityEngine.Random.Range(0, atlasCells.Length)];
        }

        private void Cleanup()
        {
            for (int i = 0; i < this.spawned.Count; i++)
            {
                if (this.spawned[i]) UnityEngine.Object.Destroy(this.spawned[i]);
            }
            this.spawned.Clear();
            this.particles.Clear();
        }

        private static bool EnsureAssets()
        {
            if (assetsBuilt) return !assetsFailed;
            assetsBuilt = true;

            try
            {
                float[] slash = LeagueVfxAssets.LoadShapeValues("katarina_base_dagger_pickup_slash.tex", false, out int slashW, out int slashH);
                float[] star = LeagueVfxAssets.LoadShapeValues("katarina_base_z_hit_flash.tex", true, out int starW, out int starH);
                float[] glow = LeagueVfxAssets.LoadShapeValues("katarina_base_r_reset_ground.tex", false, out int glowW, out int glowH);
                float[] cut = LeagueVfxAssets.LoadShapeValues("katarina_base_r_slash_01.tex", false, out int cutW, out int cutH);
                float[] impact = LeagueVfxAssets.LoadShapeValues("katarina_base_r_tar_impact.tex", false, out int impactW, out int impactH);
                if (slash == null || star == null || glow == null || cut == null || impact == null)
                {
                    assetsFailed = true;
                    return false;
                }

                quadMesh = LeagueVfxAssets.CreateQuadXY(new Rect(0f, 0f, 1f, 1f));

                atlasCells = new Mesh[4];
                for (int c = 0; c < 4; c++)
                {
                    int column = c % 2;
                    int rowFromTop = c / 2;
                    atlasCells[c] = LeagueVfxAssets.CreateQuadXY(new Rect(column * 0.5f, 0.5f - rowFromTop * 0.5f, 0.5f, 0.5f));
                }

                starMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitStar", LeagueVfxAssets.BuildShape(star, starW, starH, null, 0f, 1f, "texKatarinaPickupHitStar", TextureWrapMode.Clamp), true, 13);
                glowMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitGlow", LeagueVfxAssets.BuildShape(glow, glowW, glowH, null, 0f, 1f, "texKatarinaPickupHitGlow", TextureWrapMode.Clamp), true, 10);
                cutMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitCut", LeagueVfxAssets.BuildShape(cut, cutW, cutH, null, 0f, 1f, "texKatarinaPickupHitCut", TextureWrapMode.Clamp), true, 14);
                shardMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitShard", LeagueVfxAssets.BuildShape(impact, impactW, impactH, null, 0f, 1f, "texKatarinaPickupHitShard", TextureWrapMode.Clamp), false, 12);

                slashArcMaterials = new Material[ErosionFrames];
                spikeBlendMaterials = new Material[ErosionFrames];
                spikeAddMaterials = new Material[ErosionFrames];
                for (int k = 0; k < ErosionFrames; k++)
                {
                    float drive = 1.5f * (k + 0.5f) / ErosionFrames;
                    Texture2D arcFrame = LeagueVfxAssets.BuildShape(slash, slashW, slashH, slash, drive, ErosionFeather, "texKatarinaPickupHitArc" + k, TextureWrapMode.Clamp);
                    Texture2D spikeFrame = LeagueVfxAssets.BuildShape(impact, impactW, impactH, impact, drive, ErosionFeather, "texKatarinaPickupHitSpike" + k, TextureWrapMode.Clamp);
                    slashArcMaterials[k] = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitArc" + k, arcFrame, true, 15);
                    spikeBlendMaterials[k] = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitSpikeBlend" + k, spikeFrame, false, 11);
                    spikeAddMaterials[k] = LeagueVfxAssets.CreateMaterial("matKatarinaPickupHitSpikeAdd" + k, spikeFrame, true, 12);
                }

                if (!starMaterial || !glowMaterial || !cutMaterial || !shardMaterial || !slashArcMaterials[0] || !spikeBlendMaterials[0] || !spikeAddMaterials[0])
                {
                    assetsFailed = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("KatarinaMod: failed to build the dagger pickup hit VFX\n" + e);
                assetsFailed = true;
            }
            return !assetsFailed;
        }
    }
}
