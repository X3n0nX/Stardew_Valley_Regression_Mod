using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Regression;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Delegates;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.SaveSerialization;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using xTile.Dimensions;
using static StardewValley.Minigames.TargetGame;

namespace PrimevalTitmouse
{
    public class Regression : Mod
    {
        public static int lastTimeOfDay = 0;
        public static Random rnd = new Random();
        public static bool started = false;
        public static Body body;
        public static Config config;
        public static IModHelper help;
        public static IMonitor monitor;
        public bool shiftHeld;
        public static Data t;
        public static Farmer who;
        public static string dirtyEventToken = "dirtyEventToken";
        public static string generalEventToken = "generalEventToken";
        public static bool SelfIsFurry = false;
        public static bool WorldIsFurry = false;
        const float timeInTick = (1f / 43f); //One second realtime ~= 1/43 hours in game
        public Dictionary<string, bool> jsonLoaded = new();
        public override void Entry(IModHelper h)
        {
            var harmony = new HarmonyLib.Harmony("com.primevaltitmouse.regression");
            harmony.PatchAll();

            help = h;

            monitor = Monitor;
            config = Helper.ReadConfig<Config>();
            //t = Helper.Data.ReadJsonFile<Data>(string.Format("{0}.json", (object)config.Lang)) ?? Helper.Data.ReadJsonFile<Data>("en.json");
            t = Helper.Data.ReadJsonFile<Data>("Regression.json");
            h.Events.GameLoop.Saving += new EventHandler<SavingEventArgs>(this.BeforeSave);
            h.Events.GameLoop.DayStarted += new EventHandler<DayStartedEventArgs>(ReceiveAfterDayStarted);
            h.Events.GameLoop.OneSecondUpdateTicking += new EventHandler<OneSecondUpdateTickingEventArgs>(ReceiveUpdateTick);
            h.Events.GameLoop.TimeChanged += new EventHandler<TimeChangedEventArgs>(ReceiveTimeOfDayChanged);
            h.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(OnGameLaunched);
            h.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(ReceiveKeyPress);
            h.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(ReceiveMouseChanged);
            h.Events.Display.MenuChanged += new EventHandler<MenuChangedEventArgs>(ReceiveMenuChanged);
            h.Events.Display.RenderingHud += new EventHandler<RenderingHudEventArgs>(ReceivePreRenderHudEvent);

            h.Events.GameLoop.UpdateTicked += new EventHandler<UpdateTickedEventArgs>(OnUpdateTicked);

            h.Events.Player.Warped += new EventHandler<WarpedEventArgs>(OnWarped);

            h.ConsoleCommands.Add("dialog", "Triggers a debug dialog. Usage: dialog <npcName> <message>", TriggerDialogCommand);
            h.ConsoleCommands.Add("emote", "Sets an NPC's emote. Usage: set_emote <NPCName> <EmoteID>", SetEmoteCommand);
            // A multi action manager, as the game only allowes one action to be triggered
            // Example: $action ACTIONS CHANGE_DIAPER_OTHERS vincent \"baby print diaper\" ADD_DIALOG \"Thank you so much! Here!\" \"change_vincent\" GIVE_UNDERWEAR \"baby print diaper\"
            TriggerActionManager.RegisterAction("ACTIONS", this.ActionManager);

            // This is about you getting your diapers changed by someone
            // Example: $action DIAPER_CHANGE \"baby print diaper\" \"sams pants\"
            TriggerActionManager.RegisterAction("DIAPER_CHANGE", this.StartChange);

            // This is about you changing others diapers, parameter 2 and 3 optional
            // Example: $action CHANGE_DIAPER_OTHERS jas
            // Example: $action CHANGE_DIAPER_OTHERS vincent "baby print diaper" "toddler pants"
            TriggerActionManager.RegisterAction("CHANGE_DIAPER_OTHERS", this.StartChangeOthers);

            // This is about the player having accidents, but can also be pointed at npc.
            // Example: $action DIAPER_ACCIDENT player pee
            // Example: $action DIAPER_ACCIDENT vincent poop
            TriggerActionManager.RegisterAction("DIAPER_ACCIDENT", this.StartAccident);

            // This is adds a dialog. Parameter 3 is optional, containing a key. If there is a message with this key already present, the message will be replaced
            // Example: $action ADD_DIALOG jodi "Did you change vincent into training pants? You should know better by now!#$b#%Jodi seams to be upset with you" "change_vincent_wrong"
            TriggerActionManager.RegisterAction("ADD_DIALOG", this.AddNpcMessage);

            // This adds underwear to the players inventory, usually as a gift from an npc
            // Example: $action GIVE_UNDERWEAR "training pants"
            TriggerActionManager.RegisterAction("GIVE_UNDERWEAR", this.GiveUnderwear);


            GameStateQueryDelegate queryDelegate = (GameStateQueryDelegate)Delegate.CreateDelegate(typeof(GameStateQueryDelegate), this, "DIAPER_USED");
            GameStateQuery.Register("DIAPER_USED", queryDelegate);

            GameStateQueryDelegate queryDelegateBad = (GameStateQueryDelegate)Delegate.CreateDelegate(typeof(GameStateQueryDelegate), this, "DIAPER_USED_BAD");
            GameStateQuery.Register("DIAPER_USED_BAD", queryDelegateBad);

            WorldIsFurry = Helper.ModRegistry.IsLoaded("sion9000.AnthroCharactersContinued");
            SelfIsFurry = Helper.ModRegistry.IsLoaded("krystedez.FurryFarmer");
        }
        private void SetEmoteCommand(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log("Usage: set_emote <NPCName> <EmoteID>", LogLevel.Error);
                return;
            }

            string npcName = args[0];
            if (!int.TryParse(args[1], out int emoteId))
            {
                Monitor.Log("EmoteID must be an integer.", LogLevel.Error);
                return;
            }

            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc == null)
            {
                Monitor.Log($"NPC '{npcName}' not found.", LogLevel.Error);
                return;
            }

            if (emoteId < 0 || emoteId > 64)
            {
                Monitor.Log($"EmoteID '{emoteId}' is out of range. Valid IDs: 0-64.", LogLevel.Error);
                return;
            }

            npc.doEmote(emoteId, false);

