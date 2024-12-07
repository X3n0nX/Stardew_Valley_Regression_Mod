using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Locations;
using StardewValley.Minigames;
using StardewValley.Mods;
using StardewValley.Tools;
using System;
using System.Reflection;
using System.Text.Json.Serialization;
using static PrimevalTitmouse.Container;

namespace PrimevalTitmouse
{
    //<TODO> A lot of bladder and bowel stuff is processed similarly. Consider refactor with arrays and Function pointers.
    public class Body
    {
        // Floximo: In general you need to go to the toilet (in reality) about every 3-4 hours on average, lets go with 4 and assume 500mL and 16 waking hours a day (and we assume you healthy, not waking up at night) its around 2500mL of pee a day.
        // For Pee the gameplay aligns pretty good, considering the values this is based on are wildly out of propotions. But i guess if only 20% of the water you drink is peed out, and the rest sweat, this might be possible.
        // Poop is usually once a day on average. The values set balanced out to you needing to poop every 4 hours at least. That is a lot if you don't want to only pay potty simulator. Gets worse with bad potty training.
        // Balancing poop with the given values "foodToBowelConversion" and "requiredCaloriesPerDay" is challanging, as the bowel capacity is linear to this 2 values. 

        //Lets think of Food in Calories, and water in mL
        //For a day Laborer (like a farmer) that should be ~3500 Cal, and 14000 mL - NOTE: (Floximo) you mean 1400 mL probably. Up to 3000 mL is considered healthy
        //Of course this is dependent on amount of work, but let's go one step at a time
        private static readonly float requiredCaloriesPerDay = 3000f; // I adjusted it from 3500 to 3000, because the farmer still eats half the farm every day
        private static readonly float requiredWaterPerDay = 8000f; //8oz glasses: every 20min for 8 hours (5455mL) + every 40 min for 8 hour (2727mL) Floximo: That is rough. Possible in extrem heat but rough to drink.

        //Average # of Pees per day is ~3
        private static readonly float maxBladderCapacity = Regression.config.MaxBladderCapacity; //about 600mL => changed to 800mL as player will start with lower bladder by default
        private static readonly float minBladderContinence = 0.3f; // Also describes capacity as changes are linear
        private static readonly float waterToBladderConversion = 0.2f;//Only ~1/4 (0.225f) water becomes pee, rest is sweat etc. => changed to 0.2f (1/5) for game balance reasons (too intrusive)

        //Average # of poops per day varies wildly. Let's say about 1.5 per day.
        private static readonly float foodToBowelConversion = 0.5f; // lowered from 0.67f to 0.5f to make the numbers smaller (too much poop) and bowel capacity / 1.5f (1000f in total)
        private static readonly float maxBowelCapacity = (requiredCaloriesPerDay*foodToBowelConversion) / 1.5f / 1000f * Regression.config.MaxBowelCapacity; // The last 2 numbers usually end up as / 1200 * 1200 (cancle eachother out) to make configuration easier to understand 
        private static readonly float minBowelContinence = 0.3f; // Also describes capacity as changes are linear

        //Setup Thresholds and messages
        private static readonly float trainingThreshold = 0.5f; // we set a threshold that allowes for potty training, so that should also be the warning level, for the player to understand whats going on
        private static readonly float lastWarningThreshold = 0.8f; // with a minimum continence of 0.3, 0.8 is still warned about, as it would warn up to 0.7, while 0.69 is out of range (means only 1 warning)
        public static readonly float[] WETTING_THRESHOLDS = { trainingThreshold + 0.05f, 0.69f, lastWarningThreshold }; 
        public static readonly string[][] WETTING_MESSAGES = { Regression.t.Bladder_Yellow, Regression.t.Bladder_Orange, Regression.t.Bladder_Red};
        public static readonly string[] WETTING_MESSAGE_GREEN = Regression.t.Bladder_Green;
        public static readonly float[] MESSING_THRESHOLDS = { trainingThreshold + 0.05f, 0.69f, lastWarningThreshold };
        public static readonly string[][] MESSING_MESSAGES = { Regression.t.Bowels_Yellow, Regression.t.Bowels_Orange, Regression.t.Bowels_Red};
        public static readonly string[] MESSING_MESSAGE_GREEN = Regression.t.Bowels_Green;
        public static readonly float[] BLADDER_CONTINENCE_THRESHOLDS = { minBladderContinence, 0.5f, 0.65f, 0.8f, 1.0f };
        public static readonly string[][] BLADDER_CONTINENCE_MESSAGES = { Regression.t.Bladder_Continence_Min, Regression.t.Bladder_Continence_Red, Regression.t.Bladder_Continence_Orange, Regression.t.Bladder_Continence_Yellow, Regression.t.Bladder_Continence_Green};
        public static readonly float[] BOWEL_CONTINENCE_THRESHOLDS = { minBowelContinence, 0.5f, 0.65f, 0.8f, 1.0f };
        public static readonly string[][] BOWEL_CONTINENCE_MESSAGES = { Regression.t.Bowel_Continence_Min, Regression.t.Bowel_Continence_Red, Regression.t.Bowel_Continence_Orange, Regression.t.Bowel_Continence_Yellow, Regression.t.Bowel_Continence_Green };
        private static readonly float[] HUNGER_THRESHOLDS = { 0.0f, 0.25f };
        private static readonly string[][] HUNGER_MESSAGES = { Regression.t.Food_None, Regression.t.Food_Low };
        private static readonly float[] THIRST_THRESHOLDS = { 0.0f, 0.25f };
        private static readonly string[][] THIRST_MESSAGES = { Regression.t.Water_None, Regression.t.Water_Low };
        private static readonly string MESSY_DEBUFF = "Regression.Messy";
        private static readonly string WET_DEBUFF = "Regression.Wet";
        private static readonly int wakeUpPenalty = 4;

