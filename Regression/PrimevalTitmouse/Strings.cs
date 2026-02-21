using HarmonyLib;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.RegularExpressions;
using static PrimevalTitmouse.Strings;

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
                str = ReplaceGetChangeDialog(str, c);

                str = ReplaceConditionalOptional(str, "OnSlightlyWet", c.used && !c.used_bad);
                str = ReplaceConditionalOptional(str, "OnClean", !c.used);

                str = ReplaceChangedByNpc(str, c);
                str = ReplaceUnderwearToken(str,c);
                str = ReplaceUnderwearToken(str, c,oldNew: OldNew.New);
                str = ReplaceInspectUnderwearToken(str, c);

                str = Strings.ReplaceOr(str,!c.plural,"#");
            }

            if (b != null)
            {
                str = ReplaceConditionalOptional(str, "OnDebuffed", b.HasWetOrMessyDebuff());

                str = ReplacePantsToken(str,b.pants);
                str = str.Replace("$BEDDING_DRYTIME$", Game1.getTimeOfDayString(b.bed.timeWhenDoneDrying.time));
            }

            str = ReplaceOr(str, who.IsMale, "/");
            str = ReplaceFarmername(str);

            return str;
        }
        
        public static string InsertVariables(string msg, NPC b, Container c = null)
        {
            string str = msg;
            if (b != null && c == null)
                c = new NpcBody(b).underwear;
            if (c != null)
            {
                var changeOtherDialog = RandString(Animations.Data.Change_Other_Dialog);
                changeOtherDialog = ReplaceAndOr(changeOtherDialog, c.wetness > 0, c.messiness > 0);
                changeOtherDialog += npcUnderwearOptions(b);
                str = str.Replace("$CHANGE_OTHER_DIALOG$", changeOtherDialog);

                str = ReplaceConditionalOptional(str, "NpcOnSlightlyWet", c.used && !c.used_bad);
                str = ReplaceConditionalOptional(str, "NpcOnUsedBad", c.used && c.used_bad);
                str = ReplaceConditionalOptional(str, "NpcOnClean", !c.used);

                str = ReplaceUnderwearToken(str, c, npc: true);
                str = ReplaceUnderwearToken(str, c, npc: true,oldNew: OldNew.New);
                str = ReplaceInspectUnderwearToken(str, c, npc: true);

                str = ReplaceOr(str, !c.plural, "#");
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
        public static string ReplaceUnderwearToken(string str, Container underwear, bool npc = false, OldNew oldNew = OldNew.None)
        {
            str = ReplaceUnderwearPrefix(str,underwear.plural, npc: npc, oldNew: oldNew);
            str = ReplaceUnderwearName(str, underwear.displayName, npc: npc, oldNew: oldNew);
            str = ReplaceUnderwearDesc(str, underwear.description, npc: npc, oldNew: oldNew);

            return str;
        }

        // possible tokens:
        //      $UNDERWEAR_PREFIX$              Players new underwear prefix
        //      $OLD_UNDERWEAR_PREFIX$          Players old underwear prefix
        //      $NPC_UNDERWEAR_PREFIX$          NPCs new underwear prefix
        //      $NPC_OLD_UNDERWEAR_PREFIX$      NPCs old underwear prefix
        private static string ReplaceUnderwearPrefix(string str, bool plural = false, bool npc = false, OldNew oldNew = OldNew.None)
        {
            string token = "UNDERWEAR_PREFIX";

            if (oldNew == OldNew.Old) token = "OLD_" + token;
            if (oldNew == OldNew.New) token = "NEW_" + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, getPrefix(plural));
        }

        // possible tokens:
        //      $UNDERWEAR_NAME$                Players new underwear name
        //      $OLD_UNDERWEAR_NAME$            Players old underwear name
        //      $NPC_UNDERWEAR_NAME$            NPCs new underwear name
        //      $NPC_OLD_UNDERWEAR_NAME$        NPCs old underwear name
        private static string ReplaceUnderwearName(string str, string underwearName, bool npc = false, OldNew oldNew = OldNew.None)
        {
            string token = "UNDERWEAR_NAME";

            if (oldNew == OldNew.Old) token = "OLD_" + token;
            if (oldNew == OldNew.New) token = "NEW_" + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, underwearName);
        }

        // possible tokens:
        //      $UNDERWEAR_DESC$                Description of the players new underwear
        //      $OLD_UNDERWEAR_DESC$            Description of the players old underwear
        //      $NPC_UNDERWEAR_DESC$            Description of the npcs new underwear
        //      $NPC_OLD_UNDERWEAR_DESC$        Description of the npcs old underwear
        private static string ReplaceUnderwearDesc(string str, string description, bool npc = false, OldNew oldNew = OldNew.None)
        {
            string token = "UNDERWEAR_DESC";

            if (oldNew == OldNew.Old) token = "OLD_" + token;
            if (oldNew == OldNew.New) token = "NEW_" + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, description);
        }

        public static string ReplaceInspectUnderwearToken(string str, Container underwear, bool npc = false, bool old = false)
        {
            str = ReplaceInspectUnderwearName(str,underwear,npc: npc, old: old);
            str = ReplaceInspectUnderwearName(str, underwear, npc: npc, noPrefix: true, old: old);
            str = ReplaceInspectUnderwearDesc(str, underwear, npc: npc, old: old);

            return str;
        }

        // possible tokens:
        //      $INSPECT_UNDERWEAR_NAME$                        State and name of the players new underwear
        //      $INSPECT_UNDERWEAR_NAME_NO_PREFIX$              State and name of the players new underwear without prefix
        //      $INSPECT_OLD_UNDERWEAR_NAME$                    State and name of the players old underwear
        //      $INSPECT_OLD_UNDERWEAR_NAME_NO_PREFIX$          State and name of the players old underwear without prefix
        //      $NPC_INSPECT_UNDERWEAR_NAME$                    State and name of the npcs new underwear
        //      $NPC_INSPECT_UNDERWEAR_NAME_NO_PREFIX$          State and name of the npcs new underwear without prefix
        //      $NPC_INSPECT_OLD_UNDERWEAR_NAME$                State and name of the npcs old underwear
        //      $NPC_INSPECT_OLD_UNDERWEAR_NAME_NO_PREFIX$      State and name of the npcs old underwear without prefix
        private static string ReplaceInspectUnderwearName(string str, Container underwear, bool noPrefix = false, bool npc = false, bool old = false)
        {
            string token = "UNDERWEAR_NAME";
            string tokenPre = "INSPECT_";

            if (noPrefix) token = token + "_NO_PREFIX";
            if (old) token = "OLD_" + token;
            token = tokenPre + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, Strings.DescribeUnderwear(underwear, underwear.displayName, noPrefix));
        }

        // possible tokens:
        //      $INSPECT_UNDERWEAR_DESC$            Description of the players new underwear
        //      $INSPECT_OLD_UNDERWEAR_DESC$        Description of the players old underwear
        //      $NPC_INSPECT_UNDERWEAR_DESC$        Description of the npcs new underwear
        //      $NPC_INSPECT_OLD_UNDERWEAR_DESC$    Description of the npcs old underwear
        private static string ReplaceInspectUnderwearDesc(string str, Container underwear, bool npc = false, bool old = false)
        {
            string token = "UNDERWEAR_DESC";
            string tokenPre = "INSPECT_";

            if (old) token = "OLD_" + token;
            token = tokenPre + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

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

        // possible tokens:
        //      $PANTS_PREFIX$              Players new pants prefix
        //      $OLD_PANTS_PREFIX$          Players old pants prefix
        //      $NPC_PANTS_PREFIX$          NPCs new pants prefix
        //      $NPC_OLD_PANTS_PREFIX$      NPCs old pants prefix
        private static string ReplacePantsPrefix(string str, bool plural = false, bool npc = false, bool old = false)
        {
            string token = "PANTS_PREFIX";

            if (old) token = "OLD_" + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, getPrefix(plural));
        }

        // possible tokens:
        //      $PANTS_NAME$                Players new pants name
        //      $OLD_PANTS_NAME$            Players old pants name
        //      $NPC_PANTS_NAME$            NPCs new pants name
        //      $NPC_OLD_PANTS_NAME$        NPCs old pants name
        private static string ReplacePantsName(string str, string pantsName, bool npc = false, bool old = false)
        {
            string token = "PANTS_NAME";

            if (old) token = "OLD_" + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, pantsName);
        }

        // possible tokens:
        //      $PANTS_DESC$                Players new pants description
        //      $OLD_PANTS_DESC$            Players old pants description
        //      $NPC_PANTS_DESC$            NPCs new pants description
        //      $NPC_OLD_PANTS_DESC$        NPCs old pants description
        private static string ReplacePantsDesc(string str, string description, bool npc = false, bool old = false)
        {
            string token = "PANTS_DESC";

            if (old) token = "OLD_" + token;
            if (npc) token = "NPC_" + token;

            token = "$" + token + "$";

            return str.Replace(token, description);
        }

        #endregion

        // format string: $GETTING_CHANGED_DIALOG$ \"npcName\"
        private static string ReplaceGetChangeDialog(string str, Container underwear)
        {
            string token = "$GETTING_CHANGED_DIALOG$";

            if(!str.Contains(token)) return str;

            string gettingChangedDialog = Strings.RandString(Animations.Data.Diaper_Change_Dialog);
            gettingChangedDialog = Strings.ReplaceAndOr(gettingChangedDialog, underwear.wetness > 0, underwear.messiness > 0);

            string npcName = "";
            string[] parameters;

            str = GetParameters(str, token,out parameters);

            if (parameters.Length > 1)
            {
                npcName = parameters[0];
            }
            else throw new Exception($"Wrong Count of Parameters set for Token {token}");

            str = str.Replace(token, "#$b#" + gettingChangedDialog);

            return str;
        }

        // format string: $NPC_NAME$ \"npcName\" \"underwearName\" \"pantsName\" \"id\"
        // parameter 1: name of the npc who changes you
        // parameter 2: name of new underwear the npc will change the player
        // parameter 3: (optional) name of the pants the npc will change the player if there are dirty
        // parameter 4: (optional) id of the npc´s dialogue in "Villager_Changeing_Reactions" in file Regression.json
        //              if free try to randomize the id
        //              if no insert in "Villager_Changeing_Reactions" use gender specific dialogue
        public static string ReplaceChangedByNpc(string str, Container underwear)
        {
            string token = "$CHANGED_BY_NPC$";

            if (!str.Contains(token)) return str;

            string npcName = "";
            string underwearName = "";
            string pantsName = "";
            int id = -1;

            string[] parameters;

            str = GetParameters(str,token,out parameters);

            if (parameters.Length >= 2)
            {
                npcName = parameters[0];
                underwearName = parameters[1];

                if (parameters.Length >= 3)
                {
                    if (!int.TryParse(parameters[2], out id))
                    {
                        id = -1;
                        pantsName = parameters[2];
                    }

                    if(parameters.Length >= 4)
                    {
                        if (!int.TryParse(parameters[3], out id)) 
                            throw new Exception("Parameter for id is not an int for Token $CHANGED_BY_NPC$");
                    }
                }
            }
            else throw new Exception($"Wrong Count of Parameters set for Token {token}");

            string changedByNpc = "";
            int rndId = 0;

            if (id == -1)
            {
                Dictionary<int, string> changingDialoges = new Dictionary<int, string>();

                // npc has dialogues in "Villager_Changeing_Reactions"
                if (Animations.Data.Villager_Changeing_Dialoges.TryGetValue(npcName, out changingDialoges))
                {
                    rndId = Regression.rnd.Next(1, Animations.Data.Villager_Changeing_Dialoges[npcName].Count);
                    changedByNpc = Animations.Data.Villager_Changeing_Dialoges[npcName][rndId];
                }
                else 
                {
                    bool female = Animations.GetNpcGender(npcName) == Gender.Female ? true : false;

                    if (!female)
                    {
                        rndId = Regression.rnd.Next(1, Animations.Data.Villager_Changeing_Dialoges["adult_male"].Count);
                        changedByNpc = Animations.Data.Villager_Changeing_Dialoges["adult_male"][rndId];
                    }
                    else
                    {
                        rndId = Regression.rnd.Next(1, Animations.Data.Villager_Changeing_Dialoges["adult_female"].Count);
                        changedByNpc = Animations.Data.Villager_Changeing_Dialoges["adult_female"][rndId];
                    }
                }
            }
            else
            {
                Dictionary<int, string> changingDialoges = new Dictionary<int, string>();

                // npc has dialogues in "Villager_Changeing_Reactions"
                if (Animations.Data.Villager_Changeing_Dialoges.TryGetValue(npcName, out changingDialoges))
                {
                    if(!changingDialoges.TryGetValue(id,out changedByNpc))
                        throw new Exception($"Id {id} not found in Villager_Changeing_Dialoges for {npcName}");
                }
            }

            changedByNpc = tryGetI18nText(changedByNpc);

            changedByNpc = Strings.ReplaceAndOr(changedByNpc, underwear.wetness > 0, underwear.messiness > 0);

            changedByNpc = "#$b#" + changedByNpc;

            npcName = " \"" + npcName + "\"";
            underwearName = " \"" + underwearName + "\"";
            pantsName = pantsName != "" ? " \"" + pantsName + "\"" : "";

            string actionString = $"#$action DIAPER_CHANGE{npcName}{underwearName}{pantsName}";

            changedByNpc = changedByNpc + actionString;

            str = str.Replace(token, changedByNpc);

            return str;
        }

        public static string GetParameters(string str,string token, out string[] parameters)
        {
            string pattern = "\"([^\"]+)\"";
            parameters = null;

            if (!str.Contains(token)) return str;

            int indexParameter = str.IndexOf(token, 0) + token.Length;
            string substring = str.Substring(indexParameter, str.Length - indexParameter);
            int indexParameterEnd = substring.IndexOf("#", 0);

            if(indexParameterEnd > 0)
                substring = substring.Remove(indexParameterEnd + 1);

            MatchCollection matches = Regex.Matches(substring, pattern);
            parameters = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                parameters[i] = matches[i].Groups[1].Value;
            }

            return str.Replace(substring, "");
        }

        public static string ReplaceGenderTextNpc(string str, Gender gender)
        {
            str = str.Replace("$NPC_HE_SHE$", gender == Gender.Male ? getGenderText("He") : (gender == Gender.Female ? getGenderText("She") : getGenderText("They")));
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

        public enum OldNew
        {
            None,
            Old,
            New
        }
    }
}