            Monitor.Log($"Set {npcName}'s emote to {emoteId}.", LogLevel.Info);
        }

        /// <summary>
        /// Handler for the "dialog" console command.
        /// Usage: dialog <npcName> <message>
        /// </summary>
        private void TriggerDialogCommand(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log("Usage: dialog <npcName> <message>", LogLevel.Error);
                return;
            }

            string npcName = args[0];
            string message = string.Join(" ", args, 1, args.Length - 1);

            TriggerDebugDialog(npcName, message);
        }
        /// <summary>
        /// Handler for the "dialog" console command.
        /// Usage: trigger_dialog <npcName> <message>
        /// </summary>

        /// <summary>
        /// Triggers a dialog for the specified NPC with the given message.
        /// </summary>
        /// <param name="npcName">Name of the NPC.</param>
        /// <param name="message">Dialog message.</param>
        private void TriggerDebugDialog(string npcName, string message)
        {
            NPC npc = Game1.getCharacterFromName(npcName);
            if (npc != null)
            {
                // Create a new Dialogue object
                Dialogue dialogue = new Dialogue(npc, "", message);

                npc.setNewDialogue(dialogue, true, true);

                // Show the dialogue to the player
                Game1.drawDialogue(npc);

                // Log to confirm the dialog was triggered
                Monitor.Log($"Dialog triggered for {npcName}: {message}", LogLevel.Info);
            }
            else
            {
                Monitor.Log($"NPC '{npcName}' not found!", LogLevel.Warn);
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            return;
        }
        public bool DIAPER_USED(string[] query, GameStateQueryContext context)
        {
            var underwear = body.underwear;

            var targetNpcName = query.Length > 1 ? query[1] : null;
            if (targetNpcName != null)
            {
                switch (targetNpcName)
                {
                    case "pee":
                        return underwear.wetness > 0;
                    case "poop":
                        return underwear.messiness > 0;
                    default:
                        break;
                }

                var npc = NpcBody.ByName(targetNpcName, 20);
                if (npc == null) return false;

                underwear = npc.underwear;
                // we don't null check, that should not happen and if it does, we want to see that

                // if only one parameter used in quarry whitch is the npc name
                if (query.Length == 2 && underwear.used) return true;

            }
            switch (query.Length > 2 ? query[2]?.ToLower() : null)
            {
                case "pee":
                    return underwear.wetness > 0;
                case "poop":
                    return underwear.messiness > 0;
                default:
                    return underwear.used;
            }
        }
        public bool DIAPER_USED_BAD(string[] query, GameStateQueryContext context)
        {
            var underwear = body.underwear;

            var targetNpcName = query.Length > 1 ? query[1] : null;
            if (targetNpcName != null)
            {
                switch (targetNpcName)
                {
                    case "pee":
                        return underwear.wetness > (underwear.absorbency / 2);
                    case "poop":
                        return underwear.messiness > (underwear.containment / 2);
                    default:
                        break;
                }
                var npc = NpcBody.ByName(targetNpcName, 20);
                if (npc == null) return false;

                underwear = npc.underwear;
                // we don't null check, that should not happen and if it does, we want to see that

                // if only one parameter used in quarry whitch is the npc name
                if (query.Length == 2 && underwear.used_bad) return true;
            }
            switch (query.Length > 2 ? query[2]?.ToLower() : null)
            {
                case "pee":
                    return underwear.wetness > (underwear.absorbency / 2);
                case "poop":
                    return underwear.messiness > (underwear.containment / 2);
                default:
                    return underwear.used_bad; // if very wet OR slightly poopy (yes, even if a little, not like the specific questions on top)
            }
        }

        private void GiveUnderwear()
        {
            List<Item> objList = new List<Item>();
            foreach (string validUnderwearType in Strings.ValidUnderwearTypes())
                objList.Add(new Underwear(validUnderwearType, 20));
            objList.Add(new StardewValley.Object("399", 99, false, -1, 0));
            objList.Add(new StardewValley.Object("348", 99, false, -1, 0));
            Game1.activeClickableMenu = new ItemGrabMenu(objList);
        }

        private void ReceiveAfterDayStarted(object sender, DayStartedEventArgs e)
        {
            var configFile = "RegressionSave.json";
            if (config.ReadSaveFiles)
            {
                body = Helper.Data.ReadJsonFile<Body>(string.Format("{0}/{1}", Constants.SaveFolderName, configFile));
                if (body == null) body = new Body();
                else jsonLoaded[configFile] = true;
            }
            else
            {
                body = new Body();
            }

            started = true;
            who = Game1.player;

            configFile = "RegressionSaveInv.json";
            Dictionary<int, Dictionary<string, string>> invReplacement = null;
            if (config.ReadSaveFiles) invReplacement = Helper.Data.ReadJsonFile<Dictionary<int, Dictionary<string, string>>>(string.Format("{0}/{1}", Constants.SaveFolderName, configFile));
            if (invReplacement != null) jsonLoaded[configFile] = true;
            restoreItems(Game1.player.Items, invReplacement);

            configFile = "RegressionSaveChest.json";
            Dictionary<string, Dictionary<int, Dictionary<string, string>>> chestReplacement = null;
            if (config.ReadSaveFiles) chestReplacement = Helper.Data.ReadJsonFile<Dictionary<string, Dictionary<int, Dictionary<string, string>>>>(string.Format("{0}/{1}", Constants.SaveFolderName, configFile));
            if (chestReplacement != null) jsonLoaded[configFile] = true;

            int locId = 0;
            foreach (var location in Game1.locations)
            {
                foreach (var obj in location.Objects.Values)
                {
                    var id = string.Format("{0}-{1}-{2}", locId, obj.TileLocation.X, obj.TileLocation.Y);
                    if (obj is Chest chest)
                    {
                        restoreItems(chest.Items, chestReplacement != null && chestReplacement.ContainsKey(id) ? chestReplacement[id] : null);
                    }
                }
                locId++;
            }

            Animations.AnimateNight(body);
            HandleMorning(sender, e);
        }

        private void HandleMorning(object Sender, DayStartedEventArgs e)
        {
            body.HandleMorning();
            Mail.CheckMail();
        }

        public static Dictionary<int, Dictionary<string, string>> replaceItems(IList<Item> items)
        {
            var replacements = new Dictionary<int, Dictionary<string, string>>();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is Underwear)
                {
                    var underwear = (items[i] as Underwear);
                    items[i] = underwear.getReplacement();
                    replacements.Add(i, underwear.getAdditionalSaveData());
                }
            }

            return replacements;
        }
        public static void restoreItems(IList<Item> items, Dictionary<int, Dictionary<string, string>> invReplacement = null)
        {
            if (invReplacement != null)
            {
                foreach (KeyValuePair<int, Dictionary<string, string>> entry in invReplacement)
                {
                    var underwear = new Underwear();
                    underwear.rebuild(entry.Value, items[entry.Key]);
                    if (items[entry.Key] != null)
                    {
                        underwear.modData.CopyFrom(items[entry.Key].modData);
                    }
                    items[entry.Key] = underwear;
                }
            }


            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                // we have to access the type this way, because it's not yet of the correct class
                if (item?.modData?.ContainsKey(Container.BuildKeyFor("name", Underwear.modDataKey)) == true)
                {
                    var underwear = new Underwear();
                    underwear.modData.CopyFrom(item.modData);
                    underwear.Stack = item.Stack;
                    items[i] = underwear;
                }
            }
        }


        //Save Mod related variables in separate JSON. Also trigger night handling if not on the very first day.
        private void BeforeSave(object Sender, SavingEventArgs e)
        {
            body.bedtime = lastTimeOfDay;
            if (Game1.dayOfMonth != 1 || Game1.currentSeason != "spring" || Game1.year != 1)
                body.HandleNight();

            var chestReplacements = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            if (config.WriteSaveFiles)
            {
                int locId = 0;
                foreach (var location in Game1.locations)
                {
                    foreach (var obj in location.Objects.Values)
                    {
                        if (obj is Chest chest)
                        {
                            var id = string.Format("{0}-{1}-{2}", locId, obj.TileLocation.X, obj.TileLocation.Y);
                            chestReplacements.Add(id, replaceItems(chest.Items));
                        }
                    }

                    foreach (var furn in location.furniture.OfType<StorageFurniture>())
                    {
                        Monitor.Log(string.Format("Found storage furniture {0}", furn.DisplayName), LogLevel.Info);
                    }
                    locId++;
                }
            }
            var invReplacements = config.WriteSaveFiles ? replaceItems(Game1.player.Items) : null;


            var configFile = "RegressionSave.json";
            if (config.WriteSaveFiles || jsonLoaded.ContainsKey(configFile))
            {
                Helper.Data.WriteJsonFile(string.Format("{0}/{1}", Constants.SaveFolderName, configFile), config.WriteSaveFiles ? body : null);
                if (jsonLoaded.ContainsKey(configFile)) jsonLoaded.Remove(configFile);
            }

            configFile = "RegressionSaveInv.json";
            if (config.WriteSaveFiles || jsonLoaded.ContainsKey(configFile))
            {
                Helper.Data.WriteJsonFile(string.Format("{0}/{1}", Constants.SaveFolderName, configFile), config.WriteSaveFiles ? invReplacements : null);
                if (jsonLoaded.ContainsKey(configFile)) jsonLoaded.Remove(configFile);
            }

            configFile = "RegressionSaveChest.json";
            if (config.WriteSaveFiles || jsonLoaded.ContainsKey(configFile))
            {
                Helper.Data.WriteJsonFile(string.Format("{0}/{1}", Constants.SaveFolderName, configFile), config.WriteSaveFiles ? chestReplacements : null);
                if (jsonLoaded.ContainsKey(configFile)) jsonLoaded.Remove(configFile);
            }
        }

        public bool StartAccident(string[] args, TriggerActionContext context, out string error)
        {

            error = null;

            if (args.Length < 2)
            {
                error = "Parameter 1, type, should be given!";
                return false;
            }
            var typeStr = args[1];
            IncidentType type = IncidentType.PEE;
            switch (typeStr)
            {
                case "pee":
                    type = IncidentType.PEE;
                    break;
                case "poop":
                    type = IncidentType.POOP;
                    break;
                default:
                    error = $"Parameter 1, type '{type}' is unknown!";
                    return false;
            }

            if (args.Length < 3)
            {
                error = "Parameter 2, target, be 'player' or an npc name";
                return false;
            }
            var target = args.Length < 3 ? "player" : args[2];
            target = target.ToLower();


            if (target == "player" || target == "farmer" || target == "self")
            {
                body.Accident(type);
            }
            else
            {
                var targetNpc = NpcBody.ByName(target, 20);
                if (targetNpc == null)
                {
                    error = $"Parameter 2, '{target}' not found in local npc list in 20 range";
                    return false;
                }
                targetNpc.accidentFromFullness(type);
            }
            return true;
        }
        public static Underwear GetUnderwearFromInventory(string underwearName, bool clean = true)
        {
            if (who != null)
            {
                if (who.ActiveObject != null)
                {
                    if (who.ActiveObject is Underwear)
                    {
                        var container = ((Underwear)who.ActiveObject).container;
                        if (container.name.ToLower() == underwearName.ToLower())
                        {
                            if (!clean || !container.used)
                            {
                                return (Underwear)who.ActiveObject;
                            }

                        }
                    }
                    return null;
                }
                foreach (var item in who.Items)
                {
                    if (item is Underwear)
                    {
                        var underwearFromInventory = item as Underwear;
                        if (underwearFromInventory.container.name.ToLower() == underwearName.ToLower())
                        {
                            if (!clean || !underwearFromInventory.container.used)
                            {
                                return underwearFromInventory;
                            }
                        }
                    }
                }
            }
            return null;
        }

        /*
        public static bool MakeUnderwearActive(string underwearName, bool clean = true)
        {
            if (who.ActiveObject != null) return false;
            var found = GetUnderwearFromInventory(underwearName, clean);
            if(found == null) return false;
            who.ActiveObject = found;
            return true;
        }*/
        public static bool HasUnderwear(string underwearName, bool clean = true)
        {
            return GetUnderwearFromInventory(underwearName, clean) != null;
        }

        public bool StartChangeOthers(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                if (args.Length < 2)
                {
                    error = "Parameter 1, target, has to be the name of an npc";
                    return false;
                }
                var target = args[1];
                target = target.ToLower();
                var targetNpc = NpcBody.ByName(target, 20);
                if (targetNpc == null)
                {
                    error = $"Parameter 1, '{target}' not found in local npc list in 20 range";
                    return false;
                }

                string underwearName = args.Length > 2 ? args[2] : null;
                var newUnderwear = GetUnderwearFromInventory(underwearName);
                if (newUnderwear == null)
                {
                    error = $"Parameter 2, '{underwearName}' not found in inventory";
                    return false;
                }

                var currentUnderwear = targetNpc.underwear;
                // We create a new, untethered container first, of the same type.
                Underwear oldUnderwear = new Underwear(currentUnderwear.type, 1);
                // We "reset" that new container to the same informations (including wet or dirty), so making a copy of it.
                oldUnderwear.container.ResetToDefault(currentUnderwear);


                if (who.ActiveItem == newUnderwear)
                {
                    who.reduceActiveItemByOne();
                }
                else
                {
                    newUnderwear.Stack -= 1;
                    if (newUnderwear.Stack < 1)
                    {
                        who.removeItemFromInventory(newUnderwear);
                    }
                }

                targetNpc.change(underwearName, args.Length > 3 ? args[3] : null);


                //If the underwear returned is not removable, destroy it
                if (!oldUnderwear.container.washable)
                {
                    var msg = Strings.InsertVariables(Strings.RandString(Regression.t.Change_Other_Destroyed), targetNpc.npc, oldUnderwear.container);
                    Animations.Warn(msg);
                }
                else if (!who.addItemToInventoryBool(oldUnderwear, false)) //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(oldUnderwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }
        public bool GiveUnderwear(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                if (args.Length < 2)
                {
                    error = $"Parameter 1, needs to be a valid name of a type of underwear";
                    return false;
                }

                var amount = args.Length > 2 ? int.Parse(args[2]) : 1;

                var underwear = new Underwear(args[1], 1);

                //Put into the inventory, but pull up the management window if it can't fit
                if (!who.addItemToInventoryBool(underwear, false))
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(underwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }
        // action from dialog 
        // parameter 1: name of npc
        // parameter 2: type of underwear the npc change you
        // parameter 3: (optional) name of pants the npc change you if yours are messy
        public bool StartChange(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                string npcName = "sam";
                string underwearName = who.Gender == Gender.Female ? "polka dot panties" : "big kid undies";
                if (args.Length > 2)
                {
                    npcName = args[1];
                    underwearName = args[2];
                }

                var newUnderwear = new Underwear(underwearName, 1);

                Underwear oldUnderwear = new Underwear(body.underwear, 1);

                var dirtyPants = body.HasWetOrMessyDebuff();

                if (args.Length > 3 && dirtyPants)
                {
                    Container oldPants = new Container(body, ContainerSubtype.Pants, body.pants.type);
                    oldPants.ResetToDefault(body.pants);

                    string newPantsName = Strings.ReplaceOr(args[3],who.Gender != Gender.Female);

                    Container newPants = new Container(body,ContainerSubtype.Pants, newPantsName);

                    body.ChangeUnderwearAndPants(newUnderwear, oldUnderwear, newPants,oldPants, npcName: npcName);
                }
                else
                {
                    body.ChangeUnderwear(newUnderwear, oldUnderwear, npcName: npcName);
                }

                //body.ChangeUnderwear(newUnderwear, oldUnderwear, npc: npcName);
                //body.ResetPants();

                //If the underwear returned is not removable, destroy it
                if (!oldUnderwear.container.washable)
                {
                    Animations.Warn(Regression.t.Getting_Changed_Destroyed, body, oldUnderwear.container);
                }
                //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                else if (!who.addItemToInventoryBool(oldUnderwear, false))
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(oldUnderwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }
        public bool AddNpcMessage(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                if (args.Length < 2)
                {
                    error = "Parameter 1, target, has to be the name of an npc";
                    return false;
                }
                var target = args[1];
                target = target.ToLower();
                var targetNpc = NpcBody.ByName(target, -1);
                if (targetNpc == null)
                {
                    error = $"Parameter 1, '{target}' not found in global npc list";
                    return false;
                }
                if (args.Length < 3)
                {
                    error = "Parameter 2, message, has to be a message that is added as dialog to that npc";
                    return false;
                }
                string eventToken = null;
                if (args.Length > 3)
                {
                    eventToken = args[3];
                    if (targetNpc.CurrentDialogue.Count > 0) targetNpc.RemoveDialogue(eventToken);
                }
                var msg = args[2].Replace("___", "#").Replace("$_", "$");
                targetNpc.npc.setNewDialogue(new Dialogue(targetNpc.npc, eventToken, msg), true, false);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }
        private void ReceiveUpdateTick(object sender, OneSecondUpdateTickingEventArgs e)
        {

            //Ignore everything until we've started the day
            if (!started)
                return;


            //If time is moving, update our body state (Hunger, thirst, etc.)
            if (ShouldTimePass())
            {
                //body.HandleTime(timeInTick); to handle this cleaner, we move this to the time-changing tick


                // The following block makes npc (usually) not talk to you if you wear wet or messy pants... short of the special texts
                var isFilthy = body.pants.used;
                foreach (var npc in NpcBody.ByRange(10))
                {
                    if (npc.CurrentDialogue.Count > 0) npc.RemoveDialogue(dirtyEventToken);
                    if (npc.CurrentDialogue.Count > 0) npc.RemoveDialogue(generalEventToken);

                    if (npc.Age == 2 && !ChildrenAndDiapers) continue;

                    if (isFilthy)
                    {
                        string mod = "dirty";
                        string responseKey = mod + Animations.responseKeyAdditionForState(npc.npc, true);

                        string randNpcString = Strings.RandString(npc.GetVillagerReactions(responseKey, mod).ToArray());
                        if (randNpcString == "") continue;
                        string npcStatement = Strings.ReplaceAndOr(randNpcString, body.pants.wetness > 0, body.pants.messiness > 0);
                        npcStatement = Strings.InsertVariables(npcStatement, body, (Container)null);
                        if (npcStatement.Contains("DIAPER_CHANGE"))
                        {
                            npcStatement = Strings.ReplaceOptional(npcStatement, body.HasWetOrMessyDebuff());
                        }
                        npcStatement = Strings.InsertVariables(npcStatement, npc.npc);
                        npc.npc.setNewDialogue(new Dialogue(npc.npc, dirtyEventToken, npcStatement), true, true);
                    }
                    else if (npc.CurrentDialogue.Count <= 0)
                    {
                        string mod = "general";
                        string responseKey = mod + Animations.responseKeyAdditionForState(npc.npc, false);

                        string randNpcString = Strings.RandString(npc.GetVillagerReactions(responseKey, mod).ToArray());
                        if (randNpcString == "") continue;
                        string npcStatement = Strings.ReplaceAndOr(randNpcString, body.pants.wetness > 0, body.pants.messiness > 0);

                        if (npcStatement.Contains("DIAPER_CHANGE"))
                        {
                            npcStatement = Strings.ReplaceOptional(npcStatement, body.HasWetOrMessyDebuff());
                        }
                        npcStatement = Strings.InsertVariables(npcStatement, body, (Container)null);
                        npcStatement = Strings.InsertVariables(npcStatement, npc.npc);

                        npc.npc.setNewDialogue(new Dialogue(npc.npc, generalEventToken, npcStatement), true, true);
                    }

                }
            }
        }



        //Determine if we need to handle time passing (not the same as Game time passing)
        private static bool ShouldTimePass()
        {
            return ((Game1.game1.IsActive || Game1.options.pauseWhenOutOfFocus == false) && (Game1.paused == false && Game1.dialogueUp == false) && (Game1.currentMinigame == null && Game1.eventUp == false && Game1.activeClickableMenu == null) && Game1.fadeToBlack == false);
        }

        //Interprete key-presses
        private void ReceiveKeyPress(object sender, ButtonPressedEventArgs e)
        {
            //If we haven't started the day, ignore the key presses
            if (!started) return;

            // We ignore keypresses in menues
            if (Game1.activeClickableMenu != null) return;

            bool altDown = e.IsDown(SButton.LeftAlt);
            bool shiftDown = e.IsDown(SButton.LeftShift);

            //START Keybind-section
            if (!altDown)
            {
                bool triggered = true;

                int button = (int)e.Button;
                if (button == config.KeyGoInPants && (!shiftDown || config.KeyGoInPants != config.KeyGoInToilet))
                    body.WetAndMess(true, true);
                else if (button == config.KeyGoInToilet)
                    body.WetAndMess(true, false);
                else if (button == config.KeyPee && (!shiftDown || config.KeyPee != config.KeyPeeInToilet))
                    body.Wet(true, true);
                else if (button == config.KeyPoop && (!shiftDown || config.KeyPoop != config.KeyPoopInToilet))
                    body.Mess(true, true);
                else if (button == config.KeyPeeInToilet)
                    body.Wet(true, false);
                else if (button == config.KeyPoopInToilet)
                    body.Mess(true, false);
                else triggered = false;

                if (triggered) return;
            }
            // END Keybind-section


            //Interpret buttons differently if holding Left Alt & Debug is enabled
            if (e.IsDown(SButton.LeftAlt) && config.Debug)
            {
                switch (e.Button)
                {
                    case SButton.F1: //
                        body.DecreaseEverything();
                        break;
                    case SButton.F2: //
                        body.IncreaseEverything();
                        break;
                    case SButton.F3://
                        GiveUnderwear();
                        break;
                    //Alt F4 is reserved to close
                    case SButton.F5:
                        TimeMagic.doMagic();
                        break;
                    case SButton.F6:
                        config.Wetting = !config.Wetting;
                        break;
                    case SButton.F7:
                        config.Messing = !config.Messing;
                        break;
                    case SButton.F8:
                        config.Easymode = !config.Easymode;
                        Animations.Write(config.Easymode ? Regression.t.EasyMode_On : Regression.t.EasyMode_Off, body);
                        break;
                    case SButton.S:
                        if (e.IsDown(SButton.LeftShift))
                        {
                            body.ChangeContinence(IncidentType.POOP, 0.1f);
                        }
                        else
                        {
                            body.ChangeContinence(IncidentType.PEE, 0.1f);
                        }
                        break;
                    case SButton.W:
                        if (e.IsDown(SButton.LeftShift))
                        {
                            body.ChangeContinence(IncidentType.POOP, -0.1f);
                        }
                        else
                        {
                            body.ChangeContinence(IncidentType.PEE, -0.1f);
                        }
                        break;
                }
            }
            else
            {
                switch (e.Button)
                {
                    /*F4 is reserved for screenshot mode*/
                    case SButton.F5:
                        Animations.CheckUnderwear(body);
                        break;
                    case SButton.F6: 
                        Animations.CheckPants(body);
                        break;
                    case SButton.F7:
                        Animations.CheckContinence(body);
                        break;
                    case SButton.F8:
                        Animations.CheckPottyFeeling(body);
                        break;
                        /*case SButton.F9:
                            npcAccident(NpcByName("vincent"), IncidentType.PEE);
                            break;
                        case SButton.F10:
                            npcAccident(NpcByName("vincent"), IncidentType.POOP);
                            break;
                        case SButton.F11:
                            npcAccident(NpcByName("jas"), IncidentType.PEE);
                            break;
                        case SButton.F12:
                            npcAccident(NpcByName("jas"), IncidentType.POOP);
                            break;*/

                }
            }
        }

        //A menu has been opened, figure out if we need to modify it
        private void ReceiveMenuChanged(object sender, MenuChangedEventArgs e)
        {
            //Don't do anything if our day hasn't started
            if (!started)
                return;

            DialogueBox attemptToSleepMenu;
            ShopMenu currentShopMenu;

            //If we try to sleep, check if the bed is done drying (only matters in Hard Mode)
            if (Game1.currentLocation is FarmHouse && (attemptToSleepMenu = e.NewMenu as DialogueBox) != null && Game1.currentLocation.lastQuestionKey == "Sleep" && !config.Easymode)
            {
                //If enough time has passed, the bed has dried
                if (body.bed.drying)
                {
                    Response[] sleepAttemptResponses = attemptToSleepMenu.responses;
                    if (sleepAttemptResponses.Length == 2)
                    {
                        Response response = sleepAttemptResponses[1];
                        Game1.currentLocation.answerDialogue(response);
                        Game1.currentLocation.lastQuestionKey = null;
                        attemptToSleepMenu.closeDialogue();
                        Animations.AnimateDryingBedding(body);
                    }
                }
            }
            //If we're in the mailbox, handle the initial letter from Jodi that contains protection
            else if (e.NewMenu is LetterViewerMenu && Game1.currentLocation is Farm)
            {
                LetterViewerMenu letterMenu = (LetterViewerMenu)e.NewMenu;
                Mail.ShowLetter(letterMenu);
            }
            //If we're trying to shop, handle the underwear inventory
            else if ((currentShopMenu = e.NewMenu as ShopMenu) != null)
            {
                //Default to all underwear being available
                List<string> allUnderwear = Strings.ValidUnderwearTypes();
                if (Game1.currentLocation is SeedShop)
                {
                    foreach (string type in allUnderwear)
                    {
                        //The seed shop does not sell the Joja diaper
                        if (type == "joja diaper") continue;
                        //The seed shop does not sell every diaper and underwear as single items
                        addUnderwearToShop(currentShopMenu, type);
                    }
                }
                else if (Game1.currentLocation is JojaMart)
                {
                    // Joja shop sells big brands now, "pampers" and "dry nites". You probably also find normal undies and simple cloth diapers there.
                    // As such uses packages and has slightly lower prices (bulk)
                    // This makes sense and mirrors the advantages and disadvantages of large chains in rual areas
                    var type = "joja diaper";
                    if (allUnderwear.Contains(type))
                    {
                        addUnderwearToShop(currentShopMenu, type, 10, 0.8f);
                        addUnderwearToShop(currentShopMenu, type, 40, 0.7f);
                    }
                    type = "baby print diaper";
                    if (allUnderwear.Contains(type))
                    {
                        addUnderwearToShop(currentShopMenu, type, 20, 0.8f);
                        addUnderwearToShop(currentShopMenu, type, 60, 0.7f);
                    }
                    type = "lavender pullups";
                    if (allUnderwear.Contains(type))
                    {
                        addUnderwearToShop(currentShopMenu, type, 10, 0.8f);
                        addUnderwearToShop(currentShopMenu, type, 40, 0.7f);
                    }
                    type = "big kid undies";
                    if (allUnderwear.Contains(type))
                    {
                        addUnderwearToShop(currentShopMenu, type, 3, 0.8f);
                    }
                    type = "cloth diaper";
                    if (allUnderwear.Contains(type))
                    {
                        addUnderwearToShop(currentShopMenu, type, 5, 0.75f);
                    }

                }
            }
        }

        private static void addUnderwearToShop(ShopMenu shop, string type, int amount = 1, float priceMultiplier = 1f)
        {
            var underwear = new Underwear(type, amount);
            shop.forSale.Add(underwear);
            shop.itemPriceAndStock.Add(underwear, new ItemStockInformation((int)Math.Ceiling((float)underwear.container.price * (float)amount * priceMultiplier), StardewValley.Menus.ShopMenu.infiniteStock));
        }

        //Check if we are at a natural water source
        private static bool AtWaterSource()
        {
            GameLocation currentLocation = Game1.currentLocation;
            Vector2 toolLocation = who.GetToolLocation(false);
            int x = (int)toolLocation.X;
            int y = (int)toolLocation.Y;
            return currentLocation.doesTileHaveProperty(x / Game1.tileSize, y / Game1.tileSize, "Water", "Back") != null || currentLocation.doesTileHaveProperty(x / Game1.tileSize, y / Game1.tileSize, "WaterSource", "Back") != null;
        }

        //Check if we are at the Well (and its constructed)
        private static bool AtWell()
        {
            GameLocation currentLocation = Game1.currentLocation;
            Vector2 toolLocation = who.GetToolLocation(false);
            int x = (int)toolLocation.X;
            int y = (int)toolLocation.Y;
            Vector2 vector2 = new Vector2((float)(x / Game1.tileSize), y / Game1.tileSize);
            return currentLocation.IsBuildableLocation() && currentLocation.getBuildingAt(vector2) != null && (currentLocation.getBuildingAt(vector2).buildingType.Value.Equals("Well") && currentLocation.getBuildingAt(vector2).daysOfConstructionLeft.Value <= 0);

        }

        //Handle Mouse Clicks/Movement
        private void ReceiveMouseChanged(object sender, ButtonPressedEventArgs e)
        {
            //Ignore if we aren't started or otherwise paused
            if (!(Game1.game1.IsActive && !Game1.paused && started))
            {
                return;
            }

            
            if (e.Button == SButton.MouseLeft)
            {
                //If we try to take or put down pants, this should only work if you are allowed to change them.
                var men = Game1.activeClickableMenu as StardewValley.Menus.GameMenu;
                if (men != null)
                {
                    var inventory = men.pages[men.currentTab] as StardewValley.Menus.InventoryPage;
                    if (inventory != null)
                    {
                        var clothing = inventory.hoveredItem as StardewValley.Objects.Clothing;
                        if (clothing != null)
                        {
                            if (clothing.clothesType.Value == Clothing.ClothesType.PANTS)
                            {
                                if (body.HasWetOrMessyDebuff())
                                {
                                    Game1.activeClickableMenu = null;
                                    if (!Regression.config.PantsChangeRequiresHome || body.InPlaceWithPants())
                                    {
                                        body.ResetPants();
                                        Animations.Write(Regression.t.Change_At_Home, body);
                                    }
                                    else
                                    {
                                        Animations.Write(Regression.t.Change_Requires_Home, body);
                                    }

                                    return;

                                }
                            }
                        }
                    }
                }

                //If Left click is already being interpreted by another event (or we otherwise wouldn't process such an event. Ignore it.
                if ((Game1.dialogueUp || Game1.currentMinigame != null || (Game1.eventUp || Game1.activeClickableMenu != null) || Game1.fadeToBlack) || (who.isRidingHorse() || !who.canMove || (Game1.player.isEating || who.canOnlyWalk) || who.FarmerSprite.pauseForSingleAnimation))
                    return;

                // This block tries to figure out if the clicked point was in the toolbar. Because if it was, we assume the intent was not to use the selected item.
                var pos = e.Cursor.ScreenPixels;
                Toolbar toolbar = Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();
                if (toolbar != null)
                {
                    foreach (var button in toolbar.buttons)
                    {
                        if (button.containsPoint((int)pos.X, (int)pos.Y))
                        {
                            // The click was on the toolbar
                            return;
                        }
                    }
                }

                ////If we're holding the watering can, attempt to drink from it.
                /////This is the highest priority (apparently?)
                if (who.CurrentTool != null && who.CurrentTool is WateringCan && e.IsDown(SButton.LeftShift))
                {
                    body.DrinkWateringCan();
                    return;
                }

                //Otherwise Check if we're holding underwear
                Underwear activeObject = who.ActiveObject as Underwear;
                if (activeObject != null)
                {
                    //If the Underwear we are holding isn't currently wet, messy, or drying; change into it.
                    if ((double)activeObject.container.wetness + (double)activeObject.container.messiness == 0.0 && !activeObject.container.drying)
                    {                      

                        if (Regression.config.PantsChangeRequiresHome && body.HasWetOrMessyDebuff() && !body.InPlaceWithPants())
                        {
                            Animations.Write(Regression.t.Change_Requires_Pants, body);
                            return;
                        }

                        Underwear OldUnderwear = new Underwear(body.underwear, 1);

                        if (Regression.config.UnderwearChangeCauseExposure)
                        {
                            Animations.HandleVillager(body, false, false, false, true);
                        }

                        if (!body.HasWetOrMessyDebuff())
                        {
                            body.ChangeUnderwear(activeObject, OldUnderwear);
                        }
                        else
                        {
                            Container oldPants = new Container(body,ContainerSubtype.Pants, body.pants.type);
                            oldPants.ResetToDefault(body.pants);

                            Container newPants = new Container(body,ContainerSubtype.Pants,oldPants.type);

                            if (!Regression.config.PantsChangeRequiresHome) body.ChangeUnderwearAndPants(activeObject, OldUnderwear, newPants, oldPants);
                            else
                            {
                                if (body.InPlaceWithPants()) body.ChangeUnderwearAndPants(activeObject, OldUnderwear, newPants, oldPants);
                            }
                        }

                        //body.ChangeUnderwear(activeObject);
                        who.reduceActiveItemByOne();
                        //body.ResetPants();

                        //If the underwear returned is not removable, destroy it
                        if (!OldUnderwear.container.washable)
                        {
                            Animations.Warn(Regression.t.Change_Destroyed, body, OldUnderwear.container);
                        }
                        //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                        else if (!who.addItemToInventoryBool(OldUnderwear, false))
                        {
                            List<Item> objList = new List<Item>();
                            objList.Add(OldUnderwear);
                            Game1.activeClickableMenu = new ItemGrabMenu(objList);
                        }
                    }
                    //If it is wet, messy or drying, check if we can wash it
                    else if (activeObject.container.washable)
                    {
                        //Are we at a water source? If so, wash the underwear.
                        if (AtWaterSource())
                        {
                            activeObject.container.Wash();
                            Animations.AnimateWashingUnderwear(activeObject.container);
                        }
                    }
                    return; //Done with underwear
                }


                //If we're at a water source, and not holding underwear, drink from it.
                if ((AtWaterSource() || AtWell()) && e.IsDown(SButton.LeftShift))
                    body.DrinkWaterSource();
            }

        }
        public static bool ChildrenAndDiapers
        {
            get
            {
                string yes = "child_diaper_yes";
                string no = "child_diaper_no";

                if (Game1.player.dialogueQuestionsAnswered.Contains(no)) return false;
                if (Game1.player.dialogueQuestionsAnswered.Contains(yes)) return true;
                return true;
            }
            set
            {
                string yes = "child_diaper_yes";
                string no = "child_diaper_no";

                if (value)
                {
                    if (Game1.player.dialogueQuestionsAnswered.Contains(no)) Game1.player.DialogueQuestionsAnswered.Remove(no);
                    if (!Game1.player.dialogueQuestionsAnswered.Contains(yes)) Game1.player.DialogueQuestionsAnswered.Add(yes);
                }
                else
                {
                    if (Game1.player.dialogueQuestionsAnswered.Contains(yes)) Game1.player.DialogueQuestionsAnswered.Remove(yes);
                    if (!Game1.player.dialogueQuestionsAnswered.Contains(no)) Game1.player.DialogueQuestionsAnswered.Add(no);
                }
            }
        }

        //If approppriate, draw bars for Hunger, thirst, bladder and bowels
        public void ReceivePreRenderHudEvent(object sender, RenderingHudEventArgs args)
        {
            if (!started || Game1.currentMinigame != null || Game1.eventUp || Game1.globalFade)
                return;
            DrawStatusBars();
        }
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {



            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => config = new Config(),
                save: () => this.Helper.WriteConfig(config)
            );

            // Config of the main page. Most important options
            // Cheat Mode
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Cheat_Mode.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Cheat_Mode.Tooltip}}"),
                getValue: () => config.Debug,
                setValue: value => config.Debug = value
            );
            // Easy Mode
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Easy_Mode.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Easy_Mode.Tooltip}}"),
                getValue: () => config.Easymode,
                setValue: value => config.Easymode = value
            );
            // Wetting
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Wetting.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Wetting.Tooltip}}"),
                getValue: () => config.Wetting,
                setValue: value => config.Wetting = value
            );
            // Messing 
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Messing.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Messing.Tooltip}}"),
                getValue: () => config.Messing,
                setValue: value => config.Messing = value
            );
            // Children And Diapers
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Children_And_Diapers.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Children_And_Diapers.Tooltip}}"),
                getValue: () => ChildrenAndDiapers,
                setValue: value => ChildrenAndDiapers = value
            );
            // Max Bladder Capacity
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Bladder_Capacity.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Bladder_Capacity.Tooltip}}"),
                getValue: () => config.MaxBladderCapacity,
                setValue: value => config.MaxBladderCapacity = value,
                min: 300, max: 1800, interval: 50
            );
            // Max Bowel Capacity
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Bowel_Capacity.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Bowel_Capacity.Tooltip}}"),
                getValue: () => config.MaxBowelCapacity,
                setValue: value => config.MaxBowelCapacity = value,
                min: 300, max: 1800, interval: 50
            );
            // Always Notice Accidents
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Always_Notice_Accidents.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Always_Notice_Accidents.Tooltip}}"),
                getValue: () => config.AlwaysNoticeAccidents,
                setValue: value => config.AlwaysNoticeAccidents = value
            );
            // Pants Change Requires Home
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Pants_Change_At_Home.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Pants_Change_At_Home.Tooltip}}"),
                getValue: () => config.PantsChangeRequiresHome,
                setValue: value => config.PantsChangeRequiresHome = value
            );
            // Underwear Change Cause Exposure
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Underwear_Change_Causes_Exposure.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Underwear_Change_Causes_Exposure.Tooltip}}"),
                getValue: () => config.UnderwearChangeCauseExposure,
                setValue: value => config.UnderwearChangeCauseExposure = value
            );

            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Key Bindings",
                text: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Key_Bindings_Menu.Name}}")
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Continence",
                text: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Continence_Menu.Name}}")
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Friendships",
                text: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Friendships_Menu.Name}}")
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Save Files",
                text: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Main.Save_Files_Menu.Name}}")
            );
            // All the options related to continence balancing
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Key Bindings"
            );
            // Pee Pants
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Pee_Pants.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Pee_Pants.Tooltip}}"),
                getValue: () => (SButton)config.KeyPee,
                setValue: value => config.KeyPee = (int)value
            );
            // Poop Pants
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Poop_Pants.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Poop_Pants.Tooltip}}"),
                getValue: () => (SButton)config.KeyPoop,
                setValue: value => config.KeyPoop = (int)value
            );
            // Pee In Potty
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Pee_In_Potty.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Pee_In_Potty.Tooltip}}"),
                getValue: () => (SButton)config.KeyPeeInToilet,
                setValue: value => config.KeyPeeInToilet = (int)value
            );
            // Poop In Potty
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Poop_In_Potty.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Poop_In_Potty.Tooltip}}"),
                getValue: () => (SButton)config.KeyPoopInToilet,
                setValue: value => config.KeyPoopInToilet = (int)value
            );
            // Go In Pants
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Go_In_Pants.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Go_In_Pants.Tooltip}}"),
                getValue: () => (SButton)config.KeyGoInPants,
                setValue: value => config.KeyGoInPants = (int)value
            );
            // Go Potty
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Go_Potty.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Key_Bindings.Go_Potty.Tooltip}}"),
                getValue: () => (SButton)config.KeyGoInToilet,
                setValue: value => config.KeyGoInToilet = (int)value
            );

            // All the options related to continence balancing
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Continence"
            );
            // Nighttime Losses
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Nighttime_Losses.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Nighttime_Losses.Tooltip}}"),
                getValue: () => config.NighttimeLossMultiplier,
                setValue: value => config.NighttimeLossMultiplier = value,
                min: 0, max: 200, interval: 10
            );
            //´Nighttime Gains
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Nighttime_Gains.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Nighttime_Gains.Tooltip}}"),
                getValue: () => config.NighttimeGainMultiplier,
                setValue: value => config.NighttimeGainMultiplier = value,
                min: 0, max: 200, interval: 10
            );
            // In Underwear on Purpose Modifier
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.In_Diaper_on_Purpose_Modifier.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.In_Diaper_on_Purpose_Modifier.Tooltip}}"),
                getValue: () => config.InUnderwearOnPurposeMultiplier,
                setValue: value => config.InUnderwearOnPurposeMultiplier = value,
                min: 0, max: 200, interval: 10
            );
            // Accident Bladder Loss
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Accident_Bladder_Loss.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Accident_Bladder_Loss.Tooltip}}"),
                getValue: () => config.BladderLossContinenceRate,
                setValue: value => config.BladderLossContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            // Accident Bowel Loss
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Accident_Bowel_Loss.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Accident_Bowel_Loss.Tooltip}}"),
                getValue: () => config.BowelLossContinenceRate,
                setValue: value => config.BowelLossContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            // Toilet Bladder Gain
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Toilet_Bladder_Gain.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Toilet_Bladder_Gain.Tooltip}}"),
                getValue: () => config.BladderGainContinenceRate,
                setValue: value => config.BladderGainContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            // Toilet Bowel Gain
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Toilet_Bowel_Gain.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Toilet_Bowel_Gain.Tooltip}}"),
                getValue: () => config.BowelGainContinenceRate,
                setValue: value => config.BowelGainContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            // Start Bladder Continence
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Start_Bladder_Continence.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Start_Bladder_Continence.Tooltip}}"),
                getValue: () => config.StartBladderContinence,
                setValue: value => config.StartBladderContinence = value,
                min: (int)(Body.minBladderContinence * 100), max: 100, interval: 5
            );
            // Start Bowel Continence
            configMenu.AddNumberOption(
               mod: this.ModManifest,
               name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Start_Bowel_Continence.Name}}"),
               tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Continence.Start_Bowel_Continence.Tooltip}}"),
               getValue: () => config.StartBowelContinence,
               setValue: value => config.StartBowelContinence = value,
               min: (int)(Body.minBowelContinence * 100), max: 100, interval: 5
            );

            // All the options related to friendship changes caused by accidents
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Friendships"
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Friendships.Penalty_Peeing.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Friendships.Penalty_Peeing.Tooltip}}"),
                getValue: () => config.FriendshipPenaltyBladderMultiplier,
                setValue: value => config.FriendshipPenaltyBladderMultiplier = value,
                min: 0, max: 500, interval: 10
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Friendships.Penalty_Pooping.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Friendships.Penalty_Pooping.Tooltip}}"),
                getValue: () => config.FriendshipPenaltyBowelMultiplier,
                setValue: value => config.FriendshipPenaltyBowelMultiplier = value,
                min: 0, max: 500, interval: 10
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Friendships.Debug.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Friendships.Debug.Tooltip}}"),
                getValue: () => config.FriendshipDebug,
                setValue: value => config.FriendshipDebug = value
            );

            // All the options related to save files
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Save Files"
            );
            configMenu.AddParagraph(
                mod: this.ModManifest,
                text: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Save_Files.Headline}}")
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Save_Files.Read.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Save_Files.Read.Tooltip}}"),
                getValue: () => config.ReadSaveFiles,
                setValue: value => config.ReadSaveFiles = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Save_Files.Wtite.Name}}"),
                tooltip: () => Strings.tryGetI18nText("{{i18n:Config_Menu.Save_Files.Wtite.Tooltip}}"),
                getValue: () => config.WriteSaveFiles,
                setValue: value => config.WriteSaveFiles = value
            );
        }
        public void DrawStatusBars()
        {

            int x1 = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - (65 + (int)((StatusBars.barWidth)));
            int y1 = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - (25 + (int)((StatusBars.barHeight)));

            if (Game1.currentLocation is MineShaft || Game1.currentLocation is Woods || Game1.currentLocation is SlimeHutch || Game1.currentLocation is VolcanoDungeon || who.health < who.maxHealth)
                x1 -= 58;

            if (!config.NoHungerAndThirst || config.Debug)
            {
                float percentage1 = body.GetHungerPercent();
                StatusBars.DrawStatusBar(x1, y1, percentage1, new Color(115, byte.MaxValue, 56));
                int x2 = x1 - (10 + StatusBars.barWidth);
                float percentage2 = body.GetThirstPercent();
                StatusBars.DrawStatusBar(x2, y1, percentage2, new Color(117, 225, byte.MaxValue));
                x1 = x2 - (10 + StatusBars.barWidth);
            }
            if (config.Debug)
            {
                if (config.Messing)
                {
                    float percentage = body.GetBowelPercent();
                    StatusBars.DrawStatusBar(x1, y1, percentage, new Color(146, 111, 91));
                    x1 -= 10 + StatusBars.barWidth;
                }
                if (config.Wetting)
                {
                    float percentage = body.GetBladderPercent();
                    StatusBars.DrawStatusBar(x1, y1, percentage, new Color(byte.MaxValue, 225, 56));
                }
            }
            if (!config.Wetting && !config.Messing)
                return;
            int y3 = (Game1.player.questLog).Count == 0 ? 250 : 310;
            var x3 = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - 94;
            Animations.DrawUnderwearIcon(body.underwear, x3, y3);

            Animations.DrawStateIcon(body, IncidentType.PEE, x3, y3 + 74);
            Animations.DrawStateIcon(body, IncidentType.POOP, x3, y3 + 74 + 74);
        }

        /// <param name="oldTime">The original time in HHMM format (e.g., 1550 for 15:50).</param>
        /// <param name="newTime">The new time in HHMM format (e.g., 1600 for 16:00).</param>
        /// <returns>The difference in minutes as an integer.</returns>
        /// <exception cref="ArgumentException">Thrown when input times are invalid.</exception>
        public int GetTimeDifference(int oldTime, int newTime)
        {
            // Validate and convert HHMM to total minutes since midnight
            int ConvertToMinutes(int time)
            {
                int hours = time / 100;
                int minutes = time % 100;
                if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                    throw new ArgumentException($"Invalid time format: {time}. Expected HHMM with HH=00-23 and MM=00-59.");
                return hours * 60 + minutes;
            }

            int oldMinutes = ConvertToMinutes(oldTime);
            int newMinutes = ConvertToMinutes(newTime);

            // Calculate the difference, accounting for midnight wrap-around
            return newMinutes >= oldMinutes
                ? newMinutes - oldMinutes
                : (1440 - oldMinutes) + newMinutes;
        }
        private void ReceiveTimeOfDayChanged(object sender, TimeChangedEventArgs e)
        {

            //lastTimeOfDay = Game1.timeOfDay;

            int newTime = e.NewTime;
            int oldTime = e.OldTime;

            // Calculate the difference
            int difference = GetTimeDifference(oldTime, newTime);

            //Monitor.Log($"Time changed from {oldTime} to {newTime}. Difference: {difference} minutes.", LogLevel.Debug);
            if (difference < 120) // we make sure that this doesn't get out of hand. Daychange is handled seperatly.
            {
                // If all of the 24 hours would be done as a 1 minute tick, there would be 1440 ticks in theory 
                /*float tickLen = 1440f;
                float lengthTotal = (float) difference / (float)tickLen;*/
                // But we handle this as fraction of an hour, so its 1/60 * minutes
                float fractionOfAnHour = (float)difference * (1f / 60f);
                body.HandleTime((float)difference * (1f / 60f));

                // NPC actions are based on chance. Chances that would be vastly different if every client would run them seperatly
                if (!Game1.IsMultiplayer || Game1.IsServer)
                {

                    foreach (NPC npc in Utility.getAllCharacters())
                    {
                        new NpcBody(npc).RandomAction(fractionOfAnHour);
                    }

                }
            }


            // Update lastTimeOfDay
            lastTimeOfDay = newTime;

            if (newTime < 630)
                return;

            //If its earlier than 6:30, we aren't wet/messy don't notice that we're still soiled (or don't notice with ~5% chance even if soiled)
            if (rnd.NextDouble() < 0.0555555559694767 && body.underwear.wetness + (double)body.underwear.messiness > 0.0)
                Animations.AnimateStillSoiled(body);

            if (rnd.NextDouble() < 0.0555555559694767 && (body.NeedsChangies(IncidentType.PEE) || body.NeedsChangies(IncidentType.POOP)))
                Animations.AnimateShouldChange(body);

            // Reset all dialogue answers for changing
            if (Game1.player.dialogueQuestionsAnswered.Contains("change_other_yes"))
                Game1.player.dialogueQuestionsAnswered.Remove("change_other_yes");
            if (Game1.player.dialogueQuestionsAnswered.Contains("change_other_no"))
                Game1.player.dialogueQuestionsAnswered.Remove("change_other_no");


            ResetAllNpcChangeAnswers();
        }

        public void ResetAllNpcChangeAnswers()
        {
            foreach (NPC npc in Utility.getAllCharacters())
            {
                string yes = "dirty_change_yes_" + npc.Name.ToLower();
                string no = "dirty_change_no_" + npc.Name.ToLower();

                if (Game1.player.dialogueQuestionsAnswered.Contains(yes)) Game1.player.dialogueQuestionsAnswered.Remove(yes);
                if (Game1.player.dialogueQuestionsAnswered.Contains(no)) Game1.player.dialogueQuestionsAnswered.Remove(no);
            }
        }

        private delegate bool ActionHandler(string[] args, TriggerActionContext context, out string error);
        public bool ActionManager(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            // Remove the first arg "ACTION_MANAGER"
            args = args.Skip(1).ToArray();

            // Dictionary of known actions
            var actionHandlers = new Dictionary<string, ActionHandler>()
            {
                { "DIAPER_CHANGE", StartChange },
                { "CHANGE_DIAPER_OTHERS", StartChangeOthers },
                { "DIAPER_ACCIDENT",  StartAccident},
                { "ADD_DIALOG",  AddNpcMessage},
                { "GIVE_UNDERWEAR",  GiveUnderwear}
                // Add more actions here
            };

            int index = 0;
            while (index < args.Length)
            {
                string actionName = args[index];
                if (!actionHandlers.ContainsKey(actionName))
                {
                    /*error = $"Unknown action: {actionName}";
                    return false;*/
                    // we will just assume that its not an action but a parameter from another function
                }

                // Find the next action in the list or the end of the array
                int nextActionIndex = args.Length;
                for (int i = index + 1; i < args.Length; i++)
                {
                    if (actionHandlers.ContainsKey(args[i]))
                    {
                        nextActionIndex = i;
                        break;
                    }
                }

                // Extract arguments for this action (including the action name itself)
                string[] actionArgs = args.Skip(index).Take(nextActionIndex - index).ToArray();

                // Execute the action
                if (!actionHandlers[actionName](actionArgs, context, out error))
                {
                    return false;
                }

                // Move to the next action
                index = nextActionIndex;
            }

            return true;
        }

        private static Queue<(Action action, int delay)> actionQueue = new Queue<(Action, int)>();
        private static bool isExecutingAction = false;

        public static void QueueAction(Action action, int delay = 1000)
        {
            actionQueue.Enqueue((action, delay));
        }

        private static void ExecuteNextAction()
        {
            if (actionQueue.Count == 0) return;

            isExecutingAction = true;
            (Action action, int delay) nextAction = actionQueue.Dequeue();

            // Execute the custom action
            nextAction.action();

            // Wait before allowing the next action to execute
            DelayedAction.functionAfterDelay(() =>
            {
                isExecutingAction = false;
            }, nextAction.delay);
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return; // Ensure the game world is loaded

            // If the queue has actions and no menu/dialogue is active, execute the next action
            if (actionQueue.Count > 0 && Game1.activeClickableMenu == null && !isExecutingAction)
            {
                ExecuteNextAction();
            }
        }

        public Regression()
        {
            //base.Actor();
        }
    }
}
