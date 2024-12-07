using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Xml.Serialization;
using static PrimevalTitmouse.Container;

namespace PrimevalTitmouse
{
    public class Underwear : StardewValley.Object
    {
        public Underwear()
        {
            //base.Actor();
        }

        public Underwear(string typeName, int count)
        {
            //base.Actor();
            this.Initialize(Container.GetTypeDefault(typeName), count);
        }
        public Underwear(Container baseType, int count)
        {
            //base.Actor();
            this.Initialize(baseType,count);
        }
        public static readonly string modDataKey = "PrimevalTitmouse/Underwear";
        
        public Color color {
            get {
                string colorStr;
                modData.TryGetValue("${modDataKey}/color", out colorStr);
                if (!string.IsNullOrEmpty(colorStr))
                {
                    uint colorValue = uint.Parse(colorStr);
                    return new Color(
                        ((colorValue >> 24) & 0xFF) / 255f,
                        ((colorValue >> 16) & 0xFF) / 255f,
                        ((colorValue >> 8) & 0xFF) / 255f,
                        (colorValue & 0xFF) / 255f
                    );
                }
                return new Color();
            }
            set {
                string colorStr = ((uint)(value.R * 255) << 24 |
                       (uint)(value.G * 255) << 16 |
                       (uint)(value.B * 255) << 8 |
                       (uint)(value.A * 255)).ToString();
                modData.Add("${modDataKey}/color", colorStr);
            }
        }
        private Container _container;
        public Container container {
            get {
                if (_container == null)
                {
                    _container = new Container(this, "dinosaur undies");
                }
                return _container;
            }
        }

        public override bool canBeDropped()
        {
            return false;
        }

        public override bool canBeGivenAsGift()
        {
            return false;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            int ratio = Animations.LARGE_SPRITE_DIM / Animations.SMALL_SPRITE_DIM;
            Vector2 offset = new(Game1.tileSize/2, Game1.tileSize/2); //Center of tile
            Vector2 origin = new(Animations.LARGE_SPRITE_DIM/2, Animations.LARGE_SPRITE_DIM/2); //Center of Sprite
            Rectangle source = Animations.UnderwearRectangle(container, FullnessType.None, Animations.LARGE_SPRITE_DIM);
            spriteBatch.Draw(Animations.sprites, location + offset, new Rectangle?(source), Color.White * transparency, 0.0f, origin, Game1.pixelZoom * scaleSize/ratio, SpriteEffects.None, layerDepth);
            if (drawStackNumber.Equals(StackDrawType.Hide) || maximumStackSize() <= 1 || (scaleSize <= 0.3 || Stack == int.MaxValue) || Stack <= 1)
                return;
            Utility.drawTinyDigits(Stack, spriteBatch, location + new Vector2(Game1.tileSize - Utility.getWidthOfTinyDigitString(Stack, 3f * scaleSize) + 3f * scaleSize, (float)(Game1.tileSize - 18.0 * scaleSize + 2.0)), 3f * scaleSize, 1f, Color.White);
        }

        public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {
            Rectangle rectangle = Animations.UnderwearRectangle(this.container, FullnessType.None, Animations.LARGE_SPRITE_DIM);
            spriteBatch.Draw(Animations.sprites, objectPosition, new Rectangle?(rectangle), Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom/(Animations.LARGE_SPRITE_DIM/Animations.SMALL_SPRITE_DIM), SpriteEffects.None, Math.Max(0.0f, (f.StandingPixel.Y + 2) / 10000f));
        }

        public Dictionary<string, string> getAdditionalSaveData()
        {
            return new Dictionary<string, string>()
            {
                {
                    "type",
                    container.name
                },
                {
                    "wetness",
                    string.Format("{0}",  container.wetness)
                },
                {
                    "messiness",
                    string.Format("{0}",  container.messiness)
                },
                {
                    "durability",
                    string.Format("{0}",  container.durability)
                },
                {
                    "stack",
                    string.Format("{0}",  Stack)
                },
                {
                    "dryingTime",
                    Container.serializeDryingDate(container.timeWhenDoneDrying)
                }
            };
        }

        public override string getDescription()
        {
            string source = Strings.DescribeUnderwear(this.container, (string)null);
            return Game1.parseText(source.First().ToString().ToUpper() + source.Substring(1), Game1.smallFont, Game1.tileSize * 6 + Game1.tileSize / 6);
        }

        protected override Item GetOneNew()
        {
            //return new Underwear(this.name, this.container.wetness, this.container.messiness, -100, 1);
            return new Underwear(this.container, 1);
        }

        public StardewValley.Object getReplacement()
        {
            var saveObj = new StardewValley.Object("685", Stack, false, -1, 0);
            saveObj.modData.CopyFrom(this.modData);
            return saveObj;
        }

        public void Initialize(Container baseType, int count)
        {
            //this.container = new Container(type);
            this._container = null;
            this.container.ResetToDefault(baseType);
            if (count > 1)
                Stack = count;
            name = container.name;
            Price = container.price;
        }
        public override int maximumStackSize()
        {
            if (!container.stackable)
                return 1;
            return base.maximumStackSize();
        }

        public void rebuild(Dictionary<string, string> data, object replacement)
        {
            var container = new Container();

            container.ResetToDefault(data["type"], float.Parse(data["wetness"]), float.Parse(data["messiness"]), data.ContainsKey("durability") ? int.Parse(data["durability"]) : -100);
            if (data.ContainsKey("dryingTime"))
            {
                container.timeWhenDoneDrying = Container.parseDryingDate(data["dryingTime"]);
            }
            Initialize(container, int.Parse(data["stack"]));
        }

        public override string DisplayName
        {
            get
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Status + container.displayName);
            }
        }

        public override string Name
        {
            get
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Status + container.name);
            }
        }

        public string Status
        {
            get
            {
                if (container.messiness > 0.0 && container.wetness > 0.0)
                    return "wet and messy ";
                if (container.messiness > 0.0)
                    return "messy ";
                if (container.wetness > 0.0)
                    return "wet ";
                return container.drying ? "drying " : "";
            }
        }


        public static bool getPantsPlural(int itemNum)
        {
            //This was built based on the game's ClothingInformation.json file
            switch(itemNum)
            {
                case  -1: { return true; }
                case   0: { return true; }
                case   2: { return false; }
                case   3: { return false; }
                case   4: { return false; }
                case   5: { return true; }
                case   6: { return false; }
                case   7: { return false; }
                case   8: { return true; }
                case   9: { return true; }
                case  10: { return true; }
                case  11: { return false; }
                case  12: { return true; }
                case  13: { return true; }
                case  14: { return true; }
                case  15: { return true; }
                case 998: { return true; }
                case 999: { return true; }
            }
            return false;
        }
    }
}