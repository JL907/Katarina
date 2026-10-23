using RoR2;
using UnityEngine;

namespace KatarinaMod.Modules
{
    // Red circle on the ground around a landed dagger (and the landing preview while it falls).
    // The circle is sampled as a polar grid; every point is dropped straight down onto the terrain and
    // checked against its neighbours, so slopes, bumps and ledges don't produce spikes or floating edges.
    internal class DaggerGroundIndicator : MonoBehaviour
    {
        private const int segments = 96;
        private const int fillRings = 8;
        private const float groundOffset = 0.12f;
        private const float ringWidth = 0.15f;
        private const float pulseSpeed = 4f;
        private static readonly Color ringColor = new Color(0.95f, 0.12f, 0.18f, 0.9f);
        private static readonly Color fillColor = new Color(0.95f, 0.12f, 0.18f, 0.12f);

        // Terrain sampling.
        private const float probeAbove = 4f;      // start each ray this far above the expected height
        private const float probeDepth = 10f;     // and look this far down
        private const float maxSlope = 1.6f;      // max height change per metre before a sample counts as a ledge
        private const int smoothingPasses = 2;

        private static Material sharedMaterial;

        private LineRenderer ring;
        private Mesh fillMesh;
        private Color[] fillColors;
        private Color[] fadedColors;
        private float age;

        private Vector3 center;
        private float radius;
        private float[] heights; // index 0 = centre, then ring r (1..fillRings) * segments + i

        // Landing preview: an inner ring that grows from the centre to the edge as the dagger falls.
        private static readonly Color progressColor = new Color(1f, 0.35f, 0.4f, 0.95f);
        private LineRenderer progress;
        private float previewDuration;
        private Vector3[] progressPoints;

