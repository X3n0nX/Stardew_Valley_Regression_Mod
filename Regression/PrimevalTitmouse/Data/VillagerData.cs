using PrimevalTitmouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimevalTitmouse.Data
{
    public class VillagerData
    {
        public Dictionary<string, Dictionary<string, float>> Villager_Friendship_Modifier;
        public Dictionary<string, Dictionary<string, string[]>> Villager_Reactions;
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Villager_Underwear_Options;
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string[]>>>> Villager_Potty_Dialogs;
        public Dictionary<string, NpcChangingOptions> Villager_Changing_Options;
        public Dictionary<string, NpcPottyOptions> Villager_Potty_Options;
        public Dictionary<string, Dictionary<int, string>> Villager_Changeing_Dialoges;
        public Dictionary<string, Dictionary<int, string>> Villager_Gift_Dialoges;
    }
}
