using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueSkins
{
    internal class ShadowRig
    {
        private const float MinCorrectionBoneLength = 5f;

        internal int count;
        internal string[] names;
        internal int[] parents;
        internal int[] order;
        internal bool[] mapped;
        internal bool[] dynamic;
        internal Transform[] anchors;
        internal Matrix4x4[] anchorBind;
        internal Matrix4x4[] rotation;
        internal Matrix4x4[] chainBind;
        internal Vector3[] chainLocal;
        internal Vector3[] rest;
        internal Vector3[] baseRest;
        private readonly Side body = new Side();
        private readonly Side display = new Side();
        private string label;
        private LeagueSkinDefinition definition;
        private Matrix4x4 leagueToRenderer;

        private class Side
        {
            internal Transform[] driven;
            internal Transform[] bones;
            internal Dictionary<string, Transform> lookup;
        }

        internal static ShadowRig Create(LeagueSkinDefinition definition, LeagueSkeleton skeleton, LeagueSkeleton baseSkeleton, Dictionary<string, Transform> bodyBones, Dictionary<Transform, Matrix4x4> baseBindposes, Transform rendererTransform, Matrix4x4 leagueToRenderer, Transform fallback)
        {
            int n = skeleton.names.Length;
            ShadowRig rig = new ShadowRig
            {
                count = n,
                names = skeleton.names,
                parents = skeleton.parents,
                mapped = new bool[n],
                dynamic = new bool[n],
                anchors = new Transform[n],
                anchorBind = new Matrix4x4[n],
                rotation = new Matrix4x4[n],
                chainBind = new Matrix4x4[n],
                chainLocal = new Vector3[n],
                rest = new Vector3[n],
                baseRest = new Vector3[n],
                label = definition.folder,
                definition = definition,
                leagueToRenderer = leagueToRenderer
            };
            rig.body.driven = new Transform[n];
            rig.body.bones = new Transform[n];
            rig.display.driven = new Transform[n];
            rig.display.bones = new Transform[n];
            rig.order = TopologicalOrder(skeleton.parents);

            foreach (DynamicChainDefinition chain in definition.dynamicChains)
            {
                if (!skeleton.index.TryGetValue(chain.root, out int root))
                {
                    Debug.LogWarning("[Katarina LeagueSkins] " + definition.folder + ": dynamic chain root " + chain.root + " not in skeleton");
                    continue;
                }
                rig.dynamic[root] = true;
            }
            foreach (int j in rig.order)
            {
                int p = skeleton.parents[j];
                if (p >= 0 && rig.dynamic[p]) rig.dynamic[j] = true;
            }

            int[] baseIndex = new int[n];
            int[] anchorJoint = new int[n];
            for (int j = 0; j < n; j++)
            {
                rig.rest[j] = leagueToRenderer.MultiplyPoint3x4(skeleton.rest[j].GetColumn(3));
                baseIndex[j] = -1;
                if (!rig.dynamic[j] && baseSkeleton.index.TryGetValue(skeleton.names[j], out int b) && bodyBones.ContainsKey(skeleton.names[j]))
                {
                    baseIndex[j] = b;
                    rig.mapped[j] = true;
                    rig.baseRest[j] = leagueToRenderer.MultiplyPoint3x4(baseSkeleton.rest[b].GetColumn(3));
                }
            }

            foreach (int j in rig.order)
            {
                int a = j;
                while (a >= 0 && !rig.mapped[a]) a = skeleton.parents[a];
                anchorJoint[j] = a;
                Transform bone = a >= 0 ? bodyBones[skeleton.names[a]] : fallback;
                rig.anchors[j] = bone;
                rig.anchorBind[j] = baseBindposes.TryGetValue(bone, out Matrix4x4 bindpose) ? bindpose : bone.worldToLocalMatrix * rendererTransform.localToWorldMatrix;
            }

            Quaternion[] correction = new Quaternion[n];
            foreach (int j in rig.order)
            {
                correction[j] = Quaternion.identity;
                if (!rig.mapped[j] || IsIgnored(skeleton.names[j])) continue;

                int parentMapped = skeleton.parents[j];
                while (parentMapped >= 0 && !rig.mapped[parentMapped]) parentMapped = skeleton.parents[parentMapped];
                Quaternion inherited = parentMapped >= 0 ? correction[parentMapped] : Quaternion.identity;

                List<int> children = new List<int>();
                for (int c = 0; c < n; c++)
                {
                    if (skeleton.parents[c] != j || !rig.mapped[c] || IsIgnored(skeleton.names[c])) continue;
                    if (baseSkeleton.parents[baseIndex[c]] != baseIndex[j]) continue;
                    children.Add(c);
                }

                if (children.Count == 1)
                {
                    int c = children[0];
                    Vector3 skinBone = skeleton.rest[c].GetColumn(3) - skeleton.rest[j].GetColumn(3);
                    Vector3 baseBone = baseSkeleton.rest[baseIndex[c]].GetColumn(3) - baseSkeleton.rest[baseIndex[j]].GetColumn(3);
                    if (skinBone.magnitude >= MinCorrectionBoneLength && baseBone.magnitude >= MinCorrectionBoneLength)
                    {
                        correction[j] = Quaternion.FromToRotation(rig.rest[c] - rig.rest[j], rig.baseRest[c] - rig.baseRest[j]);
                        continue;
                    }
                }
                correction[j] = inherited;
            }

            for (int j = 0; j < n; j++)
            {
                Quaternion q = anchorJoint[j] >= 0 ? correction[anchorJoint[j]] : Quaternion.identity;
                rig.rotation[j] = LeagueSkinBuilder.AlignBindPose ? Matrix4x4.Rotate(q) : Matrix4x4.identity;
            }

            foreach (int j in rig.order)
            {
                if (!rig.dynamic[j]) continue;
                int p = skeleton.parents[j];
                if (p < 0 || !rig.dynamic[p])
                {
                    Matrix4x4 holderBind = rig.anchorBind[j] * rig.rotation[j];
                    rig.chainLocal[j] = holderBind.MultiplyPoint3x4(rig.rest[j]);
                    rig.chainBind[j] = Matrix4x4.Translate(-rig.chainLocal[j]) * holderBind;
                }
                else
                {
                    rig.chainLocal[j] = rig.chainBind[p].MultiplyPoint3x4(rig.rest[j]);
                    rig.chainBind[j] = Matrix4x4.Translate(-rig.chainLocal[j]) * rig.chainBind[p];
                }
            }

            return rig;
        }

        private static bool IsIgnored(string name)
        {
            foreach (string keyword in LeagueSkinBuilder.CorrectionIgnoredBoneKeywords)
            {
                if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static int[] TopologicalOrder(int[] parents)
        {
            List<int> order = new List<int>(parents.Length);
            bool[] done = new bool[parents.Length];
            for (int i = 0; i < parents.Length; i++) Visit(i, parents, done, order, 0);
            return order.ToArray();
        }

        private static void Visit(int i, int[] parents, bool[] done, List<int> order, int depth)
        {
            if (done[i] || depth > parents.Length) return;
            int p = parents[i];
            if (p >= 0 && p < parents.Length) Visit(p, parents, done, order, depth + 1);
            if (done[i]) return;
            done[i] = true;
            order.Add(i);
        }

        internal Matrix4x4 Bindpose(int j)
        {
            return this.dynamic[j] ? this.chainBind[j] : this.anchorBind[j] * this.rotation[j];
        }

        internal Transform BodyShadow(int j)
        {
            return this.Shadow(this.body, j, null);
        }

        internal Transform DisplayShadow(int j, Dictionary<string, Transform> displayBones)
        {
            return this.Shadow(this.display, j, displayBones);
        }

        private Transform Shadow(Side side, int j, Dictionary<string, Transform> lookup)
        {
            if (side.bones[j]) return side.bones[j];

            if (this.dynamic[j] && this.parents[j] >= 0 && this.dynamic[this.parents[j]])
            {
                Transform parent = this.Shadow(side, this.parents[j], lookup);
                if (!parent) return null;
                side.bones[j] = CreateChild(parent, "LeagueDyn_" + this.label + "_" + this.names[j], this.chainLocal[j]);
                return side.bones[j];
            }

            Transform anchor = this.anchors[j];
            if (lookup != null) anchor = anchor && lookup.TryGetValue(anchor.name, out Transform t) ? t : null;
            if (!anchor) return null;

            side.driven[j] = CreateChild(anchor, "LeagueShadow_" + this.label + "_" + this.names[j], Vector3.zero);
            side.bones[j] = this.dynamic[j]
                ? CreateChild(side.driven[j], "LeagueDyn_" + this.label + "_" + this.names[j], this.chainLocal[j])
                : side.driven[j];
            return side.bones[j];
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject shadow = new GameObject(name);
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = localPosition;
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = Vector3.one;
            return shadow.transform;
        }

        internal void AttachDriver(GameObject model, List<GameObject> parts, bool isDisplay, Dictionary<string, Transform> displayBones, Dictionary<string, Matrix4x4> bindposesByName, Behaviour[] baseDynamics)
        {
            if (!model || parts.Count == 0) return;
            Side side = isDisplay ? this.display : this.body;
            Dictionary<string, Transform> lookup = isDisplay ? displayBones : null;

            Transform[] anchors = this.anchors;
            if (isDisplay)
            {
                anchors = new Transform[this.count];
                for (int j = 0; j < this.count; j++)
                {
                    anchors[j] = this.anchors[j] && displayBones != null && displayBones.TryGetValue(this.anchors[j].name, out Transform t) ? t : null;
                }
            }

            for (int j = 0; j < this.count; j++)
            {
                if (this.dynamic[j]) this.Shadow(side, j, lookup);
            }

            List<Behaviour> dynamics = new List<Behaviour>();
            if (LeagueSkinBuilder.EnableDynamicBones)
            {
                Dictionary<string, Transform> bones = isDisplay ? displayBones : LeagueSkinBuilder.Bones(model);
                foreach (DynamicChainDefinition chain in this.definition.dynamicChains)
                {
                    int root = Array.IndexOf(this.names, chain.root);
                    if (root < 0 || !side.bones[root]) continue;
                    Behaviour dynamicBone = DynamicBones.Add(model, side.bones[root], chain, bones, bindposesByName, this.leagueToRenderer);
                    if (dynamicBone) dynamics.Add(dynamicBone);
                }
            }

            LeagueShadowSkeleton driver = model.AddComponent<LeagueShadowSkeleton>();
            driver.anchors = anchors;
            driver.anchorBind = (Matrix4x4[])this.anchorBind.Clone();
            driver.rotation = (Matrix4x4[])this.rotation.Clone();
            driver.rest = (Vector3[])this.rest.Clone();
            driver.baseRest = (Vector3[])this.baseRest.Clone();
            driver.parents = (int[])this.parents.Clone();
            driver.order = (int[])this.order.Clone();
            driver.mapped = (bool[])this.mapped.Clone();
            driver.shadows = (Transform[])side.driven.Clone();
            driver.parts = parts.ToArray();
            driver.dynamics = dynamics.ToArray();
            driver.baseDynamics = baseDynamics ?? new Behaviour[0];
        }
    }

    internal static class DynamicBones
    {
        private static Type boneType;
        private static Type colliderType;
        private static bool resolved;
        private static readonly Dictionary<int, Dictionary<string, Component>> colliderCache = new Dictionary<int, Dictionary<string, Component>>();

        private static bool Resolve()
        {
            if (resolved) return boneType != null && colliderType != null;
            resolved = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (boneType == null) boneType = assembly.GetType("DynamicBone", false);
                if (colliderType == null) colliderType = assembly.GetType("DynamicBoneCollider", false);
            }
            if (boneType == null || colliderType == null) Debug.LogWarning("[Katarina LeagueSkins] DynamicBone types not found, cloth and hair will stay rigid");
            return boneType != null && colliderType != null;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (info == null)
            {
                Debug.LogWarning("[Katarina LeagueSkins] DynamicBone has no field " + field);
                return;
            }
            try
            {
                info.SetValue(target, value);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Katarina LeagueSkins] Could not set DynamicBone." + field + ": " + e.Message);
            }
        }

        internal static Behaviour[] Existing(GameObject model)
        {
            if (!model || !Resolve()) return new Behaviour[0];
            List<Behaviour> result = new List<Behaviour>();
            foreach (Component component in model.GetComponentsInChildren(boneType, true))
            {
                if (component is Behaviour behaviour) result.Add(behaviour);
            }
            return result.ToArray();
        }

        internal static Behaviour Add(GameObject model, Transform root, DynamicChainDefinition chain, Dictionary<string, Transform> bones, Dictionary<string, Matrix4x4> bindposesByName, Matrix4x4 leagueToRenderer)
        {
            if (!Resolve()) return null;

            Behaviour dynamicBone = (Behaviour)model.AddComponent(boneType);
            dynamicBone.enabled = false;
            Set(dynamicBone, "m_Root", root);
            Set(dynamicBone, "m_UpdateRate", 60f);
            Set(dynamicBone, "m_Damping", chain.damping);
            Set(dynamicBone, "m_Elasticity", chain.elasticity);
            Set(dynamicBone, "m_Stiffness", chain.stiffness);
            Set(dynamicBone, "m_Inert", chain.inert);
            Set(dynamicBone, "m_Radius", chain.radius * LeagueSkinBuilder.LeagueUnitScale);
            Set(dynamicBone, "m_Gravity", chain.gravity);

            FieldInfo collidersField = boneType.GetField("m_Colliders", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (collidersField != null && chain.colliders != null && chain.colliders.Length > 0)
            {
                IList list = collidersField.GetValue(dynamicBone) as IList;
                if (list == null)
                {
                    list = (IList)Activator.CreateInstance(collidersField.FieldType);
                    collidersField.SetValue(dynamicBone, list);
                }
                Dictionary<string, Component> colliders = Colliders(model, bones, bindposesByName, leagueToRenderer);
                foreach (string bone in chain.colliders)
                {
                    foreach (KeyValuePair<string, Component> pair in colliders)
                    {
                        if (pair.Key.StartsWith(bone + "#", StringComparison.Ordinal)) list.Add(pair.Value);
                    }
                }
            }
            return dynamicBone;
        }

        private static Dictionary<string, Component> Colliders(GameObject model, Dictionary<string, Transform> bones, Dictionary<string, Matrix4x4> bindposesByName, Matrix4x4 leagueToRenderer)
        {
            int key = model.GetInstanceID();
            if (colliderCache.TryGetValue(key, out Dictionary<string, Component> cached)) return cached;

            Dictionary<string, Component> result = new Dictionary<string, Component>();
            for (int i = 0; i < LeagueSkinCatalog.BodyColliders.Length; i++)
            {
                BodyColliderDefinition definition = LeagueSkinCatalog.BodyColliders[i];
                if (!bones.TryGetValue(definition.bone, out Transform bone) || !bindposesByName.TryGetValue(definition.bone, out Matrix4x4 bindpose)) continue;

                Vector3 center = bindpose.MultiplyPoint3x4(leagueToRenderer.MultiplyPoint3x4(definition.center));
                float radius = definition.radius * bindpose.MultiplyVector(leagueToRenderer.MultiplyVector(Vector3.right)).magnitude;

                Component collider = bone.gameObject.AddComponent(colliderType);
                Set(collider, "m_Center", center);
                Set(collider, "m_Radius", radius);
                Set(collider, "m_Height", 0f);
                result[definition.bone + "#" + i] = collider;
            }
            colliderCache[key] = result;
            return result;
        }
    }

    [DefaultExecutionOrder(-50)]
    public class LeagueShadowSkeleton : MonoBehaviour
    {
        private const float TranslationTolerance = 0.1f;
        private const float MinBoneLength = 0.01f;

        public Transform[] anchors;
        public Matrix4x4[] anchorBind;
        public Matrix4x4[] rotation;
        public Vector3[] rest;
        public Vector3[] baseRest;
        public int[] parents;
        public int[] order;
        public bool[] mapped;
        public Transform[] shadows;
        public GameObject[] parts;
        public Behaviour[] dynamics;
        public Behaviour[] baseDynamics;

        private Vector3[] position;
        private Matrix4x4[] linear;
        private Matrix4x4[] animated;
        private bool dynamicsOn;
        private LeagueShadowSkeleton[] siblings;

        private void LateUpdate()
        {
            if (this.parts == null || this.anchors == null) return;
            bool visible = false;
            for (int i = 0; i < this.parts.Length; i++)
            {
                if (this.parts[i] && this.parts[i].activeInHierarchy)
                {
                    visible = true;
                    break;
                }
            }
            this.SetDynamics(visible);
            this.UpdateBaseDynamics(visible);
            if (!visible) return;

            int n = this.anchors.Length;
            if (this.position == null || this.position.Length != n)
            {
                this.position = new Vector3[n];
                this.linear = new Matrix4x4[n];
                this.animated = new Matrix4x4[n];
            }

            for (int k = 0; k < this.order.Length; k++)
            {
                int j = this.order[k];
                Transform anchor = this.anchors[j];
                if (!anchor) continue;

                Matrix4x4 a = anchor.localToWorldMatrix * this.anchorBind[j];
                Matrix4x4 l = a * this.rotation[j];
                int p = this.parents[j];
                Vector3 x;

                if (p < 0 || !this.anchors[p])
                {
                    x = a.MultiplyPoint3x4(this.rest[j]);
                }
                else if (this.mapped[j] && this.mapped[p])
                {
                    Vector3 animatedOffset = anchor.position - this.anchors[p].position;
                    float bindLength = this.animated[p].MultiplyVector(this.baseRest[j] - this.baseRest[p]).magnitude;
                    if (bindLength > MinBoneLength && Mathf.Abs(animatedOffset.magnitude / bindLength - 1f) > TranslationTolerance)
                    {
                        x = this.position[p] + animatedOffset;
                    }
                    else
                    {
                        x = this.position[p] + this.linear[p].MultiplyVector(this.rest[j] - this.rest[p]);
                    }
                }
                else
                {
                    x = this.position[p] + this.linear[p].MultiplyVector(this.rest[j] - this.rest[p]);
                }

                this.position[j] = x;
                this.linear[j] = l;
                this.animated[j] = a;

                Transform shadow = this.shadows[j];
                if (shadow) shadow.localPosition = anchor.InverseTransformVector(x - l.MultiplyPoint3x4(this.rest[j]));
            }
        }

        internal bool PartsVisible()
        {
            if (this.parts == null) return false;
            for (int i = 0; i < this.parts.Length; i++)
            {
                if (this.parts[i] && this.parts[i].activeInHierarchy) return true;
            }
            return false;
        }

        private void UpdateBaseDynamics(bool visible)
        {
            if (this.baseDynamics == null || this.baseDynamics.Length == 0) return;
            bool leagueSkinShown = visible;
            if (!leagueSkinShown)
            {
                if (this.siblings == null) this.siblings = this.GetComponents<LeagueShadowSkeleton>();
                for (int i = 0; i < this.siblings.Length && !leagueSkinShown; i++)
                {
                    if (this.siblings[i] && this.siblings[i] != this && this.siblings[i].PartsVisible()) leagueSkinShown = true;
                }
            }
            for (int i = 0; i < this.baseDynamics.Length; i++)
            {
                Behaviour behaviour = this.baseDynamics[i];
                if (behaviour && behaviour.enabled == leagueSkinShown) behaviour.enabled = !leagueSkinShown;
            }
        }

        private void SetDynamics(bool on)
        {
            if (this.dynamics == null || on == this.dynamicsOn) return;
            this.dynamicsOn = on;
            for (int i = 0; i < this.dynamics.Length; i++)
            {
                if (this.dynamics[i]) this.dynamics[i].enabled = on;
            }
        }

        private void OnDisable()
        {
            this.SetDynamics(false);
        }
    }
}
