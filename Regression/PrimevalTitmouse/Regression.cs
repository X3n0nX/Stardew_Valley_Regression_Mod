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
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using xTile.Dimensions;
using static StardewValley.Minigames.TargetGame;

namespace PrimevalTitmouse
{
    public class Regression : Mod
    {
        public static int lastTimeOfDay = 0;
        public static bool morningHandled = true;
        public static Random rnd = new Random();
        public static bool started = false;
        public Body body;
        public static Config config;
        public static IModHelper help;
        public static IMonitor monitor;
        public bool shiftHeld;
        public static Data t;
        public static Farmer who;
        private float tickCD1 = 0;
        private float tickCD2 = 0;
        public static string dirtyEventToken = "dirtyEventToken";
        public static string generalEventToken = "generalEventToken";
        public static bool SelfIsFurry = false;
        public static bool WorldIsFurry = false;
        const float timeInTick = (1f/43f); //One second realtime ~= 1/43 hours in game
        public Dictionary<string,bool> jsonLoaded = new();
        public override void Entry(IModHelper h)
        {
            var harmony = new HarmonyLib.Harmony("com.primevaltitmouse.regression");
            harmony.PatchAll();

            help = h;
            monitor = Monitor;
            config = Helper.ReadConfig<Config>();
            t = Helper.Data.ReadJsonFile<Data>(string.Format("{0}.json", (object)config.Lang)) ?? Helper.Data.ReadJsonFile<Data>("en.json");
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

            WorldIsFurry = Helper.ModRegistry.IsLoaded("sion9000.AnthroCharactersContinued");
            SelfIsFurry = Helper.ModRegistry.IsLoaded("krystedez.FurryFarmer");
        }



        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // Some character have random changes after warped
            foreach(var npc in e.NewLocation.characters)
            {
                var name = npc.Name.ToLower();
                if(name == "vincent")
                {
                    if (Regression.rnd.Next(2) > 0) npcChange(npc);
                    var undies = npcUnderwear(npc);
                    var result = Regression.rnd.Next(7);
                    // Vincent is oft messy or wet. Sometimes both
                    if (result <= 2) undies.AddPoop((float)Math.Max(400, 900));
                    if (result > 0 && result <= 5) undies.AddPee((float)Math.Max(300, 700));
                }
                else if(name == "jas")
                {
                    if (Regression.rnd.Next(2) > 0) npcChange(npc);
                    var undies = npcUnderwear(npc);
                    var result = Regression.rnd.Next(7);
                    // Jas is less often messy, nearly as often wet
                    if (result <= 2) undies.AddPoop((float)Math.Max(200, 700));
                    if (result > 0 && result <= 5) undies.AddPee((float)Math.Max(300, 700));
                }
                else if (name == "jas")
                {
                    if (Regression.rnd.Next(2) > 0) npcChange(npc);
                    var undies = npcUnderwear(npc);
                    var result = Regression.rnd.Next(7);
                    // Jas is less often messy, nearly as often wet
                    if (result <= 2) undies.AddPoop((float)Math.Max(200, 700));
                    if (result > 0 && result <= 5) undies.AddPee((float)Math.Max(300, 700));
                }
            }
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
                var npc = NpcByName(targetNpcName);
                if (npc == null) return false;

                underwear = npcUnderwear(npc);
                // we don't null check, that should not happen and if it does, we want to see that
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

        private void GiveUnderwear()
        {
            List<Item> objList = new List<Item>();
            foreach (string validUnderwearType in Strings.ValidUnderwearTypes())
                objList.Add(new Underwear(validUnderwearType,20));
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
            if(config.ReadSaveFiles) invReplacement = Helper.Data.ReadJsonFile<Dictionary<int, Dictionary<string, string>>>(string.Format("{0}/{1}", Constants.SaveFolderName, configFile));
            if(invReplacement != null) jsonLoaded[configFile] = true;
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
        public NPC NpcByName(string name, int range = 20)
        {
            name = name.ToLower();
            foreach (var npc in Animations.NearbyVillager(range))
            {
                if (npc.Name.ToLower() == name)
                {
                    return npc;
                }
            }
            return null;
        }
        public bool StartAccident(string[] args, TriggerActionContext context, out string error)
        {
            
            error = null;

            if (args.Length < 2)
            {
                error = "Parameter 1, target, has to be the name of an npc";
                return false;
            }
            var target = args[1];
            target = target.ToLower();

            if (args.Length < 3)
            {
                error = "Parameter 2, type, should be given!";
                return false;
            }
            var typeStr = args[2];

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
                    error = $"Parameter 2, type '{type}' is unknown!";
                    return false;
            }


            if (target == "player" || target == "farmer" || target == "self")
            {
                body.Accident(type);
            }
            else
            {
                var targetNpc = NpcByName(target);
                if (targetNpc == null)
                {
                    error = $"Parameter 1, '{target}' not found in local npc list in 20 range";
                    return false;
                }
                npcAccident(targetNpc, type);
            }
            return true;
        }
        public static Underwear GetUnderwearFromInventory(string underwearName, bool clean = true)
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
            return GetUnderwearFromInventory(underwearName,clean) != null;
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
                var targetNpc = NpcByName(target);
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

