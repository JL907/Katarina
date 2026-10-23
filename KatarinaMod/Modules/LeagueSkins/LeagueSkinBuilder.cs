using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace KatarinaMod.Modules.LeagueSkins
{
    internal class LeagueSkinDefinition
    {
        internal string folder;
        internal string meshFile;
        internal string skeletonFile;
        internal string nameToken;
        internal string displayName;
        internal string iconTexture;
        internal string defaultTexture;
        internal Dictionary<string, string> partTextures = new Dictionary<string, string>();
        internal string[] hiddenParts = new string[0];
        internal DynamicChainDefinition[] dynamicChains = new DynamicChainDefinition[0];
    }

    // A skin joint whose whole subtree is simulated by DynamicBone (hair, sashes, skirt panels).
    internal class DynamicChainDefinition
    {
        internal string root;
        internal float damping;
        internal float elasticity;
        internal float stiffness;
        internal float inert;
        // Particle radius in League units.
        internal float radius;
        // DynamicBone gravity in model units per update step.
        internal Vector3 gravity;
        // Bones (Unity rig names) whose body colliders this chain collides with.
        internal string[] colliders;
    }

    // A sphere collider on one of the rig's bones, given in League units at the base bind pose.
    internal class BodyColliderDefinition
    {
        internal string bone;
        internal Vector3 center;
        internal float radius;
    }

    internal class BuiltLeagueSkin
    {
        internal LeagueSkinDefinition definition;
        internal Sprite icon;
        internal List<GameObject> parts = new List<GameObject>();
    }

    internal static class LeagueSkinBuilder
    {
        internal const float LeagueUnitScale = 0.01f;
        internal const string BaseFolder = "Base";
        internal const string BaseSkeletonFile = "katarina.skl";
        internal const string BaseMeshFile = "katarina.skn";
        internal const bool TransferExtraBoneWeights = true;
        internal static readonly string[] BaseExcludedParts = new string[0];
        internal static readonly string[] TransferIgnoredBoneKeywords = { "Buffbone", "Vfx", "Snap", "Recall" };
        internal const string SkinNameFormat = "<color=#FFFFFF>{0}</color>";
        internal const string FallbackBone = "Root";
        internal const bool ReverseWinding = true;
        internal const float MaxRotationRebind = 30f;

        // Shadow skeleton: each skin keeps its own League skeleton as hidden bones that copy the
        // animated rig's rotations every frame (see LeagueShadowSkeleton). When false, the mesh is
        // rebound onto the champion's bones instead (older behaviour).
        internal const bool UseShadowSkeleton = true;
        // Swing the skin's limbs to match the champion's bind pose so existing animations line up.
        internal const bool AlignBindPose = true;
        internal static readonly string[] CorrectionIgnoredBoneKeywords = { "Weapon", "Buffbone" };
        // Simulate the catalog's dynamic chains (hair, cloth) with DynamicBone (shadow skeleton only).
        internal const bool EnableDynamicBones = true;

        private static LeagueSkeleton baseSkeleton;

        internal static List<BuiltLeagueSkin> BuildAll(GameObject bodyModel, SkinnedMeshRenderer bodyRenderer, CharacterModel characterModel, GameObject displayModel)
        {
            List<BuiltLeagueSkin> built = new List<BuiltLeagueSkin>();
            if (!bodyModel || !bodyRenderer || !characterModel) return built;

            if (baseSkeleton == null)
            {
                byte[] baseBytes = LeagueResources.Load(BaseFolder, BaseSkeletonFile);
                if (baseBytes == null)
                {
                    Debug.LogError("[Katarina LeagueSkins] Missing embedded base skeleton " + BaseFolder + "/" + BaseSkeletonFile);
                    return built;
                }
                baseSkeleton = LeagueSkeleton.Read(baseBytes);
            }

            BaseWeightSource weightSource = null;
            if (TransferExtraBoneWeights && !UseShadowSkeleton)
            {
                try
                {
                    weightSource = BaseWeightSource.Create(baseSkeleton, bodyModel, bodyRenderer);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Katarina LeagueSkins] Could not prepare base weights: " + e);
                }
            }

            Behaviour[] baseBodyDynamics = DynamicBones.Existing(bodyModel);
            Behaviour[] baseDisplayDynamics = DynamicBones.Existing(displayModel);

            Material template = characterModel.baseRendererInfos.Length > 0 ? characterModel.baseRendererInfos[0].defaultMaterial : null;
            List<CharacterModel.RendererInfo> rendererInfos = characterModel.baseRendererInfos.ToList();

            foreach (LeagueSkinDefinition definition in LeagueSkinCatalog.definitions)
            {
                try
                {
                    BuiltLeagueSkin skin = Build(definition, bodyModel, bodyRenderer, template, displayModel, rendererInfos, weightSource, baseBodyDynamics, baseDisplayDynamics);
                    if (skin != null) built.Add(skin);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Katarina LeagueSkins] Failed to build " + definition.folder + ": " + e);
                }
            }

            characterModel.baseRendererInfos = rendererInfos.ToArray();
            return built;
        }

        internal static void AddSkins(List<SkinDef> skins, List<BuiltLeagueSkin> leagueSkins, GameObject bodyModel, SkinnedMeshRenderer bodyRenderer, CharacterModel characterModel)
        {
            if (leagueSkins.Count == 0) return;

            List<GameObject> allParts = leagueSkins.SelectMany(s => s.parts).ToList();

            foreach (SkinDef skin in skins)
            {
                List<SkinDef.GameObjectActivation> activations = (skin.gameObjectActivations ?? new SkinDef.GameObjectActivation[0]).ToList();
                activations.Add(new SkinDef.GameObjectActivation { gameObject = bodyRenderer.gameObject, shouldActivate = true });
                foreach (GameObject part in allParts) activations.Add(new SkinDef.GameObjectActivation { gameObject = part, shouldActivate = false });
                skin.gameObjectActivations = activations.ToArray();
            }

            Sprite fallbackIcon = skins.Count > 0 ? skins[0].icon : null;

            foreach (BuiltLeagueSkin leagueSkin in leagueSkins)
            {
                LanguageAPI.Add(leagueSkin.definition.nameToken, string.Format(SkinNameFormat, leagueSkin.definition.displayName));

                List<SkinDef.GameObjectActivation> activations = new List<SkinDef.GameObjectActivation>
                {
                    new SkinDef.GameObjectActivation { gameObject = bodyRenderer.gameObject, shouldActivate = false }
                };
                foreach (GameObject part in allParts)
                {
                    activations.Add(new SkinDef.GameObjectActivation { gameObject = part, shouldActivate = leagueSkin.parts.Contains(part) });
                }

                CharacterModel.RendererInfo[] rendererInfos = new CharacterModel.RendererInfo[characterModel.baseRendererInfos.Length];
                characterModel.baseRendererInfos.CopyTo(rendererInfos, 0);

                Skins.SkinDefInfo info = new Skins.SkinDefInfo
                {
                    Name = leagueSkin.definition.nameToken,
                    NameToken = leagueSkin.definition.nameToken,
                    Icon = leagueSkin.icon ? leagueSkin.icon : fallbackIcon,
                    UnlockableDef = null,
                    RootObject = bodyModel,
                    BaseSkins = new SkinDef[0],
                    MinionSkinReplacements = new SkinDef.MinionSkinReplacement[0],
                    ProjectileGhostReplacements = new SkinDef.ProjectileGhostReplacement[0],
                    GameObjectActivations = activations.ToArray(),
                    MeshReplacements = new SkinDef.MeshReplacement[0],
                    RendererInfos = rendererInfos
                };
                skins.Add(Skins.CreateSkinDef(info));
            }
        }

        private static Dictionary<string, Transform> CollectBones(GameObject root)
        {
            Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!bones.ContainsKey(t.name)) bones[t.name] = t;
            }
            return bones;
        }

        private static string PathFrom(Transform root, Transform target)
        {
            List<string> names = new List<string>();
            for (Transform t = target; t && t != root; t = t.parent) names.Insert(0, t.name);
            return string.Join("/", names);
        }

        internal static Matrix4x4 LeagueToRenderer(GameObject bodyModel, SkinnedMeshRenderer bodyRenderer)
        {
            return bodyRenderer.transform.worldToLocalMatrix * bodyModel.transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * LeagueUnitScale);
        }

        internal static Dictionary<string, Transform> Bones(GameObject root)
        {
            return CollectBones(root);
        }

        private static BuiltLeagueSkin Build(LeagueSkinDefinition definition, GameObject bodyModel, SkinnedMeshRenderer bodyRenderer, Material template, GameObject displayModel, List<CharacterModel.RendererInfo> rendererInfos, BaseWeightSource weightSource, Behaviour[] baseBodyDynamics, Behaviour[] baseDisplayDynamics)
        {
            byte[] meshBytes = LeagueResources.Load(definition.folder, definition.meshFile);
            byte[] skeletonBytes = LeagueResources.Load(definition.folder, definition.skeletonFile);
            if (meshBytes == null || skeletonBytes == null)
            {
                Debug.LogError("[Katarina LeagueSkins] Missing embedded mesh or skeleton for " + definition.folder);
                return null;
            }

            LeagueMesh mesh = LeagueMesh.Read(meshBytes);
            LeagueSkeleton skeleton = LeagueSkeleton.Read(skeletonBytes);

            Dictionary<string, Transform> bodyBones = CollectBones(bodyModel);
            Transform rendererTransform = bodyRenderer.transform;
            Matrix4x4 leagueToRenderer = LeagueToRenderer(bodyModel, bodyRenderer);

            Dictionary<Transform, Matrix4x4> baseBindposes = new Dictionary<Transform, Matrix4x4>();
            Transform[] rendererBones = bodyRenderer.bones;
            Matrix4x4[] rendererBindposes = bodyRenderer.sharedMesh ? bodyRenderer.sharedMesh.bindposes : new Matrix4x4[0];
            for (int i = 0; i < rendererBones.Length && i < rendererBindposes.Length; i++)
            {
                if (rendererBones[i] && !baseBindposes.ContainsKey(rendererBones[i])) baseBindposes[rendererBones[i]] = rendererBindposes[i];
            }

            CheckAlignment(definition, bodyBones, baseBindposes, rendererTransform, leagueToRenderer);

            Transform fallback = bodyBones.TryGetValue(FallbackBone, out Transform root) ? root : bodyRenderer.rootBone;
            int jointCount = skeleton.names.Length;
            Transform[] jointTargets = new Transform[jointCount];
            Matrix4x4[] jointMatrices = new Matrix4x4[jointCount];
            bool[] jointIsExtra = new bool[jointCount];
            for (int j = 0; j < jointCount; j++)
            {
                int anchor = j;
                while (anchor >= 0 && !(baseSkeleton.index.ContainsKey(skeleton.names[anchor]) && bodyBones.ContainsKey(skeleton.names[anchor])))
                {
                    anchor = skeleton.parents[anchor];
                }

                jointIsExtra[j] = anchor != j;
                if (anchor >= 0)
                {
                    string name = skeleton.names[anchor];
                    Matrix4x4 baseRest = baseSkeleton.rest[baseSkeleton.index[name]];
                    Matrix4x4 skinRest = skeleton.rest[anchor];
                    jointTargets[j] = bodyBones[name];
                    if (Quaternion.Angle(baseRest.rotation, skinRest.rotation) > MaxRotationRebind)
                    {
                        Vector3 offset = (Vector3)baseRest.GetColumn(3) - (Vector3)skinRest.GetColumn(3);
                        jointMatrices[j] = leagueToRenderer * Matrix4x4.Translate(offset);
                    }
                    else
                    {
                        jointMatrices[j] = leagueToRenderer * baseRest * skinRest.inverse;
                    }
                }
                else
                {
                    jointTargets[j] = fallback;
                    jointMatrices[j] = leagueToRenderer;
                }
            }

            ShadowRig rig = UseShadowSkeleton ? ShadowRig.Create(definition, skeleton, baseSkeleton, bodyBones, baseBindposes, rendererTransform, leagueToRenderer, fallback) : null;
            List<GameObject> displayParts = new List<GameObject>();

            BuiltLeagueSkin built = new BuiltLeagueSkin { definition = definition };
            byte[] iconBytes = string.IsNullOrEmpty(definition.iconTexture) ? null : LeagueResources.Load(definition.folder, definition.iconTexture);
            if (iconBytes != null) built.icon = LeagueTextures.LoadIcon(iconBytes, definition.nameToken + "_Icon");

            Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> hidden = new HashSet<string>(definition.hiddenParts, StringComparer.OrdinalIgnoreCase);
            Transform displayRenderer = null;
            Dictionary<string, Transform> displayBones = null;
            if (displayModel)
            {
                displayRenderer = displayModel.transform.Find(PathFrom(bodyModel.transform, rendererTransform));
                displayBones = CollectBones(displayModel);
            }

            foreach (LeagueMeshPart part in mesh.parts)
            {
                if (hidden.Contains(part.name)) continue;

                List<Transform> bones = new List<Transform>();
                List<int> shadowJoints = new List<int>();
                Mesh unityMesh;
                if (rig != null)
                {
                    unityMesh = BuildShadowPartMesh(definition, part, mesh, skeleton, leagueToRenderer, shadowJoints);
                    Matrix4x4[] shadowBindposes = new Matrix4x4[shadowJoints.Count];
                    for (int b = 0; b < shadowJoints.Count; b++)
                    {
                        bones.Add(rig.BodyShadow(shadowJoints[b]));
                        shadowBindposes[b] = rig.Bindpose(shadowJoints[b]);
                    }
                    unityMesh.bindposes = shadowBindposes;
                }
                else
                {
                    unityMesh = BuildPartMesh(definition, part, mesh, skeleton, jointTargets, jointMatrices, jointIsExtra, weightSource, bones);
                    Matrix4x4[] bindposes = new Matrix4x4[bones.Count];
                    for (int b = 0; b < bones.Count; b++)
                    {
                        bindposes[b] = baseBindposes.TryGetValue(bones[b], out Matrix4x4 bindpose) ? bindpose : bones[b].worldToLocalMatrix * rendererTransform.localToWorldMatrix;
                    }
                    unityMesh.bindposes = bindposes;
                }

                Material material = GetMaterial(definition, part.name, template, materials);
                string objectName = "LeagueSkin_" + definition.folder + "_" + part.name;

                GameObject bodyPart = CreatePart(objectName, rendererTransform, unityMesh, material, bones.ToArray(), bodyRenderer.rootBone, bodyRenderer.localBounds);
                built.parts.Add(bodyPart);
                rendererInfos.Add(new CharacterModel.RendererInfo
                {
                    renderer = bodyPart.GetComponent<SkinnedMeshRenderer>(),
                    defaultMaterial = material,
                    defaultShadowCastingMode = ShadowCastingMode.On,
                    ignoreOverlays = false,
                    hideOnDeath = false
                });

                if (displayRenderer && displayBones != null)
                {
                    Transform[] displayPartBones = rig != null
                        ? shadowJoints.Select(j => rig.DisplayShadow(j, displayBones)).ToArray()
                        : bones.Select(b => displayBones.TryGetValue(b.name, out Transform t) ? t : null).ToArray();
                    Transform displayRoot = bodyRenderer.rootBone && displayBones.TryGetValue(bodyRenderer.rootBone.name, out Transform r) ? r : null;
                    displayParts.Add(CreatePart(objectName, displayRenderer, unityMesh, material, displayPartBones, displayRoot, bodyRenderer.localBounds));
                }
            }

            if (rig != null)
            {
                Dictionary<string, Matrix4x4> bindposesByName = new Dictionary<string, Matrix4x4>();
                foreach (KeyValuePair<string, Transform> pair in bodyBones)
                {
                    bindposesByName[pair.Key] = baseBindposes.TryGetValue(pair.Value, out Matrix4x4 bindpose) ? bindpose : pair.Value.worldToLocalMatrix * rendererTransform.localToWorldMatrix;
                }
                rig.AttachDriver(bodyModel, built.parts, false, null, bindposesByName, baseBodyDynamics);
                rig.AttachDriver(displayModel, displayParts, true, displayBones, bindposesByName, baseDisplayDynamics);
            }

            Debug.Log("[Katarina LeagueSkins] Built " + definition.folder + " with " + built.parts.Count + " parts");
            return built;
        }

        private static void CheckAlignment(LeagueSkinDefinition definition, Dictionary<string, Transform> bodyBones, Dictionary<Transform, Matrix4x4> baseBindposes, Transform rendererTransform, Matrix4x4 leagueToRenderer)
        {
            const string probe = "R_Hand";
            if (!bodyBones.TryGetValue(probe, out Transform bone) || !baseSkeleton.index.ContainsKey(probe)) return;

            Vector3 predicted = (leagueToRenderer * baseSkeleton.rest[baseSkeleton.index[probe]]).GetColumn(3);
            Vector3 actual = baseBindposes.TryGetValue(bone, out Matrix4x4 bindpose) ? (Vector3)bindpose.inverse.GetColumn(3) : rendererTransform.InverseTransformPoint(bone.position);
            float error = Vector3.Distance(predicted, actual);
            if (error > 0.05f)
            {
                Debug.LogWarning("[Katarina LeagueSkins] " + definition.folder + ": League skeleton does not line up with Katarina's rig (" + probe + " off by " + error + "). predicted " + predicted + " actual " + actual);
            }
        }

        private static Mesh BuildPartMesh(LeagueSkinDefinition definition, LeagueMeshPart part, LeagueMesh mesh, LeagueSkeleton skeleton, Transform[] jointTargets, Matrix4x4[] jointMatrices, bool[] jointIsExtra, BaseWeightSource weightSource, List<Transform> bones)
        {
            Dictionary<Transform, int> slots = new Dictionary<Transform, int>();
            Dictionary<int, int> localIndex = new Dictionary<int, int>();
            List<int> sourceVertices = new List<int>();
            bool relative = false;
            bool insideRange = true;
            bool belowCount = true;
            for (int i = 0; i < part.indexCount; i++)
            {
                int index = mesh.indices[part.indexStart + i];
                if (index < part.vertexStart || index >= part.vertexStart + part.vertexCount) insideRange = false;
                if (index >= part.vertexCount) belowCount = false;
            }
            if (!insideRange && belowCount) relative = true;

            int[] triangles = new int[part.indexCount];
            for (int i = 0; i < part.indexCount; i++)
            {
                int global = mesh.indices[part.indexStart + i] + (relative ? part.vertexStart : 0);
                if (!localIndex.TryGetValue(global, out int local))
                {
                    local = sourceVertices.Count;
                    sourceVertices.Add(global);
                    localIndex[global] = local;
                }
                triangles[i] = local;
            }
            int count = sourceVertices.Count;
            Vector3[] vertices = new Vector3[count];
            Vector3[] normals = new Vector3[count];
            Vector2[] uvs = new Vector2[count];
            BoneWeight[] boneWeights = new BoneWeight[count];

            List<Transform> weightBones = new List<Transform>(8);
            List<float> weightValues = new List<float>(8);
            int[] slotIds = new int[4];
            float[] slotWeights = new float[4];

            for (int v = 0; v < count; v++)
            {
                int source = sourceVertices[v];
                Vector3 position = mesh.positions[source];
                Vector3 normal = mesh.normals[source];
                Vector3 outPosition = Vector3.zero;
                Vector3 outNormal = Vector3.zero;
                float total = 0f;
                float extraWeight = 0f;
                weightBones.Clear();
                weightValues.Clear();

                for (int k = 0; k < 4; k++)
                {
                    float weight = mesh.weights[source * 4 + k];
                    if (weight <= 0f) continue;
                    int influence = mesh.boneIndices[source * 4 + k];
                    if (influence >= skeleton.influences.Length) continue;
                    int joint = skeleton.influences[influence];
                    if (joint >= jointMatrices.Length) continue;

                    Matrix4x4 matrix = jointMatrices[joint];
                    outPosition += weight * matrix.MultiplyPoint3x4(position);
                    outNormal += weight * matrix.MultiplyVector(normal);
                    total += weight;

                    if (jointIsExtra[joint] && weightSource != null) extraWeight += weight;
                    else AddWeight(weightBones, weightValues, jointTargets[joint], weight);
                }

                if (total <= 0f)
                {
                    outPosition = jointMatrices[0].MultiplyPoint3x4(position);
                    outNormal = jointMatrices[0].MultiplyVector(normal);
                    total = 1f;
                    AddWeight(weightBones, weightValues, jointTargets[0], 1f);
                }

                Vector3 finalPosition = outPosition / total;
                if (extraWeight > 0f)
                {
                    int nearest = weightSource.Nearest(finalPosition);
                    if (nearest >= 0)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            Transform bone = weightSource.bones[nearest * 4 + k];
                            float weight = weightSource.weights[nearest * 4 + k];
                            if (bone && weight > 0f) AddWeight(weightBones, weightValues, bone, extraWeight * weight);
                        }
                    }
                }
                if (weightBones.Count == 0) AddWeight(weightBones, weightValues, jointTargets[0], 1f);

                int used = 0;
                foreach (int i in Enumerable.Range(0, weightBones.Count).OrderByDescending(i => weightValues[i]).Take(4))
                {
                    Transform target = weightBones[i];
                    if (!slots.TryGetValue(target, out int slot))
                    {
                        slot = bones.Count;
                        bones.Add(target);
                        slots[target] = slot;
                    }
                    slotIds[used] = slot;
                    slotWeights[used] = weightValues[i];
                    used++;
                }

                vertices[v] = finalPosition;
                normals[v] = outNormal.normalized;
                uvs[v] = mesh.uvs[source];
                boneWeights[v] = ToBoneWeight(slotIds, slotWeights, used);
            }

            if (ReverseWinding)
            {
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    int swap = triangles[i + 1];
                    triangles[i + 1] = triangles[i + 2];
                    triangles[i + 2] = swap;
                }
            }

            Mesh unityMesh = new Mesh { name = "LeagueSkin_" + definition.folder + "_" + part.name };
            if (count > 65000) unityMesh.indexFormat = IndexFormat.UInt32;
            unityMesh.vertices = vertices;
            unityMesh.normals = normals;
            unityMesh.uv = uvs;
            unityMesh.boneWeights = boneWeights;
            unityMesh.triangles = triangles;
            unityMesh.RecalculateBounds();
            return unityMesh;
        }

        private static Mesh BuildShadowPartMesh(LeagueSkinDefinition definition, LeagueMeshPart part, LeagueMesh mesh, LeagueSkeleton skeleton, Matrix4x4 leagueToRenderer, List<int> joints)
        {
            Dictionary<int, int> slots = new Dictionary<int, int>();
            Dictionary<int, int> localIndex = new Dictionary<int, int>();
            List<int> sourceVertices = new List<int>();
            bool insideRange = true;
            bool belowCount = true;
            for (int i = 0; i < part.indexCount; i++)
            {
                int index = mesh.indices[part.indexStart + i];
                if (index < part.vertexStart || index >= part.vertexStart + part.vertexCount) insideRange = false;
                if (index >= part.vertexCount) belowCount = false;
            }
            bool relative = !insideRange && belowCount;

            int[] triangles = new int[part.indexCount];
            for (int i = 0; i < part.indexCount; i++)
            {
                int global = mesh.indices[part.indexStart + i] + (relative ? part.vertexStart : 0);
                if (!localIndex.TryGetValue(global, out int local))
                {
                    local = sourceVertices.Count;
                    sourceVertices.Add(global);
                    localIndex[global] = local;
                }
                triangles[i] = local;
            }

            int count = sourceVertices.Count;
            Vector3[] vertices = new Vector3[count];
            Vector3[] normals = new Vector3[count];
            Vector2[] uvs = new Vector2[count];
            BoneWeight[] boneWeights = new BoneWeight[count];
            int[] slotIds = new int[4];
            float[] slotWeights = new float[4];

            for (int v = 0; v < count; v++)
            {
                int source = sourceVertices[v];
                int used = 0;
                for (int k = 0; k < 4; k++)
                {
                    float weight = mesh.weights[source * 4 + k];
                    if (weight <= 0f) continue;
                    int influence = mesh.boneIndices[source * 4 + k];
                    if (influence >= skeleton.influences.Length) continue;
                    int joint = skeleton.influences[influence];
                    if (joint >= skeleton.names.Length) continue;

                    if (!slots.TryGetValue(joint, out int slot))
                    {
                        slot = joints.Count;
                        joints.Add(joint);
                        slots[joint] = slot;
                    }

                    int existing = Array.IndexOf(slotIds, slot, 0, used);
                    if (existing >= 0)
                    {
                        slotWeights[existing] += weight;
                    }
                    else
                    {
                        slotIds[used] = slot;
                        slotWeights[used] = weight;
                        used++;
                    }
                }

                if (used == 0)
                {
                    if (!slots.TryGetValue(0, out int slot))
                    {
                        slot = joints.Count;
                        joints.Add(0);
                        slots[0] = slot;
                    }
                    slotIds[0] = slot;
                    slotWeights[0] = 1f;
                    used = 1;
                }

                vertices[v] = leagueToRenderer.MultiplyPoint3x4(mesh.positions[source]);
                normals[v] = leagueToRenderer.MultiplyVector(mesh.normals[source]).normalized;
                uvs[v] = mesh.uvs[source];
                boneWeights[v] = ToBoneWeight(slotIds, slotWeights, used);
            }

            if (ReverseWinding)
            {
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    int swap = triangles[i + 1];
                    triangles[i + 1] = triangles[i + 2];
                    triangles[i + 2] = swap;
                }
            }

            Mesh unityMesh = new Mesh { name = "LeagueSkin_" + definition.folder + "_" + part.name };
            if (count > 65000) unityMesh.indexFormat = IndexFormat.UInt32;
            unityMesh.vertices = vertices;
            unityMesh.normals = normals;
            unityMesh.uv = uvs;
            unityMesh.boneWeights = boneWeights;
            unityMesh.triangles = triangles;
            unityMesh.RecalculateBounds();
            return unityMesh;
        }

        private static void AddWeight(List<Transform> bones, List<float> weights, Transform bone, float weight)
        {
            int index = bones.IndexOf(bone);
            if (index >= 0)
            {
                weights[index] += weight;
            }
            else
            {
                bones.Add(bone);
                weights.Add(weight);
            }
        }

        private static BoneWeight ToBoneWeight(int[] ids, float[] weights, int used)
        {
            int[] order = Enumerable.Range(0, used).OrderByDescending(i => weights[i]).ToArray();
            float sum = 0f;
            foreach (int i in order) sum += weights[i];
            if (sum <= 0f) sum = 1f;

            BoneWeight result = new BoneWeight();
            if (order.Length > 0) { result.boneIndex0 = ids[order[0]]; result.weight0 = weights[order[0]] / sum; }
            if (order.Length > 1) { result.boneIndex1 = ids[order[1]]; result.weight1 = weights[order[1]] / sum; }
            if (order.Length > 2) { result.boneIndex2 = ids[order[2]]; result.weight2 = weights[order[2]] / sum; }
            if (order.Length > 3) { result.boneIndex3 = ids[order[3]]; result.weight3 = weights[order[3]] / sum; }
            return result;
        }

        internal static Material GetMaterial(LeagueSkinDefinition definition, string partName, Material template, Dictionary<string, Material> cache)
        {
            string file = definition.partTextures.TryGetValue(partName, out string mapped) ? mapped : definition.defaultTexture;
            if (cache.TryGetValue(file, out Material cached)) return cached;

            byte[] bytes = LeagueResources.Load(definition.folder, file);
            if (bytes == null && file != definition.defaultTexture)
            {
                Debug.LogWarning("[Katarina LeagueSkins] Missing texture " + file + " for " + definition.folder + "/" + partName + ", using " + definition.defaultTexture);
                file = definition.defaultTexture;
                if (cache.TryGetValue(file, out cached)) return cached;
                bytes = LeagueResources.Load(definition.folder, file);
            }

            Texture2D texture = LeagueTextures.Load(bytes, file, out bool hasAlpha);
            if (!texture) Debug.LogError("[Katarina LeagueSkins] Could not load texture " + definition.folder + "/" + file);

            Material material = template ? new Material(template) : new Material(Assets.hotpoo);
            material.name = "LeagueSkin_" + definition.folder + "_" + System.IO.Path.GetFileNameWithoutExtension(file);
            material.mainTexture = texture;
            if (material.HasProperty("_NormalTex")) material.SetTexture("_NormalTex", null);
            if (material.HasProperty("_EmTex")) material.SetTexture("_EmTex", null);
            if (material.HasProperty("_EmColor")) material.SetColor("_EmColor", Color.black);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (hasAlpha)
            {
                material.EnableKeyword("CUTOUT");
                if (material.HasProperty("_EnableCutout")) material.SetFloat("_EnableCutout", 1f);
                if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            }

            cache[file] = material;
            return material;
        }

        private static GameObject CreatePart(string objectName, Transform template, Mesh mesh, Material material, Transform[] bones, Transform rootBone, Bounds localBounds)
        {
            GameObject part = new GameObject(objectName);
            part.transform.SetParent(template.parent, false);
            part.transform.localPosition = template.localPosition;
            part.transform.localRotation = template.localRotation;
            part.transform.localScale = template.localScale;
            part.layer = template.gameObject.layer;

            SkinnedMeshRenderer renderer = part.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.bones = bones;
            renderer.rootBone = rootBone;
            renderer.localBounds = localBounds;
            renderer.updateWhenOffscreen = false;

            part.SetActive(false);
            return part;
        }
    }

    internal class BaseWeightSource
    {
        internal Vector3[] positions;
        internal Transform[] bones;
        internal float[] weights;
        private float cellSize;
        private readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
        private const int MaxRings = 30;

        internal static BaseWeightSource Create(LeagueSkeleton baseSkeleton, GameObject bodyModel, SkinnedMeshRenderer bodyRenderer)
        {
            byte[] bytes = LeagueResources.Load(LeagueSkinBuilder.BaseFolder, LeagueSkinBuilder.BaseMeshFile);
            if (bytes == null)
            {
                Debug.LogWarning("[Katarina LeagueSkins] Missing embedded base mesh, extra bones will stay rigid");
                return null;
            }

            LeagueMesh baseMesh = LeagueMesh.Read(bytes);
            Dictionary<string, Transform> bodyBones = LeagueSkinBuilder.Bones(bodyModel);
            Matrix4x4 leagueToRenderer = LeagueSkinBuilder.LeagueToRenderer(bodyModel, bodyRenderer);
            HashSet<string> excluded = new HashSet<string>(LeagueSkinBuilder.BaseExcludedParts, StringComparer.OrdinalIgnoreCase);

            List<int> vertexList = new List<int>();
            foreach (LeagueMeshPart part in baseMesh.parts)
            {
                if (excluded.Contains(part.name)) continue;
                for (int v = part.vertexStart; v < part.vertexStart + part.vertexCount && v < baseMesh.positions.Length; v++) vertexList.Add(v);
            }

            BaseWeightSource result = new BaseWeightSource
            {
                positions = new Vector3[vertexList.Count],
                bones = new Transform[vertexList.Count * 4],
                weights = new float[vertexList.Count * 4]
            };

            Bounds bounds = new Bounds();
            for (int i = 0; i < vertexList.Count; i++)
            {
                int source = vertexList[i];
                Vector3 position = leagueToRenderer.MultiplyPoint3x4(baseMesh.positions[source]);
                result.positions[i] = position;
                if (i == 0) bounds = new Bounds(position, Vector3.zero);
                else bounds.Encapsulate(position);

                for (int k = 0; k < 4; k++)
                {
                    float weight = baseMesh.weights[source * 4 + k];
                    int influence = baseMesh.boneIndices[source * 4 + k];
                    if (weight <= 0f || influence >= baseSkeleton.influences.Length) continue;
                    int joint = baseSkeleton.influences[influence];
                    if (joint >= baseSkeleton.names.Length) continue;
                    string jointName = baseSkeleton.names[joint];
                    if (IsIgnored(jointName)) continue;
                    if (bodyBones.TryGetValue(jointName, out Transform bone))
                    {
                        result.bones[i * 4 + k] = bone;
                        result.weights[i * 4 + k] = weight;
                    }
                }

                float kept = 0f;
                for (int k = 0; k < 4; k++) kept += result.weights[i * 4 + k];
                if (kept >= 0.5f)
                {
                    for (int k = 0; k < 4; k++) result.weights[i * 4 + k] /= kept;
                }
                else
                {
                    for (int k = 0; k < 4; k++)
                    {
                        result.bones[i * 4 + k] = null;
                        result.weights[i * 4 + k] = 0f;
                    }
                }
            }

            result.cellSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / 40f;
            if (result.cellSize <= 0f) result.cellSize = 0.05f;
            for (int i = 0; i < result.positions.Length; i++)
            {
                if (result.weights[i * 4] <= 0f && result.weights[i * 4 + 1] <= 0f && result.weights[i * 4 + 2] <= 0f && result.weights[i * 4 + 3] <= 0f) continue;
                long key = result.Key(result.Cell(result.positions[i].x), result.Cell(result.positions[i].y), result.Cell(result.positions[i].z));
                if (!result.grid.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>();
                    result.grid[key] = list;
                }
                list.Add(i);
            }

            return result;
        }

        private static bool IsIgnored(string jointName)
        {
            foreach (string keyword in LeagueSkinBuilder.TransferIgnoredBoneKeywords)
            {
                if (jointName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private int Cell(float value)
        {
            return Mathf.FloorToInt(value / this.cellSize);
        }

        private long Key(int x, int y, int z)
        {
            return ((long)(x & 0x1FFFFF) << 42) | ((long)(y & 0x1FFFFF) << 21) | (long)(z & 0x1FFFFF);
        }

        internal int Nearest(Vector3 point)
        {
            int cx = this.Cell(point.x);
            int cy = this.Cell(point.y);
            int cz = this.Cell(point.z);
            int best = -1;
            float bestDistance = float.MaxValue;

            for (int ring = 0; ring <= MaxRings; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    for (int dy = -ring; dy <= ring; dy++)
                    {
                        for (int dz = -ring; dz <= ring; dz++)
                        {
                            if (Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz))) != ring) continue;
                            if (!this.grid.TryGetValue(this.Key(cx + dx, cy + dy, cz + dz), out List<int> list)) continue;
                            foreach (int i in list)
                            {
                                float distance = (this.positions[i] - point).sqrMagnitude;
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    best = i;
                                }
                            }
                        }
                    }
                }

                if (best >= 0)
                {
                    float reach = ring * this.cellSize;
                    if (bestDistance <= reach * reach) break;
                }
            }

            return best;
        }
    }
}
