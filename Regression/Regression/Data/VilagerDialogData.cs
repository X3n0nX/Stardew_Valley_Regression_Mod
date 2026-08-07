using System.Collections.Generic;

namespace RegressionMod
{
    public class VilagerDialogData
    {
        public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string[]>>>> Villager_Potty_Dialogs;
        public Dictionary<string, Dictionary<string, string[]>> Villager_Reactions;
        public Dictionary<string, Dictionary<string, Dictionary<int, string>>> Villager_Changeing_Dialoges;
        public Dictionary<string, Dictionary<int, string>> Villager_Gift_Dialoges;
    }
}
