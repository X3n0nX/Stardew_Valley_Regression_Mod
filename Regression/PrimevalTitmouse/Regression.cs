using Microsoft.Xna.Framework;
using Regression;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;

namespace PrimevalTitmouse
{
    public class Regression : Mod
    {
        public static int lastTimeOfDay = 0;
        public static bool morningHandled = true;
        public static Random rnd = new Random();
        public static bool started = false;
        public Body body;
        public static ModConfig config;
        public static IModHelper help;
        public static IMonitor monitor;
        public bool shiftHeld;
        public static Data t;
        public static Farmer who;
        public static int stillSoilCD = 0;
        public static bool alreadyAte = false;
        public Dictionary<string, string> diag = new Dictionary<string, string>();
        public List<string> sourcelist = new List<string>();

        public static int[] checkCooldown = new int[5];

        const float timeInTick = (1f/43f); //One second realtime ~= 1/43 hours in game




        public override void Entry(IModHelper h)
        {
            help = h;
            monitor = Monitor;
            InitializeDialogueList();
            config = Helper.ReadConfig<ModConfig>();
            t = Helper.Data.ReadJsonFile<Data>(string.Format("{0}.json", (object)config.Lang)) ?? Helper.Data.ReadJsonFile<Data>("en.json");
            h.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(this.OnGameLaunched);
            h.Events.GameLoop.Saving += new EventHandler<SavingEventArgs>(this.BeforeSave);
            h.Events.GameLoop.DayStarted += new EventHandler<DayStartedEventArgs>(ReceiveAfterDayStarted);
            h.Events.GameLoop.OneSecondUpdateTicking += new EventHandler<OneSecondUpdateTickingEventArgs>(ReceiveUpdateTick);
            h.Events.GameLoop.TimeChanged += new EventHandler<TimeChangedEventArgs>(ReceiveTimeOfDayChanged);
            h.Events.Input.ButtonsChanged += new EventHandler<ButtonsChangedEventArgs>(OnButtonsChanged);
            h.Events.Input.ButtonPressed += new EventHandler<ButtonPressedEventArgs>(ReceiveMouseChanged);
            h.Events.Display.MenuChanged += new EventHandler<MenuChangedEventArgs>(ReceiveMenuChanged);
            h.Events.Display.RenderingHud += new EventHandler<RenderingHudEventArgs>(ReceivePreRenderHudEvent);

        }

        public void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {

            var configMenu = help.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
            {
                this.Monitor.Log($"Config Menu is Null", LogLevel.Error);
                return;
            }


            this.Monitor.Log($"Successfully hooked into GMCM", LogLevel.Debug);

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => config = new ModConfig(),
                save: () => Helper.WriteConfig(config)
            );

            
            configMenu.AddTextOption(
                mod:ModManifest,
                name: () => "Regressed Nickname",
                tooltip: () => "Sets a nickname villagers will sometimes refer to you as if they see you as little.",
                getValue: () => config.babyNickname,
                setValue: value => config.babyNickname = value

            );

            

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Messing",
                tooltip: () => "Allows you to mess (poop) your underwear.",
                getValue: () => config.Messing,
                setValue: value => config.Messing = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Wetting",
                tooltip: () => "Allows you to wet (pee) your underwear.",
                getValue: () => config.Wetting,
                setValue: value => config.Wetting = value
            );

      

            configMenu.AddBoolOption(
                 mod: ModManifest,
                 name: () => "Always Notice Accidents",
                 tooltip: () => "Makes continence have no effect on wether you notice accidents.",
                 getValue: () => config.AlwaysNoticeAccidents,
                 setValue: value => config.AlwaysNoticeAccidents = value
                );

