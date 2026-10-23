using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace KatarinaMod.Modules.LeagueSkins
{
    internal static class LeagueDaggers
    {
        private const string Folder = "Daggers";

        private static readonly Vector3 FbxToMesh = new Vector3(-0.01f, 0.01f, 0.01f);

        private const string ModelChild = "Model";

        private static readonly Dictionary<string, KeyValuePair<int, string>> BundleSkins = new Dictionary<string, KeyValuePair<int, string>>
        {
            { "MERCENARY_KATARINA_NAME", new KeyValuePair<int, string>(1, "mercenaryMat") },
            { "REDCARD_KATARINA_NAME", new KeyValuePair<int, string>(2, "redCardMat") },
            { "BILGEWATER_KATARINA_NAME", new KeyValuePair<int, string>(3, "bilgeWaterMat") },
            { "KITTYCAT_KATARINA_NAME", new KeyValuePair<int, string>(4, "kittyCatMat") },
            { "HIGHCOMMAND_KATARINA_NAME", new KeyValuePair<int, string>(5, "highCommandMat") },
            { "SANDSTORM_KATARINA_NAME", new KeyValuePair<int, string>(6, "sandStormMat") },
            { "SLAYBELLE_KATARINA_NAME", new KeyValuePair<int, string>(7, "slayBelleMat") },
            { "WARRINGKINGDOMS_KATARINA_NAME", new KeyValuePair<int, string>(8, "warringKingdomsMat") },
            { "PROJECT_KATARINA_NAME", new KeyValuePair<int, string>(9, "projectMat") },
            { "BATTLEACADEMIA_KATARINA_NAME", new KeyValuePair<int, string>(12, "battleAcademiaMat") },
            { "BLOODMOON_KATARINA_NAME", new KeyValuePair<int, string>(21, "bloodMoonMat") },
            { "HIGHNOON_KATARINA_NAME", new KeyValuePair<int, string>(37, "highNoonMat") }
        };

        internal static void Apply(List<SkinDef> skins, Material template)
        {
            GameObject projectile = Projectiles.knifePrefab;
            ProjectileController controller = projectile ? projectile.GetComponent<ProjectileController>() : null;
            GameObject baseGhost = controller ? controller.ghostPrefab : null;
            if (!baseGhost)
            {
                Debug.LogWarning("[Katarina LeagueSkins] No dagger ghost to copy, skin daggers skipped");
                return;
            }

            foreach (SkinDef skin in skins)
            {
                if (!skin) continue;
                try
                {
                    GameObject ghost = null;
                    if (BundleSkins.TryGetValue(skin.nameToken, out KeyValuePair<int, string> bundle))
                    {
                        Material source = Assets.CreateMaterial(bundle.Value);
                        ghost = CreateGhost(baseGhost, bundle.Key, part => source ? new Material(source) : null, true);
                    }
                    else
                    {
                        LeagueSkinDefinition definition = LeagueSkinCatalog.definitions.FirstOrDefault(d => d.nameToken == skin.nameToken);
                        if (definition == null) continue;
                        int number = int.Parse(definition.folder.Substring(4));
                        Dictionary<string, Material> cache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
                        ghost = CreateGhost(baseGhost, number, part => LeagueSkinBuilder.GetMaterial(definition, part, template, cache), false);
                    }
                    if (!ghost) continue;

                    List<SkinDef.ProjectileGhostReplacement> replacements = (skin.projectileGhostReplacements ?? new SkinDef.ProjectileGhostReplacement[0]).ToList();
                    replacements.Add(new SkinDef.ProjectileGhostReplacement
                    {
                        projectilePrefab = projectile,
                        projectileGhostReplacementPrefab = ghost
                    });
                    skin.projectileGhostReplacements = replacements.ToArray();
                }
                catch (Exception e)
                {
                    Debug.LogError("[Katarina LeagueSkins] Failed to build dagger for " + skin.nameToken + ": " + e);
                }
            }
        }

        private static GameObject CreateGhost(GameObject baseGhost, int number, Func<string, Material> materialFor, bool flipV)
        {
            byte[] bytes = LeagueResources.Load(Folder, "katarina_dagger_" + number.ToString("00") + ".skn");
            if (bytes == null)
            {
                Debug.LogWarning("[Katarina LeagueSkins] Missing dagger " + number);
                return null;
            }
            LeagueMesh source = LeagueMesh.Read(bytes);

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int[]> submeshes = new List<int[]>();
            List<Material> materials = new List<Material>();

            foreach (LeagueMeshPart part in source.parts)
            {
                int offset = vertices.Count;
                for (int v = part.vertexStart; v < part.vertexStart + part.vertexCount; v++)
                {
                    vertices.Add(Vector3.Scale(source.positions[v], FbxToMesh));
                    normals.Add(Vector3.Scale(source.normals[v], new Vector3(-1f, 1f, 1f)).normalized);
                    Vector2 uv = source.uvs[v];
                    uvs.Add(flipV ? new Vector2(uv.x, 1f - uv.y) : uv);
                }

                int[] triangles = new int[part.indexCount];
                for (int i = 0; i + 2 < part.indexCount; i += 3)
                {
                    triangles[i] = source.indices[part.indexStart + i] - part.vertexStart + offset;
                    triangles[i + 1] = source.indices[part.indexStart + i + 2] - part.vertexStart + offset;
                    triangles[i + 2] = source.indices[part.indexStart + i + 1] - part.vertexStart + offset;
                }
                submeshes.Add(triangles);

                Material material = materialFor(part.name);
                if (material)
                {
                    material = new Material(material) { name = "KatarinaDagger" + number + "_" + part.name };
                    if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
                }
                materials.Add(material);
            }

            Mesh mesh = new Mesh { name = "KatarinaDagger" + number.ToString("00") };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = submeshes.Count;
            for (int s = 0; s < submeshes.Count; s++) mesh.SetTriangles(submeshes[s], s);
            mesh.RecalculateBounds();

            GameObject ghost = PrefabAPI.InstantiateClone(baseGhost, "KatarinaWeaponGhost" + number.ToString("00"), false);
            Transform model = FindChild(ghost.transform, ModelChild);
            MeshFilter filter = model ? model.GetComponent<MeshFilter>() : null;
            MeshRenderer renderer = model ? model.GetComponent<MeshRenderer>() : null;
            if (!filter || !renderer)
            {
                Debug.LogWarning("[Katarina LeagueSkins] Dagger ghost has no '" + ModelChild + "' mesh, dagger " + number + " skipped");
                UnityEngine.Object.Destroy(ghost);
                return null;
            }

            filter.sharedMesh = mesh;
            Material fallback = renderer.sharedMaterial;
            renderer.sharedMaterials = materials.Select(m => m ? m : fallback).ToArray();
            return ghost;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }
    }
}
