using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EasyJail", "Cloner", "1.0.0")]
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
            Config["WrongUsageMsg"] = "You need to specify a player and jail size(small/medium/large)!";
            Config["WrongUsageMsg2"] = "You need to specify a player!";
			Config["TeleportOffset (don't change unless players spawn outside the jail)"] = "(0, 1, 3)";
        }
		
        public const string Permission = "easyjail.use";

    #endregion

    #region Commands

        // Jail Command
        [Command("jail"), Permission(Permission)]
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
    
        // Find Player
        IPlayer FindPlayer(string name) {
            // Find the player
            IPlayer target = covalence.Players.FindPlayer(name);	
            // Return the player
            return target;

        }

        // Unjail Command
        [Command("unjail"), Permission(Permission)]
        void unjail(IPlayer player, string command, string[] args) {
            if (args.Length == 1) // Check if username is specified
            {
            // Get Args
            string targetstr = args[0];
        
            // Find Player object from args
            IPlayer target = FindPlayer(targetstr);

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
	#endregion
}
}