            configMenu.AddBoolOption(
                 mod: ModManifest,
                 name: () => "Easy Mode",
                 getValue: () => config.Easymode,
                 setValue: value => config.Easymode = value
                );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "No Friendship Penalty",
                tooltip: () => "Disables the small penalty to friendship for those who witness you going to the bathroom who aren't ABDL oriented.",
                getValue: () => config.NoFriendshipPenalty,
                setValue: value => config.NoFriendshipPenalty = value
            );

            configMenu.AddBoolOption(
                  mod: ModManifest,
                  name: () => "Disable Hunger and Thirst",
                  tooltip: () => "Disables the mods Hunger and Thirst system.",
                  getValue: () => config.NoHungerAndThirst,
                  setValue: value => config.NoHungerAndThirst = value
              );

            

            configMenu.AddNumberOption(
                  mod: ModManifest,
                  name: () => "Food Nourishment Multiplier",
                  tooltip: () => "Multiplies the value that every food satiates you by.",
                  getValue: () => config.foodAmtMult,
                  setValue: value => config.foodAmtMult = value

              ) ;

            configMenu.AddNumberOption(
                  mod: ModManifest,
                  name: () => "Drink Nourishment Multiplier",
                  tooltip: () => "Multiplies the value that every drink satiates you by.",
                  getValue: () => config.drinkAmtMult,
                  setValue: value => config.drinkAmtMult = value


              );
            

            configMenu.AddKeybindList(
                  mod: ModManifest,
                  name: () => "Wet",
                  getValue: () => config.WetBind,
                  setValue: value => config.WetBind = value

              );

            configMenu.AddKeybindList(
                              mod: ModManifest,
                              name: () => "Mess",
                              getValue: () => config.MessBind,
                              setValue: value => config.MessBind = value

                          );

            configMenu.AddKeybindList(
                              mod: ModManifest,
                              name: () => "Pull Down Pants",
                              tooltip: () => "Modifier key that changes wetting and messing to going on the ground.",
                              getValue: () => config.PullDownPantsBind,
                              setValue: value => config.PullDownPantsBind = value

                          );

            configMenu.AddKeybindList(
                              mod: ModManifest,
                              name: () => "Check Undies",
                              getValue: () => config.CheckUndiesBind,
                              setValue: value => config.CheckUndiesBind = value

                          );

            configMenu.AddKeybindList(
                              mod: ModManifest,
                              name: () => "Check Pants",
                              getValue: () => config.CheckPantsBind,
                              setValue: value => config.CheckPantsBind = value

                          );

            configMenu.AddKeybindList(
                  mod: ModManifest,
                  name: () => "Check Villager Diaper",
                  getValue: () => config.CheckVillagerDiaperBind,
                  setValue: value => config.CheckVillagerDiaperBind = value

              );

            configMenu.AddKeybindList(
                mod: ModManifest,
                name: () => "Ask a Villager for a change.",
                getValue: () => config.AskVillagerChangeBind,
                setValue: value => config.AskVillagerChangeBind = value

              );

            configMenu.AddBoolOption(
                  mod: ModManifest,
                  name: () => "Debug Mode",
                  getValue: () => config.Debug,
                  setValue: value => config.Debug = value
              );

        }




        public void DrawStatusBars()
        {

            int x1 = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - (65 + (int)((StatusBars.barWidth)));
            int y1 = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - (25 + (int)((StatusBars.barHeight)));

            if (Game1.currentLocation is MineShaft || Game1.currentLocation is Woods || Game1.currentLocation is SlimeHutch || Game1.currentLocation is VolcanoDungeon || who.health < who.maxHealth)
                x1 -= 58;

            if (!config.NoHungerAndThirst || PrimevalTitmouse.Regression.config.Debug)
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
            int y2 = (Game1.player.questLog).Count == 0 ? 250 : 310;
            Animations.DrawUnderwearIcon(body.underwear, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - 94, y2);
        }

        private void GiveUnderwear()
        {
            List<Item> objList = new List<Item>();
            foreach (string validUnderwearType in Strings.ValidUnderwearTypes())
                objList.Add(new Underwear(validUnderwearType, 0.0f, 0.0f, 20));
            objList.Add(new StardewValley.Object("399", 99, false, -1, 0));
            objList.Add(new StardewValley.Object("348", 99, false, -1, 0));
            Game1.activeClickableMenu = new ItemGrabMenu(objList);
        }

        private static void restoreItems(StardewValley.Inventories.Inventory items, Dictionary<int, Dictionary<string, string>> invReplacement)
        {
            foreach (KeyValuePair<int, Dictionary<string, string>> entry in invReplacement)
            {
                var underwear = new Underwear();
                underwear.rebuild(entry.Value, items[entry.Key]);
                items[entry.Key] = underwear;
            }
        }



        private void ReceiveAfterDayStarted(object sender, DayStartedEventArgs e)
        {
            t = Helper.Data.ReadJsonFile<Data>(string.Format("{0}.json", (object)config.Lang)) ?? Helper.Data.ReadJsonFile<Data>("en.json");
            body = Helper.Data.ReadJsonFile<Body>(string.Format("{0}/RegressionSave.json", Constants.SaveFolderName)) ?? new Body();
            started = true;
            who = Game1.player;

            var invReplacement = Helper.Data.ReadJsonFile<Dictionary<int, Dictionary<string, string>>>(string.Format("{0}/RegressionSaveInv.json", Constants.SaveFolderName));
            if (invReplacement != null)
            {
                restoreItems(Game1.player.Items, invReplacement);
            }

            var chestReplacement = Helper.Data.ReadJsonFile<Dictionary<string, Dictionary<int, Dictionary<string, string>>>>(string.Format("{0}/RegressionSaveChest.json", Constants.SaveFolderName));
            if (chestReplacement != null)
            {
                int locId = 0;
                foreach (var location in Game1.locations)
                {
                    foreach (var obj in location.Objects.Values)
                    {
                        var id = string.Format("{0}-{1}-{2}", locId, obj.TileLocation.X, obj.TileLocation.Y);
                        if (obj is Chest chest && chestReplacement.ContainsKey(id))
                        {
                            restoreItems(chest.Items, chestReplacement[id]);
                        }
                    }
                    locId++;
                }
                restoreItems(Game1.player.Items, invReplacement);
            }



            Animations.AnimateNight(body);
            HandleMorning(sender, e);
            checkCooldown[0] = 0; checkCooldown[1] = 0; checkCooldown[2] = 0; checkCooldown[3] = 0; checkCooldown[4] = 0;
        }

        private void HandleMorning(object Sender, DayStartedEventArgs e)
        {
            body.HandleMorning();
        }

        private static Dictionary<int, Dictionary<string, string>> replaceItems(StardewValley.Inventories.Inventory items)
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



        //Save Mod related variables in separate JSON. Also trigger night handling if not on the very first day.
        private void BeforeSave(object Sender, SavingEventArgs e)
        {
            body.bedtime = lastTimeOfDay;


            if (Game1.dayOfMonth != 1 || Game1.currentSeason != "spring" || Game1.year != 1)
                body.HandleNight();
            if (string.IsNullOrWhiteSpace(Constants.SaveFolderName))
                return;

            var chestReplacements = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

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

            var invReplacements = replaceItems(Game1.player.Items);

            Helper.Data.WriteJsonFile(string.Format("{0}/RegressionSave.json", Constants.SaveFolderName), body);
            Helper.Data.WriteJsonFile(string.Format("{0}/RegressionSaveInv.json", Constants.SaveFolderName), invReplacements);
            Helper.Data.WriteJsonFile(string.Format("{0}/RegressionSaveChest.json", Constants.SaveFolderName), chestReplacements);
        }

        private void ReceiveUpdateTick(object sender, OneSecondUpdateTickingEventArgs e)
        {

            //Ignore everything until we've started the day
            if (!started)
                return;


            //If time is moving, update our body state (Hunger, thirst, etc.)
            if (ShouldTimePass())
                this.body.HandleTime(timeInTick);

            //If we arent eating, reset already ate bool, placed before Consume call and used in conjunction with RecieveMouseUpdate or else theres a chance Consume will be skipped if you eat too fast on certain tick timings.
            if (Game1.player.isEating == false)
                alreadyAte = false;

            //Handle eating and drinking. Bool alreadyAte ensures the consume function triggers once, instead of every tick during the animation.
            if (Game1.player.isEating && Game1.activeClickableMenu == null && !alreadyAte)
            {
                body.Consume(who.itemToEat.Name);
                alreadyAte = true;
            }


        }
        //Determine if we need to handle time passing (not the same as Game time passing)
        private static bool ShouldTimePass()
        {
            return ((Game1.game1.IsActive || Game1.options.pauseWhenOutOfFocus == false) && (Game1.paused == false && Game1.dialogueUp == false) && (Game1.currentMinigame == null && Game1.eventUp == false && Game1.activeClickableMenu == null) && Game1.fadeToBlack == false);
        }

        //Interprete key-presses
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            //If we haven't started the day, ignore the key presses
            if (!started)
                return;

            //Interpret buttons differently if holding Left Alt & Debug is enabled
            if (config.Debug)
            {

                if (config.DecEverythingBind.JustPressed()) body.DecreaseEverything();

                if (config.IncEverythingBind.JustPressed()) body.IncreaseEverything();

                if (config.UndiesMenuBind.JustPressed()) GiveUnderwear();

                if (config.TimeMagicBind.JustPressed()) TimeMagic.doMagic();
            }

            if (config.WetBind.JustPressed()) body.Wet(true, !config.PullDownPantsBind.IsDown());

            if (config.MessBind.JustPressed()) body.Mess(true, !config.PullDownPantsBind.IsDown());

            if (config.CheckUndiesBind.JustPressed()) Animations.CheckUnderwear(body);

            if (config.CheckPantsBind.JustPressed()) Animations.CheckPants(body);

            if (config.CheckVillagerDiaperBind.JustPressed()) Animations.HandleCheck(body, 20, 3);

            if (config.AskVillagerChangeBind.JustPressed()) Animations.HandleAskChange(body, 20, 3);

            CheckForDialogueCommands();


            //dialogue sent via animations doesnt trigger MenuChanged for whatever reason, so it has to check when you advance the dialogue via inputs.



            /*
                case SButton.S:
                    if (e.IsDown(SButton.LeftShift))
                    {
                        body.ChangeBowelContinence(0.1f);
                    }
                    else
                    {
                        body.ChangeBladderContinence(0.1f);
                    }
                    break;
                case SButton.W:
                    if (e.IsDown(SButton.LeftShift))
                    {
                        body.ChangeBowelContinence(-0.1f);
                    }
                    else
                    {
                        body.ChangeBladderContinence(-0.1f);
                    }
                    break;
            }
            */
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
                if (body.bed.IsDrying())
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
                if (Game1.currentLocation is SeedShop)
                {
                    //The seed shop does not sell the Joja diaper and adult diapers
                    availableUnderwear.Remove("Joja diaper");
                    availableUnderwear.Remove("space diaper");
                    availableUnderwear.Remove("pawprint diaper");
                    availableUnderwear.Remove("heart diaper");
                    availableUnderwear.Remove("Jodi's diapers");
                    underwearAvailableAtShop = true;
                }
                else if (Game1.currentLocation is JojaMart)
                {
                    //Joja shop ONLY sels the Joja diaper and a cloth diaper
                    availableUnderwear.Clear();
                    availableUnderwear.Add("Joja diaper");
                    availableUnderwear.Add("cloth diaper");
                    underwearAvailableAtShop = true;
                }
                else if (Game1.currentLocation.Name == "Hospital")
                {

                    //Hospital sells both baby and adult diapers, but not undies.
                    availableUnderwear.Remove("Joja diaper");
                    availableUnderwear.Remove("cloth diaper");
                    availableUnderwear.Remove("black thong");
                    availableUnderwear.Remove("polka dot panties");
                    availableUnderwear.Remove("big kid undies");
                    availableUnderwear.Remove("dinosaur undies");
                    availableUnderwear.Remove("Jodi's diapers");
                    underwearAvailableAtShop = true;
                }

                if(underwearAvailableAtShop)
                {
                    foreach(string type in availableUnderwear)
                    {
                        Underwear underwear = new Underwear(type, 0.0f, 0.0f, 1);
                        currentShopMenu.forSale.Add(underwear);
                        currentShopMenu.itemPriceAndStock.Add(underwear, new ItemStockInformation(underwear.container.price, 999));
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

            //Handle a Left Click
            if (e.Button == SButton.MouseLeft)
            {
                //If Left click is already being interpreted by another event (or we otherwise wouldn't process such an event. Ignore it.
                if ((Game1.dialogueUp || Game1.currentMinigame != null || (Game1.eventUp || Game1.activeClickableMenu != null) || Game1.fadeToBlack) || (who.isRidingHorse() || !who.canMove || (Game1.player.isEating || who.canOnlyWalk) || who.FarmerSprite.pauseForSingleAnimation)) return;

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
                    if ((double)activeObject.container.wetness + (double)activeObject.container.messiness == 0.0 && !activeObject.container.IsDrying())
                    {
                        who.reduceActiveItemByOne(); //Take it out of inventory
                        Container container = body.ChangeUnderwear(activeObject, true); //Put on the new underwear and return the old
                        Underwear underwear = new Underwear(container.name, container.wetness, container.messiness, 1);

                        //If the underwear returned is not removable, destroy it
                        if (!container.removable) { }
                        //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                        else if (!who.addItemToInventoryBool(underwear, false))
                        {
                            List<Item> objList = new List<Item>();
                            objList.Add(underwear);
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

            //if we're free to move, and hit right click, set already ate to false as a precaution.
            //This is to prevent a rare issue where if you ate repetedly, too quickly, on certain tick timings, youd eat the next fruit before alreadyAte could be reset, causing the Consume function to whif.
            //<TODO> Very scuffed solution, could use a much better workaround, but it works for now. Don't recommend removing this until Consume is handled by a closed loop of instantaneous functions, or is at least taken off tick timing.
            if (e.Button == SButton.MouseRight && Game1.player.canMove)
            {
                alreadyAte = false;
            }


        }

        //If approppriate, draw bars for Hunger, thirst, bladder and bowels
        public void ReceivePreRenderHudEvent(object sender, RenderingHudEventArgs args)
        {
            if (!started || Game1.currentMinigame != null || Game1.eventUp || Game1.globalFade)
                return;
            DrawStatusBars();
        }

        private void ReceiveTimeOfDayChanged(object sender, TimeChangedEventArgs e)
        {

            lastTimeOfDay = Game1.timeOfDay;
            if(stillSoilCD > 0) stillSoilCD--;

            for (int i = 0; i < checkCooldown.Length; i++)
            {
                if(checkCooldown[i] > 0) checkCooldown[i]--;
                //if(config.Debug)this.Monitor.Log($"Cooldown {i} is {checkCooldown[i]}", LogLevel.Debug);
            }

            //If its 6:10AM, handle delivering mail
            if (Game1.timeOfDay == 610)
                Mail.CheckMail();

            //If its earlier than 6:30, we aren't wet/messy don't notice that we're still soiled (or do notice with ~5% chance even if soiled)
            if (rnd.NextDouble() >= 0.055555559694767 || body.underwear.wetness + (double)body.underwear.messiness <= 0.0 || Game1.timeOfDay < 630)
                return;
            //if the stillSoiled message is off cooldown, activate the message and roll a new random cooldown between 3 and 5 in game hours (to prevent spam).
            if (stillSoilCD == 0)
            {
                Animations.AnimateStillSoiled(this.body);
                stillSoilCD = rnd.Next(18, 30);
            }


        }
        
        public string ReturnCurrentDialogue()
        {
            if (Game1.dialogueUp && Game1.currentSpeaker != null)
            {
                NPC npc = Game1.currentSpeaker;
                Dialogue dialogue;
                npc.CurrentDialogue.TryPeek(out dialogue);

                if (dialogue.currentDialogueIndex > dialogue.dialogues.Count - 1) return null;


                if (dialogue.dialogues[dialogue.currentDialogueIndex].Text != null)
                {
                    string line = dialogue.dialogues[dialogue.currentDialogueIndex].Text;
                    return line;
                }
            }

            return null;
        }

        public void CheckForDialogueCommands()
        {
            if (ReturnCurrentDialogue == null) return;
            string text = ReturnCurrentDialogue();


            NPC npc = Game1.currentSpeaker;
            Dialogue dialogue;


            if (text != null && text.Contains("::"))
                {

                    string[] parsedString = text.Split("::");
                    parsedString = parsedString[1].Split(':');

                    string id = parsedString[0];
                    string command = parsedString[1];
                    string item = parsedString[2];
                    int amount = Int32.Parse(parsedString[3]);
                    string raw = "";

                    diag.TryGetValue(id, out raw);

                    this.Monitor.Log("Raw dialogue: " + raw, LogLevel.Debug);

                    string diaper = item;

                    if (command == "change")
                    {
                        if (diaper != "") body.ChangeUnderwear(new Underwear(diaper, 0, 0, amount), false);
                    }


                    if (command == "give")
                    {
                        Underwear underwear = new Underwear(diaper, 0, 0, amount);
                        who.addItemToInventoryBool(underwear);
                    }






                    dialogue = new Dialogue(npc, null, Strings.InsertVariables(Strings.ReplaceAndOr(raw, body.underwear.messiness == 0, body.underwear.messiness > 0, "&"),body,(Container)null));


                    npc.CurrentDialogue.Clear();
                    npc.CurrentDialogue.Push(dialogue);
                    Game1.drawDialogue(npc);
                }

        }


        public void InitializeDialogueList()
        {

            string path = Regression.help.DirectoryPath.ToString();
            string newPath = Path.GetFullPath(Path.Combine(path, @"..\"));
            DirectoryInfo d = new DirectoryInfo(newPath + "\\Regression Dialogue\\Dialogue\\NPCs\\");

            this.Monitor.Log("Initialize 1", LogLevel.Debug);

            var lines = File.ReadLines(d.ToString() + "trigger.txt");

                foreach (var line in lines)
                {
                    if (!line.Contains("||")) continue;
                List<string> strings = new List<string>();
                    string[] stringArray = line.Split("||");

                for (int i = 0; i < stringArray.Length; i++) strings.Add(stringArray[i]);
                   


                    string id = strings[0];
                    this.Monitor.Log(line, LogLevel.Warn);

                    diag.Add(id, strings[1]);
            }


            
        }



        public Regression()
        {
            //base.Actor();
        }
    }
}
