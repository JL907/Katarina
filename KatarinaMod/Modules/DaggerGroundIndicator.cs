using RoR2;
using UnityEngine;

namespace KatarinaMod.Modules
{
    internal class DaggerGroundIndicator : MonoBehaviour
    {
        private const int segments = 96;
        private const int fillRings = 8;
        private const float groundOffset = 0.1f;
        private const float ringWidth = 0.15f;
        private const float pulseSpeed = 4f;
        private static readonly Color ringColor = new Color(0.95f, 0.12f, 0.18f, 0.9f);
        private static readonly Color fillColor = new Color(0.95f, 0.12f, 0.18f, 0.12f);

        private static Material sharedMaterial;

        private LineRenderer ring;
        private Mesh fillMesh;
        private Color[] fillColors;
        private Color[] fadedColors;
        private float age;

        internal static GameObject Create(Vector3 daggerPosition, float radius)
        {
            if (!sharedMaterial) sharedMaterial = CreateMaterial();
            if (!sharedMaterial) return null;

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

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(base.transform, false);
            fill.transform.position = Vector3.zero;
            int vertexCount = 1 + fillRings * segments;
            Vector3[] verts = new Vector3[vertexCount];
            this.fillColors = new Color[vertexCount];
            this.fadedColors = new Color[vertexCount];
            verts[0] = ProjectToGround(center, normal);
            this.fillColors[0] = fillColor * new Color(1f, 1f, 1f, 0.5f);
            for (int r = 1; r <= fillRings; r++)
            {
                float fraction = r / (float)fillRings;
                Color color = Color.Lerp(this.fillColors[0], fillColor, fraction);
                for (int i = 0; i < segments; i++)
                {
                    int v = FillIndex(r, i);
                    if (r == fillRings)
                    {
                        verts[v] = edge[i];
                    }
                    else
                    {
                        float angle = i * Mathf.PI * 2f / segments;
                        Vector3 flat = center + toPlane * new Vector3(Mathf.Cos(angle) * radius * fraction, 0f, Mathf.Sin(angle) * radius * fraction);
                        verts[v] = ProjectToGround(flat, normal);
                    }
                    this.fillColors[v] = color;
                }
            }

            int[] tris = new int[segments * 3 + (fillRings - 1) * segments * 6];
            int k = 0;
            for (int i = 0; i < segments; i++)
            {
                tris[k++] = 0;
                tris[k++] = FillIndex(1, (i + 1) % segments);
                tris[k++] = FillIndex(1, i);
            }
            for (int r = 1; r < fillRings; r++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int innerCurrent = FillIndex(r, i);
                    int innerNext = FillIndex(r, (i + 1) % segments);
                    int outerCurrent = FillIndex(r + 1, i);
                    int outerNext = FillIndex(r + 1, (i + 1) % segments);
                    tris[k++] = innerCurrent;
                    tris[k++] = innerNext;
                    tris[k++] = outerNext;
                    tris[k++] = innerCurrent;
                    tris[k++] = outerNext;
                    tris[k++] = outerCurrent;
                }
            }
            this.fillMesh = new Mesh { vertices = verts, triangles = tris, colors = this.fillColors };
            this.fillMesh.RecalculateBounds();
            fill.AddComponent<MeshFilter>().sharedMesh = this.fillMesh;
            MeshRenderer meshRenderer = fill.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = sharedMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static int FillIndex(int ring, int segment)
        {
            return 1 + (ring - 1) * segments + segment;
        }

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
            this.age += Time.deltaTime;
            float fadeIn = Mathf.Clamp01(this.age / 0.15f);
            float pulse = 0.75f + 0.25f * Mathf.Sin(this.age * pulseSpeed);

            Color c = ringColor;
            c.a *= fadeIn * pulse;
            this.ring.startColor = this.ring.endColor = c;

            if (this.fillMesh != null)
            {
                for (int i = 0; i < this.fadedColors.Length; i++)
                {
                    this.fadedColors[i] = this.fillColors[i];
                    this.fadedColors[i].a *= fadeIn * pulse;
                }
                this.fillMesh.colors = this.fadedColors;
            }
        }

        private void OnDestroy()
        {
            if (this.fillMesh != null) Object.Destroy(this.fillMesh);
        }
    }
}