        public static readonly string modDataPrefix = "PrimevalTitmouse/Body";
        private void save(string name, int val)
        {
            Game1.player.modData[BuildKeyFor(name)] = val.ToString();
        }
        private void save(string name, float val)
        {
            Game1.player.modData[BuildKeyFor(name)] = val.ToString();
        }
        private void save(string name, bool val)
        {
            Game1.player.modData[BuildKeyFor(name)] = val.ToString();
        }
        private static int LoadInt(string name, int defaultVal)
        {
            if (!Game1.player.modData.ContainsKey(BuildKeyFor(name))) return defaultVal;
            return int.Parse(Game1.player.modData[BuildKeyFor(name)]);
        }
        private static float LoadFloat(string name, float defaultVal)
        {
            if (!Game1.player.modData.ContainsKey(BuildKeyFor(name))) return defaultVal;
            return float.Parse(Game1.player.modData[BuildKeyFor(name)]);
        }
        private static bool LoadBool(string name, bool defaultVal)
        {
            if (!Game1.player.modData.ContainsKey(BuildKeyFor(name))) return defaultVal;
            return bool.Parse(Game1.player.modData[BuildKeyFor(name)]);
        }
        private static string BuildKeyFor(string varName)
        {
            return $"{modDataPrefix}/{varName}";
        }
        private bool HasKeyFor(string varName)
        {
            return Game1.player.modData != null && Game1.player.modData.ContainsKey(BuildKeyFor(varName));
        }
        //Things that describe an individual
        public int bedtime
        {
            get => LoadInt("bedtime", 0);
            set => save("bedtime", value);
        }

        public float bladderContinence
        {
            get => LoadFloat("bladderContinence", Math.Min(1f, Math.Max(minBowelContinence, Regression.config.StartBladderContinence / 100f)));
            set => save("bladderContinence", value);
        }

        public float bladderFullness
        {
            get => LoadFloat("bladderFullness", 0f);
            set => save("bladderFullness", value);
        }

        public float bowelContinence
        {
            get => LoadFloat("bowelContinence", Math.Min(1f, Math.Max(minBowelContinence, Regression.config.StartBowelContinence / 100f)));
            set => save("bowelContinence", value);
        }

        public float bowelFullness
        {
            get => LoadFloat("bowelFullness", 0f);
            set => save("bowelFullness", value);
        }

        public float hunger
        {
            get => LoadFloat("hunger", 0f);
            set => save("hunger", value);
        }

        public float thirst
        {
            get => LoadFloat("thirst", 0f);
            set => save("thirst", value);
        }

        public bool isSleeping
        {
            get => LoadBool("isSleeping", false);
            set => save("isSleeping", value);
        }

        public int numPottyPooAtNight
        {
            get => LoadInt("numPottyPooAtNight", 0);
            set => save("numPottyPooAtNight", value);
        }

        public int numPottyPeeAtNight
        {
            get => LoadInt("numPottyPeeAtNight", 0);
            set => save("numPottyPeeAtNight", value);
        }

        public int numAccidentPooAtNight
        {
            get => LoadInt("numAccidentPooAtNight", 0);
            set => save("numAccidentPooAtNight", value);
        }

        public int numAccidentPeeAtNight
        {
            get => LoadInt("numAccidentPeeAtNight", 0);
            set => save("numAccidentPeeAtNight", value);
        }
        private float lastStamina = 0;

        public float bladderCapacity
        { 
            get
            {
                //Decrease our maximum capacity (bladder shrinks as we become incontinent)
                return bladderContinence * maxBladderCapacity;

                //Ceiling at base value and floor at 25% base value
                //return Math.Max(bladderCapacity, maxBladderCapacity * minBladderContinence);
            }
        }

        public float bowelCapacity
        {
            get
            {
                //Decrease our maximum capacity (bowel shrinks as we become incontinent)
                return bowelContinence * maxBowelCapacity;

                //Ceiling at base value and floor at 25% base value
                //return Math.Max(bowelCapacity, minBowelCapacity * minBowelContinence);
            }
        }
        private Container _bed;
        [JsonIgnore]
        public Container bed {
            get
            {
                if(_bed == null)
                {
                    _bed = new Container(this, "bed", "bed");
                }
                return _bed;
            }
        }
        private Container _pants;
        [JsonIgnore]
        public Container pants
        {
            get
            {
                if (_pants == null)
                {
                    _pants = new Container(this, "pants", "blue jeans");
                }
                return _pants;
            }
        }

