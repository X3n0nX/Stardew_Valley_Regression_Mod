using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using StardewValley.Locations;
using StardewModdingAPI;
using System.Linq;
using System.Threading;


namespace RegressionMod
{
    public class ShopManager
    {

        public static void AddItemsToShops(ShopMenu shopMenu)
        {
            if (Game1.currentLocation is SeedShop)
            {
                addUnderwearsToSeedShop(shopMenu);
                addToiletsToSeedShop(shopMenu);
            }
            else if (Game1.currentLocation is JojaMart)
            {
                addUnderwearsToJojaShop(shopMenu);
            }
            else if (Game1.currentLocation.Name == "Hospital")
            {
                addUnderwearsToHospital(shopMenu);
            }
        }

        #region Underwear

        private static void addUnderwearsToSeedShop(ShopMenu shopMenu)
        {
            List<string> allUnderwear = UnderwearHelper.ValidUnderwearTypes();

            foreach (string type in allUnderwear)
            {
                //The seed shop does not sell the Joja diaper
                if (type == UnderwearConstants.JojaDiaper) continue;
                //The seed shop does not sell every diaper and underwear as single items
                addUnderwearToShop(shopMenu, type);
            }
        }

        private static void addUnderwearsToJojaShop(ShopMenu shopMenu)
        {
            List<string> allUnderwear = UnderwearHelper.ValidUnderwearTypes();

            // Joja shop sells big brands now, "pampers" and "dry nites". You probably also find normal undies and simple cloth diapers there.
            // As such uses packages and has slightly lower prices (bulk)
            // This makes sense and mirrors the advantages and disadvantages of large chains in rual areas
            var type = UnderwearConstants.JojaDiaper;
            if (allUnderwear.Contains(type))
            {
                addUnderwearToShop(shopMenu, type, 10, 0.8f);
                addUnderwearToShop(shopMenu, type, 40, 0.7f);
            }
            type = UnderwearConstants.BabyPrintDiaper;
            if (allUnderwear.Contains(type))
            {
                addUnderwearToShop(shopMenu, type, 20, 0.8f);
                addUnderwearToShop(shopMenu, type, 60, 0.7f);
            }
            type = UnderwearConstants.LavenderPullups;
            if (allUnderwear.Contains(type))
            {
                addUnderwearToShop(shopMenu, type, 10, 0.8f);
                addUnderwearToShop(shopMenu, type, 40, 0.7f);
            }
            type = UnderwearConstants.BigKidUndies;
            if (allUnderwear.Contains(type))
            {
                addUnderwearToShop(shopMenu, type, 3, 0.8f);
            }
            type = UnderwearConstants.ClothDiaper;
            if (allUnderwear.Contains(type))
            {
                addUnderwearToShop(shopMenu, type, 5, 0.75f);
            }
            type = UnderwearConstants.LilSwabbies;
            if (allUnderwear.Contains(type))
            {
                addUnderwearToShop(shopMenu, type, 5, 0.8f);
            }
        }

        private static void addUnderwearsToHospital(ShopMenu shopMenu)
        {
            List<string> allUnderwear = UnderwearHelper.ValidUnderwearTypes();

            // The Hospital sells all kind of diapers with a little discount but no regular underware.
            foreach (string type in allUnderwear)
            {
                if (type.Contains("diaper") || 
                    type == UnderwearConstants.LavenderPullups || 
                    type == UnderwearConstants.TrainingPants || 
                    type == UnderwearConstants.LilSwabbies)
                {
                    addUnderwearToShop(shopMenu, type, 5, 0.9f);
                }
            }
        }

        private static void addUnderwearToShop(ShopMenu shop, string type, int amount = 1, float priceMultiplier = 1f)
        {
            var underwear = new Underwear(type, amount);
            shop.forSale.Add(underwear);
            shop.itemPriceAndStock.Add(underwear, new ItemStockInformation((int)Math.Ceiling((float)underwear.container.price * (float)amount * priceMultiplier), StardewValley.Menus.ShopMenu.infiniteStock));
        }

        #endregion

        #region Furniture

        private static void addToiletsToSeedShop(ShopMenu shopMenu)
        {
            foreach (var toiletVariant in HousingConstants.ToiletVariants)
            {
                try
                {
                    string qualifiedId = $"(F){toiletVariant.Id}";
                    bool alreadyListed = shopMenu.forSale.Any(item => item != null && item.QualifiedItemId == qualifiedId);
                    if (alreadyListed)
                        continue;

                    Item toilet = ItemRegistry.Create(qualifiedId);
                    if (toilet == null)
                    {
                        Regression.monitor.Log($"Toilet item '{qualifiedId}' could not be created (null result).", LogLevel.Warn);
                        continue;
                    }

                    shopMenu.forSale.Add(toilet);
                    shopMenu.itemPriceAndStock[toilet] = new ItemStockInformation(100, 99);
                }
                catch (Exception ex)
                {
                    Regression.monitor.Log($"Failed adding toilet '{toiletVariant.Id}' to Pierre: {ex.Message}", LogLevel.Warn);
                }
            }
        }

        #endregion
    }
}
