using StardewValley.Characters;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegressionMod
{
    public static class NpcHelper
    {
        private static Farmer _player => Game1.player;

        public static List<NPC> NearbyVillager(Body b, bool mess, bool inUnderwear, bool overflow, bool attempt = false)
        {
            int radius = 3;

            //If we are messing, increase the radius of noticeability (stinky)
            if (mess)
            {
                radius *= 2;
            }

            //If we pulled down our pants, quadruple the radius (not contained and visible!)
            //Double loss since you're just going in front of people (how uncouth)
            if (!inUnderwear)
            {
                radius *= 4;
            }

            //Double noticeability is we had a blow-out/leak (people can see)
            if (overflow)
                radius *= 2;

            return NearbyVillager(radius);
        }

        public static List<NPC> NearbyVillager(int radius)
        {
            var list = Utility.GetNpcsWithinDistance(((Character)_player).Tile, radius, (GameLocation)Game1.currentLocation);

            var newList = new List<NPC>();
            foreach (var npcEntry in list)
            {
                newList.Add(npcEntry);
            }
            return newList;
        }

        public static Gender GetNpcGender(string npcName)
        {
            List<NPC> npcs = Utility.getAllCharacters();

            npcName = npcName.FirstCharToUpper();

            foreach (NPC npc in npcs)
            {
                if (npc is Horse || npc is Pet) continue;

                if (npc.Name == npcName) return npc.Gender;
            }

            return Gender.Undefined;
        }

        public static List<string> npcTypeList(NPC npc)
        {
            List<string> npcType = new List<string>();

            if (npc is Horse || npc is Pet)
            {
                npcType.Add("animal");
            }
            else
            {
                switch (npc.Age)
                {
                    case 0:
                    npcType.Add("adult");
                    break;
                    case 1:
                    npcType.Add("teen");
                    break;
                    case 2:
                    npcType.Add("kid");
                    break;
                }
                npcType.Add(npc.getName().ToLower());
            }
            return npcType;
        }
    }
}
