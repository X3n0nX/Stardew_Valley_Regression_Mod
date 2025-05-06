using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using StardewModdingAPI.Events;

namespace PrimevalTitmouse
{
    public class NpcPottyOptions
    {
        public NpcPottyOptions(string npcName)
        {
            NpcPottyOptions options;

            if (string.IsNullOrEmpty(npcName) || !Regression.t.Villager_Potty_Options.TryGetValue(npcName, out options))
            {
                Regression.monitor.Log($"Npc Name not found in Villager_Potty_Options: NPC Name {npcName}");
            }
            else
            {
                this.fullnessmax = options.fullnessmax;
                this.potty_chance = options.potty_chance;
                this.min_heart_level = options.min_heart_level;
                this.questions_answered = options.questions_answered;

            }
        }

        public bool hasOptions
        {
            get
            {
                if (fullnessmax != null && fullnessmax.Count > 0) return true;
                if (potty_chance != null && potty_chance.Count > 0) return true;
                if (min_heart_level != null) return true;
                if (questions_answered != null && questions_answered.Length > 0) return true;
                return false;
            }
        }

        public bool hasOptionMinHeartLevel
        {
            get => min_heart_level != null;
        }


        public bool hasOptionQuestionsAnswered
        {
            get => (questions_answered != null && questions_answered.Length > 0);
        }

        #nullable enable
        public Dictionary<string, int>? fullnessmax;
        #nullable disable

        // maximum amount of pee the npc´s bladder can hold
        private int _fullnessMaxPee;

        public int fullnessMaxPee 
        {
            get
            {
                int max;

                if(fullnessmax != null && fullnessmax.TryGetValue("pee", out max))
                {
                    _fullnessMaxPee = max;
                }
                else
                {
                    _fullnessMaxPee = 700;
                }

                return _fullnessMaxPee;
            }
        }

        // maximum amount of poop the npc´s bowel can hold
        private int _fullnessMaxPoop;

        public int fullnessMaxPoop
        {
            get
            {
                int max;

                if(fullnessmax != null && fullnessmax.TryGetValue("poop", out max))
                {
                    _fullnessMaxPoop = max;
                }
                else
                {
                    _fullnessMaxPoop = 1000;
                }   

                return _fullnessMaxPoop;
            }
        }

        #nullable enable
        public Dictionary<string, int>? potty_chance;
        #nullable disable

        // chance for npc, to pee in toilett
        private float _pottyChancePee;

        public float pottyChancePee
        {
            get
            {
                int chance;

                if (potty_chance != null && potty_chance.TryGetValue("pee", out chance))
                {
                    _pottyChancePee = (Convert.ToSingle(chance) / 100f);
                }
                else
                {
                    _pottyChancePee = 1.0f;
                }

                return _pottyChancePee;
            }
        }

        // chance for npc, to poop in toilett
        private float _pottyChancePoop;

        public float pottyChancePoop
        {
            get
            {
                int chance;

                if (potty_chance != null && potty_chance.TryGetValue("poop", out chance))
                {
                    _pottyChancePoop = (Convert.ToSingle(chance) / 100f);
                }
                else
                {
                    _pottyChancePoop = 1.0f;
                }

                return _pottyChancePoop;
            }
        }

        // minimum heart level to npc befor potty system start working. For example: You need to know a npc a little bit better, befor you notice, they using there underwear.
        
        #nullable enable
        public int? min_heart_level;               

        // the response id´s of questions, that had to be answered, befor potty system start working. For example: A Npc tells you there problems about potty using or incontinence.
       
        public string[]? questions_answered;
        #nullable disable

    }
}
