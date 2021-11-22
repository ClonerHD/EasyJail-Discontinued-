using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using Oxide.Game.Rust.Cui;
namespace Oxide.Plugins
{
    [Info("EasyJail", "Cloner", "1.0.2")]
    [Description("Allows admins to quickly and easily jail players with support for custom jail cells!")]
    internal class EasyJail : CovalencePlugin
    {
    #region Variables

        public static List<string> JailList = new List<string>();
        public static List<Vector3> PosList = new List<Vector3>();  
        public static List<Vector3> JailPositions = new List<Vector3>();  


        [PluginReference]
        CovalencePlugin CopyPaste;  

        
        protected override void LoadDefaultConfig()
        {
            Config["PlayerAlreadyJailedMsg"] = "This Player is already jailed!";
            Config["PlayerJailedMsg"] = "The player has been jailed!";
            Config["YouJailedMsg"] = "You have been jailed!";
            Config["YouUnjailedMsg"] = "You have been unjailed!";
            Config["PlayerUnjailedMsg"] = "The player has been unjailed!";
            Config["PlayerJailErrorMsg"] = "The player could not be jailed!";
            Config["PlayerNotInJailMsg"] = "This player is not jailed!";
            Config["PlayerNotOnServerError"] = "This player is not on the server!";
            Config["WrongUsageMsg"] = "You need to specify a player and jail cell type!";
            Config["WrongUsageMsg2"] = "You need to specify a player!";
			Config["TeleportOffset (don't change unless players spawn outside the jail)"] = "(0, 1, 3)";
        }
		
        public const string Permission0 = "easyjail.jail";
        public const string Permission1 = "easyjail.unjail";
        public const string Permission2 = "easyjail.gui-open";
        public const string Permission3 = "easyjail.gui-close";

    #endregion

    #region Commands

        // Jail Command
        [Command("jail"), Permission(Permission0)]
        void Jail(IPlayer player, string command, string[] args)
        {
            if (args.Length == 2) // Check if username and jailtype are specified
            {
            // Get Args
            string targetstr = args[0];
            string cellsize = args[1];

            // Get Player object from args
            IPlayer target = FindPlayer(targetstr);
            
            // Check if player is already jailed
            for (int i=0; i< JailList.Count; i++) {
                    if (JailList[i] == target.Id) {
                    player.Reply(Config["PlayerAlreadyJailedMsg"].ToString());
                    return;
            }
            }

            // If the player isn't online, return.
            if (target == null)
            {
                player.Reply(Config["PlayerNotOnServerError"].ToString());
                return;
            }
            else
            {

                // Iplayer to baseplayer
                BasePlayer target2 = (BasePlayer)target.Object;
                BasePlayer player2 = (BasePlayer)player.Object;

                // Cast ray from player 
                RaycastHit hit;

                if (Physics.Raycast(player2.eyes.HeadRay(), out hit, 1000f)) {

                    // Paste Jail at hit position
                    var options = new List<string>{ "blockcollision", "0", "autoheight", "true"};
                    var success = CopyPaste.Call("TryPasteFromVector3", hit.point, 0f, "JailCell-" + cellsize, options.ToArray());

                    // Send chat messages
                    player.Reply(Config["PlayerJailedMsg"].ToString());
                    target.Reply(Config["YouJailedMsg"].ToString());

                    // hit.point to vector3
                    Vector3 pos = hit.point;
					
                    // Add player to the Jaillists and TP to jail
                    PosList.Add(target2.transform.position);
                    target2.Teleport(pos + StringToVector3(Config["TeleportOffset (don't change unless players spawn outside the jail)"].ToString()));
                    JailPositions.Add(pos + StringToVector3(Config["TeleportOffset (don't change unless players spawn outside the jail)"].ToString()));
                    JailList.Add(target.Id);
                    return;
                }
                else // If the raycast fails, return.
                {
                    player.Reply(Config["PlayerJailErrorMsg"].ToString());   
                    return;
                }
            }
        }
        else // If the command is not used correctly, return.
        {
            player.Reply(Config["WrongUsageMsg"].ToString());
            return;
        }
        }
    

