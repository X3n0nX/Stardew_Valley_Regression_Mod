using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace RegressionMod
{
    public class UnderwearHelper
    {
        private static Farmer who => Game1.player;

        private static TypesData typesData = Regression.typesData;

        public static List<string> ValidUnderwearTypes()
        {
            List<string> list = typesData.Type_Underwears.Keys.ToList();
            return list;
        }

        public static Underwear GetUnderwearFromInventory(string underwearName, bool clean = true)
        {
            if (who != null)
            {
                if (who.ActiveObject != null)
                {
                    if (who.ActiveObject is Underwear)
                    {
                        var container = ((Underwear)who.ActiveObject).container;
                        if (container.name.ToLower() == underwearName.ToLower())
                        {
                            if (!clean || !container.used)
                            {
                                return (Underwear)who.ActiveObject;
                            }

                        }
                    }
                    return null;
                }
                foreach (var item in who.Items)
                {
                    if (item is Underwear)
                    {
                        var underwearFromInventory = item as Underwear;
                        if (underwearFromInventory.container.name.ToLower() == underwearName.ToLower())
                        {
                            if (!clean || !underwearFromInventory.container.used)
                            {
                                return underwearFromInventory;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static bool HasUnderwear(string underwearName, bool clean = true)
        {
            return GetUnderwearFromInventory(underwearName, clean) != null;
        }

    }
}
