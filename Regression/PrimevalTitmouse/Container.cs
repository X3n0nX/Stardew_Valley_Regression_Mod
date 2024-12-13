using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Serialization;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Mods;

namespace PrimevalTitmouse
{
    public class Container
    {
        
        public static readonly string modDataAdd = "Container";

        private string modDataBaseKey; // comes from the parent
        private ModDataDictionary modDataDictionary; // also comes from the parent

        private void save(string varName, string val)
        {
            if (modDataDictionary == null) return;
            modDataDictionary[BuildKeyFor(varName)] = val;
        }
        private void save(string varName, int val)
        {
            if (modDataDictionary == null) return;
            modDataDictionary[BuildKeyFor(varName)] = val.ToString();
        }
        private void save(string varName, float val)
        {
            if (modDataDictionary == null) return;
            modDataDictionary[BuildKeyFor(varName)] = val.ToString();
        }
        private void save(string varName, bool val)
        {
            if (modDataDictionary == null) return;
            modDataDictionary[BuildKeyFor(varName)] = val.ToString();
        }
        private void save(string varName, Date val)
        {
            if (modDataDictionary == null) return;
            modDataDictionary[BuildKeyFor(varName)] = serializeDryingDate(val);
        }

        private string LoadString(string varName, string defaultVal)
        {
            if (!HasKeyFor(varName)) return defaultVal;
            return modDataDictionary[BuildKeyFor(varName)];
        }
        private int LoadInt(string varName, int defaultVal)
        {
            if (!HasKeyFor(varName)) return defaultVal;
            return int.Parse(modDataDictionary[BuildKeyFor(varName)]);
        }

        private float LoadFloat(string varName, float defaultVal)
        {
            if (!HasKeyFor(varName)) return defaultVal;
            return float.Parse(modDataDictionary[BuildKeyFor(varName)]);
        }
        private bool LoadBool(string varName, bool defaultVal)
        {
            if (!HasKeyFor(varName)) return defaultVal;
            return bool.Parse(modDataDictionary[BuildKeyFor(varName)]);
        }
        private Date LoadDate(string varName, Date defaultVal)
        {
            if (!HasKeyFor(varName)) return defaultVal;
            return parseDryingDate(modDataDictionary[BuildKeyFor(varName)]);
        }
        private string BuildKeyFor(string varName)
        {
            return BuildKeyFor(varName, modDataBaseKey);
        }
        public static string BuildKeyFor(string varName, string modDataBaseKey)
        {
            return $"{modDataBaseKey}/{modDataAdd}/{varName}";
        }
        private bool HasKeyFor(string varName)
        {
            return modDataDictionary != null && modDataDictionary.ContainsKey(BuildKeyFor(varName));
        }


        // Here starts the block that contains all values we have to save
        
        private string _name = "";
        public string name
        {
            get => LoadString("name", _name);
            set
            {
                save("name", value);
                _name = value;
            }
        }

        private string _displayName = "";
        public string displayName
        {
            get => LoadString("displayName", _displayName == "" ? name : _displayName);
            set
            {
                save("displayName", value);
                _displayName = value;
            }
        }
        private int _durability;
        public int durability
        {
            get => LoadInt("durability", _durability);
            set
            {
                save("durability", value);
                _durability = value;
            }
        }
        private float _messiness;
        public float messiness
        {
            get => LoadFloat("messiness", _messiness);
            set
            {
                save("messiness", value);
                _messiness = value;
            }
        }
        private float _wetness;
        public float wetness
        {
            get => LoadFloat("wetness", _wetness);
            set
            {
                save("wetness", value);
                _wetness = value;
            }
        }
        public string description { get => _innerContainer != null && _description == null ? _innerContainer.description : _description; set => _description = value; }

