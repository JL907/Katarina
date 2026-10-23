using BepInEx;
using KatarinaMod.Modules.Survivors;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx.Logging;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using KatarinaMod.SkillStates.Katarina.Weapon;
using KatarinaMod.SkillStates;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace KatarinaMod
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.sound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.networking", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class KatarinaPlugin : BaseUnityPlugin
    {
        public const string developerPrefix = "Lemonlust";

        public const string MODNAME = "KatarinaMod";

        public const string MODUID = "com.Lemonlust.KatarinaMod";

        public const string MODVERSION = "1.9.2";
        public static KatarinaPlugin instance;
        internal List<SurvivorBase> Survivors = new List<SurvivorBase>();

        public new ManualLogSource Logger
        {
            get
            {
                return base.Logger;
            }
        }

        private void Awake()
        {
            instance = this;
            try
            {
                Modules.Assets.Initialize();
                Modules.Config.ReadConfig();
                Modules.States.RegisterStates();
                R2API.Networking.NetworkingAPI.RegisterMessageType<Modules.DaggerPickupMessage>();
                R2API.Networking.NetworkingAPI.RegisterMessageType<Modules.DaggerLandingPreview.CancelMessage>();
                Modules.Buffs.RegisterBuffs();
                Modules.Projectiles.RegisterProjectiles();
                Modules.Tokens.AddTokens();

                new MyCharacter().Initialize();

                new Modules.ContentPacks().Initialize();

                Hook();
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (self)
            {
                if (self.HasBuff(Modules.Buffs.armorBuff))
                {
                    self.armor += 300f;
                }
            }
        }

        private void Hook()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
        }

        private void GlobalEventManager_OnCharacterDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            if (!NetworkServer.active)
            {
                return;
            }
            if (damageReport.attackerBody != null && damageReport.attacker != null && damageReport != null)
            {
                BodyIndex bodyIndex = BodyCatalog.FindBodyIndex("Katarina");
                if (damageReport.victimTeamIndex != TeamIndex.Player && damageReport.attackerBodyIndex == bodyIndex)
                {
                    SkillLocator skillLocator = damageReport.attackerBody.skillLocator;
                    if (NetworkServer.active)
                    {
                        skillLocator.DeductCooldownFromAllSkillsServer(1f);
                    }
                }
            }
        }
    }
}