        // Unjail Command
        [Command("unjail"), Permission(Permission1)]
        void unjail(IPlayer player, string command, string[] args) {
            if (args.Length == 1) // Check if username is specified
            {
            // Get Args
            string targetstr = args[0];
        
            // Find Player object from args
            IPlayer target = FindPlayer(targetstr);

            // If the player target isn't online, return
            if (target == null) {
                player.Reply(Config["PlayerNotOnServerError"].ToString());
                return;
            }

            // Iplayer to baseplayer
            BasePlayer target2 = (BasePlayer)target.Object;

            // Placeholder for the player's position.
            var ogpos = Vector3.zero;

            // Check if the player is jailed
            if (JailList.Contains(target.Id))
            {
                for (int i=0; i< JailList.Count; i++) { // Loop through the jaillist and remove the player from all of them
                    if (JailList[i] == target.Id) {
                    var iteration = i;
                    ogpos = PosList[iteration];
                    PosList.RemoveAt(iteration);
                    JailPositions.RemoveAt(iteration);
                    break; //don't need to check the remaining ones now that we found one
                }
            }
            
                // Remove the player from JailList and send chat messages
                JailList.Remove(target.Id);
                player.Reply(Config["PlayerUnjailedMsg"].ToString());
                target.Reply(Config["YouUnjailedMsg"].ToString());

                // Remove all Building Blocks near the player to make sure the jail is gone
                var entities = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
                foreach (var entity in entities)
                {
                    if (Vector3.Distance(entity.transform.position, target2.transform.position) < 15)
                    {
                        entity.Kill();
                    }
                }
                // TP
                target2.Teleport(ogpos);
            }
            else // If the player isn't jailed, return.
            {
                player.Reply(Config["PlayerNotInJailMsg"].ToString());
                return;
            }
        }
        else // If the command is not used correctly, return.
        {
            player.Reply(Config["WrongUsageMsg2"].ToString());
            return;
        }
    }

