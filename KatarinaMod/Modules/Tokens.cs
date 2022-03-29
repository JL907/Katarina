using R2API;
using System;

namespace KatarinaMod.Modules
{
    internal static class Tokens
    {
        public const string characterLore = "A leader of Ionia's growing criminal underworld, KATARINA rose to prominence in the wake of the war with Noxus. Though he began as a humble challenger in the fighting pits of Navori, he quickly gained notoriety for his savage strength, and his ability to take seemingly endless amounts of punishment. Now, having climbed through the ranks of local combatants, KATARINA has muscled to the top, reigning over the pits he once fought in.";
        public const string characterName = "<color=#FFFFFF>Katarina</color>";
        public const string characterOutro = "..expect my visit when the darkness comes. The night I think is best for hiding all.";
        public const string characterOutroFailure = "...And a horror of outer darkness after, And dust returneth to dust again";
        public const string characterSubtitle = "The Sinister Blade";

        public static string descriptionText =
             "Decisive in judgment and lethal in combat, Katarina is a Noxian assassin of the highest caliber." + Environment.NewLine + Environment.NewLine
             + "Eldest daughter to the legendary General Du Couteau, she made her talents known with swift kills against unsuspecting enemies." + Environment.NewLine + Environment.NewLine
             + "Her fiery ambition has driven her to pursue heavily-guarded targets, even at the risk of endangering her allies" + Environment.NewLine + Environment.NewLine
             + "—but no matter the mission, Katarina will not hesitate to execute her duty amid a whirlwind of serrated daggers.";

        internal static void AddTokens()
        {
            //<color=#c9aa71> tan
            //<color=#ffffff> white
            //<color=#f68835> orange
            //<color=#d62d20> red
            //<color=#AF1AAF> purple
            LanguageAPI.Add("KATARINA_NAME", characterName);

            LanguageAPI.Add("KATARINA_DESCRIPTION", descriptionText);
            LanguageAPI.Add("KATARINA_SUBTITLE", characterSubtitle);
            LanguageAPI.Add("KATARINA_LORE", characterLore);
            LanguageAPI.Add("KATARINA_OUTRO_FLAVOR", characterOutro);
            LanguageAPI.Add("KATARINA_OUTRO_FAILURE", characterOutroFailure);

            LanguageAPI.Add("KATARINA_DEFAULT_SKIN_NAME", "<color=#FFFFFF>KATARINA</color>");
            LanguageAPI.Add("MERCENARY_KATARINA_NAME", "<color=#FFFFFF>MERCENARY KATARINA</color>");
            LanguageAPI.Add("REDCARD_KATARINA_NAME", "<color=#FFFFFF>RED CARD KATARINA</color>");
            LanguageAPI.Add("BILGEWATER_KATARINA_NAME", "<color=#FFFFFF>BILGEWATER KATARINA</color>");
            LanguageAPI.Add("KITTYCAT_KATARINA_NAME", "<color=#FFFFFF>KITTY CAT KATARINA</color>");
            LanguageAPI.Add("HIGHCOMMAND_KATARINA_NAME", "<color=#FFFFFF>HIGH COMMAND KATARINA</color>");
            LanguageAPI.Add("SANDSTORM_KATARINA_NAME", "<color=#FFFFFF>SAND STORM KATARINA</color>");
            LanguageAPI.Add("SLAYBELLE_KATARINA_NAME", "<color=#FFFFFF>SLAY BELLE KATARINA</color>");
            LanguageAPI.Add("WARRINGKINGDOMS_KATARINA_NAME", "<color=#FFFFFF>WARRING KINGDOMS KATARINA</color>");
            LanguageAPI.Add("PROJECT_KATARINA_NAME", "<color=#FFFFFF>PROJECT: KATARINA</color>");
            LanguageAPI.Add("BATTLEACADEMIA_KATARINA_NAME", "<color=#FFFFFF>BATTLE ACADEMIA KATARINA</color>");
            LanguageAPI.Add("BLOODMOON_KATARINA_NAME", "<color=#FFFFFF>BLOOD MOON KATARINA</color>");

            LanguageAPI.Add("KATARINA_PASSIVE_NAME", "<color=#AF1AAF>VORACITY</color>");
            LanguageAPI.Add("KATARINA_PASSIVE_DESC",
                "Whenever Katarina retrieves a <color=#AF1AAF>Dagger</color>, she uses it to slash around herself, dealing <color=#f68835>400%</color> damage to nearby enemies." + Environment.NewLine
                + "A <color=#AF1AAF>Dagger</color> will disappear after being on the ground for <color=#ffffff>6</color> seconds." + Environment.NewLine
                + "Kills reduces the current <color=#c9aa71>cooldowns</color> of Katarina's abilities by <color=#ffffff>1</color> second.");

            LanguageAPI.Add("KATARINA_PRIMARY_NAME", "<color=#AF1AAF>BASIC ATTACK</color>");
            LanguageAPI.Add("KATARINA_PRIMARY_DESC", "Basic Attacks do <color=#f68835>280%</color> damage.");

            LanguageAPI.Add("KATARINA_SECONDARY_NAME", "<color=#AF1AAF>BOUNCING BLADE</color>");
            LanguageAPI.Add("KATARINA_SECONDARY_DESC",
                "Katarina throws a <color=#AF1AAF>Dagger</color> at the target enemy that can bounce to up to four additional nearby enemies, dealing <color=#f68835>200%</color> damage." + Environment.NewLine
                + "The <color=#AF1AAF>Dagger</color> then lands onto the ground after <color=#ffffff>1</color> second.");

            LanguageAPI.Add("KATARINA_UTILITY_NAME", "<color=#AF1AAF>SHUNPO</color>");
            LanguageAPI.Add("KATARINA_UTILITY_DESC",
                "Katarina tosses a <color=#AF1AAF>Dagger</color> into the air above her current location and blinks toward her target or a fixed distance in front of her." + Environment.NewLine
                + "Katarina will blink toward her target if the basic attack button is held." + Environment.NewLine
                + "Shunpo will <color=#c9aa71>refund</color> 75% of it's total <color=#c9aa71>cooldown</color> when Katarina retrieves a <color=#AF1AAF>Dagger.</color>");

            LanguageAPI.Add("KATARINA_SPECIAL_NAME", "<color=#AF1AAF>DEATH LOTUS</color>");
            LanguageAPI.Add("KATARINA_SPECIAL_DESC",
                "Katarina channels for up to <color=#ffffff>2.5</color> seconds, rapidly throwing a dagger every <color=#ffffff>0.166</color> seconds to the closest nearby enemies." + Environment.NewLine
                + "Each dagger deals <color=#f68835>100%</color> damage." + Environment.NewLine
                + "Attack speed increases the amount of daggers thrown");
        }
    }
}