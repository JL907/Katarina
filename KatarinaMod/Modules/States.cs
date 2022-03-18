using KatarinaMod.SkillStates;
using KatarinaMod.SkillStates.BaseStates;
using KatarinaMod.SkillStates.Katarina;
using KatarinaMod.SkillStates.Katarina.Weapon;
using System;
using System.Collections.Generic;

namespace KatarinaMod.Modules
{
    public static class States
    {
        internal static List<Type> entityStates = new List<Type>();

        internal static void RegisterStates()
        {
            entityStates.Add(typeof(BaseMeleeAttack));
            entityStates.Add(typeof(SlashCombo));

            entityStates.Add(typeof(ThrowDagger));

            entityStates.Add(typeof(Shunpo));

            entityStates.Add(typeof(Voracity));

            entityStates.Add(typeof(Deathlotus));
        }
    }
}