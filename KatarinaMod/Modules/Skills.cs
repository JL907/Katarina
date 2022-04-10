using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatarinaMod.Modules
{
    internal static class Skills
    {
        internal static List<SkillDef> skillDefs = new List<SkillDef>();
        internal static List<SkillFamily> skillFamilies = new List<SkillFamily>();

        // this could all be a lot cleaner but at least it's simple and easy to work with
        internal static void AddPrimarySkill(GameObject targetPrefab, SkillDef skillDef)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();

            SkillFamily skillFamily = skillLocator.primary.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }

        internal static void AddSecondarySkill(GameObject targetPrefab, SkillDef skillDef)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();

            SkillFamily skillFamily = skillLocator.secondary.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }

        internal static void AddSecondarySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            foreach (SkillDef i in skillDefs)
            {
                AddSecondarySkill(targetPrefab, i);
            }
        }

        internal static void AddSpecialSkill(GameObject targetPrefab, SkillDef skillDef)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();

            SkillFamily skillFamily = skillLocator.special.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }

        internal static void AddSpecialSkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            foreach (SkillDef i in skillDefs)
            {
                AddSpecialSkill(targetPrefab, i);
            }
        }

        internal static void AddUtilitySkill(GameObject targetPrefab, SkillDef skillDef)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();

            SkillFamily skillFamily = skillLocator.utility.skillFamily;

            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }

        internal static void AddUtilitySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            foreach (SkillDef i in skillDefs)
            {
                AddUtilitySkill(targetPrefab, i);
            }
        }

        internal static SkillDef CreatePrimarySkillDef(SerializableEntityStateType state, string stateMachine, string skillNameToken, string skillDescriptionToken, Sprite skillIcon, bool agile)
        {
            SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();

            skillDef.skillName = skillNameToken;
            skillDef.skillNameToken = skillNameToken;
            skillDef.skillDescriptionToken = skillDescriptionToken;
            skillDef.icon = skillIcon;

            skillDef.activationState = state;
            skillDef.activationStateMachineName = stateMachine;
            skillDef.baseMaxStock = 1;
            skillDef.baseRechargeInterval = 0;
            skillDef.beginSkillCooldownOnSkillEnd = false;
            skillDef.canceledFromSprinting = false;
            skillDef.forceSprintDuringState = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.interruptPriority = InterruptPriority.Any;
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.isCombatSkill = true;
            skillDef.mustKeyPress = false;
            skillDef.cancelSprintingOnActivation = false;
            skillDef.rechargeStock = 1;
            skillDef.requiredStock = 0;
            skillDef.stockToConsume = 0;
            ((ScriptableObject)skillDef).name = "KatarinaPrimary";
            skillDefs.Add(skillDef);

            return skillDef;
        }

        internal static SkillDef CreateSkillDef(SkillDefInfo skillDefInfo)
        {
            SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();

            skillDef.skillName = skillDefInfo.skillName;
            skillDef.skillNameToken = skillDefInfo.skillNameToken;
            skillDef.skillDescriptionToken = skillDefInfo.skillDescriptionToken;
            skillDef.icon = skillDefInfo.skillIcon;

            skillDef.activationState = skillDefInfo.activationState;
            skillDef.activationStateMachineName = skillDefInfo.activationStateMachineName;
            skillDef.baseMaxStock = skillDefInfo.baseMaxStock;
            skillDef.baseRechargeInterval = skillDefInfo.baseRechargeInterval;
            skillDef.beginSkillCooldownOnSkillEnd = skillDefInfo.beginSkillCooldownOnSkillEnd;
            skillDef.canceledFromSprinting = skillDefInfo.canceledFromSprinting;
            skillDef.forceSprintDuringState = skillDefInfo.forceSprintDuringState;
            skillDef.fullRestockOnAssign = skillDefInfo.fullRestockOnAssign;
            skillDef.interruptPriority = skillDefInfo.interruptPriority;
            skillDef.resetCooldownTimerOnUse = skillDefInfo.resetCooldownTimerOnUse;
            skillDef.isCombatSkill = skillDefInfo.isCombatSkill;
            skillDef.mustKeyPress = skillDefInfo.mustKeyPress;
            skillDef.cancelSprintingOnActivation = skillDefInfo.cancelSprintingOnActivation;
            skillDef.rechargeStock = skillDefInfo.rechargeStock;
            skillDef.requiredStock = skillDefInfo.requiredStock;
            skillDef.stockToConsume = skillDefInfo.stockToConsume;

            skillDef.keywordTokens = skillDefInfo.keywordTokens;

            skillDefs.Add(skillDef);

            return skillDef;
        }

        internal static void CreateSkillFamilies(GameObject targetPrefab)
        {
            foreach (GenericSkill obj in targetPrefab.GetComponentsInChildren<GenericSkill>())
            {
                KatarinaPlugin.DestroyImmediate(obj);
            }

            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();
            
            skillLocator.passiveSkill.enabled = true;
            skillLocator.passiveSkill.skillNameToken = "KATARINA_PASSIVE_NAME";
            skillLocator.passiveSkill.skillDescriptionToken = "KATARINA_PASSIVE_DESC";
            skillLocator.passiveSkill.icon = Assets.mainAssetBundle.LoadAsset<Sprite>("Katarina_Passive");


            skillLocator.primary = targetPrefab.AddComponent<GenericSkill>();
            SkillFamily primaryFamily = ScriptableObject.CreateInstance<SkillFamily>();
            (primaryFamily as ScriptableObject).name = targetPrefab.name + "PrimaryFamily";
            primaryFamily.variants = new SkillFamily.Variant[0];
            skillLocator.primary._skillFamily = primaryFamily;

            skillLocator.secondary = targetPrefab.AddComponent<GenericSkill>();
            SkillFamily secondaryFamily = ScriptableObject.CreateInstance<SkillFamily>();
            (secondaryFamily as ScriptableObject).name = targetPrefab.name + "SecondaryFamily";
            secondaryFamily.variants = new SkillFamily.Variant[0];
            skillLocator.secondary._skillFamily = secondaryFamily;

            skillLocator.utility = targetPrefab.AddComponent<GenericSkill>();
            SkillFamily utilityFamily = ScriptableObject.CreateInstance<SkillFamily>();
            (utilityFamily as ScriptableObject).name = targetPrefab.name + "UtilityFamily";
            utilityFamily.variants = new SkillFamily.Variant[0];
            skillLocator.utility._skillFamily = utilityFamily;

            skillLocator.special = targetPrefab.AddComponent<GenericSkill>();
            SkillFamily specialFamily = ScriptableObject.CreateInstance<SkillFamily>();
            (specialFamily as ScriptableObject).name = targetPrefab.name + "SpecialFamily";
            specialFamily.variants = new SkillFamily.Variant[0];
            skillLocator.special._skillFamily = specialFamily;

            skillFamilies.Add(primaryFamily);
            skillFamilies.Add(secondaryFamily);
            skillFamilies.Add(utilityFamily);
            skillFamilies.Add(specialFamily);
        }
    }
}

internal class SkillDefInfo
{
    public SerializableEntityStateType activationState;
    public string activationStateMachineName;
    public int baseMaxStock;
    public float baseRechargeInterval;
    public bool beginSkillCooldownOnSkillEnd;
    public bool canceledFromSprinting;
    public bool cancelSprintingOnActivation;
    public bool forceSprintDuringState;
    public bool fullRestockOnAssign;
    public InterruptPriority interruptPriority;
    public bool isCombatSkill;
    public string[] keywordTokens;
    public bool mustKeyPress;
    public int rechargeStock;
    public int requiredStock;
    public bool resetCooldownTimerOnUse;
    public string skillDescriptionToken;
    public Sprite skillIcon;
    public string skillName;
    public string skillNameToken;
    public int stockToConsume;
}