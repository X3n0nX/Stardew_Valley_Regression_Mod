using StardewValley;
using System.Numerics;

namespace RegressionMod
{
    public class GameHelper
    {
        /// <summary>Get the viewport coordinates from the current cursor position.</summary>
        public static Vector2 GetScreenCoordinatesFromCursor()
        {
            return new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY());
        }
    }
}
