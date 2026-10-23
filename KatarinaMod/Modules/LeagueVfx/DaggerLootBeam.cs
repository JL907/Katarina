using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueVfx
{
    internal class DaggerLootBeam : MonoBehaviour
    {
        private static readonly Color coreColor = new Color(1f, 0.42f, 0.42f, 0.9f);
        private static readonly Color outerColor = new Color(0.9f, 0.05f, 0.08f, 0.6f);
        private static readonly Color groundColor = new Color(0.95f, 0.1f, 0.12f, 0.7f);
        private static readonly Color flareColor = new Color(1f, 0.6f, 0.58f, 0.9f);
        private static readonly Color sparkColor = new Color(1f, 0.3f, 0.25f, 1f);

        private const float BeamHeight = 7f;
        private const float CoreWidth = 0.35f;
        private const float OuterWidth = 1.3f;
        private const float GroundRadius = 1.6f;
        private const float FlareSize = 1.1f;
        private const float RiseTime = 0.25f;
        private const float PulseSpeed = 2.2f;
        private const int SparkCount = 7;
        private const float SparkCycle = 1.8f;
        private const float SparkSize = 0.18f;
        private const float Brightness = 1.6f;

        private static bool assetsBuilt;
        private static bool assetsFailed;
        private static Mesh beamMesh;
        private static Mesh centeredQuad;
        private static Mesh groundQuad;
        private static Material coreMaterial;
        private static Material outerMaterial;
        private static Material groundMaterial;
        private static Material flareMaterial;
        private static Material sparkMaterial;

        private class Part
        {
            internal Transform transform;
            internal MeshRenderer renderer;
            internal MaterialPropertyBlock block = new MaterialPropertyBlock();
        }

        private Part core;
        private Part outer;
        private Part ground;
        private Part flare;
        private readonly List<Part> sparks = new List<Part>();
        private float[] sparkOffsets;
        private Vector2[] sparkDrift;
        private Vector3 groundPoint;
        private float age;

        internal static GameObject Create(Vector3 daggerPosition)
        {
            if (!EnsureAssets()) return null;

            GameObject obj = new GameObject("KatarinaDaggerLootBeam");
            obj.transform.position = daggerPosition;
            obj.AddComponent<DaggerLootBeam>().Build(daggerPosition);
            return obj;
        }

        private void Build(Vector3 daggerPosition)
        {
            this.groundPoint = daggerPosition;
            if (Physics.Raycast(daggerPosition + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 15f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                this.groundPoint = hit.point;
            }

            this.outer = this.AddPart("BeamOuter", beamMesh, outerMaterial);
            this.core = this.AddPart("BeamCore", beamMesh, coreMaterial);
            this.ground = this.AddPart("GroundGlow", groundQuad, groundMaterial);
            this.flare = this.AddPart("Flare", centeredQuad, flareMaterial);

            this.sparkOffsets = new float[SparkCount];
            this.sparkDrift = new Vector2[SparkCount];
            for (int i = 0; i < SparkCount; i++)
            {
                this.sparks.Add(this.AddPart("Spark", centeredQuad, sparkMaterial));
                this.sparkOffsets[i] = (i + Random.value * 0.5f) / SparkCount;
                this.sparkDrift[i] = Random.insideUnitCircle * 0.35f;
            }

            this.ground.transform.position = this.groundPoint + Vector3.up * 0.06f;
            this.ground.transform.localScale = Vector3.one * (GroundRadius * 2f);
            this.UpdateParts(0f);
        }

        private Part AddPart(string name, Mesh mesh, Material material)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(base.transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return new Part { transform = obj.transform, renderer = renderer };
        }

        private void LateUpdate()
        {
            this.age += Time.deltaTime;
            this.UpdateParts(this.age);
        }

        private void UpdateParts(float time)
        {
            Camera camera = LeagueVfxAssets.BillboardCamera();
            Vector3 cameraPosition = camera ? camera.transform.position : base.transform.position + Vector3.back;
            Vector3 basePosition = base.transform.position;

            float rise = Mathf.SmoothStep(0f, 1f, time / RiseTime);
            float pulse = 0.85f + 0.15f * Mathf.Sin(time * PulseSpeed * Mathf.PI);
            float pop = 1f + 1.2f * Mathf.Exp(-time * 6f);

            Vector3 flat = cameraPosition - basePosition;
            flat.y = 0f;
            Quaternion beamRotation = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-flat.normalized, Vector3.up) : Quaternion.identity;

            this.core.transform.position = this.groundPoint;
            this.core.transform.rotation = beamRotation;
            this.core.transform.localScale = new Vector3(CoreWidth * pop, BeamHeight * rise, 1f);
            SetColor(this.core, coreColor, pulse * pop);

            this.outer.transform.position = this.groundPoint;
            this.outer.transform.rotation = beamRotation;
            this.outer.transform.localScale = new Vector3(OuterWidth, BeamHeight * 0.85f * rise, 1f);
            SetColor(this.outer, outerColor, pulse);

            SetColor(this.ground, groundColor, pulse * Mathf.Clamp01(time / RiseTime));
            this.ground.transform.rotation = Quaternion.Euler(0f, time * 20f, 0f);

            this.flare.transform.position = basePosition;
            this.flare.transform.rotation = Quaternion.LookRotation(basePosition - cameraPosition, Vector3.up) * Quaternion.Euler(0f, 0f, time * 30f);
            this.flare.transform.localScale = Vector3.one * (FlareSize * (0.9f + 0.1f * pulse) * pop);
            SetColor(this.flare, flareColor, pulse);

            for (int i = 0; i < this.sparks.Count; i++)
            {
                float t = Mathf.Repeat(time / SparkCycle + this.sparkOffsets[i], 1f);
                Vector2 drift = this.sparkDrift[i];
                Vector3 position = this.groundPoint + new Vector3(drift.x * (0.3f + t), t * BeamHeight * 0.6f, drift.y * (0.3f + t));
                Part spark = this.sparks[i];
                spark.transform.position = position;
                spark.transform.rotation = Quaternion.LookRotation(position - cameraPosition, Vector3.up);
                spark.transform.localScale = Vector3.one * (SparkSize * (1f - 0.5f * t));
                float fade = Mathf.Clamp01(t * 6f) * Mathf.Clamp01((1f - t) * 3f) * Mathf.Clamp01(time / RiseTime);
                SetColor(spark, sparkColor, fade);
            }
        }

        private static void SetColor(Part part, Color color, float intensity)
        {
            Color c = color;
            c.r *= Brightness;
            c.g *= Brightness;
            c.b *= Brightness;
            c.a = Mathf.Clamp01(c.a * intensity);
            LeagueVfxAssets.SetColor(part.renderer, part.block, c);
        }

        private static bool EnsureAssets()
        {
            if (assetsBuilt) return !assetsFailed;
            assetsBuilt = true;

            try
            {
                beamMesh = LeagueVfxAssets.CreateQuadXY(new Rect(0f, 0f, 1f, 1f));
                Vector3[] vertices = beamMesh.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i].y += 0.5f;
                beamMesh.vertices = vertices;
                beamMesh.RecalculateBounds();

                centeredQuad = LeagueVfxAssets.CreateQuadXY(new Rect(0f, 0f, 1f, 1f));
                groundQuad = LeagueVfxAssets.CreateQuad();

                coreMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaLootBeamCore", BeamTexture(0.12f, "texKatarinaLootBeamCore"), true, 20);
                outerMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaLootBeamOuter", BeamTexture(0.24f, "texKatarinaLootBeamOuter"), true, 19);
                groundMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaLootGround", GroundTexture(), true, 18);
                flareMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaLootFlare", FlareTexture(), true, 21);
                sparkMaterial = LeagueVfxAssets.CreateMaterial("matKatarinaLootSpark", SoftDot(32, 0.22f, "texKatarinaLootSpark"), true, 22);

                assetsFailed = !coreMaterial || !outerMaterial || !groundMaterial || !flareMaterial || !sparkMaterial;
            }
            catch (System.Exception e)
            {
                Debug.LogError("KatarinaMod: failed to build the dagger loot beam\n" + e);
                assetsFailed = true;
            }
            return !assetsFailed;
        }

        private static Texture2D BeamTexture(float sigma, string name)
        {
            const int width = 64;
            const int height = 256;
            float[] values = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                float vertical = Mathf.Pow(1f - v, 1.6f) * Mathf.SmoothStep(0f, 1f, v / 0.03f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1) - 0.5f;
                    values[y * width + x] = Mathf.Exp(-u * u / (2f * sigma * sigma)) * vertical;
                }
            }
            return LeagueVfxAssets.BuildShape(values, width, height, null, 0f, 1f, name, TextureWrapMode.Clamp);
        }

        private static Texture2D GroundTexture()
        {
            const int size = 128;
            float[] values = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float disc = Mathf.Exp(-r * r / (2f * 0.3f * 0.3f));
                    float ring = 0.35f * Mathf.Exp(-(r - 0.8f) * (r - 0.8f) / (2f * 0.05f * 0.05f));
                    values[y * size + x] = r >= 1f ? 0f : Mathf.Clamp01(disc + ring) * Mathf.SmoothStep(0f, 1f, (1f - r) / 0.1f);
                }
            }
            return LeagueVfxAssets.BuildShape(values, size, size, null, 0f, 1f, "texKatarinaLootGround", TextureWrapMode.Clamp);
        }

        private static Texture2D FlareTexture()
        {
            const int size = 128;
            float[] values = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float dot = Mathf.Exp(-r * r / (2f * 0.12f * 0.12f));
                    float star = Mathf.Exp(-Mathf.Abs(dx) * 30f) * Mathf.Exp(-Mathf.Abs(dy) * 2.5f) + Mathf.Exp(-Mathf.Abs(dy) * 30f) * Mathf.Exp(-Mathf.Abs(dx) * 2.5f);
                    float edge = Mathf.Clamp01((1f - r) / 0.15f);
                    values[y * size + x] = Mathf.Clamp01(dot + 0.6f * star) * edge;
                }
            }
            return LeagueVfxAssets.BuildShape(values, size, size, null, 0f, 1f, "texKatarinaLootFlare", TextureWrapMode.Clamp);
        }

        private static Texture2D SoftDot(int size, float sigma, string name)
        {
            float[] values = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float r2 = dx * dx + dy * dy;
                    values[y * size + x] = r2 >= 1f ? 0f : Mathf.Exp(-r2 / (2f * sigma * sigma));
                }
            }
            return LeagueVfxAssets.BuildShape(values, size, size, null, 0f, 1f, name, TextureWrapMode.Clamp);
        }
    }
}