                var currentUnderwear = npcUnderwear(targetNpc);
                // We create a new, untethered container first, of the same type.
                Underwear oldUnderwear = new Underwear(currentUnderwear.name, 1);
                // We "reset" that new container to the same informations (including wet or dirty), so making a copy of it.
                oldUnderwear.container.ResetToDefault(currentUnderwear);


                if (who.ActiveItem == newUnderwear)
                {
                    who.reduceActiveItemByOne();
                }
                else {
                    newUnderwear.Stack -= 1;
                    if (newUnderwear.Stack < 1)
                    {
                        who.removeItemFromInventory(newUnderwear);
                    }
                }

                npcChange(targetNpc, underwearName, args.Length > 3 ? args[3] : null);

                
                //If the underwear returned is not removable, destroy it
                if (!oldUnderwear.container.washable)
                {
                    var msg = Strings.InsertVariables(Strings.RandString(Regression.t.Change_Other_Destroyed), targetNpc,oldUnderwear.container);
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
        public bool StartChange(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                var underwearName = "big kid undies";
                if (args.Length > 1)
                {
                    underwearName = args[1];
                }

                var diaper = new Underwear(underwearName, 1);
                
                Underwear underwear = new Underwear(body.underwear, 1);

                body.ChangeUnderwear(diaper);
                body.ResetPants();

                //If the underwear returned is not removable, destroy it
                if (!underwear.container.washable)
                {
                    Animations.Warn(Regression.t.Getting_Changed_Destroyed, body, underwear.container);
                }
                //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                else if (!who.addItemToInventoryBool(underwear, false))
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(underwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
                var dirtyPants = body.HasWetOrMessyDebuff();


                if (args.Length > 2 && dirtyPants)
                {
                    body.pants.displayName = args[2];
                    body.pants.description = args[2];
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
                var targetNpc = NpcByName(target);
                if (targetNpc == null)
                {
                    error = $"Parameter 1, '{target}' not found in local npc list in 20 range";
                    return false;
                }
                if (args.Length < 3)
                {
                    error = "Parameter 2, message, has to be a message that is added as dialog to that npc";
                    return false;
                }
                string eventToken = null;
                if(args.Length > 3)
                {
                    eventToken = args[3];
                    if (targetNpc.CurrentDialogue.Count > 0) RemoveDialogueFromNPC(targetNpc, eventToken);
                }
                var msg = args[2].Replace("___", "#").Replace("$_", "$");
                targetNpc.setNewDialogue(new Dialogue(targetNpc, eventToken, msg), true, false);
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
                this.body.HandleTime(timeInTick);
                if (tickCD1 != 0) //Former Bug: When consuming items, they would give 2-3 goes at the "Handle eating and drinking." if statement below. Cause: This function would trigger multiple times while the if statement below was still true, causing it to fire multiple times.
                {
                    tickCD2 += 1; //This should trigger multiple times during the eating animation.
                }
                if (tickCD2 >= 2) //Setting this to 2 should be able to balance preventing double triggers and ensuring no loss in intentional triggers.
                {
                    tickCD1 = 0;
                    tickCD2 = 0;
                }

                
                // The following block makes npc (usually) not talk to you if you wear wet or messy pants... short of the special texts
                
                var newList = new List<NPC>();
                var isFilthy = body.pants.used;
                var list = Utility.GetNpcsWithinDistance(((Character)Animations.player).Tile, 10, (GameLocation)Game1.currentLocation);
                foreach (var npc in list)
                {
                    if (npc.CurrentDialogue.Count > 0) RemoveDialogueFromNPC(npc, dirtyEventToken);
                    if(npc.CurrentDialogue.Count > 0) RemoveDialogueFromNPC(npc, generalEventToken);
                    

                    if (isFilthy)
                    {
                        var mod = "dirty";
                        var responseKey = mod + Animations.responseKeyAdditionForState(npc);
                        
                        var randNpcString = Strings.RandString(GetVillagerReactions(npc,responseKey).ToArray());
                        if (randNpcString == "") continue;
                        var npcStatement = Strings.ReplaceAndOr(randNpcString, body.pants.wetness > 0, body.pants.messiness > 0);
                        npcStatement = Strings.InsertVariables(npcStatement, body, (Container)null);
                        if (npcStatement.Contains("DIAPER_CHANGE"))
                        {
                            npcStatement = Strings.ReplaceOptional(npcStatement, body.HasWetOrMessyDebuff());
                        }
                        npcStatement = Strings.InsertVariables(npcStatement, npc);
                        npc.setNewDialogue(new Dialogue(npc, dirtyEventToken, npcStatement), true, true);
                    }
                    else if (npc.CurrentDialogue.Count <= 0)
                    {
                        var mod = "general";
                        var responseKey = mod + Animations.responseKeyAdditionForState(npc);

                        var randNpcString = Strings.RandString(GetVillagerReactions(npc, responseKey).ToArray());
                        if (randNpcString == "") continue;
                        var npcStatement = Strings.ReplaceAndOr(randNpcString, body.pants.wetness > 0, body.pants.messiness > 0);
                        if (npcStatement.Contains("DIAPER_CHANGE")) {
                            npcStatement = Strings.ReplaceOptional(npcStatement, body.HasWetOrMessyDebuff());
                        }
                        npcStatement = Strings.InsertVariables(npcStatement, body, (Container)null);
                        npcStatement = Strings.InsertVariables(npcStatement, npc);

                        npc.setNewDialogue(new Dialogue(npc, dirtyEventToken, npcStatement), true, true);
                    }

                }


            }

            //Handle eating and drinking.
            if (Game1.player.isEating && Game1.activeClickableMenu == null && tickCD1 == 0)
            {
                body.Consume(who.itemToEat.Name);
                tickCD1 += 1;
            }
        }
        private List<string> GetVillagerReactions(NPC npc, string responseKey)
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
            return stringList3;
        }
        public static string npcDefaultUnderwear(NPC npc) {
            switch (npc.Name.ToLower())
            {
                case "vincent":
                    return "baby print diaper";
                case "jas":
                    return "lavender pullups";
                default:
                    return npc.Gender == Gender.Male ? "big kid undies" : "polka dot panties";
            }
        }
        public static Container npcUnderwear(NPC npc)
        {
            return new Container(npc, "underwear", npcDefaultUnderwear(npc));
        }
        public static string npcDefaultPantsName(NPC npc)
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
        public static Container npcPants(NPC npc)
        {
            var staticType = "blue jeans";
            var defaultName = npcDefaultPantsName(npc);
            var pants = new Container(npc, "pants", staticType);
            if(pants.displayName == staticType && defaultName != "" && defaultName != pants.displayName)
            {
                pants.displayName = defaultName;
                pants.description = defaultName;
            }
            return pants;
        }

        // a very simplified accident mechanic that assumes that the npc has an accident in a way that the diaper can be anywhere from full to empty
        public static void npcAccident(NPC npc, IncidentType type)
        {
            
            var underwear = npcUnderwear(npc);
            var accidentSize = (float)Regression.rnd.Next((int)(underwear.GetCapacity(type) * 0.1f), (int)underwear.GetCapacity(type));
            if (type == IncidentType.PEE)
            {
                npc.movementPause = 3000;
                npc.doEmote(28, false);
                underwear.AddPee(accidentSize);
                Game1.playSound("wateringCan");
            }
            else
            {
                npc.movementPause = 3000;
                npc.doEmote(12, false);
                underwear.AddPoop(accidentSize);
                Animations.AnimateMessingEnd(npc);
            }
           
            
            
        }
        public static void npcChange(NPC npc, string underwearName = null, string pantsName = null)
        {
            var underwear = npcUnderwear(npc);
            if (underwearName != null)
            {
                underwear.ResetToDefault(underwearName);
            }
            else
            {
                underwear.ResetToDefault(npcDefaultUnderwear(npc));
            }
            
            var pants = npcPants(npc);
            pants.ResetToDefault(pants,0,0);
        }
        // This refers to the "general" interaction that can contain dialog to getting changed or doing changes, depending on the character interacted with. It could also be both.
        public static bool canGetGiveChangeNpc(NPC npc)
        {
            int heartLevelForNpc = Game1.player.getFriendshipHeartLevelForNPC(npc.Name);
            switch (npc.Name.ToLower()) {
                case "vincent": // can get changed
                    return heartLevelForNpc >= 6 && Game1.player.getFriendshipHeartLevelForNPC("Jodi") >= 4;
                case "jas": // can get changed
                    return heartLevelForNpc >= 6 && Game1.player.getFriendshipHeartLevelForNPC("Marnie") >= 4;
                case "sam": // can get changed AND give changes
                    return heartLevelForNpc >= 8 && Game1.player.dialogueQuestionsAnswered.Contains("124") || Game1.player.dialogueQuestionsAnswered.Contains("125");
                case "jodi": // can give changes
                    return heartLevelForNpc >= 6;
                case "abigail":
                    return heartLevelForNpc >= 8;
                case "gus":
                    return heartLevelForNpc >= 8;
                case "maru":
                    return heartLevelForNpc >= 4;
                case "penny":
                    return heartLevelForNpc > 6;
                default:
                    return false;
            }
        }

        private void RemoveDialogueFromNPC(NPC npc, string keyRemove)
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
                    case SButton.F5://Alt F4 is reserved to close
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
                    case SButton.F5:
                        Animations.CheckUnderwear(body);
                        break;
                    case SButton.F6: /*F4 is reserved for screenshot mode*/
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
            else if((currentShopMenu = e.NewMenu as ShopMenu) != null)
            {
                //Default to all underwear being available
                List<string> allUnderwear = Strings.ValidUnderwearTypes();
                List<string> availableUnderwear = allUnderwear;
                bool underwearAvailableAtShop = false;
                if(Game1.currentLocation is SeedShop)
                {
                    //The seed shop does not sell the Joja diaper
                    availableUnderwear.Remove("joja diaper");
                    underwearAvailableAtShop = true;
                } else if(Game1.currentLocation is JojaMart)
                {
                    //Joja shop ONLY sels the Joja diaper and a cloth diaper
                    availableUnderwear.Clear();
                    availableUnderwear.Add("joja diaper");
                    availableUnderwear.Add("cloth diaper");
                    underwearAvailableAtShop = true;
                }

                if(underwearAvailableAtShop)
                {
                    foreach(string type in availableUnderwear)
                    {
                        Underwear underwear = new Underwear(type, 1);
                        currentShopMenu.forSale.Add(underwear);
                        currentShopMenu.itemPriceAndStock.Add(underwear, new ItemStockInformation(underwear.container.price, StardewValley.Menus.ShopMenu.infiniteStock));
                    }
                }
            }
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

            //If we try to take or put down pants, this should only work if you are allowed to change them.
            if (e.Button == SButton.MouseLeft)
            {
                
                var men = Game1.activeClickableMenu as StardewValley.Menus.GameMenu;
                if (men != null)
                {
                    var inventory = men.pages[men.currentTab] as StardewValley.Menus.InventoryPage;
                    if (inventory != null)
                    {
                        var clothing = inventory.hoveredItem as StardewValley.Objects.Clothing;
                        if(clothing != null)
                        {
                            if(clothing.clothesType.Value == Clothing.ClothesType.PANTS)
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
                    this.body.DrinkWateringCan();
                    return;
                }

                //Otherwise Check if we're holding underwear
                Underwear activeObject = who.ActiveObject as Underwear;
                if (activeObject != null)
                {
                    //If the Underwear we are holding isn't currently wet, messy, or drying; change into it.
                    if ((double)activeObject.container.wetness + (double)activeObject.container.messiness == 0.0 && !activeObject.container.drying)
                    {
                        if(Regression.config.PantsChangeRequiresHome && body.HasWetOrMessyDebuff() && !body.InPlaceWithPants())
                        {
                            Animations.Write(Regression.t.Change_Requires_Pants, body);
                            return;
                        }

                        Underwear OldUnderwear = new Underwear(body.underwear, 1);

                        body.ChangeUnderwear(activeObject);
                        who.reduceActiveItemByOne();
                        body.ResetPants();

                        //If the underwear returned is not removable, destroy it
                        if (!OldUnderwear.container.washable) {
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
                if ((AtWaterSource()|| AtWell()) && e.IsDown(SButton.LeftShift))
                  this.body.DrinkWaterSource();
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
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Cheat Mode",
                tooltip: () => "Allowes to spawn items, change potty training and displays debug related messages. (Debug)",
                getValue: () => config.Debug,
                setValue: value => config.Debug = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Easy Mode",
                tooltip: () => "Hunger and Thirst are refilled every morning and the wet beds dried. (Easymode)",
                getValue: () => config.Easymode,
                setValue: value => config.Easymode = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Wetting",
                tooltip: () => "This activates pee and bladder events.",
                getValue: () => config.Wetting,
                setValue: value => config.Wetting = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Messing",
                tooltip: () => "This activates poop and bowel events.",
                getValue: () => config.Messing,
                setValue: value => config.Messing = value
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Bladder Capacity (mL)",
                tooltip: () => "600 is around 3 potty runs a day. (MaxBladderCapacity)",
                getValue: () => config.MaxBladderCapacity,
                setValue: value => config.MaxBladderCapacity = value,
                min: 300, max: 1800, interval: 50
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Bowel Capacity (mL)",
                tooltip: () => "1000 is around 1.5 potty runs a day. (MaxBowelCapacity)",
                getValue: () => config.MaxBowelCapacity,
                setValue: value => config.MaxBowelCapacity = value,
                min: 300, max: 1800, interval: 50
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Always notice accidents",
                tooltip: () => "Defines if you will notice accidents on low control values. (AlwaysNoticeAccidents)",
                getValue: () => config.AlwaysNoticeAccidents,
                setValue: value => config.AlwaysNoticeAccidents = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Pants Change at Home",
                tooltip: () => "Changing your pants (in case you soiled your cloth) requires you to be at home. (Usually on) (PantsChangeRequiresHome)",
                getValue: () => config.PantsChangeRequiresHome,
                setValue: value => config.PantsChangeRequiresHome = value
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Key Bindings",
                text: () => "Key Bindings"
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Continence",
                text: () => "Continence"
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Friendships",
                text: () => "Friendships"
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Continence",
                text: () => "Continence"
            );
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Save Files",
                text: () => "Save Files"
            );
            // All the options related to continence balancing
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Key Bindings"
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Pee Pants",
                tooltip: () => "The key you want to press to just piddle yourself. (KeyPee)",
                getValue: () => (SButton)config.KeyPee,
                setValue: value => config.KeyPee = (int)value
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Poop Pants",
                tooltip: () => "The key you want to press to just poop yourself. (KeyPoop)",
                getValue: () => (SButton)config.KeyPoop,
                setValue: value => config.KeyPoop = (int)value
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Pee In Potty",
                tooltip: () => "The key you want to press to pee in the potty like a good girl or boy. (KeyPeeInToilet)",
                getValue: () => (SButton)config.KeyPeeInToilet,
                setValue: value => config.KeyPeeInToilet = (int)value
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Poop In Potty",
                tooltip: () => "The key you want to press to poop in the potty like a good girl or boy. (KeyPoopInToilet)",
                getValue: () => (SButton)config.KeyPoopInToilet,
                setValue: value => config.KeyPoopInToilet = (int)value
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Go In Pants",
                tooltip: () => "The key you want to press to pee and poop yourself. (KeyGoInPants)",
                getValue: () => (SButton)config.KeyGoInPants,
                setValue: value => config.KeyGoInPants = (int)value
            );
            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Go Potty",
                tooltip: () => "The key you want to press to pee and poop in the potty like a good girl or boy. (KeyGoInToilet)",
                getValue: () => (SButton)config.KeyGoInToilet,
                setValue: value => config.KeyGoInToilet = (int)value
            );

            // All the options related to continence balancing
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Continence"
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Nighttime Losses",
                tooltip: () => "How serious the loss of potty training is at night, compared to daytime. Usually 50 (half). (NighttimeLossMultiplier)",
                getValue: () => config.NighttimeLossMultiplier,
                setValue: value => config.NighttimeLossMultiplier = value,
                min: 0, max: 200, interval: 10
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Nighttime Gains",
                tooltip: () => "How big the gains are if you stay dry/clean at night, compared to daytime. Usually 50 (half). (NighttimeGainMultiplier)",
                getValue: () => config.NighttimeGainMultiplier,
                setValue: value => config.NighttimeGainMultiplier = value,
                min: 0, max: 200, interval: 10
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Accident Bladder Loss",
                tooltip: () => $"2 is a 2% continence loss for {config.MaxBladderCapacity}mL accidents. (BladderLossContinenceRate)",
                getValue: () => config.BladderLossContinenceRate,
                setValue: value => config.BladderLossContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Accident Bowel Loss",
                tooltip: () => $"3 is a 3% continence loss for {config.MaxBowelCapacity}mL accidents. (BowelLossContinenceRate)",
                getValue: () => config.BowelLossContinenceRate,
                setValue: value => config.BowelLossContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Toilet Bladder Gain",
                tooltip: () => $"3 is a 3% continence gain for making it to the toilet with a bladder that is at least half full. (BladderGainContinenceRate)",
                getValue: () => config.BladderGainContinenceRate,
                setValue: value => config.BladderGainContinenceRate = value,
                min: 0, max: 20, interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Toilet Bowel Gain",
                tooltip: () => $"3 is a 3% continence gain for making it to the toilet with a bowel that is at least half full. (BowelGainContinenceRate)",
                getValue: () => config.BowelGainContinenceRate,
                setValue: value => config.BowelGainContinenceRate = value,
                min: 0, max: 20, interval: 1
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Start Bladder Continence",
                tooltip: () => $"Defines the starting (new game) bladder continence. Usually 70. Also applies to old saves without this mod activated (StartBladderContinence)",
                getValue: () => config.StartBladderContinence,
                setValue: value => config.StartBladderContinence = value,
                min: (int)(Body.minBladderContinence * 100), max: 100, interval: 5
            );
            configMenu.AddNumberOption(
               mod: this.ModManifest,
               name: () => "Start Bowel Continence",
               tooltip: () => $"Defines the starting (new game) bowel continence. Usually 90. Also applies to old saves without this mod activated (StartBowelContinence)",
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
                name: () => "Penalty Peeing",
                tooltip: () => "How peeing in public impacts friendships. 100 is normal, 50 would be half, 200 double the impact. 0 deactivates loss of frienship for pee incidents. (FriendshipPenaltyBladderMultiplier)",
                getValue: () => config.FriendshipPenaltyBladderMultiplier,
                setValue: value => config.FriendshipPenaltyBladderMultiplier = value,
                min: 0, max: 500, interval: 10
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Penalty Pooping",
                tooltip: () => "How pooping in public impacts friendships. 100 is normal, 50 would be half, 200 double the impact. 0 deactivates loss of frienship for poop incidents. (FriendshipPenaltyBowelMultiplier)",
                getValue: () => config.FriendshipPenaltyBowelMultiplier,
                setValue: value => config.FriendshipPenaltyBowelMultiplier = value,
                min: 0, max: 500, interval: 10
            );

            // All the options related to save files
            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Save Files"
            );
            configMenu.AddParagraph(
                mod: this.ModManifest,
                text: () => "Starting at version 1.5.0, save files (json) are no longer required/created. The only use of this functions is to load old save files or manually edit saves."
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Read Save Files",
                tooltip: () => "This will activate reading of the (legacy) save files. This will also delete save files from the last day, if a new one starts.",
                getValue: () => config.ReadSaveFiles,
                setValue: value => config.ReadSaveFiles = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Write Save Files",
                tooltip: () => "This will activate writing of the (legacy) save files. It is recommended to disable this option.",
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
        private void ReceiveTimeOfDayChanged(object sender, TimeChangedEventArgs e)
        {
            lastTimeOfDay = Game1.timeOfDay;

            //If its 6:10AM, handle delivering mail
            if (Game1.timeOfDay == 610)
                Mail.CheckMail();

            if (Game1.timeOfDay < 630)
                return;

            //If its earlier than 6:30, we aren't wet/messy don't notice that we're still soiled (or don't notice with ~5% chance even if soiled)
            if (rnd.NextDouble() < 0.0555555559694767 && body.underwear.wetness + (double)body.underwear.messiness > 0.0)
                Animations.AnimateStillSoiled(this.body);

            if (rnd.NextDouble() < 0.0555555559694767 && (body.NeedsChangies(IncidentType.PEE) || body.NeedsChangies(IncidentType.POOP)))
                Animations.AnimateShouldChange(this.body);

            if (Game1.player.dialogueQuestionsAnswered.Contains("change_other_yes"))
                Game1.player.dialogueQuestionsAnswered.Remove("change_other_yes");
            if (Game1.player.dialogueQuestionsAnswered.Contains("change_other_no"))
                Game1.player.dialogueQuestionsAnswered.Remove("change_other_no");
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
