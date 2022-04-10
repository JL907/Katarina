using BepInEx.Configuration;
using System;

namespace KatarinaMod.Modules
{
    public static class Config
    {
        public static ConfigEntry<float> armorGrowth;
        public static ConfigEntry<float> baseArmor;
        public static ConfigEntry<float> baseCrit;
        public static ConfigEntry<float> baseDamage;
        public static ConfigEntry<float> baseHealth;
        public static ConfigEntry<float> baseMovementSpeed;
        public static ConfigEntry<float> baseRegen;
        public static ConfigEntry<float> bonusHealthCoefficient;
        public static ConfigEntry<float> damageGrowth;
        public static ConfigEntry<int> jumpCount;
        public static ConfigEntry<float> regenGrowth;
        public static ConfigEntry<float> healthGrowth;

        public static ConfigEntry<float> voracity_damageCoefficient;
        public static ConfigEntry<float> voracity_procCoefficient;
        public static ConfigEntry<float> voracity_radius;
        public static ConfigEntry<float> voracity_shunpoRecharge;

        public static ConfigEntry<float> bouncingBlades_damageCoefficient;
        public static ConfigEntry<float> bouncingBlades_coolDown;
        public static ConfigEntry<float> bouncingBlades_damageCoefficientPerBounce;
        public static ConfigEntry<float> bouncingBlades_procCoefficient;
        public static ConfigEntry<int> bouncingBlades_maxBounceCount;

        public static ConfigEntry<float> basicAttack_damageCoefficient;
        public static ConfigEntry<float> basicAttack_procCoefficient;

        public static ConfigEntry<float> deathLotus_procCoefficient;
        public static ConfigEntry<float> deathLotus_coolDown;
        public static ConfigEntry<float> deathLotus_damageCoefficient;
        public static ConfigEntry<float> deathLotus_radius;

        public static ConfigEntry<float> shunpo_coolDown;
        public static void ReadConfig()
        {
            baseHealth = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Base Health"), 101f, new ConfigDescription("", null, Array.Empty<object>()));
            healthGrowth = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Health Growth"), 33f, new ConfigDescription("", null, Array.Empty<object>()));
            baseRegen = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Base Health Regen"), 1.5f, new ConfigDescription("", null, Array.Empty<object>()));
            regenGrowth = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Health Regen Growth"), 0.2f, new ConfigDescription("", null, Array.Empty<object>()));
            baseArmor = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Base Armor"), 20f, new ConfigDescription("", null, Array.Empty<object>()));
            armorGrowth = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Armor Growth"), 0f, new ConfigDescription("", null, Array.Empty<object>()));
            baseDamage = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Base Damage"), 12f, new ConfigDescription("", null, Array.Empty<object>()));
            damageGrowth = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Damage Growth"), 2.4f, new ConfigDescription("", null, Array.Empty<object>()));
            baseMovementSpeed = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Base Movement Speed"), 7f, new ConfigDescription("", null, Array.Empty<object>()));
            baseCrit = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("01 - Character Stats", "Base Crit"), 1f, new ConfigDescription("", null, Array.Empty<object>()));
            jumpCount = KatarinaPlugin.instance.Config.Bind<int>(new ConfigDefinition("01 - Character Stats", "Jump Count"), 2, new ConfigDescription("", null, Array.Empty<object>()));

            voracity_damageCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("02 - Voracity Passive", "Damage Coefficient"), 4f, new ConfigDescription("", null, Array.Empty<object>()));
            voracity_procCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("02 - Voracity Passive", "Proc Coefficient"), 1f, new ConfigDescription("", null, Array.Empty<object>()));
            voracity_radius = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("02 - Voracity Passive", "Radius"), 15f, new ConfigDescription("", null, Array.Empty<object>()));
            voracity_shunpoRecharge = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("02 - Voracity Passive", "Shunpo Recharge"), 6f, new ConfigDescription("", null, Array.Empty<object>()));

            basicAttack_damageCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("03 - Basic Attack", "Damage Coefficient"), 2.8f, new ConfigDescription("", null, Array.Empty<object>()));
            basicAttack_procCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("03 - Basic Attack", "Proc Coefficient"), 1f, new ConfigDescription("", null, Array.Empty<object>()));

            bouncingBlades_damageCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("04 - Bouncing Blades", "Damage Coefficient"), 2f, new ConfigDescription("", null, Array.Empty<object>()));
            bouncingBlades_damageCoefficientPerBounce = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("04 - Bouncing Blades", "Damage Coefficient Per Bounce"), 1f, new ConfigDescription("", null, Array.Empty<object>()));
            bouncingBlades_procCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("04 - Bouncing Blades", "Proc Coefficient"), 1f, new ConfigDescription("", null, Array.Empty<object>()));
            bouncingBlades_maxBounceCount = KatarinaPlugin.instance.Config.Bind<int>(new ConfigDefinition("04 - Bouncing Blades", "Max Bounce Count"), 4, new ConfigDescription("", null, Array.Empty<object>()));
            bouncingBlades_coolDown = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("04 - Bouncing Blades", "Cooldown"), 4f, new ConfigDescription("", null, Array.Empty<object>()));


            deathLotus_damageCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("05 - Death Lotus", "Damage Coefficient"), 1f, new ConfigDescription("", null, Array.Empty<object>()));
            deathLotus_procCoefficient = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("05 - Death Lotus", "Proc Coefficient"), 0.25f, new ConfigDescription("", null, Array.Empty<object>()));
            deathLotus_coolDown = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("05 - Death Lotus", "Cooldown"), 16f, new ConfigDescription("", null, Array.Empty<object>()));
            deathLotus_radius = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("05 - Death Lotus", "Radius"), 30f, new ConfigDescription("", null, Array.Empty<object>()));

            shunpo_coolDown = KatarinaPlugin.instance.Config.Bind<float>(new ConfigDefinition("06 - Shunpo", "Cooldown"), 6f, new ConfigDescription("", null, Array.Empty<object>()));

        }

        // this helper automatically makes config entries for disabling survivors
        internal static ConfigEntry<bool> CharacterEnableConfig(string characterName)
        {
            return KatarinaPlugin.instance.Config.Bind<bool>(new ConfigDefinition(characterName, "Enabled"), true, new ConfigDescription("Set to false to disable this character"));
        }

        internal static ConfigEntry<bool> EnemyEnableConfig(string characterName)
        {
            return KatarinaPlugin.instance.Config.Bind<bool>(new ConfigDefinition(characterName, "Enabled"), true, new ConfigDescription("Set to false to disable this enemy"));
        }
    }
}