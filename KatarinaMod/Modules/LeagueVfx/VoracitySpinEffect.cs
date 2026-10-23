using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueVfx
{
    internal class VoracitySpinEffect : MonoBehaviour
    {
        internal static GameObject prefab;

        private const float LeagueDaggerRadius = 340f;
        private const float ChestOffset = 0f;
        private const float DefaultRadius = 15f;

        private const float SpinHalfSize = 2.8f;
        private const float SpinBrightness = 2.4f;
        private const float GroundDelay = 0.05f;
        private const float SpinBlackGamma = 0.45f;
        private const float SpinBlackAlpha = 3f;
        private const float RingBrightness = 1.8f;
        private const float RingErosionSpeed = 1.8f;
        private const float DurationScale = 2f;

        private const float RingOuter = 30.553f * 11.5f;
        private const float RingInner = 25.225f * 11.5f;
        private const int RingSegments = 96;

        private const int DiscRings = 12;
        private const int DiscSegments = 40;

        private const int ErosionFrames = 10;
        private const float ErosionSoftness = 0.3f;

        private static bool assetsBuilt;
        private static bool assetsFailed;
        private static Mesh quadMesh;
        private static Material spinMaterial;
        private static Material spinBlackMaterial;
        private static Material ringFlashMaterial;
        private static Material[] ringRedMaterials;
        private static Material[] ringGlowMaterials;

        private class Element
        {
            internal GameObject gameObject;
            internal MeshRenderer renderer;
            internal MaterialPropertyBlock block = new MaterialPropertyBlock();
            internal Material[] frames;
            internal float start;
            internal float life;
            internal float[] colorTimes;
            internal Color[] colors;
            internal float alpha = 1f;
            internal float brightness = 1f;
            internal float frameSpeed = 1f;

            internal Mesh discMesh;
            internal Vector3[] discLocal;
            internal Vector2[] discUV;
            internal float halfSize;
            internal float[] scaleTimes;
            internal float[] scales;

            internal float spinSpeed;
            internal float angle;

            internal bool followsBody;
            internal Quaternion tilt = Quaternion.identity;
        }

        private readonly List<Element> elements = new List<Element>();
        private readonly List<UnityEngine.Object> spawned = new List<UnityEngine.Object>();
        private EffectComponent effectComponent;
        private CharacterBody body;
        private bool started;
        private float age;

        internal static void CreatePrefab()
        {
            GameObject template = new GameObject("KatarinaVoracitySpinEffect");
            prefab = PrefabAPI.InstantiateClone(template, "KatarinaVoracitySpinEffect", false);
            UnityEngine.Object.Destroy(template);

            prefab.AddComponent<VoracitySpinEffect>();

            EffectComponent effect = prefab.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.soundName = "";

            VFXAttributes attributes = prefab.AddComponent<VFXAttributes>();
            attributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            attributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;

            prefab.AddComponent<DestroyOnTimer>().duration = 1f * DurationScale + 0.2f;

            Assets.AddNewEffectDef(prefab);
        }

        internal static void Spawn(CharacterBody body, float radius)
        {
            if (!prefab || !body) return;

            EffectData data = new EffectData
            {
                origin = body.footPosition,
                rotation = Quaternion.identity,
                scale = radius
            };
            data.SetNetworkedObjectReference(body.gameObject);
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

            if (this.body) base.transform.position = ChestPoint(this.body);

            float deltaTime = Time.deltaTime;
            this.age += deltaTime;
            for (int i = 0; i < this.elements.Count; i++) this.Tick(this.elements[i], deltaTime);
        }

        private void Begin()
        {
            this.Cleanup();

            float radius = DefaultRadius;
            EffectData data = this.effectComponent ? this.effectComponent.effectData : null;
            if (data != null)
            {
                if (data.scale > 0f) radius = data.scale;
                GameObject root = data.ResolveNetworkedObjectReference();
                this.body = root ? root.GetComponent<CharacterBody>() : null;
            }

            float radiusScale = radius / LeagueDaggerRadius;
            Vector3 origin = base.transform.position;

            Vector3 center = this.body ? ChestPoint(this.body) : origin + Vector3.up * 1.2f;
            base.transform.position = center;

            float bladesHalf = 370f * radiusScale;
            Vector3[] disc = BuildDisc(bladesHalf, out int[] discTriangles);

            Element spinBlack = this.AddDisc("Spin_Black", spinBlackMaterial, center, disc, discTriangles, bladesHalf);
            spinBlack.start = GroundDelay;
            spinBlack.life = UnityEngine.Random.Range(0.24f, 0.3f);
            spinBlack.alpha = SpinBlackAlpha;
            spinBlack.colorTimes = new float[] { 0f, 0.2f, 0.5f, 1f };
            spinBlack.colors = new Color[]
            {
                new Color(0.764706f, 0.019608f, 0.019608f, 0f),
                new Color(0.580392f, 0.101961f, 0.101961f, 1f),
                new Color(0.333333f, 0.011765f, 0.011765f, 1f),
                new Color(0.031373f, 0f, 0f, 0f)
            };
            spinBlack.spinSpeed = 660f;
            spinBlack.angle = UnityEngine.Random.Range(0f, 360f);

            Mesh ring = this.BuildRing();

            Element flash = this.AddMesh("Knife_Edge_OG", ringFlashMaterial, center, ring);
            flash.start = GroundDelay;
            flash.life = UnityEngine.Random.Range(0.06f, 0.1f);
            flash.colorTimes = new float[] { 0f, 0.7f, 1f };
            flash.colors = new Color[]
            {
                new Color(1f, 0.937255f, 0.917647f, 1f),
                new Color(1f, 0.25098f, 0.117647f, 1f),
                new Color(0.584314f, 0.019608f, 0.019608f, 0f)
            };

            Element ringRed = this.AddMesh("Knife_Edge_Fade_Red", ringRedMaterials[0], center, ring);
            ringRed.frames = ringRedMaterials;
            ringRed.frameSpeed = RingErosionSpeed;
            ringRed.start = GroundDelay + 0.01f;
            ringRed.life = 0.6f;
            ringRed.colorTimes = new float[] { 0f, 1f };
            ringRed.colors = new Color[] { new Color(0.819608f, 0f, 0f, 1f), new Color(0.101961f, 0.007843f, 0.003922f, 1f) };

            Element ringGlow = this.AddMesh("Knife_Edge_Fade", ringGlowMaterials[0], center, ring);
            ringGlow.frames = ringGlowMaterials;
            ringGlow.frameSpeed = RingErosionSpeed;
            ringGlow.brightness = RingBrightness;
            ringGlow.start = GroundDelay + 0.01f;
            ringGlow.life = 0.6f;
            ringGlow.colorTimes = new float[] { 0f, 0.1f, 0.4f };
            ringGlow.colors = new Color[]
            {
                new Color(1f, 0.823529f, 0.733333f, 1f),
                new Color(0.639216f, 0f, 0f, 1f),
                new Color(0.666667f, 0.027451f, 0.356863f, 1f)
            };

            Element spin = this.AddMesh("Spin", spinMaterial, center, quadMesh);
            spin.life = UnityEngine.Random.Range(0.24f, 0.3f);
            spin.colorTimes = new float[] { 0f, 0.5f, 1f };
            spin.colors = new Color[]
            {
                new Color(0.619608f, 1f, 0.92549f, 0f),
                new Color(0.960784f, 1f, 0.905882f, 1f),
                new Color(1f, 0f, 0f, 0f)
            };
            spin.brightness = SpinBrightness;
            spin.spinSpeed = 1100f;
            spin.angle = UnityEngine.Random.Range(0f, 360f);
            spin.followsBody = true;
            spin.gameObject.transform.localScale = Vector3.one * (2f * SpinHalfSize);
            this.PlaceOnBody(spin);

            for (int i = 0; i < this.elements.Count; i++)
            {
                this.elements[i].start *= DurationScale;
                this.elements[i].life *= DurationScale;
            }

            for (int i = 0; i < this.elements.Count; i++) this.Tick(this.elements[i], 0f);
        }

        private void Tick(Element element, float deltaTime)
        {
            if (!element.gameObject) return;

            float t = (this.age - element.start) / element.life;
            bool visible = t >= 0f && t < 1f;
            if (element.renderer.enabled != visible) element.renderer.enabled = visible;
            if (!visible) return;

            element.angle += element.spinSpeed * deltaTime;

            if (element.frames != null)
            {
                float erosion = t * element.frameSpeed;
                if (erosion >= 1f)
                {
                    element.renderer.enabled = false;
                    return;
                }
                int frame = Mathf.Clamp((int)(erosion * element.frames.Length), 0, element.frames.Length - 1);
                element.renderer.sharedMaterial = element.frames[frame];
            }

            Color color = LeagueVfxAssets.Evaluate(t, element.colorTimes, element.colors);
            color.r *= element.brightness;
            color.g *= element.brightness;
            color.b *= element.brightness;
            color.a = Mathf.Clamp01(color.a * element.alpha);
            LeagueVfxAssets.SetColor(element.renderer, element.block, color);

            if (element.discMesh)
            {
                float scale = element.scales != null ? LeagueVfxAssets.Evaluate(t, element.scaleTimes, element.scales) : 1f;
                UpdateDiscUV(element, element.halfSize * scale);
            }

            if (element.followsBody) this.PlaceOnBody(element);
        }

        private void PlaceOnBody(Element element)
        {
            element.gameObject.transform.position = base.transform.position + Vector3.up * 0.01f;
            element.gameObject.transform.rotation = element.tilt * Quaternion.Euler(0f, element.angle, 0f);
        }

        private static Vector3 ChestPoint(CharacterBody body)
        {
            return body.corePosition + Vector3.up * ChestOffset;
        }

        private Element AddMesh(string name, Material material, Vector3 position, Mesh mesh)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(base.transform, false);
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.enabled = false;
            this.spawned.Add(obj);

            Element element = new Element { gameObject = obj, renderer = renderer, life = 1f };
            this.elements.Add(element);
            return element;
        }

        private Element AddDisc(string name, Material material, Vector3 center, Vector3[] local, int[] triangles, float halfSize)
        {
            Mesh mesh = new Mesh { name = "KatarinaVfx" + name };
            mesh.MarkDynamic();
            mesh.vertices = local;
            Color32[] colors = new Color32[local.Length];
            Vector3[] normals = new Vector3[local.Length];
            for (int i = 0; i < local.Length; i++)
            {
                colors[i] = new Color32(255, 255, 255, 255);
                normals[i] = Vector3.up;
            }
            mesh.colors32 = colors;
            mesh.normals = normals;
            mesh.uv = new Vector2[local.Length];
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            this.spawned.Add(mesh);

            Element element = this.AddMesh(name, material, center, mesh);
            element.discMesh = mesh;
            element.discLocal = local;
            element.discUV = new Vector2[local.Length];
            element.halfSize = halfSize;
            return element;
        }

        private static void UpdateDiscUV(Element element, float halfSize)
        {
            Vector2[] uv = element.discUV;
            Vector3[] local = element.discLocal;
            if (halfSize < 0.001f)
            {
                for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(-1f, -1f);
            }
            else
            {
                float radians = element.angle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radians);
                float sin = Mathf.Sin(radians);
                float inverse = 0.5f / halfSize;
                for (int i = 0; i < uv.Length; i++)
                {
                    float x = local[i].x;
                    float z = local[i].z;
                    uv[i] = new Vector2(0.5f + (cos * x - sin * z) * inverse, 0.5f + (sin * x + cos * z) * inverse);
                }
            }
            element.discMesh.uv = uv;
        }

        private static Vector3[] BuildDisc(float radius, out int[] triangles)
        {
            Vector3[] local = new Vector3[1 + DiscRings * DiscSegments];
            local[0] = Vector3.zero;
            for (int r = 0; r < DiscRings; r++)
            {
                float distance = radius * (r + 1) / DiscRings;
                for (int s = 0; s < DiscSegments; s++)
                {
                    float angle = s * Mathf.PI * 2f / DiscSegments;
                    local[1 + r * DiscSegments + s] = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                }
            }

            List<int> tris = new List<int>(DiscSegments * 3 + (DiscRings - 1) * DiscSegments * 6);
            for (int s = 0; s < DiscSegments; s++)
            {
                tris.Add(0);
                tris.Add(1 + (s + 1) % DiscSegments);
                tris.Add(1 + s);
            }
            for (int r = 0; r < DiscRings - 1; r++)
            {
                int inner = 1 + r * DiscSegments;
                int outer = inner + DiscSegments;
                for (int s = 0; s < DiscSegments; s++)
                {
                    int next = (s + 1) % DiscSegments;
                    tris.Add(inner + s); tris.Add(inner + next); tris.Add(outer + s);
                    tris.Add(inner + next); tris.Add(outer + next); tris.Add(outer + s);
                }
            }
            triangles = tris.ToArray();
            return local;
        }

        private Mesh BuildRing()
        {
            EffectData data = this.effectComponent ? this.effectComponent.effectData : null;
            float groundScale = ((data != null && data.scale > 0f) ? data.scale : DefaultRadius) / LeagueDaggerRadius;

            const int rows = 4;
            int columns = RingSegments + 1;
            Vector3[] vertices = new Vector3[rows * columns];
            Vector2[] uv = new Vector2[vertices.Length];
            Color32[] colors = new Color32[vertices.Length];
            Vector3[] normals = new Vector3[vertices.Length];
            for (int row = 0; row < rows; row++)
            {
                float v = row / (float)(rows - 1);
                float distance = Mathf.Lerp(RingOuter, RingInner, v) * groundScale;
                for (int c = 0; c < columns; c++)
                {
                    float u = c / (float)RingSegments;
                    float angle = -u * Mathf.PI * 2f;
                    int i = row * columns + c;
                    vertices[i] = new Vector3(Mathf.Cos(angle) * distance, 0.02f, Mathf.Sin(angle) * distance);
                    uv[i] = new Vector2(u, v);
                    colors[i] = new Color32(255, 255, 255, 255);
                    normals[i] = Vector3.up;
                }
            }

            int[] triangles = new int[(rows - 1) * RingSegments * 6];
            int t = 0;
            for (int row = 0; row < rows - 1; row++)
            {
                for (int c = 0; c < RingSegments; c++)
                {
                    int a = row * columns + c;
                    int b = a + columns;
                    triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                    triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
                }
            }

            Mesh mesh = new Mesh { name = "KatarinaVfxPickupRing" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            this.spawned.Add(mesh);
            return mesh;
        }

        private void Cleanup()
        {
            for (int i = 0; i < this.spawned.Count; i++)
            {
                if (this.spawned[i]) UnityEngine.Object.Destroy(this.spawned[i]);
            }
            this.spawned.Clear();
            this.elements.Clear();
        }

        private static bool EnsureAssets()
        {
            if (assetsBuilt) return !assetsFailed;
            assetsBuilt = true;

            try
            {
                Texture2D blades = LeagueVfxAssets.LoadShape("katarina_base_r_blurryblades_v2.tex", true);
                Texture2D bladesDark = LeagueVfxAssets.LoadShape("katarina_base_r_blurryblades_v3.tex", true, SpinBlackGamma);
                Color32[] ring = LeagueVfxAssets.LoadPixels("katarina_base_r_ring_pickup.tex", out int ringWidth, out int ringHeight);
                Color32[] erosion = LeagueVfxAssets.LoadPixels("katarina_base_q_bladetrail_mult.tex", out int erosionWidth, out int erosionHeight);
                if (!blades || !bladesDark || ring == null || erosion == null)
                {
                    assetsFailed = true;
                    return false;
                }

                quadMesh = LeagueVfxAssets.CreateQuad();

                spinBlackMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupSpinBlack", bladesDark, false, 1);
                ringFlashMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupRingFlash", BuildRingTexture(ring, ringWidth, ringHeight, null, 0, 0, 0f, "texKatarinaPickupRing"), false, 3);
                spinMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaPickupSpin", blades, true, 5);

                ringRedMaterials = new Material[ErosionFrames];
                ringGlowMaterials = new Material[ErosionFrames];
                for (int k = 0; k < ErosionFrames; k++)
                {
                    float threshold = Mathf.Lerp(-ErosionSoftness, 1f, k / (float)(ErosionFrames - 1));
                    Texture2D frame = BuildRingTexture(ring, ringWidth, ringHeight, erosion, erosionWidth, erosionHeight, threshold, "texKatarinaPickupRingEroded" + k);
                    ringRedMaterials[k] = LeagueVfxAssets.CreateMaterial("matKatarinaPickupRingRed" + k, frame, false, 2);
                    ringGlowMaterials[k] = LeagueVfxAssets.CreateMaterial("matKatarinaPickupRingGlow" + k, frame, true, 4);
                }

                if (!spinMaterial || !spinBlackMaterial || !ringFlashMaterial || !ringRedMaterials[0] || !ringGlowMaterials[0])
                {
                    assetsFailed = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("KatarinaMod: failed to build the dagger pickup VFX\n" + e);
                assetsFailed = true;
            }
            return !assetsFailed;
        }

        private static Texture2D BuildRingTexture(Color32[] ring, int ringWidth, int ringHeight, Color32[] erosion, int erosionWidth, int erosionHeight, float threshold, string name)
        {
            const int width = 256;
            const int height = 32;
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                float strip = LeagueVfxAssets.Alpha(ring, ringWidth, ringHeight, 0.5f, v);
                for (int x = 0; x < width; x++)
                {
                    float visible = 1f;
                    if (erosion != null)
                    {
                        float map = LeagueVfxAssets.Alpha(erosion, erosionWidth, erosionHeight, x / (float)(width - 1), v);
                        visible = Mathf.SmoothStep(0f, 1f, (map - threshold) / ErosionSoftness);
                    }
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(strip * visible) * 255f);
                    pixels[y * width + x] = new Color32(a, a, a, a);
                }
            }
            return LeagueVfxAssets.Build(pixels, width, height, name, TextureWrapMode.Clamp);
        }
    }
}
