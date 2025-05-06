using HarmonyLib;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
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
            float peeFill = u.wetness / u.absorbency;
            float poopFill = u.messiness / u.containment;
            if ((double)peeFill == 0.0 && (double)poopFill == 0.0)
            {
                newValue = !u.drying ? Strings.tryGetI18nText(Strings.t.Underwear_Clean).Replace("$UNDERWEAR_DESC$", newValue) :
                                        Strings.tryGetI18nText(Strings.t.Underwear_Drying).Replace("$UNDERWEAR_DESC$", newValue);
            }
            else
            {
                if ((double)poopFill > 0.0)
                {
                    for (int index = 0; index < Strings.t.Underwear_Messy.Length; ++index)
                    {
                        float num3 = (float)(((double)index + 1.0) / ((double)Strings.t.Underwear_Messy.Length - 1.0));
                        if (index == Strings.t.Underwear_Messy.Length - 1 || (double)poopFill <= (double)num3)
                        {
                            newValue = Strings.ReplaceOptional(Strings.tryGetI18nText(Strings.t.Underwear_Messy[index]).Replace("$UNDERWEAR_DESC$", newValue), (double)peeFill > 0.0);
                            break;
                        }
                    }
                }
                if ((double)peeFill > 0.0)
                {
                    for (int index = 0; index < Strings.t.Underwear_Wet.Length; ++index)
                    {
                        float num3 = (float)(((double)index + 1.0) / ((double)Strings.t.Underwear_Wet.Length - 1.0));
                        if (index == Strings.t.Underwear_Wet.Length - 1 || (double)peeFill <= (double)num3)
                        {
                            string input = Strings.tryGetI18nText(Strings.t.Underwear_Wet[index]).Replace("$UNDERWEAR_DESC$", newValue);
                            Regex regex = new Regex("<([^>]*)>");
                            newValue = (double)poopFill != 0.0 ? regex.Replace(input, "$1") : regex.Replace(input, "");
                            break;
                        }
                    }
                }
            }
            string newOrOld = "";
            if (u.washable && u.InnerContainer != null && u.durability < u.InnerContainer.durability)
            {
                newOrOld = "used ";
                if (u.durability < u.InnerContainer.durability / 2)
                {
                    newOrOld = "old ";
                }
            }
            if (noPrefix) return newValue;
            return getPrefix(u.plural) + " " + newOrOld + newValue;
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
            list.Add($"#$r change_other_no 0 Change_Diaper_Refuse#" + Regression.help.Translation.Get("Change_Other.Dialog.Answer_No"));
            return string.Join("#", list);
        }

        public static string InsertVariables(string msg, Body b, Container c = null)
        {
            string str = msg;
            if (b != null && c == null)
                c = b.underwear;
            if (c != null)
            {
                //var gettingChangedDialog = Strings.RandString(Animations.Data.Diaper_Change_Dialog);
                //gettingChangedDialog = Strings.ReplaceAndOr(gettingChangedDialog, c.wetness > 0, c.messiness > 0);
                //str = str.Replace("$GETTING_CHANGED_DIALOG$", "#$b#" + gettingChangedDialog);

                str = Strings.ReplaceGetChange(str, c);

                str = Strings.ReplaceConditionalOptional(str, "OnSlightlyWet", c.used && !c.used_bad);
                str = Strings.ReplaceConditionalOptional(str, "OnClean", !c.used);

                str = ReplaceUnderwearToken(str,c);
                str = ReplaceInspectUnderwearToken(str, c);

                str = Strings.ReplaceOr(str,!c.plural,"#");

                /*
                str = Strings.ReplaceOr(str.Replace("$UNDERWEAR_NAME$", c.displayName).
                  Replace("$UNDERWEAR_PREFIX$", c.GetPrefix()).
                  Replace("$UNDERWEAR_DESC$", c.description).
                  Replace("$INSPECT_UNDERWEAR_NAME$", Strings.DescribeUnderwear(c, c.displayName)).
                  Replace("$INSPECT_UNDERWEAR_NAME_NO_PREFIX$", Strings.DescribeUnderwear(c, c.displayName, true)).
                  Replace("$INSPECT_UNDERWEAR_DESC$", Strings.DescribeUnderwear(c, c.description)), !c.plural, "#");
                */
            }

            if (b != null)
            {
                str = Strings.ReplaceConditionalOptional(str, "OnDebuffed", b.HasWetOrMessyDebuff());

                str = ReplacePantsToken(str,b.pants);
                str = str.Replace("$BEDDING_DRYTIME$", Game1.getTimeOfDayString(b.bed.timeWhenDoneDrying.time));

                /*
                str = str.Replace("$PANTS_NAME$", b.pants.displayName).
                  Replace("$PANTS_PREFIX$", b.pants.GetPrefix()).
                  Replace("$PANTS_DESC$", b.pants.description).
                  Replace("$BEDDING_DRYTIME$", Game1.getTimeOfDayString(b.bed.timeWhenDoneDrying.time));
                */
            }

            return Strings.ReplaceOr(str, Strings.who.IsMale, "/").Replace("$FARMERNAME$", Strings.who.Name);
        }
        
        public static string InsertVariables(string msg, NPC b, Container c = null)
        {
            string str = msg;
            if (b != null && c == null)
                c = new NpcBody(b).underwear;
            if (c != null)
            {
                var changeOtherDialog = Strings.RandString(Animations.Data.Change_Other_Dialog);
                changeOtherDialog = Strings.ReplaceAndOr(changeOtherDialog, c.wetness > 0, c.messiness > 0);
                changeOtherDialog += npcUnderwearOptions(b);
                str = str.Replace("$CHANGE_OTHER_DIALOG$", changeOtherDialog);

                str = Strings.ReplaceConditionalOptional(str, "NpcOnSlightlyWet", c.used && !c.used_bad);
                str = Strings.ReplaceConditionalOptional(str, "NpcOnUsedBad", c.used && c.used_bad);
                str = Strings.ReplaceConditionalOptional(str, "NpcOnClean", !c.used);

                str = ReplaceUnderwearToken(str, c, npc: true);
                str = ReplaceInspectUnderwearToken(str, c, npc: true);

                str = Strings.ReplaceOr(str, !c.plural, "#");

                /*
                str = Strings.ReplaceOr(str.Replace("$NPC_UNDERWEAR_NAME$", c.displayName).
                  Replace("$NPC_UNDERWEAR_PREFIX$", c.GetPrefix()).
                  Replace("$NPC_UNDERWEAR_DESC$", c.description).
                  Replace("$NPC_INSPECT_UNDERWEAR_NAME$", Strings.DescribeUnderwear(c, c.displayName)).
                  Replace("$NPC_INSPECT_UNDERWEAR_NAME_NO_PREFIX$", Strings.DescribeUnderwear(c, c.displayName, true)).
                  Replace("$NPC_INSPECT_UNDERWEAR_DESC$", Strings.DescribeUnderwear(c, c.description)), !c.plural, "#");
                */
            }

            if (b != null)
            {
                var pants = new NpcBody(b).pants;
                if (pants != null)
                {
                    str = ReplaceNpcName(str, b.Name.FirstCharToUpper());
                    str = ReplacePantsToken(str, pants, true);
                    str = ReplaceGenderTextNpc(str, b.Gender);
                }
                else Regression.monitor.Log($"NPC Error: {b.Name}", StardewModdingAPI.LogLevel.Info);
                /*
                var pants = new NpcBody(b).pants;
                str = str.Replace("$NPC_PANTS_NAME$", pants.displayName).Replace("$NPC_PANTS_PREFIX$", pants.GetPrefix()).Replace("$NPC_PANTS_DESC$", pants.description);
                str = str.Replace("$NPC_NAME$", b.Name.FirstCharToUpper());
                str = str.Replace("$NPC_HE_SHE$", b.Gender == Gender.Male ? getGenderText("he") : (b.Gender == Gender.Female ? getGenderText("she") : getGenderText("they")));
                str = str.Replace("$NPC_HIS_HER$", b.Gender == Gender.Male ? getGenderText("his") : (b.Gender == Gender.Female ? getGenderText("her") : getGenderText("their")));
                str = str.Replace("$NPC_HIM_HER$", b.Gender == Gender.Male ? getGenderText("him") : (b.Gender == Gender.Female ? getGenderText("her") : getGenderText("them")));
                str = str.Replace("$NPC_HIS_HER_IS_ARE$", b.Gender == Gender.Male ? getGenderText("his") : (b.Gender == Gender.Female ? getGenderText("her") : getGenderText("their")));
                */
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
            return Strings.tryGetI18nText(msgs[Regression.rnd.Next(msgs.Length)]);
            //return msgs[Regression.rnd.Next(msgs.Length)];
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
            //list.Remove("legs");
            //list.Remove("blue jeans");
            //list.Remove("bed");
            return list;
        }

        #region Replace Tokens
        public static string ReplaceFarmername(string str)
        {
            string token = "$FARMERNAME$";
            return str.Replace(token, Strings.who.Name);
        }

        public static string ReplaceNpcName(string str, string npcName)
        {
            string token = "$NPC_NAME$";
            return str.Replace(token, npcName.FirstCharToUpper());
        }

        #region Replace Token Underwear
        public static string ReplaceUnderwearToken(string str, Container underwear, bool npc = false, bool old = false)
        {
            str = ReplaceUnderwearPrefix(str,underwear.plural, npc: npc, old: old);
            str = ReplaceUnderwearName(str, underwear.displayName, npc: npc, old: old);
            str = ReplaceUnderwearDesc(str, underwear.description, npc: npc, old: old);

            return str;
        }
        private static string ReplaceUnderwearPrefix(string str, bool plural = false, bool npc = false, bool old = false)
        {
            string token = "$UNDERWEAR_PREFIX$";

            if (old) token = token.Insert(1, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, getPrefix(plural));
        }
        private static string ReplaceUnderwearName(string str, string underwearName, bool npc = false, bool old = false)
        {
            string token = "$UNDERWEAR_NAME$";

            if (old) token = token.Insert(1,"OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, underwearName);
        }
        private static string ReplaceUnderwearDesc(string str, string description, bool npc = false, bool old = false)
        {
            string token = "$UNDERWEAR_DESC$";

            if (old) token = token.Insert(1, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, description);
        }

        public static string ReplaceInspectUnderwearToken(string str, Container underwear, bool npc = false, bool old = false)
        {
            str = ReplaceInspectUnderwearName(str,underwear,npc: npc, old: old);
            str = ReplaceInspectUnderwearName(str, underwear, npc: npc, noPrefix: true, old: old);
            str = ReplaceInspectUnderwearDesc(str, underwear, npc: npc, old: old);

            return str;
        }

        private static string ReplaceInspectUnderwearName(string str, Container underwear, bool noPrefix = false, bool npc = false, bool old = false)
        {
            string token;

            if (noPrefix) token = "$INSPECT_UNDERWEAR_NAME_NO_PREFIX$";
            else token = "$INSPECT_UNDERWEAR_NAME$";

            if (old) token = token.Insert(9, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, Strings.DescribeUnderwear(underwear, underwear.displayName, noPrefix));
        }
        private static string ReplaceInspectUnderwearDesc(string str, Container underwear, bool npc = false, bool old = false)
        {
            string token = "$INSPECT_UNDERWEAR_DESC$";

            if (old) token = token.Insert(9, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, Strings.DescribeUnderwear(underwear, underwear.description));
        }
        #endregion

        #region Replace Token Pants
        public static string ReplacePantsToken(string str, Container pants, bool npc = false, bool old = false)
        {
            str = ReplacePantsPrefix(str, pants.plural, npc, old: old);
            str = ReplacePantsName(str, pants.displayName, npc, old: old);
            str = ReplacePantsDesc(str, pants.description, npc, old: old);

            return str;
        }
        private static string ReplacePantsPrefix(string str, bool plural = false, bool npc = false, bool old = false)
        {
            string token = "$PANTS_PREFIX$";

            if (old) token = token.Insert(1, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, getPrefix(plural));
        }
        private static string ReplacePantsName(string str, string pantsName, bool npc = false, bool old = false)
        {
            string token = "$PANTS_NAME$";

            if (old) token = token.Insert(1, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, pantsName);
        }
        private static string ReplacePantsDesc(string str, string description, bool npc = false, bool old = false)
        {
            string token = "$PANTS_DESC$";

            if (old) token = token.Insert(1, "OLD_");
            if (npc) token = token.Insert(1, "NPC_");

            return str.Replace(token, description);
        }

        #endregion

        private static string ReplaceGetChange(string str, Container underwear)
        {
            string token = "$GETTING_CHANGED_DIALOG$";

            if(!str.Contains(token)) return str;

            string gettingChangedDialog = Strings.RandString(Animations.Data.Diaper_Change_Dialog);
            gettingChangedDialog = Strings.ReplaceAndOr(gettingChangedDialog, underwear.wetness > 0, underwear.messiness > 0);

            int indexParameter = str.IndexOf(token, 0) + token.Length;
            string substring = str.Substring(indexParameter, str.Length - indexParameter);

            //str = str.Remove(indexParameter);

            // format string: $GETTING_CHANGED_DIALOG$ \"npcName\"
            string pattern = "\"([^\"]+)\"";
            
            MatchCollection matches = Regex.Matches(substring,pattern);

            Match match = Regex.Match(substring,pattern);

            str = Regex.Replace(str, pattern, "");

            if (match.Success)
            {
                string npcName = match.Groups[1].Value;

                gettingChangedDialog =  Strings.InsertVariable(gettingChangedDialog, "$NPC_NAME$", npcName);
            }
            else throw new Exception("Wrong Count of Parameters set for Token $GETTING_CHANGED_DIALOG$");

            /*
            if (matches.Count > 0)
            {
                List<string> parameters = new List<string>();   

                foreach(Match match in matches)
                {
                    parameters.Add(match.Groups[1].Value);
                }

                string npcName = parameters[0];

                gettingChangedDialog = Strings.ReplaceNpcName(gettingChangedDialog, npcName);

                Regression.monitor.Log($"Change Dialog: {gettingChangedDialog}",LogLevel.Info);
            }
            else throw new Exception("Wrong Count of Parameters set for Token $GETTING_CHANGED_DIALOG$");
            */
            str = str.Replace(token, "#$b#" + gettingChangedDialog);

            return str;
        }

        public static string ReplaceGenderTextNpc(string str, Gender gender)
        {
            str = str.Replace("$NPC_HE_SHE$", gender == Gender.Male ? getGenderText("He") : (gender == Gender.Female ? getGenderText("Hhe") : getGenderText("They")));
            str = str.Replace("$NPC_HIS_HER$", gender == Gender.Male ? getGenderText("His") : (gender == Gender.Female ? getGenderText("Her") : getGenderText("Their")));
            str = str.Replace("$NPC_HIM_HER$", gender == Gender.Male ? getGenderText("Him") : (gender == Gender.Female ? getGenderText("Her") : getGenderText("Them")));
            str = str.Replace("$NPC_HIS_HER_IS_ARE$", gender == Gender.Male ? getGenderText("His") : (gender == Gender.Female ? getGenderText("Her") : getGenderText("Their")));

            return str;
        }

        private static string getPrefix(bool plural)
        {
            return plural ? Regression.help.Translation.Get("Numeric_Translation.Plural") : Regression.help.Translation.Get("Numeric_Translation.Sigle");
        }

        private static string getGenderText(string key)
        {
            key = "Gender_Text" + "." + key;

            return Regression.help.Translation.Get(key);
        }

        #endregion

        public static string tryGetI18nText(string key)
        {

            string pattern = @"\{\{i18n:(.*?)\}\}"; ;
            Regex regex = new Regex(pattern);

            return regex.Replace(key, match =>
            {
                string st = match.Groups[1].Value;
                return Regression.help.Translation.Get(st);
            });
        }
    }
}
