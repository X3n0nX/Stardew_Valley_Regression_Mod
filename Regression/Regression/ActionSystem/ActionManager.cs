using StardewValley;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegressionMod
{

    public static class ActionManager
    {
        private static Farmer _who => Regression.who;
        private static Body _body => Regression.body;
        private static Config _config => Regression.config;
        private static ChangeData _changeData => Regression.changeData;

        private delegate bool ActionHandler(string[] args, TriggerActionContext context, out string error);

        public static void RegisterActions()
        {
            // A multi action manager, as the game only allowes one action to be triggered
            // Example: $action ACTIONS CHANGE_DIAPER_OTHERS vincent \"baby print diaper\" ADD_DIALOG \"Thank you so much! Here!\" \"change_vincent\" GIVE_UNDERWEAR \"baby print diaper\"
            TriggerActionManager.RegisterAction(ActionConstants.MultiActions, MultiActions);

            // This is about you getting your diapers changed by someone
            // Example: $action DIAPER_CHANGE \"baby print diaper\" \"sams pants\"
            TriggerActionManager.RegisterAction(ActionConstants.DiaperChange, StartChange);

            // This is about you changing others diapers, parameter 2 and 3 optional
            // Example: $action CHANGE_DIAPER_OTHERS jas
            // Example: $action CHANGE_DIAPER_OTHERS vincent \"baby print diaper\" \"toddler pants\"
            TriggerActionManager.RegisterAction(ActionConstants.ChangeDiaperOther, StartChangeOthers);

            // This is about the player having accidents, but can also be pointed at npc.
            // Example: $action DIAPER_ACCIDENT player pee
            // Example: $action DIAPER_ACCIDENT vincent poop
            TriggerActionManager.RegisterAction(ActionConstants.DiaperAccident, StartAccident);

            // This is adds a dialog. Parameter 3 is optional, containing a key. If there is a message with this key already present, the message will be replaced
            // Example: $action ADD_DIALOG jodi \"Did you change vincent into training pants? You should know better by now!#$b#%Jodi seams to be upset with you\" "change_vincent_wrong"
            TriggerActionManager.RegisterAction(ActionConstants.AddDialog, AddNpcMessage);

            // This adds underwear to the players inventory, usually as a gift from an npc
            // Example: $action GIVE_UNDERWEAR "training pants"
            TriggerActionManager.RegisterAction(ActionConstants.GiveUnderwear, GiveUnderwear);
        }

        private static bool MultiActions(string[] args, TriggerActionContext context, out string error)
        {
            error = null;

            // Remove the first arg "ACTION_MANAGER"
            args = args.Skip(1).ToArray();

            // Dictionary of known actions
            var actionHandlers = new Dictionary<string, ActionHandler>()
            {
                { ActionConstants.DiaperChange, StartChange },
                { ActionConstants.ChangeDiaperOther, StartChangeOthers },
                { ActionConstants.DiaperAccident,  StartAccident},
                { ActionConstants.AddDialog,  AddNpcMessage},
                { ActionConstants.GiveUnderwear,  GiveUnderwear}
                // Add more actions here
            };

            int index = 0;
            while (index < args.Length)
            {
                string actionName = args[index];
                if (!actionHandlers.ContainsKey(actionName))
                {
                    /*error = $"Unknown action: {actionName}";
                    return false;*/
                    // we will just assume that its not an action but a parameter from another function
                }

                // Find the next action in the list or the end of the array
                int nextActionIndex = args.Length;
                for (int i = index + 1; i < args.Length; i++)
                {
                    if (actionHandlers.ContainsKey(args[i]))
                    {
                        nextActionIndex = i;
                        break;
                    }
                }

                // Extract arguments for this action (including the action name itself)
                string[] actionArgs = args.Skip(index).Take(nextActionIndex - index).ToArray();

                // Execute the action
                if (!actionHandlers[actionName](actionArgs, context, out error))
                {
                    return false;
                }

                // Move to the next action
                index = nextActionIndex;
            }

            return true;
        }


        // action from dialog 
        // parameter 1: name of npc that change you
        // parameter 2: type of underwear the npc change you
        // parameter 3: (optional) name of pants the npc change you if yours are messy
        private static bool StartChange(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                string npcName = "sam";
                string underwearName = _who.Gender == Gender.Female ? "polka dot panties" : "big kid undies";
                if (args.Length > 2)
                {
                    npcName = args[1];
                    underwearName = args[2];
                }
                Underwear newUnderwear = new Underwear(underwearName, 1);

                Underwear oldUnderwear = new Underwear(_body.underwear, 1);

                bool dirtyPants = _body.HasWetOrMessyDebuff();

                if (args.Length > 3 && dirtyPants)
                {
                    Container oldPants = new Container(_body, ContainerSubtype.Pants, _body.pants.type);
                    oldPants.ResetToDefault(_body.pants);

                    string newPantsName = Strings.ReplaceOr(args[3], _who.Gender != Gender.Female);

                    Container newPants = new Container(_body, ContainerSubtype.Pants, newPantsName);

                    _body.ChangeUnderwearAndPants(newUnderwear, oldUnderwear, newPants, oldPants, npcName: npcName);
                }
                else
                {
                    _body.ChangeUnderwear(newUnderwear, oldUnderwear, npcName: npcName);
                }

                //body.ChangeUnderwear(newUnderwear, oldUnderwear, npc: npcName);
                //body.ResetPants();

                //If the underwear returned is not removable, destroy it
                bool returnDiaper = oldUnderwear.container.washable ? _config.ReturnUsedCloth : _config.ReturnUsedDisposable;
                if (!returnDiaper)
                {
                    Dialoges.Warn(_changeData.Getting_Changed_Destroyed, _body, oldUnderwear.container);
                }
                //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                else if (!_who.addItemToInventoryBool(oldUnderwear, false))
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(oldUnderwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }

        // action from dialog 
        // parameter 1: name of npc you wanne change
        // parameter 2: type of underwear you wanne change the npc into
        private static bool StartChangeOthers(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                if (args.Length < 2)
                {
                    error = "Parameter 1, target, has to be the name of an npc";
                    return false;
                }
                var target = args[1];
                target = target.ToLower();
                var targetNpc = NpcHelper.GetNpcByName(target, 20);
                if (targetNpc == null)
                {
                    error = $"Parameter 1, '{target}' not found in local npc list in 20 range";
                    return false;
                }

                string underwearName = args.Length > 2 ? args[2] : null;
                var newUnderwear = UnderwearHelper.GetUnderwearFromInventory(underwearName);
                if (newUnderwear == null)
                {
                    error = $"Parameter 2, '{underwearName}' not found in inventory";
                    return false;
                }

                var currentUnderwear = targetNpc.underwear;
                // We create a new, untethered container first, of the same type.
                Underwear oldUnderwear = new Underwear(currentUnderwear.type, 1);
                // We "reset" that new container to the same informations (including wet or dirty), so making a copy of it.
                oldUnderwear.container.ResetToDefault(currentUnderwear);


                if (_who.ActiveItem == newUnderwear)
                {
                    _who.reduceActiveItemByOne();
                }
                else
                {
                    newUnderwear.Stack -= 1;
                    if (newUnderwear.Stack < 1)
                    {
                        _who.removeItemFromInventory(newUnderwear);
                    }
                }

                targetNpc.change(underwearName, args.Length > 3 ? args[3] : null);


                //If the underwear returned is not removable, destroy it
                bool returnDiaper = oldUnderwear.container.washable ? _config.ReturnUsedCloth : _config.ReturnUsedDisposable;
                if (!returnDiaper)
                {
                    var msg = Strings.InsertVariables(Strings.RandString(_changeData.Change_Other_Destroyed), targetNpc.npc, oldUnderwear.container);
                    Dialoges.Warn(msg);
                }
                else if (!_who.addItemToInventoryBool(oldUnderwear, false)) //Otherwise put the old underwear into the inventory, but pull up the management window if it can't fit
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(oldUnderwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }

        // action from dialog 
        // parameter 1: type of accident, pee or poop
        // parameter 2: target of the accident, player or an npc name
        private static bool StartAccident(string[] args, TriggerActionContext context, out string error)
        {

            error = null;

            if (args.Length < 2)
            {
                error = "Parameter 1, type, should be given!";
                return false;
            }
            var typeStr = args[1];
            IncidentType type = IncidentType.PEE;
            switch (typeStr)
            {
                case "pee":
                type = IncidentType.PEE;
                break;
                case "poop":
                type = IncidentType.POOP;
                break;
                default:
                error = $"Parameter 1, type '{type}' is unknown!";
                return false;
            }

            if (args.Length < 3)
            {
                error = "Parameter 2, target, be 'player' or an npc name";
                return false;
            }
            var target = args.Length < 3 ? "player" : args[2];
            target = target.ToLower();


            if (target == "player" || target == "farmer" || target == "self")
            {
                _body.Accident(type);
            }
            else
            {
                var targetNpc = NpcHelper.GetNpcByName(target, 20);
                if (targetNpc == null)
                {
                    error = $"Parameter 2, '{target}' not found in local npc list in 20 range";
                    return false;
                }
                targetNpc.accidentFromFullness(type);
            }
            return true;
        }

        private static bool AddNpcMessage(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                if (args.Length < 2)
                {
                    error = "Parameter 1, target, has to be the name of an npc";
                    return false;
                }
                var target = args[1];
                target = target.ToLower();
                var targetNpc = NpcHelper.GetNpcByName(target, -1);
                if (targetNpc == null)
                {
                    error = $"Parameter 1, '{target}' not found in global npc list";
                    return false;
                }
                if (args.Length < 3)
                {
                    error = "Parameter 2, message, has to be a message that is added as dialog to that npc";
                    return false;
                }
                string eventToken = null;
                if (args.Length > 3)
                {
                    eventToken = args[3];
                    if (targetNpc.CurrentDialogue.Count > 0) targetNpc.RemoveDialogue(eventToken);
                }
                var msg = args[2].Replace("___", "#").Replace("$_", "$");
                targetNpc.npc.setNewDialogue(new Dialogue(targetNpc.npc, eventToken, msg), true, false);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }

        // action from dialog 
        // parameter 1: type of underwear
        private static bool GiveUnderwear(string[] args, TriggerActionContext context, out string error)
        {
            try
            {
                if (args.Length < 2)
                {
                    error = $"Parameter 1, needs to be a valid name of a type of underwear";
                    return false;
                }

                var amount = args.Length > 2 ? int.Parse(args[2]) : 1;

                var underwear = new Underwear(args[1], 1);

                //Put into the inventory, but pull up the management window if it can't fit
                if (!_who.addItemToInventoryBool(underwear, false))
                {
                    List<Item> objList = new List<Item>();
                    objList.Add(underwear);
                    Game1.activeClickableMenu = new ItemGrabMenu(objList);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            error = null;
            return true;
        }

    }
}
