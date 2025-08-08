# RTyping

Very rough (like always) dalamud plugin for displaying the chat typing status of party members marked as trusted.

Shows an icon next to the player's job icon in the party list, and next to their nameplate if nearby.

## Limitations
This plugin will only work with party members who are also using the plugin. It also does not work with chat-replacing plugins, unless added via IPC.

The partylist marker will not show up for alliances.

The nameplate marker can only show up if the party member's nameplate could render (regardless of if it is visible or not via player configuration) and isn't culled due to too many characters on screen.

## How to Use
The icons should appear automatically, the conditions for their appearance can be changed in the configuration menu from inside the Plugin Installer or with the `/rtyping` command.

## Installation

To install the plugin from the official Dalamud repo, enable `Get plugin testing builds` under the `Experimental` tab in Dalamud Settings.

Once added, look for RTyping in the Plugin Installer.

## Special Thanks
* Dandybadger - For the plugin icon