        public bool stackable
        {
            get
            {
                return !used && !drying && (InnerContainer == null || durability == InnerContainer.durability);
            }
        }
        public struct Date
        {
            public int time;
            public int day;
            public int season;
            public int year;
        }
        private string _timeWhenDoneDrying = "";
        public Date timeWhenDoneDrying
        {
            get => parseDryingDate(LoadString("timeWhenDoneDrying", _timeWhenDoneDrying));
            set
            {
                save("timeWhenDoneDrying", value);
                _timeWhenDoneDrying = serializeDryingDate(value);
            }
        }
        public static Date parseDryingDate(string date)
        {
            var dateObj = new Date();
            if (date == "")
            {
                dateObj.time = 0;
                dateObj.day = 0;
                dateObj.season = 0;
                dateObj.year = 0;
                return dateObj;
            }

            string[] splitted = date.Split("-");
            dateObj.time = int.Parse(splitted[0]);
            dateObj.day = int.Parse(splitted[1]);
            dateObj.season = int.Parse(splitted[2]);
            dateObj.year = int.Parse(splitted[3]);

            return dateObj;
        }
        public static string serializeDryingDate(Date timeWhenDoneDrying)
        {
            return string.Format("{0}-{1}-{2}-{3}", timeWhenDoneDrying.time, timeWhenDoneDrying.day, timeWhenDoneDrying.season, timeWhenDoneDrying.year);
        }
        public bool drying {
            get
            {
                if (timeWhenDoneDrying.Equals(new Date())) return false;
                Date currentDate;
                currentDate.time = Game1.timeOfDay;
                currentDate.day = Game1.dayOfMonth;
                currentDate.season = Utility.getSeasonNumber(Game1.currentSeason);
                currentDate.year = Game1.year;

                bool yearEq = currentDate.year == timeWhenDoneDrying.year;
                bool seasonEq = currentDate.season == timeWhenDoneDrying.season;
                bool dayEq = currentDate.day == timeWhenDoneDrying.day;
                bool timeEq = currentDate.time == timeWhenDoneDrying.time;
                bool yearGt = currentDate.year > timeWhenDoneDrying.year;
                bool seasonGt = currentDate.season > timeWhenDoneDrying.season;
                bool dayGt = currentDate.day > timeWhenDoneDrying.day;
                bool timeGt = currentDate.time > timeWhenDoneDrying.time;
                if ((yearGt) || (yearEq && seasonGt) || (yearEq && seasonEq && dayGt) || (yearEq && seasonEq && dayEq && (timeGt || timeEq)))
                {
                    timeWhenDoneDrying = new Date();
                    wetness = 0;
                    messiness = 0;
                    return false;
                }
                return true;
            }
        }


        // Here starts the block that contains all values we can take from type
        private string _description;
        private float _absorbency;
        private float _containment;
        private bool _plural;
        private int _price;
        private int _spriteIndex;
        private bool _washable;
        private int _dryingTime;
        private bool _removable;

        
        public float absorbency { get => InnerContainer != null ? InnerContainer.absorbency : _absorbency; set => _absorbency = value; }
        public float containment { get => InnerContainer != null ? InnerContainer.containment : _containment; set => _containment = value; }
        public bool plural { get => InnerContainer != null ? InnerContainer.plural : _plural; set => _plural = value; }
        public int price { get => InnerContainer != null ? InnerContainer.price : _price; set => _price = value; }
        public int spriteIndex { get => InnerContainer != null ? InnerContainer.spriteIndex : _spriteIndex; set => _spriteIndex = value; }
        public bool washable { get => InnerContainer != null ? InnerContainer.washable : _washable; set => _washable = value; }
        public int dryingTime { get => InnerContainer != null ? InnerContainer.dryingTime : _dryingTime; set => _dryingTime = value; }
        public bool removable { get => InnerContainer != null ? InnerContainer.removable : _removable; set => _removable = value; }
        
        public bool used { get => wetness > 0 || messiness > 0; }


