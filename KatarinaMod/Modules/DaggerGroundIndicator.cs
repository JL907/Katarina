using RoR2;
using UnityEngine;

namespace KatarinaMod.Modules
{
    // League-style ground circle under a landed dagger, showing the area Katarina can pick it up from.
    // Built at runtime (ring + soft fill) and shaped to the ground beneath it.
    internal class DaggerGroundIndicator : MonoBehaviour
    {
        private const int segments = 96;
        private const float groundOffset = 0.1f;    // lift off the ground to avoid z-fighting
        private const float ringWidth = 0.15f;
        private const float pulseSpeed = 4f;
        private static readonly Color ringColor = new Color(0.95f, 0.12f, 0.18f, 0.9f);
        private static readonly Color fillColor = new Color(0.95f, 0.12f, 0.18f, 0.12f);

        private static Material sharedMaterial;

        private LineRenderer ring;
        private Mesh fillMesh;
        private Color[] fillColors;
        private float age;

        internal static GameObject Create(Vector3 daggerPosition, float radius)
        {
            if (!sharedMaterial) sharedMaterial = CreateMaterial();
            if (!sharedMaterial) return null;

            // centre the circle on the ground below the dagger (it may be stuck in a wall)
            Vector3 center = daggerPosition;
            Vector3 normal = Vector3.up;
            if (Physics.Raycast(daggerPosition + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 15f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                center = hit.point;
                normal = hit.normal;
            }

            GameObject obj = new GameObject("KatarinaDaggerGroundIndicator");
            obj.transform.position = center;
            obj.AddComponent<DaggerGroundIndicator>().Build(center, normal, radius);
            return obj;
        }

        // Unity's default sprite material: alpha blended, uses vertex colours, depth tested against the world.
        // Shader.Find can't be used: RoR2BepInExPack routes it to RoR2's shader lookup, which only knows game shaders.
        private static Material CreateMaterial()
        {
            Shader shader = null;

            GameObject temp = new GameObject("KatarinaShaderProbe");
            try
            {
                SpriteRenderer probe = temp.AddComponent<SpriteRenderer>();
                if (probe.sharedMaterial) shader = probe.sharedMaterial.shader;
            }
            finally
            {
                Object.Destroy(temp);
            }

            if (!shader)
            {
                Material canvasMaterial = Canvas.GetDefaultCanvasMaterial();
                if (canvasMaterial) shader = canvasMaterial.shader;
            }

            if (!shader)
            {
                Debug.LogWarning("KatarinaMod: no shader available for the dagger ground indicator; it will be skipped.");
                return null;
            }
            return new Material(shader) { name = "matKatarinaDaggerGroundIndicator" };
        }

        private void Build(Vector3 center, Vector3 normal, float radius)
        {
            Vector3[] edge = new Vector3[segments];
            Quaternion toPlane = Quaternion.FromToRotation(Vector3.up, normal);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 flat = center + toPlane * new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                edge[i] = ProjectToGround(flat, normal);
            }
            Vector3 centerPoint = center + normal * groundOffset;

            // outer ring
            this.ring = base.gameObject.AddComponent<LineRenderer>();
            this.ring.sharedMaterial = sharedMaterial;
            this.ring.useWorldSpace = true;
            this.ring.loop = true;
            this.ring.positionCount = segments;
            this.ring.SetPositions(edge);
            this.ring.widthMultiplier = ringWidth;
            this.ring.numCornerVertices = 2;
            this.ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            this.ring.receiveShadows = false;
            this.ring.startColor = this.ring.endColor = ringColor;

            // soft fill: a fan from the centre to the ring, in local space of this object
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(base.transform, false);
            fill.transform.position = Vector3.zero;
            Vector3[] verts = new Vector3[segments + 1];
            int[] tris = new int[segments * 3];
            this.fillColors = new Color[segments + 1];
            verts[0] = centerPoint;
            this.fillColors[0] = fillColor * new Color(1f, 1f, 1f, 0.5f); // lighter in the middle
            for (int i = 0; i < segments; i++)
            {
                verts[i + 1] = edge[i];
                this.fillColors[i + 1] = fillColor;
                tris[i * 3] = 0;
                tris[i * 3 + 1] = 1 + (i + 1) % segments;
                tris[i * 3 + 2] = 1 + i;
            }
            this.fillMesh = new Mesh { vertices = verts, triangles = tris, colors = this.fillColors };
            this.fillMesh.RecalculateBounds();
            fill.AddComponent<MeshFilter>().sharedMesh = this.fillMesh;
            MeshRenderer meshRenderer = fill.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = sharedMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        // drop each edge point onto the terrain so the circle hugs slopes and bumps
        private static Vector3 ProjectToGround(Vector3 point, Vector3 normal)
        {
            if (Physics.Raycast(point + normal * 2f, -normal, out RaycastHit hit, 4f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                return hit.point + hit.normal * groundOffset;
            }
            return point + normal * groundOffset;
        }

        private void Update()
        {
            // gentle pulse, with a quick fade-in when the dagger lands
            this.age += Time.deltaTime;
            float fadeIn = Mathf.Clamp01(this.age / 0.15f);
            float pulse = 0.75f + 0.25f * Mathf.Sin(this.age * pulseSpeed);

            Color c = ringColor;
            c.a *= fadeIn * pulse;
            this.ring.startColor = this.ring.endColor = c;

            if (this.fillMesh != null)
            {
                Color[] colors = new Color[this.fillColors.Length];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = this.fillColors[i];
                    colors[i].a *= fadeIn * pulse;
                }
                this.fillMesh.colors = colors;
            }
        }

        private void OnDestroy()
        {
            if (this.fillMesh != null) Object.Destroy(this.fillMesh);
        }
    }
}