    // List-Jail Command
    [Command("jail-list"), Permission(Permission2)]
    void ListJail(IPlayer player, string command, string[] args) {
        var pos = 0.8f;

        // Iplayer to baseplayer
        BasePlayer player2 = (BasePlayer)player.Object;

        // create cui container
        CuiElementContainer container = new CuiElementContainer();

        // create small ui panel that's tall
        container.Add(new CuiPanel
        {
            Image = { Color = "0.1 0.1 0.1 0.8" },
            RectTransform = { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.9" },
            CursorEnabled = true,
        }, "Hud", "JailList");
        // Add close button
        container.Add(new CuiButton
        {
            Button = { Command = "close-list", Color = "1 0.1 0.1 0.7" },
            RectTransform = { AnchorMin = "0.9 0.9", AnchorMax = "0.99 0.99" },
            Text = { Text = "X", FontSize = 20, Align = TextAnchor.MiddleCenter }
        }, "JailList");

        // Add title
        container.Add(new CuiLabel
        {
            Text = { Text = "Jailed Players", FontSize = 20, Align = TextAnchor.MiddleCenter },
            RectTransform = { AnchorMin = "0.3 0.9", AnchorMax = "0.7 0.99" }
        }, "JailList");

        if (JailList.Count != 0) // Check if there are even any players jailed.
        {
        // for each player in the jaillist, add them to the list
        for (int i=0; i< JailList.Count; i++) {
            // Get player
            IPlayer user = covalence.Players.FindPlayerById(JailList[i]);
            // get players name
            string name = user.Name;

            // Add the player to the list
            container.Add(new CuiLabel
            {
                Text = { Text = name, FontSize = 20, Align = TextAnchor.LowerLeft},
                RectTransform = { AnchorMin = "0.05 " + pos.ToString(), AnchorMax = "0.3 " + (pos + 0.05f).ToString()}
            }, "JailList");

            // add button next to the player's name that says unjail

            container.Add(new CuiButton
            {
                Button = { Command = "unjail " + name, Color = "0.1 0.8 0.1 0.7" },
                RectTransform = { AnchorMin = "0.4 " + pos.ToString(), AnchorMax = "0.55 " + (pos + 0.05f).ToString()},
                Text = { Text = "Unjail", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, "JailList");
            container.Add(new CuiButton
            {
                Button = { Command = "kick " + name, Color = "0.8 0.8 0.1 0.7" },
                RectTransform = { AnchorMin = "0.6 " + pos.ToString(), AnchorMax = "0.75 " + (pos + 0.05f).ToString()},
                Text = { Text = "Kick", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, "JailList");
            container.Add(new CuiButton
            {
                Button = { Command = "ban " + name, Color = "0.8 0.1 0.1 0.7" },
                RectTransform = { AnchorMin = "0.8 " + pos.ToString(), AnchorMax = "0.95 " + (pos + 0.05f).ToString()},
                Text = { Text = "Ban", FontSize = 20, Align = TextAnchor.MiddleCenter }
            }, "JailList");
            
            pos -= 0.1f;
        }
        }
        else { // There are no players jailed so we show a different message.
            container.Add(new CuiLabel
            {
                Text = { Text = "There are currently no jailed players.", FontSize = 35, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.9" }
            }, "JailList");

        }
        // show the cui container
        CuiHelper.AddUi(player2, container); 

    }

    // Close-List Command
    [Command("close-list"), Permission(Permission3)]
    void CloseList(IPlayer player, string command, string[] args) {
        // Iplayer to baseplayer
        BasePlayer player2 = (BasePlayer)player.Object;

        // Destroy the UI element
        CuiHelper.DestroyUi(player2, "JailList");
    }
    #endregion

    #region Hooks

        // If a player disconnects and is jailed, unjail them.
        void OnPlayerDisconnected(BasePlayer player, string reason)
        { 
                // Remove all Building Blocks near the player to make sure the jail is gone
                var entities = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
                foreach (var entity in entities)
                {
                    if (Vector3.Distance(entity.transform.position, player.transform.position) < 15)
                    {
                        entity.Kill();
                    }
                }

            // Get IPlayer
            IPlayer target = FindPlayer(player.displayName);

            // Remove the player from all lists.
            if (JailList.Contains(target.Id))
            {
                for (int i=0; i< JailList.Count; i++) {
                    if (JailList[i] == target.Id) {
                    var iteration = i;
                    JailList.RemoveAt(iteration);
                    PosList.RemoveAt(iteration);
                    JailPositions.RemoveAt(iteration);
                    break; //don't need to check the remaining ones now that we found one
                }
            }
        }
    }
    // When a player respawns and is in jail, tp them back jail.
    void OnPlayerRespawned(BasePlayer player)
    {
        // Get IPlayer
        IPlayer target = FindPlayer(player.displayName);

        // If the player is jailed, find their entry in the jailpositions list and bring them back
        if (JailList.Contains(target.Id))
        {
            for (int i=0; i< JailList.Count; i++) {
                if (JailList[i] == target.Id) {
                var iteration = i;
                player.Teleport(JailPositions[iteration]);
                break; //don't need to check the remaining ones now that we found one
            }
        }
        }
    }	
    #endregion
	
	#region Functions
    
    // Convert string to vector3 (used to read teleport offset from config)
	public static Vector3 StringToVector3(string sVector)
    {
         // Remove the parentheses
         if (sVector.StartsWith ("(") && sVector.EndsWith (")")) {
             sVector = sVector.Substring(1, sVector.Length-2);
         }
 
         // split the items
         string[] sArray = sVector.Split(',');
 
         // store as a Vector3
         Vector3 result = new Vector3(
             float.Parse(sArray[0]),
             float.Parse(sArray[1]),
             float.Parse(sArray[2]));
 
         return result;
    }

    // Find Player
    IPlayer FindPlayer(string name) {
            // Find the player
            IPlayer target = covalence.Players.FindPlayer(name);	
            // Return the player
            return target;

    }
	#endregion
}
}