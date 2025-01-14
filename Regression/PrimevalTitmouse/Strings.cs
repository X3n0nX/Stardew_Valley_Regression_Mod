using Microsoft.Xna.Framework.Input;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrimevalTitmouse
{
    //Lots of Regex functions to handle variability in our strings.
    public static class Strings
    {
        private static Data t = Regression.t;
        private static Farmer who = Game1.player;

        public static string DescribeUnderwear(Container u, string baseDescription = null, bool noPrefix = false)
        {
            string newValue = baseDescription != null ? baseDescription : u.description;
            float num1 = u.wetness / u.absorbency;
            float num2 = u.messiness / u.containment;
            if ((double)num1 == 0.0 && (double)num2 == 0.0)
            {
                newValue = !u.drying ? Strings.t.Underwear_Clean.Replace("$UNDERWEAR_DESC$", newValue) : Strings.t.Underwear_Drying.Replace("$UNDERWEAR_DESC$", newValue);
            }
            else
            {
                if ((double)num2 > 0.0)
                {
                    for (int index = 0; index < Strings.t.Underwear_Messy.Length; ++index)
                    {
                        float num3 = (float)(((double)index + 1.0) / ((double)Strings.t.Underwear_Messy.Length - 1.0));
                        if (index == Strings.t.Underwear_Messy.Length - 1 || (double)num2 <= (double)num3)
                        {
                            newValue = Strings.ReplaceOptional(Strings.t.Underwear_Messy[index].Replace("$UNDERWEAR_DESC$", newValue), (double)num1 > 0.0);
                            break;
                        }
                    }
                }
                if ((double)num1 > 0.0)
                {
                    for (int index = 0; index < Strings.t.Underwear_Wet.Length; ++index)
                    {
                        float num3 = (float)(((double)index + 1.0) / ((double)Strings.t.Underwear_Wet.Length - 1.0));
                        if (index == Strings.t.Underwear_Wet.Length - 1 || (double)num1 <= (double)num3)
                        {
                            string input = Strings.t.Underwear_Wet[index].Replace("$UNDERWEAR_DESC$", newValue);
                            Regex regex = new Regex("<([^>]*)>");
                            newValue = (double)num2 != 0.0 ? regex.Replace(input, "$1") : regex.Replace(input, "");
                            break;
                        }
                    }
                }
            }
            string newOrOld = "";
            if(u.washable && u.InnerContainer != null && u.durability < u.InnerContainer.durability)
            {
                newOrOld = "used ";
                if(u.durability < u.InnerContainer.durability / 2)
                {
                    newOrOld = "old ";
                }
            }
            if (noPrefix) return newValue;
            return u.GetPrefix() + " " + newOrOld + newValue;
        }


        public static string InsertVariables(string msg, Body b, Container c = null)
        {
            string str = msg;
            if (b != null && c == null)
                c = b.underwear;
            if (c != null)
            {
                var gettingChangedDialog = Strings.RandString(Animations.Data.Diaper_Change_Dialog);
                gettingChangedDialog = Strings.ReplaceAndOr(gettingChangedDialog, c.wetness > 0, c.messiness > 0);
                str = str.Replace("$GETTING_CHANGED_DIALOG$", "#$b#" + gettingChangedDialog);

                str = Strings.ReplaceConditionalOptional(str, "OnSlightlyWet", c.used && !c.used_bad);
                str = Strings.ReplaceConditionalOptional(str, "OnClean", !c.used);
                str = Strings.ReplaceOr(str.Replace("$UNDERWEAR_NAME$", c.displayName).Replace("$UNDERWEAR_PREFIX$", c.GetPrefix()).Replace("$UNDERWEAR_DESC$", c.description).Replace("$INSPECT_UNDERWEAR_NAME$", Strings.DescribeUnderwear(c, c.displayName)).Replace("$INSPECT_UNDERWEAR_NAME_NO_PREFIX$", Strings.DescribeUnderwear(c, c.displayName,true)).Replace("$INSPECT_UNDERWEAR_DESC$", Strings.DescribeUnderwear(c, c.description)), !c.plural, "#");

            }
               
            if (b != null)
            {
                str = Strings.ReplaceConditionalOptional(str, "OnDebuffed", b.HasWetOrMessyDebuff());
                str = str.Replace("$PANTS_NAME$", b.pants.displayName).Replace("$PANTS_PREFIX$", b.pants.GetPrefix()).Replace("$PANTS_DESC$", b.pants.description).Replace("$BEDDING_DRYTIME$", Game1.getTimeOfDayString(b.bed.timeWhenDoneDrying.time));
            }
               
            return Strings.ReplaceOr(str, Strings.who.IsMale, "/").Replace("$FARMERNAME$", Strings.who.Name);
        }
        public static string npcUnderwearOptions(NPC npc)
        {
            var modifiers = Animations.Data.Villager_Underwear_Options;
            Dictionary<string, Dictionary<string, string>> foundDict = null;

            foreach (string key2 in Animations.npcTypeList(npc))
            {
                Dictionary<string, Dictionary<string, string>> dictionary;
                if (modifiers.TryGetValue(key2, out dictionary))
                {
                    foundDict = dictionary;
                }
            }

            if (foundDict == null) return "";
            var list = new List<string>();
            foreach (string key in foundDict.Keys)
            {
                if (!Regression.HasUnderwear(key)) continue;
                var entry = foundDict[key];
                // #$r change_other_yes 2 Change_Diaper_Accept#Yes
                list.Add($"#$r change_other_yes {entry["friendship"]} {entry["dialog_key"]} {(entry.TryGetValue("observerfriendship", out string val) ? val : "")}#{key.FirstCharToUpper()}");
            }
            list.Add($"#$r change_other_no 0 Change_Diaper_Refuse#Not now");
            return string.Join("#", list);
        }
        public static string InsertVariables(string msg, NPC b, Container c = null)
        {
            string str = msg;
            if (b != null && c == null)
                c =  new NpcBody(b).underwear;
            if (c != null)
            {
                var changeOtherDialog = Strings.RandString(Animations.Data.Change_Other_Dialog);
                changeOtherDialog = Strings.ReplaceAndOr(changeOtherDialog, c.wetness > 0, c.messiness > 0);
                changeOtherDialog += npcUnderwearOptions(b);
                str = str.Replace("$CHANGE_OTHER_DIALOG$", changeOtherDialog);

                str = Strings.ReplaceConditionalOptional(str, "NpcOnSlightlyWet", c.used && !c.used_bad);
                str = Strings.ReplaceConditionalOptional(str, "NpcOnUsedBad", c.used && c.used_bad);
                str = Strings.ReplaceConditionalOptional(str, "NpcOnClean", !c.used);
                str = Strings.ReplaceOr(str.Replace("$NPC_UNDERWEAR_NAME$", c.displayName).Replace("$NPC_UNDERWEAR_PREFIX$", c.GetPrefix()).Replace("$NPC_UNDERWEAR_DESC$", c.description).Replace("$NPC_INSPECT_UNDERWEAR_NAME$", Strings.DescribeUnderwear(c, c.displayName)).Replace("$NPC_INSPECT_UNDERWEAR_NAME_NO_PREFIX$", Strings.DescribeUnderwear(c, c.displayName,true)).Replace("$NPC_INSPECT_UNDERWEAR_DESC$", Strings.DescribeUnderwear(c, c.description)), !c.plural, "#");

            }

            if (b != null)
            {
                var pants = new NpcBody(b).pants;
                str = str.Replace("$NPC_PANTS_NAME$", pants.displayName).Replace("$NPC_PANTS_PREFIX$", pants.GetPrefix()).Replace("$NPC_PANTS_DESC$", pants.description);
                str = str.Replace("$NPC_NAME$", b.Name.FirstCharToUpper());
                str = str.Replace("$NPC_HE_SHE$", b.Gender == Gender.Male ? "he" : (b.Gender == Gender.Female ? "she" : "they"));
                str = str.Replace("$NPC_HIS_HER$", b.Gender == Gender.Male ? "his" : (b.Gender == Gender.Female ? "her" : "their"));
                str = str.Replace("$NPC_HIM_HER$", b.Gender == Gender.Male ? "him" : (b.Gender == Gender.Female ? "her" : "them"));
                str = str.Replace("$NPC_HIS_HER_IS_ARE$", b.Gender == Gender.Male ? "his" : (b.Gender == Gender.Female ? "her" : "their"));
            }


            return str;
        }


        public static string InsertVariable(string inputString, string variableName, string variableValue)
        {
            string outputString = inputString;
            outputString = outputString.Replace(variableName, variableValue);
            return outputString;
        }

        public static string RandString(string[] msgs = null)
        {
            if (msgs == null || msgs.Length == 0) return "";
            return msgs[Regression.rnd.Next(msgs.Length)];
        }

        public static string ReplaceAndOr(string str, bool first, bool second, string splitChar = "&")
        {
            Regex regex = new Regex("<([^>" + splitChar + "]*)" + splitChar + "([^>]*)>");
            if (first && !second)
                return regex.Replace(str, "$1");
            if (!first & second)
                return regex.Replace(str, "$2");
            if (first & second)
                return regex.Replace(str, "$1 and $2");
            return regex.Replace(str, "");
        }

        public static string ReplaceOptional(string str, bool keep)
        {
            return new Regex("<([^>]*)>").Replace(str, keep ? "$1" : "");
        }
        /// <summary>
        /// Replaces or removes [triggerText: ...] tokens in the dialogue string.
        /// </summary>
        /// <param name="str">The original dialogue string.</param>
        /// <param name="key">The key-triggerText that causes that token to be replaced or not.</param>
        /// <param name="keep">If true, replace with the inner text; if false, remove the token.</param>
        /// <returns>The processed dialogue string.</returns>
        public static string ReplaceConditionalOptional(string str, string key, bool keep)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(key))
                return str;

            // Escape the triggerKey to handle any special regex characters
            string escapedKey = Regex.Escape(key);

            string pattern = $@"\[{escapedKey}:\s*(.*?)\]";

            Regex triggerTextRegex = new Regex(pattern, RegexOptions.Compiled);
            return triggerTextRegex.Replace(str, keep ? " $1" : "");
        }
        public static string ReplaceOr(string str, bool first, string splitChar = "/")
        {
            return new Regex("<([^>" + splitChar + "]*)" + splitChar + "([^>]*)>").Replace(str, first ? "$1" : "$2");
        }

        public static List<string> ValidUnderwearTypes()
        {
            List<string> list = Regression.t.Underwear_Options.Keys.ToList<string>();
            list.Remove("legs");
            list.Remove("blue jeans");
            list.Remove("bed");
            return list;
        }
    }
}
