using HarmonyLib;
using System.Xml;
using StardewValley.Inventories;
using StardewValley;
using System.Collections.Generic;

namespace PrimevalTitmouse
{
    [HarmonyPatch(typeof(Farmer), "doneEating")]
    public static class Farmer_DoneEating_Patch
    {
        static bool Prefix(Farmer __instance)
        {
            if (__instance.mostRecentlyGrabbedItem == null || __instance.itemToEat == null || !__instance.IsLocalPlayer)
            {
                return true;
            }
            Regression.body.Consume(__instance.itemToEat);

            // We just want to do something on done eating, we let the original function proceede
            return true;
        }
    }
}