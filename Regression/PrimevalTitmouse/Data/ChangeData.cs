using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimevalTitmouse.Data
{
    public class ChangeData
    {
        public string LookPants;
        public string Underwear_Clean;
        public string Underwear_Drying;
        public string[] Underwear_Messy;
        public string[] Underwear_Wet;
        public Dictionary<string, Dictionary<string, string>> Underwear_States;
        public string[] Still_Soiled;
        public string[] Should_Change;
        
        public string[] Cant_Remove;
        public string[] Change_Underwear;
        public string[] Change_Underwear_Pants;
        public string[] Change_Underwear_by_Npc;
        public string[] Change_Underwear_Pants_by_Npc;
        public string[] Change_Destroyed;
        public string[] Change_Other_Destroyed;
        public string[] Getting_Changed_Destroyed;
        public string Change_Requires_Pants;
        public string Change_Requires_Home;
        public string Change_At_Home;
        public string PeekWaistband;
        public string[] Diaper_Change_Dialog;
        public string[] Change_Other_Dialog;        
    }
}