        private Container _underwear;
        [JsonIgnore]
        public Container underwear
        {
            get
            {
                if (_underwear == null)
                {
                    _underwear = new Container(this, "underwear", "dinosaur undies");
                }
                return _underwear;
            }
        }

        public Body()
        {

        }

        public bool IsAllowedResource(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return Regression.config.Wetting;
                case IncidentType.POOP:
                    return Regression.config.Messing;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }

        public float GetBladderTrainingThreshold()
        {
            return bladderCapacity * trainingThreshold;
         }

        public float GetBowelTrainingThreshold()
        {

            return bowelCapacity * trainingThreshold;
        }
        public float GetTrainingThreshold(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return GetBladderTrainingThreshold();
                case IncidentType.POOP:
                    return GetBowelTrainingThreshold();
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetAttemptThreshold(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return GetBladderAttemptThreshold();
                case IncidentType.POOP:
                    return GetBowelAttemptThreshold();
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetBladderAttemptThreshold()
        {
            return bladderCapacity * 0.15f;
        }

        public float GetBowelAttemptThreshold()
        {
                return bowelCapacity * 0.15f;
        }
        public float GetFullness(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return bladderFullness;
                case IncidentType.POOP:
                    return bowelFullness;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public void SetFullness(IncidentType type, float value)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    bladderFullness = Math.Max(0f, value);
                    return;
                case IncidentType.POOP:
                    bowelFullness = Math.Max(0f, value);
                    return;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetMaxCapacity(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return maxBladderCapacity;
                case IncidentType.POOP:
                    return maxBowelCapacity;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetCapacity(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return bladderCapacity;
                case IncidentType.POOP:
                    return bowelCapacity;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetContinence(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return bladderContinence;
                case IncidentType.POOP:
                    return bowelContinence;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        // This function sets, it doesn't substract, be aware to use a calculation that is aware of that, like GetContinence(type) + change
        public void SetContinence(IncidentType type, float value)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    bladderContinence = Math.Max(minBladderContinence,Math.Min(1f, value));
                    return;
                case IncidentType.POOP:
                    bowelContinence = Math.Max(minBowelContinence, Math.Min(1f, value));
                    return;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetHungerPercent()
        {
            return (requiredCaloriesPerDay - hunger) / requiredCaloriesPerDay;
        }

        public float GetThirstPercent()
        {
            return (requiredWaterPerDay - thirst) / requiredWaterPerDay;
        }

        public float GetBowelPercent()
        {
            return bowelFullness / bowelCapacity;
        }

        public float GetBladderPercent()
        {
            return bladderFullness / bladderCapacity;
        }

        public void AddResource(IncidentType type, float amount)
        {
            //If Resource (poop/pee) is disabled, don't do anything
            if (!IsAllowedResource(type))
                return;

            var fullness = GetFullness(type);
            var capacity = GetCapacity(type);
            //Increment the current amount
            //We allow bladder to go over-full, to simulate the possibility of multiple night wettings
            //This is determined by the amount of water you have in your system when you go to bed
            float oldFullnessPercent = fullness / capacity;

            SetFullness(type, fullness + amount);

            fullness = GetFullness(type);
            //Did we go over? Then have an accident.
            if (fullness >= capacity)
            {
                if (!isSleeping)
                {
                    if (MinorAccident(type)) return;
                }
                Accident(type, voluntary: false, inUnderwear: true);
            }
            else
            {
                //If we have no room left, or randomly based on our current continence level warn about how badly we need to pee
                /*if ((newFullness <= 0.0 ? 1.0 : bladderContinence / (4f * newFullness)) > Regression.rnd.NextDouble())
                {
                    Warn(1-oldFullness, 1-newFullness, WETTING_THRESHOLDS, WETTING_MESSAGES, false);
                }*/
                float newFullnessPercent = fullness / capacity;
                // No randomness in this. We get warned later and it is more urgent immediatly, giving less time, but reliable.
                if (newFullnessPercent > (1 - GetContinence(type)))
                {
                    // old and new is inverted, because we expect a rising, not falling trend
                    Warn(newFullnessPercent, oldFullnessPercent, type == IncidentType.POOP ? MESSING_THRESHOLDS : WETTING_THRESHOLDS, type == IncidentType.POOP ? MESSING_MESSAGES : WETTING_MESSAGES, false);
                }
            }
        }

        //Change current Food value and handle warning messages
        //Notice that we do things here even if Hunger and Thirst are disabled
        //This is due to Food and Water's effect on Wetting/Messing
        public void AddFood(float amount, float conversionRatio = 1f)
        {
            //How full are we?
            float oldPercent = (requiredCaloriesPerDay - hunger) / requiredCaloriesPerDay;
            hunger -= amount;
            float newPercent = (requiredCaloriesPerDay - hunger) / requiredCaloriesPerDay;

            //Convert food lost into poo at half rate
            if (amount < 0 && hunger < requiredCaloriesPerDay)
                AddResource(IncidentType.POOP, amount * -1f * conversionRatio * foodToBowelConversion);

            //If we go over full, add additional to bowels at half rate
            if (hunger < 0)
            {
                AddResource(IncidentType.POOP, hunger * -0.5f * conversionRatio * foodToBowelConversion);
                hunger = 0f;
                newPercent =(requiredCaloriesPerDay - hunger) / requiredCaloriesPerDay;
            }

            if (Regression.config.NoHungerAndThirst)
            {
                hunger = 0; //Reset if disabled
                return;
            }

            //If we're starving and not eating, take a stamina hit
            if (hunger > requiredCaloriesPerDay && amount < 0)
            {
                //Take percentage off stamina equal to percentage above max hunger
                Game1.player.stamina += newPercent * Game1.player.MaxStamina;
                hunger = requiredCaloriesPerDay;
                newPercent = 1;
            }

            Warn(oldPercent, newPercent, HUNGER_THRESHOLDS, HUNGER_MESSAGES, false);
        }

        public void AddWater(float amount, float conversionRatio = 1f)
        {
            //How full are we?
            float oldPercent = (requiredWaterPerDay - thirst) / requiredWaterPerDay;
            thirst -= amount;
            float newPercent = (requiredWaterPerDay - thirst) / requiredWaterPerDay;

            //Convert water lost into pee at half rate
            if (amount < 0 && thirst < requiredWaterPerDay)
                AddResource(IncidentType.PEE, amount * -1f * conversionRatio * waterToBladderConversion);

            //Also if we go over full, add additional to Bladder at half rate
            if (thirst < 0)
            {
                AddResource(IncidentType.PEE, (thirst * -0.5f * conversionRatio * waterToBladderConversion));
                thirst = 0f;
                newPercent = (requiredWaterPerDay - thirst) / requiredWaterPerDay;
            }

            if (Regression.config.NoHungerAndThirst)
            {
                thirst = 0; //Reset if disabled
                return;
            }

            //If we're starving and not eating, take a stamina hit
            if (thirst > requiredWaterPerDay && amount < 0)
            {
                //Take percentage off health equal to percentage above max thirst
                float lostHealth = newPercent * (float)Game1.player.maxHealth;
                Game1.player.health = Game1.player.health + (int)lostHealth;
                thirst = requiredWaterPerDay;
                newPercent = (requiredWaterPerDay - thirst) / requiredWaterPerDay;
            }

            Warn(oldPercent, newPercent, THIRST_THRESHOLDS, THIRST_MESSAGES, false);
        }

        //Note that a NEGATIVE percent is a LOSS of continence
        public void ChangeContinence(IncidentType type, float percent = 0.01f)
        {
            //OLD: If we're increasing, no need to warn. (maybe we should tell people that they're regaining?)
            // We now only return if something has changed. Otherwise we can handle changes now in both directions (new)
            if (percent == 0f)
                return;
            float previousContinence = GetContinence(type);
            SetContinence(type, previousContinence + percent);

            //Change of bladder capacity is no longer nessesary. Handled by getter.

            if (Regression.config.Debug)
                Animations.Say(string.Format("{0} continence changed by {1} to {2}, {0} capacity now {3}", type == IncidentType.POOP ? "Bowel": "Bladder", previousContinence, GetContinence(type), GetCapacity(type)), (Body)null);

            //Warn that we may be losing control
            if(type == IncidentType.POOP)
            {
                Warn(previousContinence, bowelContinence, BOWEL_CONTINENCE_THRESHOLDS, BOWEL_CONTINENCE_MESSAGES, true);
            }
            else
            {
                Warn(previousContinence, bladderContinence, BLADDER_CONTINENCE_THRESHOLDS, BLADDER_CONTINENCE_MESSAGES, true);
            }
        }

        //Put on underwear and clean pants
        private void ChangeUnderwear(Container container)
        {
            this.underwear.ResetToDefault(container);
            Animations.Say(Regression.t.Change, this);
        }
        public void ChangeUnderwear(Underwear uw)
        {
            ChangeUnderwear(uw.container);
        }

        public StardewValley.Objects.Clothing GetPantsStardew()
        {
            var farmer = Animations.GetWho();
            StardewValley.Objects.Clothing pants = (StardewValley.Objects.Clothing)farmer.pantsItem.Value;
            if (pants != null) pants.LoadData(); // YES, this is nessesary to load the data of the pants before accessing them... if there are pants (watch out for null)

            return pants;
        }
        public void ResetPants()
        {
            var myPants = GetPantsStardew();
            Container newPants = GetTypeDefault(myPants == null ? "legs" : "blue jeans");
            
            pants.ResetToDefault(newPants);

            if (myPants != null)
            {
                pants.displayName = myPants.displayName.ToLower();
                pants.description = myPants.description.ToLower();
            }
            CleanPants();
        }

        //If we put on our pants, remove wet/messy debuffs
        public void CleanPants()
        {
            RemoveBuff(WET_DEBUFF);
            RemoveBuff(MESSY_DEBUFF);
        }
        public bool HasWetOrMessyDebuff()
        {
            return Game1.player.hasBuff(MESSY_DEBUFF) || Game1.player.hasBuff(WET_DEBUFF);
        }

        //Debug Function, Add a bit of everything
        public void DecreaseEverything()
        {
            AddWater(requiredWaterPerDay * -0.1f, 0f);
            AddFood(requiredCaloriesPerDay * -0.1f, 0f);
            AddResource(IncidentType.PEE, maxBladderCapacity * -0.1f);
            AddResource(IncidentType.POOP, maxBladderCapacity * -0.1f);
        }

        public void IncreaseEverything()
        {
            AddWater(requiredWaterPerDay * 0.1f, 0f);
            AddFood(requiredCaloriesPerDay * 0.1f, 0f);
            AddResource(IncidentType.PEE, maxBladderCapacity * 0.1f);
            AddResource(IncidentType.POOP, maxBladderCapacity * 0.1f);
        }

        public void DrinkWateringCan()
        {
            Farmer player = Game1.player;
            WateringCan currentTool = (WateringCan)player.CurrentTool;
            // Half a watering can should be good in any case?!
            if (currentTool.WaterLeft * 200 >= thirst)
            {
                this.AddWater(thirst);
                currentTool.WaterLeft -= (int)(thirst * 200f);
                Animations.AnimateDrinking(false);
            }
            else if (currentTool.WaterLeft > 0)
            {
                this.AddWater(currentTool.WaterLeft * 200);
                currentTool.WaterLeft = 0;
                Animations.AnimateDrinking(false);
            }
            else
            {
                player.doEmote(4);
                Game1.showRedMessage("Out of water");
            }
        }

        public void DrinkWaterSource()
        {
            this.AddWater(thirst);
            Animations.AnimateDrinking(true);
        }

        public bool InToilet(bool inUnderwear)
        {
            return !inUnderwear && (Game1.currentLocation is FarmHouse || Game1.currentLocation is JojaMart || Game1.currentLocation is Club || Game1.currentLocation is MovieTheater || Game1.currentLocation is IslandFarmHouse || Game1.currentLocation.Name == "Saloon" || Game1.currentLocation.Name == "Hospital" || Game1.currentLocation.Name == "BathHouse_MensLocker" || Game1.currentLocation.Name == "BathHouse_WomensLocker");
        }
        public bool InPlaceWithPants()
        {
            return Game1.currentLocation is FarmHouse;
        }
        public bool NeedsChangies(IncidentType type)
        {
            // If the underwear was already used and the capacity remaining is smaller than the size of an accident of this type
            return underwear.GetUsed(type) > 0 && underwear.GetCapacity(type) - underwear.GetUsed(type) < GetCapacity(type);
        }

        public bool IsTryingToHoldIt(IncidentType type, float vsAmount)
        {
            float capacity = underwear.GetCapacity(type);
            float used = underwear.GetUsed(type);

            if (!underwear.removable) return false; // If we don't have pants or training pants, there is no point
            if(used > 300) return false; // If the underwear is already heavily used, we stop trying
            if(used > GetCapacity(type) / 3) return false; // If its more than 1/3 our bladder/bowel size already, we stop trying
            if ((vsAmount + used) > capacity) return false; // If the underwear would be more than full, there is no point
            
            return true;
        }
        // Minor incident on very full bladder/bowel. Balances the players chances to get to the potty, even if that means minor incidents. Also obviously cute
        public bool MinorAccident(IncidentType type)
        {
            float capacity = GetCapacity(type);
            float fullness = GetFullness(type);
            float fullnessPercent = fullness / capacity;
            if (fullnessPercent < lastWarningThreshold) return false; // Just to make it easily readable that this is for after and inside the last warning threshold
            if (fullnessPercent > 1.1f) return false; // If its too much, leaking will not do
            
            // We can lose maximal the amount until the last warning threshold, because we would trigger that warning again. This only happens after the last warning anyway.
            float amount = Math.Min((fullnessPercent - lastWarningThreshold - 0.01f) * capacity, GetMaxCapacity(type) * 0.8f);
            if (!IsTryingToHoldIt(type, amount)) return false;

            if (type == IncidentType.POOP)
            {
                Animations.AnimateMessingMinor(this);
            }
            else
            {
                Animations.AnimateWettingMinor(this);
            }
            
            ChangeContinence(type, CalculateContinenceLossOrGain(type, false, true, amount / GetMaxCapacity(type)));
            AddAccidentFromFullness(type, amount);
            return true;
            //AddMess(amountToLose);
            //_ = this.pants.AddPoop(this.underwear.AddPoop(bowelFullness));
        }
        public void ChangeFullness(IncidentType type, float amount)
        {
            // We intentionally allow fullness over max 
            switch (type)
            {
                case IncidentType.PEE:
                    SetFullness(type, bladderFullness + amount);
                    break;
                case IncidentType.POOP:
                    SetFullness(type, bowelFullness + amount);
                    break;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public void AddAccidentFromFullness(IncidentType type, float amount)
        {
            ChangeFullness(type, -amount);
            AddAccident(type, amount);
        }
        public void AddAccident(IncidentType type, float amount)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    if (isSleeping)
                    {
                        _ = this.bed.AddPee(this.pants.AddPee(this.underwear.AddPee(amount)));
                    }
                    else
                    {
                        _ = this.pants.AddPee(this.underwear.AddPee(amount));
                    }
                    break;
                case IncidentType.POOP:
                    if (isSleeping)
                    {
                        _ = this.bed.AddPoop(this.pants.AddPoop(this.underwear.AddPoop(amount)));
                    }
                    else
                    {
                        _ = this.pants.AddPoop(this.underwear.AddPoop(amount));
                    }
                    break;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }

        }
        // Minor accidents (leaking) can happen before failure and is usually not causing someone else to notice
        public void AccidentMinor(IncidentType type)
        {
            if (isSleeping) return;


        }
        public void Accident(IncidentType type, bool voluntary = false, bool inUnderwear = true)
        {
            float attemptThreshold = GetAttemptThreshold(type);
            float capacity = GetCapacity(type);
            float fullness = GetFullness(type);
            float continence = GetContinence(type);
            float maxCapacity = GetMaxCapacity(type); // yes, this is the maximum capacity a bladder/bowel can have, used for changing continence

            //If we're sleeping check if we have an accident or get up to use the potty
            if (isSleeping)
            {
                //When we're sleeping, our fullness can exceed our capacity since we calculate for the whole night at once
                //Hehehe, this may be evil, but with a smaller bladder/bowel, you'll have to go multiple times a night
                //So roll the dice each time >:)
                //<TODO>: Give stamina penalty every time you get up to go potty. Since you disrupted sleep.
                int numIncidentAmount = (int)((fullness - attemptThreshold) / capacity);
                float additionalAmount = fullness - (numIncidentAmount * capacity);
                int numAccident = 0;
                int numPotty = 0;

                if (additionalAmount > 0)
                    numIncidentAmount++;

                for (int i = 0; i < numIncidentAmount; i++)
                {
                    //Randomly decide if we get up. Less likely if we have lower continence
                    bool lclVoluntary = voluntary || Regression.rnd.NextDouble() < (double)continence;
                    StartAccident(type, lclVoluntary && underwear.removable, true); //Always in underwear in bed
                    float amountToLose = (i != numIncidentAmount - 1) ? capacity : additionalAmount;
                    if (!lclVoluntary)
                    {
                        numAccident++;
                        ChangeContinence(type, CalculateContinenceLossOrGain(type, voluntary, inUnderwear, amountToLose / maxCapacity)); // maxCapacity is correct, for balancing reasons
                        //Any overage in the container, add to the pants. Ignore overage over that.
                        //When sleeping, the pants are actually the bed
                        AddAccidentFromFullness(type, amountToLose);
                    }
                    else
                    {
                        if (!underwear.removable) //Certain underwear can't be taken off to use the toilet (ie diapers)
                        {
                            numAccident++;
                            ChangeContinence(type, CalculateContinenceLossOrGain(type, voluntary, inUnderwear, amountToLose / maxCapacity)); // maxCapacity is correct, for balancing reasons
                            AddAccidentFromFullness(type, amountToLose);
                        }
                        else
                        {
                            numPotty++;
                            ChangeFullness(type, -amountToLose);
                        }
                    }
                }
                switch (type)
                {
                    case IncidentType.PEE:
                        numPottyPeeAtNight = numPotty;
                        numAccidentPeeAtNight = numAccident;
                        break;
                    case IncidentType.POOP:
                        numPottyPooAtNight = numPotty;
                        numAccidentPooAtNight = numAccident;
                        break;
                    default:
                        throw new Exception("Not implemented: type " + type.ToString());
                }

            }
            else if (inUnderwear)
            {
                StartAccident(type,voluntary, true);
                bool attemptOnly = fullness < attemptThreshold;
                if (!attemptOnly)
                {
                    ChangeContinence(type, CalculateContinenceLossOrGain(type, voluntary, inUnderwear, fullness / maxCapacity)); // yes this is correct, we do relative to max bladder to temper loss on near no control (and frequent accidents)
                    AddAccidentFromFullness(type, fullness);
                }
                FinalizeAccident(type, voluntary, true, attemptOnly); // Trying in your underwear is different, people might be acting differently
            }
            else
            {

                if (underwear.removable)
                {
                    bool attemptOnly = fullness < attemptThreshold;
                    StartAccident(type, voluntary, false);
                    if (!attemptOnly)
                    {
                        ChangeContinence(type, CalculateContinenceLossOrGain(type, voluntary, inUnderwear, fullness / capacity)); // yes this is correct, its capacity as the gain (for making it) should be relative to the bladder state, not max
                        ChangeFullness(type, -this.bowelFullness);
                    }
                    FinalizeAccident(type, voluntary, true, attemptOnly); // Trying in your underwear is different, people might be acting differently
                }

            }
        }
        public void Mess(bool voluntary = false, bool inUnderwear = true)
        {
            Accident(IncidentType.POOP,voluntary,inUnderwear);
        }
        public void StartAccident(IncidentType type, bool voluntary = false, bool inUnderwear = true)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    if (!Regression.config.Wetting) return;
                    break;
                case IncidentType.POOP:
                    if (!Regression.config.Messing) return;
                    break;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
            float attemptThreshold = GetAttemptThreshold(type);
            float fullness = GetFullness(type);

            if (fullness < attemptThreshold)
            {
                switch (type)
                {
                    case IncidentType.PEE:
                        Animations.AnimatePeeAttempt(this, inUnderwear);
                        break;
                    case IncidentType.POOP:
                        Animations.AnimatePoopAttempt(this, inUnderwear);
                        break;
                    default:
                        throw new Exception("Not implemented: type " + type.ToString());
                }
            }
            else
            {
                switch (type)
                {
                    case IncidentType.PEE:
                        Animations.AnimateWettingStart(this, voluntary, inUnderwear);
                        break;
                    case IncidentType.POOP:
                        Animations.AnimateMessingStart(this, voluntary, inUnderwear);
                        break;
                    default:
                        throw new Exception("Not implemented: type " + type.ToString());
                }
            }
            switch (type)
            {
                case IncidentType.PEE:
                    Animations.AnimateWettingEnd(this);
                    break;
                case IncidentType.POOP:
                    Animations.AnimateMessingEnd(this);
                    break;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        // percentLost: The amount of gain or loss is dependend on how much percent of the maxBladder or maxBowel we lost
        // This allowes for small gains or losses on small accidents and reduces edge cases 
        public float CalculateContinenceLossOrGain(IncidentType type, bool voluntary, bool inUnderwear, float percentLost)
        {
            float fullness = GetFullness(type);

            int rate;
            if (voluntary)
            {
                rate = type == IncidentType.POOP ? Regression.config.BowelGainContinenceRate : Regression.config.BladderGainContinenceRate;
            }
            else
            {
                rate = type == IncidentType.POOP ? Regression.config.BowelLossContinenceRate : Regression.config.BladderLossContinenceRate;
            }


            //If we have an accident (not voluntary), decrease continence
            //If we use the potty before we REALLY have to go (we go before we reach some threshold), increase continence
            //Otherwise, if it is voluntary but waited until we almost had an accident (fullness above some threshold) don't change anything
            if (!voluntary)
                return -0.01f * rate * percentLost * situationMultiplier(voluntary, inUnderwear);
            else if (fullness > GetTrainingThreshold(type))
                return 0.01f * rate * percentLost * situationMultiplier(voluntary, inUnderwear);
            else
            {
                return 0f;
            }
        }
        private void FinalizeAccident(IncidentType type, bool voluntary = false, bool inUnderwear = true, bool attemptOnly = false)
        {
            var dirt = type == IncidentType.POOP ? pants.messiness : pants.wetness;
            if (!this.InToilet(inUnderwear))
                _ = Animations.HandleVillager(this, type == IncidentType.POOP, inUnderwear, dirt > 0, false);
            if (attemptOnly || dirt <= 0.0 || !inUnderwear)
                return;
            HandleOverflow(type);
        }

        private void HandleOverflow(IncidentType type)
        {
            if (isSleeping)
                return;

            switch (type)
            {
                case IncidentType.PEE:
                    _HandlePeeOverflow();
                    break;
                case IncidentType.POOP:
                    _HandlePoopOverflow();
                    break;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        // Overflow functions are so different that it is less confusing to keep them as-is

        private void _HandlePeeOverflow()
        {
            Animations.Write(Regression.t.Pee_Overflow, this, Animations.peeAnimationTime);

            int defenseReduction = -Math.Max(Math.Min((int)(pants.wetness / pants.absorbency * 10.0), 10), 1);

            Buff buff = new Buff(id: WET_DEBUFF, displayName: "Wet", effects: new BuffEffects()
            {
                Defense = { defenseReduction }
            })
            {
                description = string.Format("{0} {1} Defense.", Strings.RandString(Regression.t.Debuff_Wet_Pants), defenseReduction),
                millisecondsDuration = 1080000,
                glow = pants.messiness != 0.0 ? Color.Brown : Color.Yellow
            };
            if (Game1.player.hasBuff(WET_DEBUFF))
                this.RemoveBuff(WET_DEBUFF);
            Game1.player.applyBuff(buff);
        }

        private void _HandlePoopOverflow()
        {
            Animations.Write(Regression.t.Poop_Overflow, this, Animations.poopAnimationTime);
            float howMessy = pants.messiness / pants.containment;
            int speedReduction = howMessy >= 0.5 ? (howMessy > 1.0 ? -3 : -2) : -1;
            Buff buff = new Buff(id: MESSY_DEBUFF, displayName: "Messy", effects: new BuffEffects() {
                Speed = { speedReduction }
            })
            {
                description = string.Format("{0} {1} Speed.", Strings.RandString(Regression.t.Debuff_Messy_Pants), (object)speedReduction),
                millisecondsDuration = 1080000,
                glow = Color.Brown
            };
            if (Game1.player.hasBuff(MESSY_DEBUFF))
                this.RemoveBuff(MESSY_DEBUFF);
            Game1.player.applyBuff(buff);
        }
        public float situationMultiplier(bool voluntary, bool inUnderwear)
        {
            float multiplier = 1.0f;
            // If we are sleeping, night time modifiers apply. As the player is no longer in charge we use other values for game balance reasons
            if (isSleeping)
            {
                multiplier = (voluntary ? (float)Regression.config.NighttimeGainMultiplier : (float)Regression.config.NighttimeLossMultiplier) / 100f;
            }
            // InToilet does its own checks, making sure it's a valid use of the toilet. Handled differently, usually giving a gain bonus. Loss doesn't mather as the function checks on valid attempts.
            else if (InToilet(inUnderwear))
            {
                multiplier = (float)Regression.config.ToiletGainMultiplier / 100f;
            }
            // If we voluntary pee/poop our pants, this adds situational modifiers. Usually negates possible gains or at least reduces them.
            else if(voluntary && inUnderwear)
            {
                multiplier = (float)Regression.config.GoingVoluntaryInUnderwearGainMultiplier / 100f;
            }
     
            return multiplier;
        }

        public void Wet(bool voluntary = false, bool inUnderwear = true)
        {
            Accident(IncidentType.PEE,voluntary,inUnderwear);
        }

        public void HandleMorning()
        {
            isSleeping = false;
            var dryingTime = 0;
            if (Regression.config.Easymode)
            {
                hunger = 0;
                thirst = 0;
            }
            else
            {

                Farmer player = Game1.player;
                if (bed.messiness > 0.0 || bed.wetness > 0.0)
                {
                    dryingTime = 1000;
                    player.stamina -= 20f;
                }
                else if (bed.wetness > 0.0)
                {
                    dryingTime = 600;
                    player.stamina -= 10f;
                }

                int timesUpAtNight = Math.Max(numPottyPeeAtNight, numPottyPooAtNight);
                player.stamina -= (timesUpAtNight * wakeUpPenalty);

            }

            Animations.AnimateMorning(this);
            bed.Wash(dryingTime);
            ResetPants();
        }

        public void HandleNight()
        {
            isSleeping = true;
            if (bedtime <= 0)
                return;

            //How long are we sleeping? (Minimum of 4 hours)
            const int timeInDay = 2400;
            const int wakeUpTime = timeInDay + 600;
            const float sleepRate = 3.0f; //Let's say body functions change @ 1/3 speed while sleeping. Arbitrary.
            int timeSlept = wakeUpTime - bedtime; //Bedtime will never exceed passout-time of 2:00AM (2600)
            HandleTime(timeSlept / 100.0f / sleepRate);
        }

        //If Stamina has decreased, Use up Food and water along with it
        public void HandleStamina()
        {
            float staminaDifference = (float)(Game1.player.stamina - this.lastStamina) / Game1.player.maxStamina.Value;
            if ((double)staminaDifference == 0.0)
                return;
            if (staminaDifference < 0.0)
            {
                this.AddFood( staminaDifference * requiredCaloriesPerDay * 0.25f);
                this.AddWater(staminaDifference * requiredWaterPerDay    * 0.10f);
            }
            this.lastStamina = Game1.player.stamina;
        }


        public void HandleTime(float hours)
        {
            this.HandleStamina();
            //normally divide 24hr/day, but this only happens while awake,
            //We have night set to go at 1/3 rate. Assume 8hr sleep. So we need to adjust by 8*(2/3)
            this.AddWater((float)(requiredWaterPerDay * (double)hours / -18.67));
            this.AddFood((float)(requiredCaloriesPerDay * (double)hours / -18.67));
        }

        public bool IsFishing()
        {
            FishingRod currentTool;
            return (currentTool = Game1.player.CurrentTool as FishingRod) != null && (currentTool.isCasting || currentTool.isTimingCast || (currentTool.isNibbling || currentTool.isReeling) || currentTool.castedButBobberStillInAir || currentTool.pullingOutOfWater);
        }

        public void RemoveBuff(string which)
        {
            Game1.player.buffs.Remove(which);
        }

        public static string[] GetMessagesByTreshold(float oldPercent, float newPercent, float[] thresholds, string[][] msgs)
        {
            if (newPercent > oldPercent)
            {
                for (int index = thresholds.Length-1; index > 0; --index)
                {
                    if ((double)oldPercent <= (double)thresholds[index] && (double)newPercent > (double)thresholds[index])
                    {
                        if(thresholds.Length-1 > index)
                        {
                            return msgs[index+1];
                        }
                        
                    }
                }

            }
            else
            {
                for (int index = 0; index < thresholds.Length; ++index)
                {
                    if ((double)oldPercent > (double)thresholds[index] && (double)newPercent <= (double)thresholds[index])
                    {
                        return msgs[index];
                    }
                }
            }
            
            return null;
        }
        public void Warn(float oldPercent, float newPercent, float[] thresholds, string[][] msgs, bool write = false)
        {
            if (isSleeping)
                return;

            var messages = GetMessagesByTreshold(oldPercent, newPercent, thresholds, msgs);
            if (messages == null) return;
            if (write)
            {
                Animations.Write(messages, this);
            }
            Animations.Warn(messages, this);
        }

        //<TODO> Expand Consumables to add food. But we'd need a lot more info. For now, treat all food the same.
        public void Consume(string itemName)
        {
            Consumable item;
            if(Animations.GetData().Consumables.TryGetValue(itemName, out item))
            {
                this.AddFood(item.calorieContent);
                this.AddWater(item.waterContent);
            } else
            {
                this.AddFood(400);
                this.AddWater(10);
            }
        }
    }
    public enum IncidentType
    {
        PEE,
        POOP
    }
}
