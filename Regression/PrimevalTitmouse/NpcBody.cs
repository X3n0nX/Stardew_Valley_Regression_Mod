using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Network;
using StardewValley.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimevalTitmouse
{
    public class NpcBody
    {
        public NPC npc;
        public NpcBody(NPC npc)
        {
            this.npc = npc;
        }

        public static NpcBody ByName(string name, int range = 20)
        {
            name = name.ToLower();
            foreach (var npc in range <= 0 ? Utility.getAllCharacters() : Animations.NearbyVillager(range))
            {
                if (npc.Name.ToLower() == name)
                {
                    return new NpcBody(npc);
                }
            }
            return null;
        }
        public static List<NpcBody> ByRange(int range = 10)
        {
            var list = new List<NpcBody>();
            foreach (var npc in Utility.GetNpcsWithinDistance(((Character)Animations.player).Tile, range, (GameLocation)Game1.currentLocation))
            {
                list.Add(new NpcBody(npc));
            }
            return list;
        }
        // a very simplified accident mechanic that assumes that the npc has an accident.
        public void accident(IncidentType type, float accidentSize)
        {
            if (type == IncidentType.PEE)
            {
                underwear.AddPee(accidentSize);
                if (isWatching)
                {
                    npc.movementPause = 3000;
                    npc.doEmote(28, false);
                    Game1.playSound("wateringCan");
                }
                Regression.monitor.Log($"{npc.Name} piddled themselfs", LogLevel.Debug);
            }
            else
            {
                accidentSize = (accidentSize * 1.3f);
                underwear.AddPoop(accidentSize);
                if (isWatching)
                {
                    npc.movementPause = 3000;
                    npc.doEmote(12, false);
                    Animations.AnimateMessingEnd(npc);
                }
                Regression.monitor.Log($"{npc.Name} pooped themselfs", LogLevel.Debug);
            }
        }
        public void accidentFromFullness(IncidentType type)
        {
            var fullness = GetFullness(type);
            var accidentSize = (float)Regression.rnd.Next(300, 800);
            if (type == IncidentType.PEE)
            {
                if (fullness < 200) fullness += 200;
            }
            else
            {
                if (fullness < 300) fullness += 300;
            }
            accident(type,fullness);
            SetFullness(type, 0);
        }

        public void change(string underwearName = null, string pantsName = null)
        {
            var underwear = this.underwear;
            if (underwearName != null)
            {
                underwear.ResetToDefault(underwearName);
            }
            else
            {
                underwear.ResetToDefault(defaultUnderwear);
            }

            var pants = this.pants;
            pants.ResetToDefault(pants, 0, 0);
            Regression.monitor.Log($"{npc.Name} got changed and is now wearing {underwear.name} and {pants.name}", LogLevel.Debug);
        }

        public bool canGetGiveChangeNpc
        {
            get
            {
                int heartLevelForNpc = Game1.player.getFriendshipHeartLevelForNPC(npc.Name);
                switch (npc.Name.ToLower())
                {
                    case "vincent": // can get changed
                        return true;//Regression.ChildrenAndDiapers && heartLevelForNpc >= 6 && Game1.player.getFriendshipHeartLevelForNPC("Jodi") >= 4;
                    case "jas": // can get changed
                        return true;//Regression.ChildrenAndDiapers && heartLevelForNpc >= 6 && Game1.player.getFriendshipHeartLevelForNPC("Marnie") >= 4;
                    case "sam": // can get changed AND give changes
                        return true;//heartLevelForNpc >= 8 && Game1.player.dialogueQuestionsAnswered.Contains("124") || Game1.player.dialogueQuestionsAnswered.Contains("125");
                    case "jodi": // can give changes
                        return heartLevelForNpc >= 6;
                    case "abigail":
                        return true;//heartLevelForNpc >= 8;
                    case "gus":
                        return heartLevelForNpc >= 8;
                    case "maru":
                        return true;//heartLevelForNpc >= 4 || Game1.currentLocation.Name == "Hospital";
                    case "penny":
                        return true;//heartLevelForNpc > 6;
                    default:
                        return false;
                }
            }
        }

        public bool canGiveDirtyChangeNpc
        {
            get
            {
                int heartLevelForNpc = Game1.player.getFriendshipHeartLevelForNPC(npc.Name);
                switch (npc.Name.ToLower())
                {
                    case "gus":
                        return Game1.currentLocation.Name == "Saloon";
                    case "abigail":
                        return canGetGiveChangeNpc;
                    case "maru":
                        return canGetGiveChangeNpc || Game1.currentLocation.Name == "Hospital";
                    case "penny":
                        return canGetGiveChangeNpc;
                    case "sam": // will do it in the house of his mum if required
                    case "jodi": // will do it if its really important in her house
                        return Game1.player.currentLocation == npc.getHome();
                    default:
                        return false;
                }
            }
        }
        public float GetFullnessMax(IncidentType type)
        {
            switch (npc.Name.ToLower())
            {
                case "vincent":
                    if (type == IncidentType.PEE) return 400;
                    return 900;
                case "jas":
                    if (type == IncidentType.PEE) return 400;
                    return 800;
                case "sam":
                    if (type == IncidentType.PEE) return 550;
                    return 900;
                default:
                    if (type == IncidentType.PEE) return 700;
                    return 1000;
            }
        }
        public bool GetSuccessNext(IncidentType type)
        {
            var key = $"Potty/{type}/Success";
            if (!npc.modData.ContainsKey(key)) return false;
            return bool.Parse(npc.modData[key]);
        }
        public void SetSuccessNext(IncidentType type, bool value)
        {
            var key = $"Potty/{type}/Success";
            npc.modData[key] = value.ToString();
        }
        public float GetFullness(IncidentType type)
        {
            var key = $"Potty/{type}/Fullness";
            if (!npc.modData.ContainsKey(key)) return 0.0f;
            return float.Parse(npc.modData[key]);
        }
        public void SetFullness(IncidentType type, float value)
        {
            var key = $"Potty/{type}/Fullness";
            npc.modData[key] = value.ToString();
        }
        public bool isWatching
        {
            get
            {
                if (Game1.player.currentLocation != npc.currentLocation) return false; // Forcing silent if different map
                else if (Vector2.Distance(Game1.player.Tile, npc.Tile) > 30) return false; // If the distance is to far, we silence it
                return true;
            }
        }

        public bool RandomAction(float hours)
        {
            if (npc == null) return false;
            if (!npc.IsVillager) return false;

            // Chances per hour
            float getChangedChance = 1f;


            var underwear = this.underwear;
            switch (npc.Name.ToLower())
            {
                case "vincent": // is in diapers in general
                    if (!Regression.ChildrenAndDiapers) return false;
                    getChangedChance = 0.15f; // basechance getting changed (low, toddlers get only changed with a reason)
                    if (underwear.messiness > 0) getChangedChance = 0.4f; // chance of discovery, trying to hide it
                    if (underwear.messiness > (underwear.containment / 2f)) getChangedChance = 0.9f; // stinky, but still better at hiding
                    if (underwear.wetness > (underwear.absorbency / 2f)) getChangedChance = 0.5f; // chance that a waddle will be noticed
                    break;
                case "jas": // is in training pants in general
                    if (!Regression.ChildrenAndDiapers) return false;
                    getChangedChance = 0.15f; // basechance getting changed (low, toddlers get only changed with a reason)
                    if (underwear.messiness > 0) getChangedChance = 0.5f; // chance of discovery or jas tries to ask for help
                    if (underwear.messiness > (underwear.containment / 2f)) getChangedChance = 1.0f; // chance of discovery or jas tries to ask rises for full diapers
                    if (underwear.wetness > (underwear.absorbency / 2f)) getChangedChance = 0.7f; // chance that a waddle will be noticed or she wants to be clean
                    break;
                case "sam": // can get changed AND give changes
                    getChangedChance = 0.3f; // adults tend to get changed for other reasons, like getting out of slightly wet diapers, sam less so
                    if (underwear.messiness > 0) getChangedChance = 0.8f;
                    if (underwear.wetness > (underwear.absorbency / 2f)) getChangedChance = 0.7f;
                    break; // Always does it in secret
                case "abigail":
                    //if (Game1.player.getFriendshipHeartLevelForNPC(npc.Name) < 4) return false;

                    getChangedChance = 0.4f; // adults tend to get changed for other reasons, like getting out of slightly wet diapers
                    if (underwear.wetness > (underwear.absorbency / 2f)) getChangedChance = 0.9f;
                    if (underwear.messiness > 0) getChangedChance = 1.0f;

                    break;
                default:
                    return false;
            }


            if (getChangedChance > 0 && (underwear.messiness > 0 || underwear.wetness > 0) && Regression.rnd.NextDouble() <= getChangedChance * hours)
            {
                if (isWatching) npc.doEmote(12, false);
                change();
                return true;
            }

            if (PottyChanceIncidentWorker(IncidentType.PEE)) return true;
            if (PottyChanceIncidentWorker(IncidentType.POOP)) return true;


            return false;
        }

        private bool PottyChanceIncidentWorker(IncidentType incidentType)
        {
            var pottyChance = 1.0f;
            switch (npc.Name.ToLower())
            {
                case "vincent": // is in diapers in general
                    if (!Regression.ChildrenAndDiapers) return false;
                    pottyChance = 0f;
                    break;
                case "jas": // is in training pants in general
                    if (!Regression.ChildrenAndDiapers) return false;
                    pottyChance = incidentType == IncidentType.PEE ? 0.3f : 0.45f;
                    break;
                case "sam": // can get changed AND give changes
                    pottyChance = incidentType == IncidentType.PEE ? 0.2f : 0.6f;
                    break; // Always does it in secret
                case "abigail":
                    //if (Game1.player.getFriendshipHeartLevelForNPC(npc.Name) < 4) return false;
                    pottyChance = incidentType == IncidentType.PEE ? 0.3f : 0.8f;
                    break;
                default:
                    return false;
            }


            var color = incidentType == IncidentType.PEE ? new Color(byte.MaxValue, 225, 56) : new Color(146, 111, 91);
            var fullness = GetFullness(incidentType);


            var max = GetFullnessMax(incidentType);
            if (fullness >= max)
            {
                var success = GetSuccessNext(incidentType);
                var dialogs = PottyDialogs(incidentType, false, success);
                if (dialogs != null && dialogs.Length > 0)
                {
                    npc.showTextAboveHead(Strings.RandString(dialogs), color, 1, 3000, 2000);
                }
                if (success)
                {
                    npc.doEmote(5, false);
                    npc.movementPause = 3000;
                    SetFullness(incidentType, 0);
                }
                else
                {
                    accidentFromFullness(incidentType);
                }
                SetSuccessNext(incidentType, Regression.rnd.NextDouble() < pottyChance);
                return true;

            }
            else if (fullness + 70f >= max)
            {
                var success = GetSuccessNext(incidentType);
                var dialogs = PottyDialogs(incidentType, true, success);
                if (dialogs != null && dialogs.Length > 0)
                {
                    npc.showTextAboveHead(Strings.RandString(dialogs), color, 0, 3000, 0);
                }
            }
            SetFullness(incidentType, fullness + 35f + (35f * (float)Regression.rnd.NextDouble()));

            return false;
        }
        public string[] PottyDialogs(IncidentType type,bool preStage, bool success)
        {
            var dialogsStorage = Animations.Data.Potty_Dialogs;
            var stageStr = preStage ? "pre" : "post";  
            var typeStr = type == IncidentType.PEE ? "pee" : "poop";
            var successStr = success ? "success" : "fail";

            Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> dictionary;
            Dictionary<string, Dictionary<string, string[]>> typeDict;
            Dictionary<string, string[]> stageDict = null;
            string[] successDialogs;
            if (dialogsStorage.TryGetValue(npc.Name.ToLower(), out dictionary) && dictionary.TryGetValue(typeStr, out typeDict) && typeDict.TryGetValue(stageStr, out stageDict) && stageDict.TryGetValue(successStr, out successDialogs))
            {
                return successDialogs;
            }
            return null;
        }
        public void RemoveDialogue(string keyRemove)
        {

            // Temporary stack to hold dialogues we want to keep
            Stack<Dialogue> tempStack = new Stack<Dialogue>();

            // Iterate through the stack to find the dialogue to remove
            while (npc.CurrentDialogue.Count > 0)
            {
                Dialogue current = npc.CurrentDialogue.Pop();

                if (current.TranslationKey != keyRemove)
                {
                    tempStack.Push(current); // Keep this dialogue if it doesn't match
                }
            }

            // Restore the remaining dialogues back into the original stack
            while (tempStack.Count > 0)
            {
                npc.CurrentDialogue.Push(tempStack.Pop());
            }
        }

        public string defaultUnderwear
        {
            get
            {
                switch (npc.Name.ToLower())
                {
                    case "vincent":
                        return "baby print diaper";
                    case "jas":
                        return Regression.rnd.Next(1, 3) != 3 ? "training pants" : "lavender pullups"; // sometimes the training pants are all dirty
                    case "sam":
                        return Regression.rnd.Next(1, 4) != 4 ? "baby print diaper" : "joja diaper"; // sometimes sam tries bigger diapers
                    case "abigail":
                        return "joja diaper";
                    default:
                        return npc.Gender == Gender.Male ? "big kid undies" : "polka dot panties";
                }
            }

        }
        public Container underwear
        {
            get
            {
                return new Container(npc, "underwear", defaultUnderwear);
            }
        }
        public string npcDefaultPantsName
        {
            get
            {
                switch (npc.Name.ToLower())
                {
                    case "vincent":
                        return "toddler pants";
                    case "jas":
                        return "purple toddler skirts";
                    default:
                        return npc.Gender == Gender.Male ? "pants" : "skirt";
                }
            }

        }
        public Container pants
        {
            get
            {
                var staticType = "blue jeans";
                var defaultName = npcDefaultPantsName;
                var pants = new Container(npc, "pants", staticType);
                if (pants.displayName == staticType && defaultName != "" && defaultName != pants.displayName)
                {
                    pants.displayName = defaultName;
                    pants.description = defaultName;
                }
                return pants;
            }
        }

        public Stack<Dialogue> CurrentDialogue
        {
            get
            {
                return npc.CurrentDialogue;
            }
        }
        public int Age
        {
            get
            {
                return npc.Age;
            }
        }
        public List<string> GetVillagerReactions(string responseKey, string fallbackKey = null)
        {
            var npcType = Animations.npcTypeList(npc);
            List<string> stringList3 = new List<string>();
            foreach (string key2 in npcType)
            {
                Dictionary<string, string[]> dictionary;
                string[] strArray;
                if (Animations.Data.Villager_Reactions.TryGetValue(key2, out dictionary) && dictionary.TryGetValue(responseKey, out strArray))
                {
                    stringList3 = new List<string>(); // We could remove this line again, but the general texts are more meant as fallback, they often don't fit well if custom texts are defined
                    stringList3.AddRange((IEnumerable<string>)strArray);
                }
            }
            if (fallbackKey != null && stringList3.Count < 1) return GetVillagerReactions(fallbackKey, null);
            return stringList3;
        }
    }
}