        private Container _innerContainer = null;
        public Container InnerContainer
        {
            get
            {
                if(_innerContainer == null && modDataDictionary != null)
                {
                    _innerContainer = GetTypeDefault(name);
                }

                return _innerContainer;
            }
        }
        //This class describes anything that we could wet/mess in. Usually underwear, but it could also be something like the bed.
        //These functions are pretty self-explanatory
        public Container()
        {
            wetness = 0.0f;
            messiness = 0.0f;
        }
        // in this case we have a parent. We keep track and update this parent for network sync purposes
        public Container(Underwear underwear, string fallbackType)
        {
            modDataBaseKey = Underwear.modDataKey;
            modDataDictionary = underwear.modData;
            if (name == "")
            {
                Regression.monitor.Log(modDataBaseKey + " had no name, and there is no fallback possible for underwear");
            }
        }
        public Container(Body body,string subtype, string fallbackType)
        {
            modDataBaseKey = Body.modDataPrefix + "/" + subtype;
            modDataDictionary = Game1.player.modData;
            if(name == "")
            {
                Regression.monitor.Log($"{modDataBaseKey} for body had no name, so fallback {fallbackType} was used");
                ResetToDefault(fallbackType);
            }
        }
        public Container(NPC npc, string subtype, string fallbackType)
        {
            modDataBaseKey = "NPC/" + subtype;
            modDataDictionary = npc.modData;
            if (name == "")
            {
                Regression.monitor.Log($"{modDataBaseKey} for {npc.Name} had no name, so fallback {fallbackType} was used");
                ResetToDefault(fallbackType);
            }
        }
        /*public Container(string type)
        {
            Container c;

            if (!Regression.t.Underwear_Options.TryGetValue(type, out c))
                throw new Exception(string.Format("Invalid underwear choice: {0}", type));

            Initialize(c, c.wetness, c.messiness, c.durability);
        }

        public Container(Container c)
        {
            Initialize(c, c.wetness, c.messiness, c.durability);
        }


        public Container(string type, float wetness, float messiness, int durability)
        {
            this.wetness = 0.0f;
            this.messiness = 0.0f;
            Initialize(type, wetness, messiness, durability);
        }*/

        public string GetPrefix()
        {
            if (plural) return "a pair of";
            return "a";
        }

        public void Wash(int dryingTimeOverwrite = -1)
        {
            if (washable)
            {
                if(durability != -1 && durability != 0) //infinite durability if -1
                {
                    durability--;
                }
                var done = new Date();
                done.time = Game1.timeOfDay + (dryingTimeOverwrite == -1 ? dryingTime : dryingTimeOverwrite);
                done.day = Game1.dayOfMonth;
                done.season = Utility.getSeasonNumber(Game1.currentSeason);
                done.year = Game1.year;

                if(done.time >= 2400)
                {
                    done.time -= 2400;
                    done.day += 1;
                }
                if(done.day > 28)
                {
                    done.day -= 28;
                    done.season += 1;
                }
                if(done.season > 4)
                {
                    done.season -= 4;
                    done.year += 1;
                }
                timeWhenDoneDrying = done;
            }
        }

        public bool MarkedForDestroy()
        {
            return (durability == 0)&&washable;
        }


        public float AddPee(float amount)
        {
            wetness += amount;
            float difference = wetness - absorbency;
            if (difference > 0)
            {
                wetness = absorbency;
                return difference;
            }
            return 0.0f;
        }

        public float AddPoop(float amount)
        {
            messiness += amount;
            float difference = messiness - containment;
            if (difference > 0)
            {
                messiness = containment;
                return difference;
            }
            return 0.0f;
        }
        public float GetCapacity(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return absorbency;
                case IncidentType.POOP:
                    return containment;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public float GetUsed(IncidentType type)
        {
            switch (type)
            {
                case IncidentType.PEE:
                    return wetness;
                case IncidentType.POOP:
                    return messiness;
                default:
                    throw new Exception("Not implemented: type " + type.ToString());
            }
        }
        public void ResetToDefault(Container c, float wetness = -100, float messiness = -100, int durability = -100)
        {
            this._innerContainer = null;
            this.name = c.name;
            this.timeWhenDoneDrying = c.timeWhenDoneDrying;

            this.wetness = c.wetness;
            this.messiness = c.messiness;
            this.durability = c.durability;
            this.displayName = c.displayName;
            this.description = c.description;
            if (wetness != -100) this.wetness = wetness;
            if (messiness != -100) this.messiness = messiness;
            if (durability != -100) this.durability = durability;
        }

        public void ResetToDefault(string type, float wetness = -100, float messiness = -100, int durability = -100)
        {
            ResetToDefault(GetTypeDefault(type), wetness, messiness, durability);
        }
        public static Container GetTypeDefault(string type)
        {
            Container c;

            if (!Regression.t.Underwear_Options.TryGetValue(type, out c))
                throw new Exception(string.Format("Invalid underwear choice: {0}", type));

            return c;
        }
    }
}
