﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Achievements;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace KatarinaMod.Modules
{
    internal interface IModdedUnlockableDataProvider
    {
        string AchievementDescToken { get; }
        string AchievementIdentifier { get; }
        string AchievementNameToken { get; }
        Func<string> GetHowToUnlock { get; }
        Func<string> GetUnlocked { get; }
        string PrerequisiteUnlockableIdentifier { get; }
        Sprite Sprite { get; }
        string UnlockableIdentifier { get; }
        string UnlockableNameToken { get; }
    }

    internal static class Unlockables
    {
        internal static List<AchievementDef> achievementDefs = new List<AchievementDef>();
        internal static List<UnlockableDef> unlockableDefs = new List<UnlockableDef>();
        private static readonly List<(AchievementDef achDef, UnlockableDef unlockableDef, String unlockableName)> moddedUnlocks = new List<(AchievementDef achDef, UnlockableDef unlockableDef, string unlockableName)>();
        private static readonly HashSet<string> usedRewardIds = new HashSet<string>();
        private static bool addingUnlockables;
        public static bool ableToAdd { get; private set; } = false;

        public static UnlockableDef AddUnlockable<TUnlockable>(bool serverTracked) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new()
        {
            TUnlockable instance = new TUnlockable();

            string unlockableIdentifier = instance.UnlockableIdentifier;

            if (!usedRewardIds.Add(unlockableIdentifier)) throw new InvalidOperationException($"The unlockable identifier '{unlockableIdentifier}' is already used by another mod or the base game.");

            AchievementDef achievementDef = new AchievementDef
            {
                identifier = instance.AchievementIdentifier,
                unlockableRewardIdentifier = instance.UnlockableIdentifier,
                prerequisiteAchievementIdentifier = instance.PrerequisiteUnlockableIdentifier,
                nameToken = instance.AchievementNameToken,
                descriptionToken = instance.AchievementDescToken,
                achievedIcon = instance.Sprite,
                type = instance.GetType(),
                serverTrackerType = (serverTracked ? instance.GetType() : null),
            };

            UnlockableDef unlockableDef = CreateNewUnlockable(new UnlockableInfo
            {
                Name = instance.UnlockableIdentifier,
                HowToUnlockString = instance.GetHowToUnlock,
                UnlockedString = instance.GetUnlocked,
                SortScore = 200
            });

            unlockableDefs.Add(unlockableDef);
            achievementDefs.Add(achievementDef);

            moddedUnlocks.Add((achievementDef, unlockableDef, instance.UnlockableIdentifier));

            if (!addingUnlockables)
            {
                addingUnlockables = true;
                IL.RoR2.AchievementManager.CollectAchievementDefs += CollectAchievementDefs;
                IL.RoR2.UnlockableCatalog.Init += Init_Il;
            }

            return unlockableDef;
        }

        public static ILCursor CallDel_<TDelegate>(this ILCursor cursor, TDelegate target, out Int32 index)
where TDelegate : Delegate
        {
            index = cursor.EmitDelegate<TDelegate>(target);
            return cursor;
        }

        public static ILCursor CallDel_<TDelegate>(this ILCursor cursor, TDelegate target)
            where TDelegate : Delegate => cursor.CallDel_(target, out _);

        internal static UnlockableDef CreateNewUnlockable(UnlockableInfo unlockableInfo)
        {
            UnlockableDef newUnlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();

            newUnlockableDef.nameToken = unlockableInfo.Name;
            newUnlockableDef.cachedName = unlockableInfo.Name;
            newUnlockableDef.getHowToUnlockString = unlockableInfo.HowToUnlockString;
            newUnlockableDef.getUnlockedString = unlockableInfo.UnlockedString;
            newUnlockableDef.sortScore = unlockableInfo.SortScore;

            return newUnlockableDef;
        }

        private static void CollectAchievementDefs(ILContext il)
        {
            var f1 = typeof(AchievementManager).GetField("achievementIdentifiers", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (f1 is null) throw new NullReferenceException($"Could not find field in {nameof(AchievementManager)}");
            var cursor = new ILCursor(il);
            _ = cursor.GotoNext(MoveType.After,
                x => x.MatchEndfinally(),
                x => x.MatchLdloc(1)
            );

            void EmittedDelegate(List<AchievementDef> list, Dictionary<String, AchievementDef> map, List<String> identifiers)
            {
                ableToAdd = false;
                for (Int32 i = 0; i < moddedUnlocks.Count; ++i)
                {
                    var (ach, unl, unstr) = moddedUnlocks[i];
                    if (ach is null) continue;
                    identifiers.Add(ach.identifier);
                    list.Add(ach);
                    map.Add(ach.identifier, ach);
                }
            }

            _ = cursor.Emit(OpCodes.Ldarg_0);
            _ = cursor.Emit(OpCodes.Ldsfld, f1);
            _ = cursor.EmitDelegate<Action<List<AchievementDef>, Dictionary<String, AchievementDef>, List<String>>>(EmittedDelegate);
            _ = cursor.Emit(OpCodes.Ldloc_1);
        }

        private static void Init_Il(ILContext il) => new ILCursor(il)
            .GotoNext(MoveType.AfterLabel, x => x.MatchCallOrCallvirt(typeof(UnlockableCatalog), nameof(UnlockableCatalog.SetUnlockableDefs)))
    .CallDel_(ArrayHelper.AppendDel(unlockableDefs));

        internal struct UnlockableInfo
        {
            internal Func<string> HowToUnlockString;
            internal string Name;
            internal int SortScore;
            internal Func<string> UnlockedString;
        }
    }

    internal abstract class ModdedUnlockable : BaseAchievement, IModdedUnlockableDataProvider
    {
        #region Implementation

        public void Revoke()
        {
            if (base.userProfile.HasAchievement(this.AchievementIdentifier))
            {
                base.userProfile.RevokeAchievement(this.AchievementIdentifier);
            }

            base.userProfile.RevokeUnlockable(UnlockableCatalog.GetUnlockableDef(this.UnlockableIdentifier));
        }

        #endregion Implementation

        #region Contract

        public abstract string AchievementDescToken { get; }
        public abstract string AchievementIdentifier { get; }
        public abstract string AchievementNameToken { get; }
        public abstract Func<string> GetHowToUnlock { get; }
        public abstract Func<string> GetUnlocked { get; }
        public abstract string PrerequisiteUnlockableIdentifier { get; }
        public abstract Sprite Sprite { get; }
        public abstract string UnlockableIdentifier { get; }
        public abstract string UnlockableNameToken { get; }

        #endregion Contract

        #region Virtuals

        public override bool wantsBodyCallbacks { get => base.wantsBodyCallbacks; }

        public override BodyIndex LookUpRequiredBodyIndex()
        {
            return base.LookUpRequiredBodyIndex();
        }

        public override void OnBodyRequirementBroken() => base.OnBodyRequirementBroken();

        public override void OnBodyRequirementMet() => base.OnBodyRequirementMet();

        public override void OnGranted() => base.OnGranted();

        public override void OnInstall()
        {
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            base.OnUninstall();
        }

        public override Single ProgressForAchievement() => base.ProgressForAchievement();

        #endregion Virtuals
    }
}