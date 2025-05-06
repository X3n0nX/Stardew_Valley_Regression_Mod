using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PrimevalTitmouse;
using StardewValley;
using StardewValley.GameData.Characters;

namespace PrimevalTitmouse
{
    public  class NpcChangingOptions
    {
        public NpcChangingOptions(string npcName)
        {
            NpcChangingOptions options;

            if (string.IsNullOrEmpty(npcName) || !Regression.t.Villager_Changing_Options.TryGetValue(npcName, out options))
            {
                Regression.monitor.Log($"Npc Name not found in Villager_Changing_Options: NPC Name {npcName}");
            }
            else
            {
                this._get_changed = options.get_changed;
                this._give_change = options.give_change;
                this._give_dirty_change = options.give_dirty_change;
                this._change_automaticly = options.change_automaticly;
            }
        }

        public bool hasOptions
        {
            get
            {
                if(hasOptionGetChange) return true;
                if (hasOptionGiveChange) return true;
                if(hasOptionGiveDirtyChange) return true;
                if (hasOptionChangeAutomaticly) return true;
                return false;
            }
        }

        public bool hasOptionGetChange
        {
            get
            {
                if (get_changed != null && get_changed.hasOptions) return true;
                return false;
            }
        }

        public bool hasOptionGiveChange
        {
            get
            {
                if (give_change != null && give_change.hasOptions) return true;
                return false;
            }
        }

        public bool hasOptionGiveDirtyChange
        {
            get
            {
                if (give_dirty_change != null && give_dirty_change.hasOptions) return true;
                return false;
            }
        }

        public bool hasOptionChangeAutomaticly
        {
            get
            {
                if (change_automaticly != null && change_automaticly.hasOptions) return true;
                return false;
            }
        }

        // Parameters for player can give change to npc
        #nullable enable
        private SingleNpcChangingOption? _get_changed;
        
        public SingleNpcChangingOption? get_changed

        {
            get => _get_changed;
            set => _get_changed = value;
        }

        // Parameters for getting changed by npc
        private SingleNpcChangingOption? _give_change;

        public SingleNpcChangingOption? give_change

        {
            get => _give_change;
            set => _give_change = value;
        }

        // Parameters for getting changed by npc if players underwear is messy
        private SingleNpcChangingOption? _give_dirty_change;

        public SingleNpcChangingOption? give_dirty_change

        {
            get => _give_dirty_change;
            set => _give_dirty_change = value;
        }

        // Parameters for npc getting changed automaticly
        private NpcChangeAutomaticly? _change_automaticly;

        public NpcChangeAutomaticly? change_automaticly
        {
            get => _change_automaticly;
            set => _change_automaticly = value;
        }
        #nullable disable
    }

    public class SingleNpcChangingOption
    {
        public bool hasOptions
        {
            get
            {
                if(hasOptionMinHeartLevel || hasOptionMinHeartLevelOther || hasOptionLocations || hasOptionQuestionnAnswered) return true;
                else return false;
            }
        }

        public bool hasOptionMinHeartLevel
        {
            get => _min_heart_level >= 0;
        }

        public bool hasOptionMinHeartLevelOther
        {
            get => (_min_heart_level_other != null && _min_heart_level_other.Count > 0);
        }

        public bool hasOptionLocations
        {
            get => (_locations != null && _locations.Count > 0);
        }

        public bool hasOptionQuestionnAnswered
        {
            get => (_questions_answered != null && _questions_answered.Count > 0);
        }

        #nullable enable
        private int? _min_heart_level;

        public int? min_heart_level
        {
            get => _min_heart_level;
            set => _min_heart_level = value;
        }

        
        private Dictionary<string, int>? _min_heart_level_other;

        public Dictionary<string, int>? min_heart_level_other
        {
            get =>_min_heart_level_other;
            set => _min_heart_level_other = value;
        }

        private Dictionary<string, int>? _locations;

        public Dictionary<string, int>? locations
        {
            get =>_locations;
            set => _locations = value;
        }

        private Dictionary<string, int>? _questions_answered;

        public Dictionary<string, int>? questions_answered
        {
            get => _questions_answered;
            set => _questions_answered = value;
        }
        #nullable disable
    }

