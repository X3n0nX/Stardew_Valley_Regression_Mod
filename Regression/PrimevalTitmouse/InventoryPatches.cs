using HarmonyLib;
using System.Xml;
using StardewValley.Inventories;
using StardewValley;
using System.Collections.Generic;

namespace PrimevalTitmouse
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.WriteXml))]
    public static class Inventory_WriteXml_Patch
    {
        static bool Prefix(Inventory __instance, XmlWriter writer)
        {
            // Use Harmony's AccessTools or reflection to get the Items field/prop
            var itemsField = AccessTools.Field(__instance.GetType(), "Items");
            var items = (IList<Item>)itemsField.GetValue(__instance);

            // Replace your Underwear items with a known type before serialization
            var replacements = PrimevalTitmouse.Regression.replaceItems(items);

            // Now it's save to call the original methode
            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.ReadXml))]
    public static class Inventory_ReadXml_Patch
    {
        static void Postfix(object __instance, XmlReader reader)
        {
            var itemsField = AccessTools.Field(__instance.GetType(), "Items");
            var items = (IList<Item>)itemsField.GetValue(__instance);

            // Original ReadXml is done, now restore items
            PrimevalTitmouse.Regression.restoreItems(items);
        }
    }
}