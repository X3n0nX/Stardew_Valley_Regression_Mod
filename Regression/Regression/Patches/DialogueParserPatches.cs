using HarmonyLib;
using StardewValley;

namespace RegressionMod
{
    [HarmonyPatch(typeof(Dialogue), "parseDialogueString")]
    public static class Dialogue_ParseDialogueString_Patch
    {
        static bool Prefix(Dialogue __instance, ref string masterString, string translationKey)
        {
            
            masterString = Strings.InsertVariables(masterString, __instance.speaker);
            masterString = Strings.InsertVariables(masterString, Regression.body);
            // Now it's save to call the original methode
            return true;
        }
    }
}