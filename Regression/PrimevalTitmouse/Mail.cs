﻿using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace PrimevalTitmouse
{
  public static class Mail
  {
    private static bool letterShown = false;
    public static IModHelper helper;
    private static readonly string initialRegressionLetterTitle = "jodi_initial_regression";
    private static string letterContents = Regression.t.Jodi_Initial_Letter[0];
    private static List<Item> initialSupplies = new();

    public static void CheckMail()
    {
      //Give an extra letter in the beginning to give some starting supplies
      if (!Game1.player.hasOrWillReceiveMail(initialRegressionLetterTitle))
      {
        initialSupplies = new();
        letterShown = false;
        //Always give turnips and diapers
        //initialSupplies.Add(new StardewValley.Object("399", 40, false, -1, 0)); // Spring Onion - it needs definitly a bit of food at the start
        initialSupplies.Add(new StardewValley.Object("403", 20, false, -1, 0)); // Field Snacks might be a better choise
                                                                                // field snacks might be better as a generic choise and are not sold 
                                                                                //initialSupplies.Add(new Underwear("pawprint diaper", 0.0f, 0.0f, 40));
        initialSupplies.Add(new Underwear("training pants", 5)); // training pants are washable, they might be a good start equipment and are "not that great"
        initialSupplies.Add(new Underwear("joja diaper", 25)); // joja diapers are cheap and the player would want to replace them at some point, but they are a good night diaper
                                                               //If we're in Hard mode, also give pull-up.
        /*if (!Regression.config.Easymode)
        {
            initialSupplies.Add(new Underwear("training pants", 0.0f, 0.0f, 5)); // training pants are washable, they might be a good start equipment and are "not that great"
        }*/
        letterContents += "[#]A Little... Protection.";
        Game1.mailbox.Add(initialRegressionLetterTitle);
        Dictionary<string, string> mails = Game1.content.Load<Dictionary<string, string>>("Data\\mail");
        if (!mails.ContainsKey(initialRegressionLetterTitle))
          mails.Add(initialRegressionLetterTitle, letterContents);

        //Just to test we haven't broken other letters;
        //Game1.mailbox.Add("robinWell");
      }
    }

    //The typical, built-in "%item <id> <qty>%% doesn't work for 2 reasons
    //1) It only supports 1 item, were we want to give multiple
    //2) It only supports items with IDs, whereas our underwear is a non-ID'd object
    public static void ShowLetter(LetterViewerMenu letterViewer)
    {
      string mail = letterViewer.mailTitle;
      if (mail == initialRegressionLetterTitle && !letterShown)
      {
        letterShown = true;

        //Find expected total width of all item boxes
        const int boxHeight = 96;
        const int boxWidth = 96;
        const int boxSpacing = 32;
        const int elementWidth = boxWidth + boxSpacing;
        int totalWidth = (elementWidth) * initialSupplies.Count;
        int amountOfLeftoverSpace = letterViewer.width - totalWidth;
        int margin = amountOfLeftoverSpace / 2;

        int itemIndex = 0;
        //Only handle 1 Row of items. If it becomes necessary, we can extend this logic to multiple rows
        foreach (StardewValley.Object item in initialSupplies)
        {
          int itemBoxX = letterViewer.xPositionOnScreen + margin + ((elementWidth) * itemIndex);
          int itemBoxY = letterViewer.yPositionOnScreen + letterViewer.height - boxSpacing - boxHeight;
          letterViewer.itemsToGrab.Add(new ClickableComponent(new Microsoft.Xna.Framework.Rectangle(itemBoxX, itemBoxY, boxWidth, boxHeight), item));
          itemIndex++;
        }
      }
    }
  }
}
