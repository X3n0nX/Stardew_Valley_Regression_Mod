using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PrimevalTitmouse
{
    //Added enum to indicate type instead of strings.
    public enum FullnessType
    {
        None,
        Clear,
        Wet,
        Messy,
        WetMessy,
        Drying
    }

    internal static class Animations
    {
        //<FIXME> Adding Leo here as a quick fix to a softlock issue due to not having ABDL dialogue written
        private static readonly List<string> NPC_LIST = new List<string> { "Linus", "Krobus", "Dwarf", "Leo" };
        public static readonly int poopAnimationTime = 2000; //ms
        public static readonly int peeAnimationTime = 2000; //ms
        //Magic Constants
        public const string SPRITES = "Assets/sprites.png";
        public const int PAUSE_TIME = 20000;
        public const float DRINK_ANIMATION_INTERVAL = 80f;
        public const int DRINK_ANIMATION_FRAMES = 8;
        public const int LARGE_SPRITE_DIM = 64;
        public const int SMALL_SPRITE_DIM = 16;
        public const int DIAPER_HUD_DIM = 64;
        enum FaceDirection : int
        {
            Down = 2,
            Left = 1,
            Right = 3,
            Up = 0
        };

        public static Texture2D sprites;
        private static Data t;
        public static Farmer player => Game1.player;

        //Static Accessor Methods. Ensure that variables are initialized.
        public static Data GetData()
        {
            t ??= Regression.t;
            return t;
        }
        public static Texture2D GetSprites()
        {
            sprites ??= Regression.help.ModContent.Load<Texture2D>(SPRITES);
            return sprites;
        }

        

        public static float ZoomScale()
        {
            return Game1.options.zoomLevel / Game1.options.uiScale;
        }

        public static void AnimateDrinking(bool waterSource = false)
        {
            //If we aren't facing downward, turn
            if (player.getFacingDirection() != (int)FaceDirection.Down)
                player.faceDirection((int)FaceDirection.Down);


            //Stop doing anything that would prevent us from moving
            //Essentially take control of the variable
            player.forceCanMove();


            //Stop any form of animation
            player.completelyStopAnimatingOrDoingAction();


            // ISSUE: method pointer
            //Start Drinking animation. While drinking pause time and don't allow movement.
            player.FarmerSprite.animateOnce(StardewValley.FarmerSprite.drink, DRINK_ANIMATION_INTERVAL, DRINK_ANIMATION_FRAMES, new AnimatedSprite.endOfAnimationBehavior(EndDrinking));
            player.freezePause = PAUSE_TIME;
            player.canMove = false;

            //If we drink from the watering can, don't say anything
            if (!waterSource)
                return;

            //Otherwise say something about it
            Say(Animations.GetData().Drink_Water_Source, null);
        }

        //Not really an animation. Just say the bedding's current state.
        public static void AnimateDryingBedding(Body b)
        {
            Write(Animations.GetData().Bedding_Still_Wet, b);
        }


        public static void AnimateMessingStart(Body b, bool voluntary, bool inUnderwear)
        {
            if (b.underwear.removable || inUnderwear)
                Game1.playSound("slosh");

            if (b.isSleeping || !voluntary && !Regression.config.AlwaysNoticeAccidents && (double)b.bowelContinence + 0.449999988079071 <= Regression.rnd.NextDouble())
                return;

            if (!(b.underwear.removable || inUnderwear))
            {
                Animations.Say(Animations.GetData().Cant_Remove, b);
                return;
            }

            if (!inUnderwear)
            {
                if (b.InToilet(inUnderwear))
                    Say(Animations.GetData().Poop_Toilet, b);
                else
                    Say(Animations.GetData().Poop_Voluntary, b);
            }
            else if (voluntary)
                Say(Animations.GetData().Mess_Voluntary, b);
            else
                Say(Animations.GetData().Mess_Accident, b);


            player.doEmote(12, false);
            if (b.IsFishing() || !player.canMove) return; // We skip the actual animations if nessesary

            player.jitterStrength = 1.0f;
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(192, 1152, Game1.tileSize, Game1.tileSize), 50f, 4, 0, player.position.Value - new Vector2(((Character)Animations.player).facingDirection.Value == 1 ? 0.0f : (float)-Game1.tileSize, (float)(Game1.tileSize * 2)), false, ((Character)Animations.player).facingDirection.Value == 1, (float)((Character)Animations.player).StandingPixel.Y / 10000f, 0.01f, Microsoft.Xna.Framework.Color.White, 1f, 0.0f, 0.0f, 0.0f, false));

            player.freezePause = poopAnimationTime;
            player.canMove = false;
        }
        public static void AnimateMessingEnd(Body b)
        {

            if (b.IsFishing()) return;
            Game1.playSound("coin");
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(192, 1152, Game1.tileSize, Game1.tileSize), 50f, 4, 0, Animations.player.position.Value - new Vector2(Animations.player.facingDirection.Value == 1 ? 0.0f : -Game1.tileSize, Game1.tileSize * 2), false, Animations.player.facingDirection.Value == 1, Animations.player.StandingPixel.Y / 10000f, 0.01f, Microsoft.Xna.Framework.Color.White, 1f, 0.0f, 0.0f, 0.0f, false));
        }
        public static void AnimateMessingMinor(Body b)
        {
            Animations.Say(Animations.GetData().Bowels_Leak, b);
            ((Character)Animations.player).doEmote(12, false);
        }

        public static void AnimateWettingStart(Body b, bool voluntary, bool inUnderwear)
        {
            if (b.underwear.removable || inUnderwear)
                Game1.playSound("wateringCan");

            if (b.isSleeping || !voluntary && !Regression.config.AlwaysNoticeAccidents && (double)b.bladderContinence + 0.200000002980232 <= Regression.rnd.NextDouble())
                return;

            if (!(b.underwear.removable || inUnderwear))
            {
                Animations.Say(Animations.GetData().Cant_Remove, b);
                return;
            }

            if (!inUnderwear)
            {
                if (b.InToilet(inUnderwear))
                    Animations.Say(Animations.GetData().Pee_Toilet, b);
                else
                    Animations.Say(Animations.GetData().Pee_Voluntary, b);

                ((GameLocation)Animations.player.currentLocation).temporarySprites.Add(new TemporaryAnimatedSprite(13, (Vector2)((Character)Game1.player).position.Value, Microsoft.Xna.Framework.Color.White, 10, ((Random)Game1.random).NextDouble() < 0.5, 70f, 0, (int)Game1.tileSize, 0.05f, -1, 0));
                HoeDirt terrainFeature;
                if (Animations.player.currentLocation.terrainFeatures.ContainsKey(((Character)Animations.player).Tile) && (terrainFeature = Animations.player.currentLocation.terrainFeatures[((Character)Animations.player).Tile] as HoeDirt) != null)
                    terrainFeature.state.Value = 1;
            }
            else if (voluntary)
                Animations.Say(Animations.GetData().Wet_Voluntary, b);
            else
                Animations.Say(Animations.GetData().Wet_Accident, b);

            // We skip the actual animations if nessesary
            if (b.IsFishing() || !Animations.player.canMove) return;

            player.jitterStrength = 0.5f;
            player.freezePause = peeAnimationTime; //milliseconds
            player.canMove = false;
            ((Character)Animations.player).doEmote(28, false);
        }
        public static void AnimateWettingMinor(Body b)
        {
            Animations.Say(Animations.GetData().Bladder_Leak, b);
            ((Character)Animations.player).doEmote(28, false);
        }


        public static void AnimateWettingEnd(Body b)
        {
            if (b.IsFishing()) return;
            if ((double)b.pants.wetness > (double)b.pants.absorbency)
            {
                ((GameLocation)player.currentLocation).temporarySprites.Add(new TemporaryAnimatedSprite(13, (Vector2)((Character)Game1.player).position.Value, Microsoft.Xna.Framework.Color.White, 10, ((Random)Game1.random).NextDouble() < 0.5, 70f, 0, (int)Game1.tileSize, 0.05f, -1, 0));
                HoeDirt terrainFeature;
                if (Animations.player.currentLocation.terrainFeatures.ContainsKey(((Character)Animations.player).Tile) && (terrainFeature = player.currentLocation.terrainFeatures[((Character)Animations.player).Tile] as HoeDirt) != null)
                    terrainFeature.state.Value = 1;
            }
        }

        public static void AnimateMorning(Body b)
        {
            bool flag = (double)b.pants.wetness > 0.0;
            bool second = (double)b.pants.messiness > 0.0;
            string msg = "" + Strings.RandString(Animations.GetData().Wake_Up_Underwear_State);
            if (second)
            {
                msg = msg + " " + Strings.ReplaceOptional(Strings.RandString(Animations.GetData().Messed_Bed), flag);
                if (!Regression.config.Easymode)
                    msg = msg + " " + Strings.ReplaceAndOr(Strings.RandString(Animations.GetData().Washing_Bedding), flag, second, "&");
            }
            else if (flag)
            {
                msg = msg + " " + Strings.RandString(Animations.GetData().Wet_Bed);
                if (!Regression.config.Easymode)
                    msg = msg + " " + Strings.ReplaceAndOr(Strings.RandString(Animations.GetData().Spot_Washing_Bedding), flag, second, "&");
            }
            Animations.Write(msg, b);
        }
        private static string GetNumericTranslationOnceTwice(int amount)
        {
            switch (amount)
            {
                case 1:
                    return "once";
                case 2:
                    return "twice";
                default:
                    return $"{amount} times";
            }
        }
        private static string GetNumericTranslationTimes(int amount)
        {
            switch (amount)
            {
                case 1:
                    return "one time";
                case 2:
                    return "two times";
                case 3:
                    return "three times";
                case 4:
                    return "four times";
                case 5:
                    return "five times";
                default:
                    return $"{amount} times";
            }
        }
        // Function to make the first character of the input string upper case
        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
        public static void AnimateNight(Body b)
        {
            bool first = b.numPottyPeeAtNight > 0;
            bool second = b.numPottyPooAtNight > 0;
            if (!(first | second) || !b.IsAllowedResource(IncidentType.PEE) && !b.IsAllowedResource(IncidentType.POOP))
                return;

            var list = Animations.GetData().Night;
            // assumtion: if we wake up to pee/poop, we do that togehter if possible. So if we wake up 2 times for pee and 1 time for poop, it is likely we only got up twice, not 3 times
            int gotUpAtNight = Math.Max(b.numPottyPeeAtNight, b.numPottyPooAtNight);
            string toiletMsg = "";

            if (gotUpAtNight > 0)
            {
                toiletMsg = Strings.ReplaceAndOr(Strings.RandString(list["Toilet_Night"]), first, second, "&");
            }
            else
            {
                toiletMsg = Strings.RandString(list["Sleep_All_Night"]);
            }
            toiletMsg = FirstCharToUpper(toiletMsg);

            if (b.numAccidentPooAtNight == 0 && b.numAccidentPeeAtNight == 0)
                toiletMsg += ".";
            else
            {
                var butPlaceholder = false;
                if (gotUpAtNight > 0)
                {
                    butPlaceholder = true;
                    if (!b.underwear.removable)
                    {
                        toiletMsg += " " + Strings.RandString(list["Underwear_Stuck"]);
                        butPlaceholder = false;
                    }
                }
                toiletMsg += ".";

                var accidentReport = Strings.ReplaceOptional(Strings.ReplaceAndOr(Strings.RandString(list["Accident_At_Night"]), b.numAccidentPeeAtNight > 0, b.numAccidentPooAtNight > 0), butPlaceholder);
                accidentReport = FirstCharToUpper(accidentReport);

                toiletMsg += " " + accidentReport + ".";

                if (b.numAccidentPooAtNight > 0 || b.numAccidentPeeAtNight > 0)
                    toiletMsg += " " + FirstCharToUpper(Strings.RandString(list["Belittle_Accidents"])) + ".";
            }
            toiletMsg = Strings.InsertVariable(toiletMsg, "$TOILET_ONCE_TWICE_TOTAL", GetNumericTranslationOnceTwice(gotUpAtNight));
            toiletMsg = Strings.InsertVariable(toiletMsg, "$TOILET_TIMES_TOTAL", GetNumericTranslationTimes(gotUpAtNight));
            toiletMsg = Strings.InsertVariable(toiletMsg, "$ACCIDENT_ONCE_TWICE_TOTAL", GetNumericTranslationOnceTwice(Math.Max(b.numAccidentPeeAtNight,b.numAccidentPooAtNight)));
            toiletMsg = Strings.InsertVariable(toiletMsg, "$ACCIDENT_TIMES_TOTAL", GetNumericTranslationTimes(Math.Max(b.numAccidentPeeAtNight,b.numAccidentPooAtNight)));
            Write(toiletMsg, b);
        }

        public static void AnimatePeeAttempt(Body b, bool inUnderwear)
        {

            if (b.IsFishing()) return;
            if (inUnderwear)
                Say(Animations.GetData().Wet_Attempt, b);
            else if (b.InToilet(inUnderwear))
                Say(Animations.GetData().Pee_Toilet_Attempt, b);
            else
                Say(Animations.GetData().Pee_Attempt, b);
        }

        public static void AnimatePoopAttempt(Body b, bool inUnderwear)
        {

            if (b.IsFishing()) return;
            if (inUnderwear)
                Animations.Say(Animations.GetData().Mess_Attempt, b);
            else if (b.InToilet(inUnderwear))
                Animations.Say(Animations.GetData().Poop_Toilet_Attempt, b);
            else
                Animations.Say(Animations.GetData().Poop_Attempt, b);
        }

        public static void AnimateStillSoiled(Body b)
        {
            string baseString = Strings.RandString(Animations.GetData().Still_Soiled);

            baseString = baseString.Replace("$UNDERWEAR_LONGDESC$", Strings.DescribeUnderwear(b.underwear, b.underwear.displayName, true));
            Animations.Say(baseString, b);
        }
        public static void AnimateShouldChange(Body b)
        {
            string baseString = Strings.RandString(Animations.GetData().Should_Change);

            baseString = baseString.Replace("$UNDERWEAR_LONGDESC$", Strings.DescribeUnderwear(b.underwear, b.underwear.displayName, true));
            Animations.Say(baseString, b);
        }
        public static void AnimateWashingUnderwear(Container c)
        {
            if (c.MarkedForDestroy())
            {
                Animations.Write(Strings.InsertVariables(Animations.GetData().Overwashed_Underwear[0], (Body)null, c), (Body)null);
                Game1.player.reduceActiveItemByOne();
            }
            else
            {
                Animations.Write(Strings.InsertVariables(Strings.RandString(Animations.GetData().Washing_Underwear), (Body)null, c), (Body)null);
            }
        }

        public static void CheckPants(Body b)
        {
            StardewValley.Objects.Clothing pants = b.GetPantsStardew();
            b.pants.displayName = pants.displayName;
            b.pants.description = pants.displayName;
            b.pants.plural = true;
            Animations.Say(Animations.GetData().LookPants[0] + " " + Strings.DescribeUnderwear(b.pants, null) + ".", b);
        }
        public static bool WarnCurrentThreshold(float[] thresholds, string[][] messages, float currentValue, bool greaterAs = false)
        {
            for (int index = thresholds.Length - 1; index >= 0; index--)
            {
                if (thresholds[index] > currentValue && !greaterAs) continue;
                if (thresholds[index] <= currentValue && greaterAs) continue;

                Animations.Warn(messages[index]);
                return true;
            }
            return false;
        }
        public static void CheckContinence(Body b)
        {
            if (b.IsAllowedResource(IncidentType.PEE))
            {
                WarnCurrentThreshold(Body.BLADDER_CONTINENCE_THRESHOLDS, Body.BLADDER_CONTINENCE_MESSAGES, b.bladderContinence);
            }

            if (b.IsAllowedResource(IncidentType.POOP))
            {
                WarnCurrentThreshold(Body.BOWEL_CONTINENCE_THRESHOLDS, Body.BOWEL_CONTINENCE_MESSAGES, b.bowelContinence);
            }
        }
        public static void CheckPottyFeeling(Body b)
        {
            float newFullness = b.bladderFullness / b.bladderCapacity;

            bool pottyMessage = false;
            if (newFullness > (1 - b.bladderContinence))
            {
                pottyMessage = WarnCurrentThreshold(Body.WETTING_THRESHOLDS, Body.WETTING_MESSAGES, newFullness, false); // index offset is 1, because we want the current, not next message
            }
            if (!pottyMessage) Warn(Body.WETTING_MESSAGE_GREEN);

            newFullness = b.bowelFullness / b.bowelCapacity;
            pottyMessage = false;
            if (newFullness > (1 - b.bowelContinence))
            {
                pottyMessage = WarnCurrentThreshold(Body.MESSING_THRESHOLDS, Body.MESSING_MESSAGES, newFullness, false); // index offset is 1, because we want the current, not next message
            }
            if (!pottyMessage) Warn(Body.MESSING_MESSAGE_GREEN);
        }
        public static void CheckUnderwear(Body b)
        {
            Say(Animations.GetData().PeekWaistband[0] + " " + Strings.DescribeUnderwear(b.underwear, (string)null) + ".", b);
        }

        public static void DrawUnderwearIcon(Container c, int x, int y)
        {
            Microsoft.Xna.Framework.Color defaultColor = Microsoft.Xna.Framework.Color.White;

            Texture2D underwearSprites = Animations.GetSprites();
            Microsoft.Xna.Framework.Rectangle srcBoxCurrent = Animations.UnderwearRectangle(c, FullnessType.None, LARGE_SPRITE_DIM);

            Microsoft.Xna.Framework.Rectangle destBoxCurrent = new Microsoft.Xna.Framework.Rectangle(x, y, DIAPER_HUD_DIM, DIAPER_HUD_DIM);

            ((SpriteBatch)Game1.spriteBatch).Draw(underwearSprites, destBoxCurrent, srcBoxCurrent, defaultColor);
            if (Game1.getMouseX() >= x && Game1.getMouseX() <= x + DIAPER_HUD_DIM && Game1.getMouseY() >= y && Game1.getMouseY() <= y + DIAPER_HUD_DIM)
            {
                string source = Strings.DescribeUnderwear(c, (string)null);
                string str = source.First<char>().ToString().ToUpper() + source.Substring(1);
                int num = Game1.tileSize * 6 + Game1.tileSize / 6;
                IClickableMenu.drawHoverText((SpriteBatch)Game1.spriteBatch, Game1.parseText(str, (SpriteFont)Game1.tinyFont, num), (SpriteFont)Game1.smallFont, 0, 0, -1, (string)null, -1, (string[])null, (Item)null, 0, null, -1, -1, -1, 1f, (CraftingRecipe)null);
            }
        }

        private static void EndDrinking(Farmer who)
        {
            player.completelyStopAnimatingOrDoingAction();
            player.forceCanMove();
        }

        public static void HandlePasserby()
        {
            if (Utility.isThereAFarmerOrCharacterWithinDistance(player.Tile, 3, (GameLocation)Game1.currentLocation) is not NPC npc || NPC_LIST.Contains(npc.Name))
                return;
            npc.CurrentDialogue.Push(new Dialogue(npc, null, "Oh wow, your diaper is all wet!"));
        }
        private static List<NPC> NearbyVillager(Body b, bool mess, bool inUnderwear, bool overflow, bool attempt = false)
        {
            int radius = 3;

            //If we are messing, increase the radius of noticeability (stinky)
            if (mess)
            {
                radius *= 2;
            }

            //If we pulled down our pants, quadruple the radius (not contained and visible!)
            //Double loss since you're just going in front of people (how uncouth)
            if (!inUnderwear)
            {
                radius *= 4;
            }

            //Double noticeability is we had a blow-out/leak (people can see)
            if (overflow)
                radius *= 2;

            var list = Utility.GetNpcsWithinDistance(((Character)Animations.player).Tile, radius, (GameLocation)Game1.currentLocation);

            var newList = new List<NPC>();
            foreach(var npcEntry in list)
            {
                if (!NPC_LIST.Contains(npcEntry.Name))
                {
                    newList.Add(npcEntry);
                }
            }
            return newList;
        }
        public static Dictionary<string,int> FriendshipLossOnAccident(Body b, bool mess, bool inUnderwear, bool overflow, bool attempt = false)
        {
            var list = new Dictionary<string,int>();
            
            //Get NPC in radius
            var nearby = NearbyVillager(b, mess, inUnderwear, overflow, attempt);
           
            foreach (var npc in nearby)
            {
                int finalLossValue = FriendshipLossOnAccident(npc, mess,inUnderwear,overflow,attempt);
                list[npc.getName().ToLower()] = finalLossValue;
            }
            return list;
        }
        public static int FriendshipLossOnAccident(NPC npc, bool mess, bool inUnderwear, bool overflow, bool attempt = false)
        {
            int heartLevelForNpc = player.getFriendshipHeartLevelForNPC(npc.getName().ToLower());
            int baseFriendshipLoss = (mess ? Regression.config.FriendshipPenaltyBowelMultiplier : Regression.config.FriendshipPenaltyBladderMultiplier);

            //Does this leave the possibility of friendship gain if we have enough hearts already? Maybe because they find the vulnerability endearing?
            float friendshipLoss = -1 + (heartLevelForNpc - 2) / 2;

            var modifiers = Animations.GetData().Villager_Friendship_Modifier;
            var modifierKey = modifierForIncident(npc, mess, inUnderwear, overflow, attempt);

            float finalModifier = 1.0f;
            foreach (string key2 in npcTypeList(npc))
            {
                float floatOut;
                Dictionary<string, float> dictionary;
                if (modifiers.TryGetValue(key2, out dictionary) && dictionary.TryGetValue(modifierKey, out floatOut))
                {
                    finalModifier = floatOut;
                }
            }

            return (int)Math.Floor((baseFriendshipLoss / 100 / 5) * friendshipLoss * finalModifier);
        }
        public static List<string> npcTypeList(NPC npc)
        {
            List<string> npcType = new List<string>();
            
            if (npc is Horse || npc is Pet)
            {
                npcType.Add("animal");
            }
            else
            {
                switch (npc.Age)
                {
                    case 0:
                        npcType.Add("adult");
                        break;
                    case 1:
                        npcType.Add("teen");
                        break;
                    case 2:
                        npcType.Add("kid");
                        break;
                }
                npcType.Add(npc.getName().ToLower());
            }
            return npcType;
        }
        public static string modifierForState(NPC npc)
        {
            return "dirty";
        }
        public static string modifierForIncident(NPC npc, bool mess, bool inUnderwear, bool overflow, bool attempt = false)
        {
            if (!inUnderwear)
            {
                //If we weren't wearing underwear, we tried on the ground.
                //But did we succeed or just try to go?
                return attempt ? "ground_attempt" : "ground";
            }
            else
            {
                //Otherwise, we are soiling ourselves
                return "soiled";
            }
        }
        public static string responseKeyAdditionForState(NPC npc)
        {
            return "";
        }
        public static string responseKeyAdditionForIncident(NPC npc, bool inUnderwear, int friendshipLoss)
        {
            // we only have special responses for soiling our underwear
            if (inUnderwear)
            {
                //Animals only have a "nice" response
                if (npcTypeList(npc).Contains("animal"))
                {
                    return "nice";
                }
                else
                {
                    int heartLevelForNpc = player.getFriendshipHeartLevelForNPC(npc.getName());
                    //If we have a really high relationship with the NPC, they're very nice about our accident
                    if (heartLevelForNpc >= 8)
                    {
                        return "verynice";
                    }
                    //Otherwise they'll be mean or nice depending on how much friendship we're losing
                    if (friendshipLoss < 0) return "nice";
                    return "mean";
                }
            }
            return "";
        }
        public static bool HandleVillager(Body b, bool mess, bool inUnderwear, bool overflow, bool attempt = false)
        {
            bool someoneNoticed = false;

            //Get NPC in radius
            var nearby = NearbyVillager(b, mess, inUnderwear, overflow, attempt);

            foreach (var npc in nearby)
            {
                someoneNoticed = true;

                //Make a list based on who saw us.
                var npcType = npcTypeList(npc);

                string npcName = "";
                if (npc is Horse || npc is Pet)
                {
                    npcName += string.Format("{0}: ", npc.Name);
                }

                //What did we do? Use to figure out the response.
                string modifierKey = modifierForIncident(npc,mess,inUnderwear,overflow,attempt);
                int finalLossValue = FriendshipLossOnAccident(npc,mess,inUnderwear,overflow,attempt);

                //If we're in debug mode, notify how the relationship was effected
                if (Regression.config.Debug && finalLossValue < 0)
                    Animations.Say(string.Format("{0} ({1}) changed friendship from {2} by {3}.", npc.Name, npc.Age, player.getFriendshipHeartLevelForNPC(npc.getName()), finalLossValue), (Body)null);


                //If we didn't lose any friendship, or we disabled friendship penalties (by adjusting it to 0), then don't adjust the value
                if (finalLossValue < 0)
                    player.changeFriendship(finalLossValue, npc);

                string responseKey = modifierKey + "_" + responseKeyAdditionForIncident(npc,inUnderwear, finalLossValue);
                List<string> stringList3 = new List<string>();
                foreach (string key2 in npcType)
                {
                    Dictionary<string, string[]> dictionary;
                    string[] strArray;
                    if (Animations.GetData().Villager_Reactions.TryGetValue(key2, out dictionary) && dictionary.TryGetValue(responseKey, out strArray))
                    {
                        stringList3 = new List<string>(); // We could remove this line again, but the general texts are more meant as fallback, they often don't fit well if custom texts are defined
                        stringList3.AddRange((IEnumerable<string>)strArray);
                    }
                        
                }

                //Construct and say Statement
                string npcStatement = npcName + Strings.InsertVariables(Strings.ReplaceAndOr(Strings.RandString(stringList3.ToArray()), !mess, mess, "&"), b, (Container)null);
                npc.setNewDialogue(new Dialogue(npc, null, npcStatement), true, true);
                Game1.drawDialogue(npc);
            }

            return someoneNoticed;
        }

        public static Texture2D LoadTexture(string file)
        {
            return Regression.help.ModContent.Load<Texture2D>(Path.Combine("Assets", file));
        }

        public static void Say(string msg, Body b = null)
        {
            Game1.showGlobalMessage(Strings.InsertVariables(msg, b, (Container)null));
        }

        public static void Say(string[] msgs, Body b = null)
        {
            Animations.Say(Strings.RandString(msgs), b);
        }

        public static Microsoft.Xna.Framework.Rectangle UnderwearRectangle(Container c, FullnessType type = FullnessType.None, int height = LARGE_SPRITE_DIM)
        {
            if (c.spriteIndex == -1)
                throw new Exception("Invalid sprite index.");
            int num = 0;
            //Using switch statement instead of ternary operator for better readability and to add another type.
            if (type != FullnessType.None)
            {
                switch (type)
                {
                    case (FullnessType.Clear):
                        {
                            num = 0;
                            break;
                        }
                    case (FullnessType.Messy):
                        {
                            num = LARGE_SPRITE_DIM;
                            break;
                        }
                    case (FullnessType.Wet):
                        {
                            num = LARGE_SPRITE_DIM * 2;
                            break;
                        }
                    case (FullnessType.WetMessy):
                        {
                            num = LARGE_SPRITE_DIM * 3;
                            break;
                        }
                    case (FullnessType.Drying):
                        {
                            num = LARGE_SPRITE_DIM * 4;
                            break;
                        }
                    default:
                        {
                            num = 0;
                            break;
                        }
                }
            }
            else
            {
                if (!c.drying)
                {
                    if ((double)c.messiness <= .0f)
                    {
                        if ((double)c.wetness <= .0f)
                            num = 0;
                        else
                            num = LARGE_SPRITE_DIM;
                    }
                    else
                    {
                        if ((double)c.wetness <= .0f)
                            num = LARGE_SPRITE_DIM * 2;
                        else
                            num = LARGE_SPRITE_DIM * 3;
                    }
                }
                else
                {
                    num = LARGE_SPRITE_DIM * 4;
                }
            }
            return new Microsoft.Xna.Framework.Rectangle(c.spriteIndex * LARGE_SPRITE_DIM, num + (LARGE_SPRITE_DIM - height), LARGE_SPRITE_DIM, height);
        }

        public static void Warn(string msg, Body b = null)
        {
            Game1.addHUDMessage(new HUDMessage(Strings.InsertVariables(msg, b, (Container)null), 2));
        }

        public static void Warn(string[] msgs, Body b = null)
        {
            Animations.Warn(Strings.RandString(msgs), b);
        }

        public static void Write(string msg, Body b = null, int delay = 0)
        {
            DelayedAction.showDialogueAfterDelay(Strings.InsertVariables(msg, b, (Container)null), delay);
        }

        public static void Write(string[] msgs, Body b = null, int delay = 0)
        {
            Animations.Write(Strings.RandString(msgs), b, delay);
        }
    }
}
