using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace PrimevalTitmouse
{
    public sealed class ModConfig
    {
        public bool AlwaysNoticeAccidents { get; set; } = false;
        public bool Debug { get; set; } = false;
        public bool Easymode { get; set; } = false;
        public string Lang { get; set; } = "en";
        public bool Messing { get; set; } = true;
        public bool NoFriendshipPenalty { get; set; } = true;
        public bool NoHungerAndThirst { get; set; } = false;
        public bool Wetting { get; set; } = true;

        public int SpriteSheet { get; set; } = 1;
        public int BladderLossContinenceRate { get; set; } = 1;
        public int BowelLossContinenceRate { get; set; } = 1;
        public int BladderGainContinenceRate { get; set; } = 1;
        public int BowelGainContinenceRate { get; set; } = 1;
        public float foodAmtMult { get; set; } = 1;
        public float drinkAmtMult { get; set; } = 1;
        public string babyNickname { get; set; } = "";

        public KeybindList WetBind { get; set; } = KeybindList.ForSingle(SButton.F1);

        public KeybindList MessBind { get; set; } = KeybindList.ForSingle(SButton.F2);

        public KeybindList PullDownPantsBind { get; set; } = KeybindList.ForSingle(SButton.LeftShift);

        public KeybindList CheckUndiesBind { get; set; } = KeybindList.ForSingle(SButton.F5);

        public KeybindList CheckPantsBind { get; set; } = KeybindList.ForSingle(SButton.F6);

        public KeybindList CheckVillagerDiaperBind { get; set; } = KeybindList.ForSingle(SButton.F3);

        public KeybindList AskVillagerChangeBind { get; set; } = KeybindList.ForSingle(SButton.F4);

        public KeybindList DecEverythingBind { get; set; } = KeybindList.Parse("LeftAlt + F1");
        public KeybindList IncEverythingBind { get; set; } = KeybindList.Parse("LeftAlt + F2");

        public KeybindList UndiesMenuBind { get; set; } = KeybindList.Parse("LeftAlt + F3");

        public KeybindList TimeMagicBind { get; set; } = KeybindList.Parse("LeftAlt + F5");



    }
}
