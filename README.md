# **Jail-System-for-Rust**
Allows admins to quickly and easily jail players with support for custom jail cells!

## Chat commands

* `/jail [Player] [Jail Type]`
* `/unjail <player>`
* `/jail-list`

## Permissions

* `easyjail.jail`
* `easyjail.unjail`
* `easyjail.gui-open`
* `easyjail.gui-close`

## depends on
* `https://umod.org/plugins/copy-paste`

## Usage

1. Build the jail cell
2. Copy it using `/copy JailCell-[jail cell name here]`
3. You are now able to jail players in this cell using `/jail <player> [cell name from the last command]`


## Configuration
*   "PlayerAlreadyJailedMsg": "This Player is already jailed!",
*   "PlayerJailedMsg": "The player has been jailed!",
*   "PlayerJailErrorMsg": "The player could not be jailed!",
*   "PlayerNotInJailMsg": "This player is not jailed!",
*  "PlayerNotOnServerError": "This player is not on the server!",
*   "PlayerUnjailedMsg": "The player has been unjailed!",
*   "TeleportOffset (don't change unless players spawn outside the jail)": "(0, 1, 0)",
*   "WrongUsageMsg": "You need to specify a player and jail cell type!",
*   "WrongUsageMsg2": "You need to specify a player!",
*   "YouJailedMsg": "You have been jailed!",
*   "YouUnjailedMsg": "You have been unjailed!" 
  ## Credits
*   **Cloner**
*   **Brimeggi**
