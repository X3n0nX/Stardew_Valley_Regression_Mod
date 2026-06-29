using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegressionMod
{
    internal class Toilets
    {       

        public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    foreach (var toiletVariant in HousingConstants.ToiletVariants)
                    {
                        // Data/Furniture format:
                        // name/type/tilesheet size/bounding box/rotations/price/placement/display/sprite index/texture
                        // Draw as 1x2 (full toilet), but use 1x1 collision/seat on the bowl tile.
                        data[toiletVariant.Id] = $"{toiletVariant.Id}/chair/1 2/1 1/1/100/-1/{toiletVariant.DisplayName}/{toiletVariant.SpriteIndex}/{HousingConstants.ToiletTextureAssetKey}";
                    }
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(HousingConstants.ToiletTextureAssetKey))
            {
                e.LoadFromModFile<Texture2D>("Assets/toilets.png", AssetLoadPriority.Exclusive);
            }
        }

        public static bool IsToiletFurnitureId(string id)
        {
            return HousingConstants.ToiletVariants.Any(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
