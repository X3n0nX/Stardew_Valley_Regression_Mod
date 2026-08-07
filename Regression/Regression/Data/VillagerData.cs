using System.Collections.Generic;

namespace RegressionMod
{
    public class VillagerData
    {
        public Dictionary<string, Dictionary<string, float>> Villager_Friendship_Modifier;
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Villager_Underwear_Options;
        public Dictionary<string, NpcChangingOptions> Villager_Changing_Options;
        public Dictionary<string, NpcPottyOptions> Villager_Potty_Options;                
    }
}
