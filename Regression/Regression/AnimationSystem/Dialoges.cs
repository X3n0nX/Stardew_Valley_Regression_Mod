using StardewValley;
using static RegressionMod.Regression;


namespace RegressionMod
{
    public class Dialoges
    {
        public static void Warn(string msg, Body b = null, Container c = null)
        {
            msg = Strings.tryGetI18nText(msg);
            msg = Strings.InsertVariables(msg, b, c);

            QueueAction(() =>
            {
                Game1.addHUDMessage(new HUDMessage(msg, 2));
            });

        }

        // randomize between multiple warnings
        public static void Warn(string[] msgs, Body b = null, Container c = null)
        {
            Warn(Strings.RandString(msgs), b, c);
        }

        public static void Write(string msg, Body b = null, Container c = null, int delay = 0)
        {
            msg = Strings.tryGetI18nText(msg);
            msg = Strings.InsertVariables(msg, b, c);
            QueueAction(() =>
            {
                DelayedAction.showDialogueAfterDelay(msg, 10);
            }, delay);

        }

        public static void Write(string[] msgs, Body b = null, Container c = null, int delay = 0)
        {
            Write(Strings.RandString(msgs), b, c, delay);
        }

        public static void Say(string msg, Body b = null)
        {
            msg = Strings.tryGetI18nText(msg);
            QueueAction(() =>
            {
                Game1.showGlobalMessage(Strings.InsertVariables(msg, b, (Container)null));
            });
        }

        public static void Say(string[] msgs, Body b = null)
        {
            Say(Strings.RandString(msgs), b);
        }
    }
}
