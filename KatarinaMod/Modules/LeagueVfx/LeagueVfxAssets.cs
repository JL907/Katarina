using KatarinaMod.Modules.LeagueSkins;
using RoR2;
using UnityEngine;
using UnityEngine.Rendering;

namespace KatarinaMod.Modules.LeagueVfx
{
    internal static class LeagueVfxAssets
    {
        internal const string Folder = "Vfx";

        private static bool shadersSearched;
        private static Shader cloudRemap;
        private static Shader fallback;
        private static Texture2D ramp;

        internal static Color32[] LoadPixels(string file, out int width, out int height)
        {
            width = height = 0;
            byte[] data = LeagueResources.Load(Folder, file);
            if (data == null)
            {
                Debug.LogWarning("KatarinaMod: missing VFX texture " + file);
                return null;
            }
            return LeagueTextures.LoadPixels(data, out width, out height);
        }

        internal static float Alpha(Color32[] pixels, int width, int height, float u, float v)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (width - 1)), 0, width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (height - 1)), 0, height - 1);
            return pixels[y * width + x].a / 255f;
        }

        internal static Texture2D LoadShape(string file, bool clearBorder)
        {
            return LoadShape(file, clearBorder, 1f);
        }

        internal static Texture2D LoadShape(string file, bool clearBorder, float gamma)
        {
            Color32[] source = LoadPixels(file, out int width, out int height);
            if (source == null) return null;

            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte a = source[(height - 1 - y) * width + x].a;
                    if (gamma != 1f) a = (byte)Mathf.RoundToInt(Mathf.Pow(a / 255f, gamma) * 255f);
                    if (clearBorder && (x == 0 || y == 0 || x == width - 1 || y == height - 1)) a = 0;
                    pixels[y * width + x] = new Color32(a, a, a, a);
                }
            }
            return Build(pixels, width, height, file, clearBorder ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
        }

        internal static float[] LoadShapeValues(string file, bool useLuminance, out int width, out int height)
        {
            Color32[] source = LoadPixels(file, out width, out height);
            if (source == null) return null;

            float[] values = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 c = source[(height - 1 - y) * width + x];
                    values[y * width + x] = useLuminance ? Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f : c.a / 255f;
                }
            }
            return values;
        }

        internal static Texture2D BuildShape(float[] values, int width, int height, float[] erosionMap, float drive, float feather, string name, TextureWrapMode wrap)
        {
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                float v = values[i];
                if (erosionMap != null) v *= Mathf.SmoothStep(0f, 1f, (erosionMap[i] - drive) / feather);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
                pixels[i] = new Color32(a, a, a, a);
            }
            return Build(pixels, width, height, name, wrap);
        }

        internal static Camera BillboardCamera()
        {
            LocalUser user = LocalUserManager.GetFirstLocalUser();
            if (user != null && user.cameraRigController && user.cameraRigController.sceneCam) return user.cameraRigController.sceneCam;
            return Camera.main;
        }

        internal static Mesh CreateQuadXY(Rect uv)
        {
            Mesh mesh = new Mesh { name = "KatarinaVfxQuadXY" };
            mesh.vertices = new Vector3[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            mesh.uv = new Vector2[] { new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax) };
            mesh.colors32 = new Color32[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            mesh.normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        internal static Texture2D Build(Color32[] pixels, int width, int height, string name, TextureWrapMode wrap)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.wrapMode = wrap;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static void FindShaders()
        {
            if (shadersSearched) return;
            shadersSearched = true;

            cloudRemap = LegacyResourcesAPI.Load<Shader>("Shaders/FX/HGCloudRemap");
            if (!cloudRemap) cloudRemap = Shader.Find("Hopoo Games/FX/Cloud Remap");
            if (cloudRemap) return;

            GameObject probe = new GameObject("KatarinaVfxShaderProbe");
            try
            {
                SpriteRenderer sprite = probe.AddComponent<SpriteRenderer>();
                if (sprite.sharedMaterial) fallback = sprite.sharedMaterial.shader;
            }
            finally
            {
                Object.Destroy(probe);
            }
            Debug.LogWarning("KatarinaMod: HGCloudRemap not found, League VFX will use the sprite shader (no additive blending).");
        }

        private static Texture2D Ramp()
        {
            if (ramp) return ramp;
            Color32[] pixels = new Color32[256 * 4];
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 256; x++) pixels[y * 256 + x] = new Color32(255, 255, 255, (byte)x);
            }
            ramp = new Texture2D(256, 4, TextureFormat.RGBA32, false);
            ramp.name = "texKatarinaVfxWhiteRamp";
            ramp.wrapMode = TextureWrapMode.Clamp;
            ramp.SetPixels32(pixels);
            ramp.Apply(false, true);
            return ramp;
        }

        internal static Material CreateMaterial(string name, Texture texture, bool additive, int queueOffset)
        {
            FindShaders();
            Material material;
            if (cloudRemap)
            {
                material = new Material(cloudRemap);
                material.SetTexture("_RemapTex", Ramp());
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                material.SetColor("_TintColor", Color.white);
                material.SetFloat("_Boost", 1f);
                material.SetFloat("_AlphaBoost", 1f);
                material.SetFloat("_AlphaBias", 0f);
                material.SetFloat("_InvFade", 20f);
                material.SetFloat("_Cull", 0f);
                material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
                material.SetFloat("_DepthOffset", 0f);
                material.SetFloat("_ExternalAlpha", 1f);
                material.SetFloat("_Fade", 1f);
                SetToggle(material, "_CloudsOn", "USE_CLOUDS", false);
                SetToggle(material, "_CloudOffsetOn", "CLOUDOFFSET", false);
                SetToggle(material, "_DisableRemapOn", "DISABLEREMAP", false);
                SetToggle(material, "_FadeCloseOn", "FADECLOSE", false);
                SetToggle(material, "_VertexColorOn", "VERTEXCOLOR", false);
                SetToggle(material, "_VertexAlphaOn", "VERTEXALPHA", false);
                SetToggle(material, "_CalcTextureAlphaOn", "CALCTEXTUREALPHA", false);
                SetToggle(material, "_VertexOffsetOn", "VERTEXOFFSET", false);
                SetToggle(material, "_FresnelOn", "FRESNEL", false);
                SetToggle(material, "_SkyboxOnly", "SKYBOX_ONLY", false);
                SetToggle(material, "_UseUV1On", "USE_UV1", false);
            }
            else if (fallback)
            {
                material = new Material(fallback);
            }
            else
            {
                return null;
            }

            material.name = name;
            material.SetTexture("_MainTex", texture);
            material.renderQueue = (int)RenderQueue.Transparent + queueOffset;
            return material;
        }

        private static void SetToggle(Material material, string property, string keyword, bool on)
        {
            material.SetFloat(property, on ? 1f : 0f);
            if (on) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        internal static void SetColor(Renderer renderer, MaterialPropertyBlock block, Color color)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor("_TintColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        internal static Color Evaluate(float t, float[] times, Color[] values)
        {
            if (t <= times[0]) return values[0];
            for (int i = 1; i < times.Length; i++)
            {
                if (t <= times[i]) return Color.LerpUnclamped(values[i - 1], values[i], (t - times[i - 1]) / (times[i] - times[i - 1]));
            }
            return values[values.Length - 1];
        }

        internal static float Evaluate(float t, float[] times, float[] values)
        {
            if (t <= times[0]) return values[0];
            for (int i = 1; i < times.Length; i++)
            {
                if (t <= times[i]) return Mathf.LerpUnclamped(values[i - 1], values[i], (t - times[i - 1]) / (times[i] - times[i - 1]));
            }
            return values[values.Length - 1];
        }

        internal static Mesh CreateQuad()
        {
            Mesh mesh = new Mesh { name = "KatarinaVfxQuad" };
            mesh.vertices = new Vector3[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
            mesh.uv = new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.colors32 = new Color32[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
