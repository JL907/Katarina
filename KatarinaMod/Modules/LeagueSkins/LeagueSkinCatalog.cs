using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueSkins
{
    internal static class LeagueSkinCatalog
    {
        // Katarina's base skin has no emote/recall props; each skin lists its own hidden parts below.
        private static readonly string[] EmoteProps = new string[0];

        // Body colliders for DynamicBone, fitted to the base Katarina mesh (League units, base bind pose).
        internal static readonly BodyColliderDefinition[] BodyColliders =
        {
            Collider("Head", -0.4f, 149.3f, 8.1f, 6.0f),
            Collider("Neck", -0.1f, 139.4f, 3.1f, 3.4f),
            Collider("Spine2", 0.0f, 120.8f, 11.3f, 9.3f),
            Collider("Spine2", 0.9f, 130.3f, 8.1f, 4.7f),
            Collider("Spine1", 0.0f, 108.3f, 9.5f, 7.1f),
            Collider("Pelvis", 0.2f, 95.6f, 6.7f, 9.8f),
            Collider("L_Clavicle", -9.5f, 134.0f, 3.8f, 2.7f),
            Collider("L_Shoulder", -17.4f, 125.8f, 2.3f, 4.6f),
            Collider("L_Shoulder", -21.6f, 113.6f, 1.3f, 4.0f),
            Collider("L_Elbow", -27.6f, 103.1f, 1.4f, 3.8f),
            Collider("L_Elbow", -31.4f, 92.6f, 3.1f, 3.1f),
            Collider("L_Hip", -11.5f, 82.0f, 10.2f, 7.3f),
            Collider("L_Hip", -14.4f, 68.6f, 11.3f, 5.8f),
            Collider("L_Hip", -15.1f, 54.9f, 7.5f, 5.0f),
            Collider("L_KneeLower", -19.1f, 43.1f, 2.8f, 5.9f),
            Collider("L_KneeLower", -21.1f, 31.3f, 1.0f, 5.3f),
            Collider("L_KneeLower", -22.4f, 19.2f, 0.3f, 4.1f),
            Collider("R_Clavicle", 9.5f, 133.9f, 3.7f, 2.8f),
            Collider("R_Shoulder", 17.4f, 125.8f, 2.3f, 4.6f),
            Collider("R_Shoulder", 21.6f, 113.6f, 1.3f, 4.0f),
            Collider("R_Elbow", 27.6f, 103.1f, 1.4f, 3.8f),
            Collider("R_Elbow", 31.4f, 92.6f, 3.1f, 3.1f),
            Collider("R_Hip", 11.1f, 81.9f, 10.5f, 7.2f),
            Collider("R_Hip", 14.3f, 68.5f, 11.3f, 5.6f),
            Collider("R_Hip", 15.2f, 54.9f, 7.9f, 5.1f),
            Collider("R_KneeLower", 19.1f, 43.1f, 2.8f, 5.9f),
            Collider("R_KneeLower", 21.1f, 31.3f, 1.0f, 5.3f),
            Collider("R_KneeLower", 22.4f, 19.2f, 0.3f, 4.1f)
        };

        // Which body colliders each kind of chain collides with.
        private static readonly string[] HairColliders = { "Head", "Neck", "Spine2", "Spine1", "Pelvis", "L_Clavicle", "R_Clavicle", "L_Shoulder", "R_Shoulder" };
        private static readonly string[] BangColliders = { "Neck", "L_Clavicle", "R_Clavicle", "L_Shoulder", "R_Shoulder" };
        private static readonly string[] SashColliders = { "Spine2", "Spine1", "Pelvis", "L_Clavicle", "R_Clavicle", "L_Shoulder", "R_Shoulder", "L_Elbow", "R_Elbow" };
        private static readonly string[] SkirtColliders = { "L_Hip", "R_Hip", "L_KneeLower", "R_KneeLower" };
        private static readonly string[] CoatColliders = { "Pelvis", "L_Hip", "R_Hip", "L_KneeLower", "R_KneeLower" };

        // Long hair: starts from the base skin's own DynamicBone tuning (damping .611, elasticity .085, stiffness .037).
        private static DynamicChainDefinition Hair(string root)
        {
            return Chain(root, 0.6f, 0.085f, 0.04f, 0.2f, 2f, Vector3.zero, HairColliders);
        }

        // Short front locks: stiffer so they keep their shape around the face.
        private static DynamicChainDefinition Bang(string root)
        {
            return Chain(root, 0.5f, 0.2f, 0.5f, 0.3f, 1.5f, Vector3.zero, BangColliders);
        }

        private static DynamicChainDefinition Sash(string root)
        {
            return Chain(root, 0.3f, 0.06f, 0.08f, 0.3f, 2.5f, new Vector3(0f, -0.0015f, 0f), SashColliders);
        }

        private static DynamicChainDefinition Skirt(string root)
        {
            return Chain(root, 0.35f, 0.12f, 0.3f, 0.3f, 1f, new Vector3(0f, -0.001f, 0f), SkirtColliders);
        }

        // Coat tails hanging behind the legs: a little looser than skirt panels so they trail when running.
        private static DynamicChainDefinition Coat(string root)
        {
            return Chain(root, 0.3f, 0.08f, 0.2f, 0.3f, 2f, new Vector3(0f, -0.0012f, 0f), CoatColliders);
        }

        private static DynamicChainDefinition Chain(string root, float damping, float elasticity, float stiffness, float inert, float radius, Vector3 gravity, string[] colliders)
        {
            return new DynamicChainDefinition
            {
                root = root,
                damping = damping,
                elasticity = elasticity,
                stiffness = stiffness,
                inert = inert,
                radius = radius,
                gravity = gravity,
                colliders = colliders
            };
        }

        private static BodyColliderDefinition Collider(string bone, float x, float y, float z, float radius)
        {
            return new BodyColliderDefinition { bone = bone, center = new Vector3(x, y, z), radius = radius };
        }

        private static LeagueSkinDefinition Dynamics(LeagueSkinDefinition definition, params DynamicChainDefinition[] chains)
        {
            definition.dynamicChains = chains;
            return definition;
        }

        // Katarina's standard hair rig (used by most skins): ponytail with side locks, a bang and a back lock.
        private static DynamicChainDefinition[] StandardHair()
        {
            return new[] { Hair("Hair_M1"), Bang("Hair_R_bang1"), Hair("Hair_R_Back1") };
        }

        private static DynamicChainDefinition[] Join(DynamicChainDefinition[] a, params DynamicChainDefinition[] b)
        {
            List<DynamicChainDefinition> list = new List<DynamicChainDefinition>(a);
            list.AddRange(b);
            return list.ToArray();
        }

        private static LeagueSkinDefinition Skin(int id, string displayName, string defaultTexture, Dictionary<string, string> partTextures, params string[] hiddenParts)
        {
            string number = id.ToString("00");
            List<string> hidden = new List<string>(EmoteProps);
            hidden.AddRange(hiddenParts);
            return new LeagueSkinDefinition
            {
                folder = "Skin" + number,
                meshFile = "katarina_skin" + number + ".skn",
                skeletonFile = "katarina_skin" + number + ".skl",
                nameToken = "KATARINA_SKIN" + number + "_NAME",
                displayName = displayName,
                iconTexture = "katarinaloadscreen_" + id + ".tex",
                defaultTexture = defaultTexture,
                partTextures = partTextures ?? new Dictionary<string, string>(),
                hiddenParts = hidden.ToArray()
            };
        }

        internal static readonly LeagueSkinDefinition[] definitions =
        {
            Dynamics(Skin(10, "DEATH SWORN KATARINA", "katarina_skin10_tx_cm.tex", null),
                Hair("Hair_Top1"), Hair("Hair_Hi1"), Hair("L_Hair_Hi1"), Hair("R_Hair_Hi1")),

            Dynamics(Skin(29, "BATTLE QUEEN KATARINA", "katarina_skin29_tx_cm.tex",
                new Dictionary<string, string>
                {
                    { "Blade1", "katarina_skin29_blade1_tx_cm.tex" },
                    { "Gem1", "katarina_skin29_blade1_tx_cm.tex" }
                },
                "Lizard", "Joke",
                "Blade2", "Blade3", "Blade4", "Blade5", "Blade6",
                "Gem2", "Gem3", "Gem4", "Gem5", "Gem6"),
                Hair("Hair_Base"), Bang("FrontHair1"), Coat("Coattails")),

            Dynamics(Skin(47, "FAERIE COURT KATARINA", "katarina_skin47_tx_cm.tex", null,
                "Glow", "Wing"),
                StandardHair()),

            Dynamics(Skin(48, "PRESTIGE FAERIE COURT KATARINA", "katarina_skin48_tx_cm.tex", null,
                "Glow", "Wing", "WingCrystal"),
                StandardHair()),

            Dynamics(Skin(59, "CHOSEN OF THE WOLF KATARINA", "katarina_skin59_tx_cm.tex", null),
                StandardHair()),

            Dynamics(Skin(60, "PRESTIGE MASQUE OF THE BLACK ROSE KATARINA", "katarina_skin60_tx_cm.tex", null,
                "mask", "mask2", "mask3", "cloak", "cloak2", "cloak3"),
                Join(StandardHair(),
                    Sash("R_Cloth"), Sash("L_Cloth"),
                    Skirt("skirtA1"), Skirt("skirtB1"))),

            Dynamics(Skin(70, "PETALS OF SPRING KATARINA", "katarina_skin70_tx_cm.tex", null,
                "Cup", "Tree", "Leaf"),
                StandardHair())
        };
    }
}
