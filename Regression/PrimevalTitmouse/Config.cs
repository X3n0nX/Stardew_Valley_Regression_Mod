namespace PrimevalTitmouse
{
    public class Config
    {
        public bool AlwaysNoticeAccidents = true;
        public bool Debug = false;
        public bool Easymode = false;
        public bool PantsChangeRequiresHome = true;
        public bool UnderwearChangeCauseExposure = true;
        public bool Wetting = true;
        public bool Messing = true;
        public int FriendshipPenaltyBladderMultiplier = 100;
        public int FriendshipPenaltyBowelMultiplier = 200;
        public bool FriendshipDebug = false;
        public bool NoHungerAndThirst = false;
        public int NighttimeLossMultiplier = 50;
        public int NighttimeGainMultiplier = 50;
        public int InUnderwearOnPurposeMultiplier = 50;
        public int BladderLossContinenceRate = 2;
        public int BowelLossContinenceRate = 4;
        public int BladderGainContinenceRate = 3;
        public int BowelGainContinenceRate = 4;
        public int MaxBladderCapacity = 600;
        public int MaxBowelCapacity = 1000;
        public int StartBladderContinence = 70;
        public int StartBowelContinence = 90;
        public bool ReadSaveFiles = true;
        public bool WriteSaveFiles = false;

        public int KeyGoInPants = 0;
        public int KeyPee = 112;
        public int KeyPoop = 113;
        public int KeyGoInToilet = 0;
        public int KeyPeeInToilet = 112;
        public int KeyPoopInToilet = 113;

    }
}
