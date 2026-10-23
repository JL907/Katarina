using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueVfx
{
    // Motion-blur style afterimages: every frame while `emitting`, snapshots the meshes under `source`
    // and redraws those snapshots as glowing red silhouettes that fade out quickly.
    // Drawn with Graphics.DrawMesh, so no extra GameObjects are created.
    internal class DaggerAfterimages : MonoBehaviour
    {
        private static readonly Color Tint = new Color(1f, 0.12f, 0.22f, 1f);
        private const float Interval = 0.012f;
        private const float Lifetime = 0.16f;
        private const float StartAlpha = 0.6f;
        private const int MaxSnapshots = 20;

        private static Material material;
        private static bool materialFailed;

        internal bool emitting;

        private class Part
        {
            internal Transform transform;
            internal Mesh mesh;
            internal int layer;
        }

        private class Snapshot
        {
            internal Matrix4x4[] matrices;
            internal float time;
            internal bool used;
        }

        private readonly List<Part> parts = new List<Part>();
        private Snapshot[] snapshots;
        private int next;
        private float lastSnapshotTime = -1f;
        private MaterialPropertyBlock block;

        internal void SetSource(Transform source)
        {
            this.parts.Clear();
            if (source)
            {
                foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!filter.sharedMesh) continue;
                    if (!filter.GetComponent<MeshRenderer>()) continue;
                    this.parts.Add(new Part { transform = filter.transform, mesh = filter.sharedMesh, layer = filter.gameObject.layer });
                }
            }

            this.snapshots = new Snapshot[MaxSnapshots];
            for (int i = 0; i < MaxSnapshots; i++)
            {
                this.snapshots[i] = new Snapshot { matrices = new Matrix4x4[this.parts.Count] };
            }
            this.Clear();
        }

        internal void Clear()
        {
            if (this.snapshots != null)
            {
                foreach (Snapshot s in this.snapshots) s.used = false;
            }
            this.next = 0;
            this.lastSnapshotTime = -1f;
        }

        private void OnDisable()
        {
            this.emitting = false;
            this.Clear();
        }

        private void LateUpdate()
        {
            if (this.snapshots == null || this.parts.Count == 0) return;
            if (!EnsureMaterial()) return;

            float now = Time.time;
            if (this.emitting && (this.lastSnapshotTime < 0f || now - this.lastSnapshotTime >= Interval))
            {
                this.lastSnapshotTime = now;
                Snapshot snapshot = this.snapshots[this.next];
                this.next = (this.next + 1) % MaxSnapshots;
                snapshot.used = true;
                snapshot.time = now;
                for (int i = 0; i < this.parts.Count; i++)
                {
                    Transform t = this.parts[i].transform;
                    snapshot.matrices[i] = t ? t.localToWorldMatrix : Matrix4x4.zero;
                }
            }

            if (this.block == null) this.block = new MaterialPropertyBlock();
            foreach (Snapshot snapshot in this.snapshots)
            {
                if (!snapshot.used) continue;
                float age = now - snapshot.time;
                if (age >= Lifetime)
                {
                    snapshot.used = false;
                    continue;
                }
                // Skip the snapshot taken this frame: it would sit exactly on top of the real dagger.
                if (age <= 0f) continue;

                float fade = 1f - age / Lifetime;
                Color color = Tint;
                color.a = StartAlpha * fade * fade;
                this.block.SetColor("_TintColor", color);
                this.block.SetColor("_Color", color);

                for (int i = 0; i < this.parts.Count; i++)
                {
                    Part part = this.parts[i];
                    if (!part.mesh) continue;
                    for (int sub = 0; sub < part.mesh.subMeshCount; sub++)
                    {
                        Graphics.DrawMesh(part.mesh, snapshot.matrices[i], material, part.layer, null, sub, this.block, false, false);
                    }
                }
            }
        }

        private static bool EnsureMaterial()
        {
            if (material) return true;
            if (materialFailed) return false;
            material = LeagueVfxAssets.CreateMaterial("matKatarinaDaggerAfterimage", Texture2D.whiteTexture, true, 5);
            if (!material)
            {
                materialFailed = true;
                return false;
            }
            return true;
        }
    }
}