        internal static GameObject Create(Vector3 daggerPosition, float radius)
        {
            if (!sharedMaterial) sharedMaterial = CreateMaterial();
            if (!sharedMaterial) return null;

            Vector3 center = daggerPosition;
            if (Physics.Raycast(daggerPosition + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 15f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            {
                center = hit.point;
            }

            GameObject obj = new GameObject("KatarinaDaggerGroundIndicator");
            obj.transform.position = center;
            obj.AddComponent<DaggerGroundIndicator>().Build(center, radius);
            return obj;
        }

        internal static GameObject CreatePreview(Vector3 landingPoint, float radius, float duration)
        {
            GameObject obj = Create(landingPoint, radius);
            if (!obj) return null;
            obj.name = "KatarinaDaggerLandingPreview";
            obj.GetComponent<DaggerGroundIndicator>().AddProgressRing(duration);
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

        private static int Index(int ring, int segment)
        {
            return 1 + (ring - 1) * segments + segment;
        }

        private Vector2 Offset(int ring, int segment)
        {
            float angle = segment * Mathf.PI * 2f / segments;
            float r = this.radius * ring / fillRings;
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        private Vector3 GridPoint(int ring, int segment)
        {
            if (ring == 0) return new Vector3(this.center.x, this.heights[0] + groundOffset, this.center.z);
            Vector2 o = this.Offset(ring, segment);
            return new Vector3(this.center.x + o.x, this.heights[Index(ring, segment)] + groundOffset, this.center.z + o.y);
        }

        // Drops a ray straight down near `expectedY` and returns the ground height closest to it,
        // so overhangs above and lower floors below are ignored.
        private static bool SampleHeight(float x, float z, float expectedY, out float y)
        {
            y = expectedY;
            Vector3 origin = new Vector3(x, expectedY + probeAbove, z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, probeAbove + probeDepth, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
            bool found = false;
            float best = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                float d = Mathf.Abs(hit.point.y - expectedY);
                if (d < best)
                {
                    best = d;
                    y = hit.point.y;
                    found = true;
                }
            }
            return found;
        }

        private void SampleTerrain()
        {
            int count = 1 + fillRings * segments;
            this.heights = new float[count];
            bool[] valid = new bool[count];
            this.heights[0] = this.center.y;
            valid[0] = true;

            float step = this.radius / fillRings;
            float maxStep = maxSlope * step;

            // Work outward ring by ring; each sample is expected near the sample just inside it.
            for (int r = 1; r <= fillRings; r++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float inner = r == 1 ? this.heights[0] : this.heights[Index(r - 1, i)];
                    Vector2 o = this.Offset(r, i);
                    int k = Index(r, i);
                    if (SampleHeight(this.center.x + o.x, this.center.z + o.y, inner, out float y) && Mathf.Abs(y - inner) <= maxStep)
                    {
                        this.heights[k] = y;
                        valid[k] = true;
                    }
                    else
                    {
                        // Ledge, wall or nothing below: carry the inner height so the circle stays flat there
                        // instead of spiking; fixed up from neighbours below.
                        this.heights[k] = inner;
                        valid[k] = false;
                    }
                }

                // Fill invalid samples on this ring from the nearest valid neighbours around the ring.
                for (int i = 0; i < segments; i++)
                {
                    int k = Index(r, i);
                    if (valid[k]) continue;
                    int left = -1, right = -1, dl = 0, dr = 0;
                    for (int s = 1; s < segments && left < 0; s++)
                    {
                        int j = Index(r, (i - s + segments) % segments);
                        if (valid[j]) { left = j; dl = s; }
                    }
                    for (int s = 1; s < segments && right < 0; s++)
                    {
                        int j = Index(r, (i + s) % segments);
                        if (valid[j]) { right = j; dr = s; }
                    }
                    if (left >= 0 && right >= 0)
                    {
                        float t = dl / (float)(dl + dr);
                        float around = Mathf.Lerp(this.heights[left], this.heights[right], t);
                        float inner = r == 1 ? this.heights[0] : this.heights[Index(r - 1, i)];
                        this.heights[k] = Mathf.Clamp(around, inner - maxStep, inner + maxStep);
                    }
                }
            }

            // Light smoothing around each ring removes stair-steps from small rocks and mesh seams.
            float[] temp = new float[segments];
            for (int pass = 0; pass < smoothingPasses; pass++)
            {
                for (int r = 1; r <= fillRings; r++)
                {
                    for (int i = 0; i < segments; i++)
                    {
                        float a = this.heights[Index(r, (i - 1 + segments) % segments)];
                        float b = this.heights[Index(r, i)];
                        float c = this.heights[Index(r, (i + 1) % segments)];
                        temp[i] = 0.25f * a + 0.5f * b + 0.25f * c;
                    }
                    for (int i = 0; i < segments; i++) this.heights[Index(r, i)] = temp[i];
                }
            }
        }

        private void Build(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
            this.SampleTerrain();

            Vector3[] edge = new Vector3[segments];
            for (int i = 0; i < segments; i++) edge[i] = this.GridPoint(fillRings, i);

            this.ring = base.gameObject.AddComponent<LineRenderer>();
            this.ring.sharedMaterial = sharedMaterial;
            this.ring.useWorldSpace = true;
            this.ring.loop = true;
            this.ring.positionCount = segments;
            this.ring.SetPositions(edge);
            this.ring.widthMultiplier = ringWidth;
            this.ring.numCornerVertices = 2;
            this.ring.alignment = LineAlignment.View;
            this.ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            this.ring.receiveShadows = false;
            this.ring.startColor = this.ring.endColor = ringColor;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(base.transform, false);
            fill.transform.position = Vector3.zero;
            fill.transform.rotation = Quaternion.identity;
            int vertexCount = 1 + fillRings * segments;
            Vector3[] verts = new Vector3[vertexCount];
            this.fillColors = new Color[vertexCount];
            this.fadedColors = new Color[vertexCount];
            verts[0] = this.GridPoint(0, 0);
            this.fillColors[0] = fillColor * new Color(1f, 1f, 1f, 0.5f);
            for (int r = 1; r <= fillRings; r++)
            {
                float fraction = r / (float)fillRings;
                Color color = Color.Lerp(this.fillColors[0], fillColor, fraction);
                for (int i = 0; i < segments; i++)
                {
                    int v = Index(r, i);
                    verts[v] = this.GridPoint(r, i);
                    this.fillColors[v] = color;
                }
            }

            int[] tris = new int[segments * 3 + (fillRings - 1) * segments * 6];
            int k = 0;
            for (int i = 0; i < segments; i++)
            {
                tris[k++] = 0;
                tris[k++] = Index(1, (i + 1) % segments);
                tris[k++] = Index(1, i);
            }
            for (int r = 1; r < fillRings; r++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int innerCurrent = Index(r, i);
                    int innerNext = Index(r, (i + 1) % segments);
                    int outerCurrent = Index(r + 1, i);
                    int outerNext = Index(r + 1, (i + 1) % segments);
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

        // Height of the sampled terrain at a fractional ring position (0 = centre, fillRings = edge).
        private float HeightAt(float ringPosition, int segment)
        {
            ringPosition = Mathf.Clamp(ringPosition, 0f, fillRings);
            int lower = Mathf.FloorToInt(ringPosition);
            int upper = Mathf.Min(lower + 1, fillRings);
            float t = ringPosition - lower;
            float a = lower == 0 ? this.heights[0] : this.heights[Index(lower, segment)];
            float b = upper == 0 ? this.heights[0] : this.heights[Index(upper, segment)];
            return Mathf.Lerp(a, b, t);
        }

        private void AddProgressRing(float duration)
        {
            this.previewDuration = Mathf.Max(0.05f, duration);
            GameObject child = new GameObject("Progress");
            child.transform.SetParent(base.transform, false);
            this.progress = child.AddComponent<LineRenderer>();
            this.progress.sharedMaterial = sharedMaterial;
            this.progress.useWorldSpace = true;
            this.progress.loop = true;
            this.progress.positionCount = segments;
            this.progress.widthMultiplier = ringWidth * 1.3f;
            this.progress.numCornerVertices = 2;
            this.progress.alignment = LineAlignment.View;
            this.progress.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            this.progress.receiveShadows = false;
            this.progress.startColor = this.progress.endColor = progressColor;
            this.progressPoints = new Vector3[segments];
            this.UpdateProgress();
        }

        private void UpdateProgress()
        {
            float t = Mathf.Clamp01(this.age / this.previewDuration);
            float fraction = Mathf.SmoothStep(0f, 1f, t);
            float r = this.radius * fraction;
            float ringPosition = fraction * fillRings;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float y = this.HeightAt(ringPosition, i) + groundOffset * 1.5f;
                this.progressPoints[i] = new Vector3(this.center.x + Mathf.Cos(angle) * r, y, this.center.z + Mathf.Sin(angle) * r);
            }
            this.progress.SetPositions(this.progressPoints);
            Color c = progressColor;
            c.a *= Mathf.Clamp01(this.age / 0.15f);
            this.progress.startColor = this.progress.endColor = c;
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

            if (this.progress) this.UpdateProgress();
        }

        private void OnDestroy()
        {
            if (this.fillMesh != null) Object.Destroy(this.fillMesh);
        }
    }
}
