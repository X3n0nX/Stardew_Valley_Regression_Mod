using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace PrimevalTitmouse.Data
{
    public class GeneralData
    {
        public string EasyMode_On;
        public string EasyMode_Off;

        public string Jodi_Initial_Letter;

        public Dictionary<string, string[]> Night;
        public string[] Wake_Up_Underwear_State;
        public string[] Wet_Bed;
        public string[] Messed_Bed;

        public string[] Washing_Bedding;
        public string[] Spot_Washing_Bedding;
        public string[] Washing_Underwear;
        public string[] Overwashed_Underwear;
        public string[] Bedding_Still_Wet;

        public Dictionary<string, Debuff> Debuffs;

        public string[] Passby;
    }
}