    public class NpcChangeAutomaticly
    {
        public bool hasOptions
        {
            get
            {
                if (hasOptionHomeNpc || hasOptionHomeUnderwear || hasOptionOutsideNpc || hasOptionOutsideUnderwear || hasOptionOutsideLocations) return true;
                return false;
            }
        }

        public bool hasOptionHomeNpc
        {
            get
            {
                if (AtHomeNpcs != null && AtHomeNpcs.Length > 0) return true;
                return false;
            }
        }

        public bool hasOptionHomeUnderwear
        {
            get
            {
                if (AtHomeUnderwear != null && AtHomeUnderwear.Length > 0) return true;
                return false;
            }
        }

        public bool hasOptionOutsideNpc
        {
            get
            {
                if (OutsideNpcs != null && OutsideNpcs.Length > 0) return true;
                return false;
            }
        }

        public bool hasOptionOutsideUnderwear
        {
            get
            {
                if (OutsideUnderwear != null && OutsideUnderwear.Length > 0) return true;
                return false;
            }
        }

        public bool hasOptionOutsideLocations
        {
            get
            {
                if (OutsideLocation != null && OutsideLocation.Length > 0) return true;
                return false;
            }
        }

        #nullable enable
        private Dictionary<string, string[]>? _at_home;

        public Dictionary<string, string[]>? at_home
        {
            get
            {
                if (_at_home == null) _at_home = new Dictionary<string, string[]>();
                return _at_home;
            }
            set => _at_home = value;
        }
        #nullable disable

        public string[] AtHomeNpcs
        {
            get
            {
                string[] npcs = null;
                if (_at_home != null)
                {
                    _at_home.TryGetValue("npc_in_range", out npcs);
                }
                return npcs;
            }
        }

        public string[] AtHomeUnderwear
        {
            get
            {
                string[] underwear = null;
                if (_at_home != null)
                {
                    _at_home.TryGetValue("wearing_underwear", out underwear);
                }
                return underwear;
            }
        }
        #nullable enable
        private Dictionary<string, string[]>? _outside;

        public Dictionary<string, string[]>? outside
        {
            get
            {
                if (_outside == null) _outside = new Dictionary<string, string[]>();
                return _outside;
            }
            set => _outside = value;
        }
        #nullable disable

        public string[] OutsideNpcs
        {
            get
            {
                string[] npcs = null;
                if (_outside != null)
                {
                    _outside.TryGetValue("npc_in_range", out npcs);
                }
                return npcs;
            }
        }

        public string[] OutsideUnderwear
        {
            get
            {
                string[] underwear = null;

                if (_outside != null)
                {
                    _outside.TryGetValue("wearing_underwear", out underwear);
                }
                return underwear;
            }
        }

        public string[] OutsideLocation
        {
            get
            {
                string[] locations = null;

                if (_outside != null)
                {
                    _outside.TryGetValue("at_location", out locations);
                }
                return locations;
            }
        }

        #nullable enable
        private Dictionary<string, int>? _changeing_chance;

        public Dictionary<string, int>? changeing_chance
        {
            get
            {
                if(_changeing_chance == null) _changeing_chance = new Dictionary<string, int>();
                return _changeing_chance;
            }
            set => _changeing_chance = value;
        }
        #nullable disable

        public float ChangeingChanceBase
        {
            get
            {
                if (_changeing_chance == null || _changeing_chance.Count == 0)
                {
                    return 1.0f;
                }
                else
                {
                    int chance;

                    if (_changeing_chance.TryGetValue("base_chance", out chance)) return (Convert.ToSingle(chance) / 100f);
                    return 1.0f;
                }
            }
        }

        public float ChangeingChancePoopy
        {
            get
            {
                int chance;

                if (_changeing_chance.TryGetValue("poopy", out chance)) return (Convert.ToSingle(chance) / 100f);
                return 0.0f;
            }
        }

        public float ChangeingChanceWetHalfCapacity
        {
            get
            {
                int chance;

                if (_changeing_chance.TryGetValue("wet_half_capacity", out chance)) return (Convert.ToSingle(chance) / 100f);
                return 0.0f;
            }
        }

        public float ChangeingChanceMessyHalfCapacity
        {
            get
            {
                int chance;

                if (_changeing_chance.TryGetValue("messy_half_capacity", out chance)) return (Convert.ToSingle(chance) / 100f);
                return 0.0f;
            }
        }
    }
}
