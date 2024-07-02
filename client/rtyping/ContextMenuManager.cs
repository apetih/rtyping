using Dalamud.Game.Gui.ContextMenu;
using System;

namespace rtyping
{
    public class ContextMenuManager : IDisposable
    {
        private Plugin Plugin;

        public ContextMenuManager(Plugin plugin)
        {
            this.Plugin = plugin;
            Plugin.ContextMenu.OnMenuOpened += ContextMenu_OnOpenGameObjectContextMenu;
        }

        private void RemoveTrusted(IMenuItemClickedArgs args)
        {
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            var SelectedPlayer = $"{((MenuTargetDefault)args.Target).TargetName}@{((MenuTargetDefault)args.Target).TargetHomeWorld.Id}";
            if (!trustedList.Contains(SelectedPlayer)) return;
            this.Plugin.Configuration.TrustedCharacters.Remove(SelectedPlayer);
            this.Plugin.Configuration.TrustedCharacters = trustedList;
            this.Plugin.Configuration.Save();

            Plugin.ChatGui.Print($"{((MenuTargetDefault)args.Target).TargetName} has been removed as a trusted character.", "RTyping", 576);
        }

        private void AddTrusted(IMenuItemClickedArgs args)
        {
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            var SelectedPlayer = $"{((MenuTargetDefault)args.Target).TargetName}@{((MenuTargetDefault)args.Target).TargetHomeWorld.Id}";
            if (trustedList.Contains(SelectedPlayer)) return;
            this.Plugin.Configuration.TrustedCharacters.Add(SelectedPlayer);
            this.Plugin.Configuration.TrustedCharacters = trustedList;
            this.Plugin.Configuration.Save();

            Plugin.ChatGui.Print($"{((MenuTargetDefault)args.Target).TargetName} has been added as a trusted character.", "RTyping", 576);
        }

        private void ContextMenu_OnOpenGameObjectContextMenu(IMenuOpenedArgs args)
        {
            if (!IsValidAddon(args)) return;
            if (this.Plugin.Configuration.TrustAnyone) return;

            var trustedList = this.Plugin.Configuration.TrustedCharacters;

            var SelectedPlayer = $"{((MenuTargetDefault)args.Target).TargetName}@{((MenuTargetDefault)args.Target).TargetHomeWorld.Id}";
            if (SelectedPlayer == $"{Plugin.ClientState.LocalPlayer!.Name}@{Plugin.ClientState.LocalPlayer!.HomeWorld.Id}") return;

            if (trustedList.Contains(SelectedPlayer))
                args.AddMenuItem(new()
                {
                    PrefixChar = 'R',
                    PrefixColor = 576,
                    Name = "Remove RTyping Trusted",
                    OnClicked = this.RemoveTrusted
                });
            else
                args.AddMenuItem(new()
                {
                    PrefixChar = 'R',
                    PrefixColor = 576,
                    Name = "Add RTyping Trusted",
                    OnClicked = this.AddTrusted
                });
        }

        private static bool IsValidAddon(IMenuOpenedArgs args)
        {
            switch (args.AddonName)
            {
                default:
                    return false;

                case null:
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "FreeCompany":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                case "_PartyList":
                case "LinkShell":
                case "CrossWorldLinkshell":
                case "ContentMemberList":
                case "BlackList":
                    return ((MenuTargetDefault)args.Target).TargetHomeWorld.Id != 0 && ((MenuTargetDefault)args.Target).TargetHomeWorld.Id != 65535;
            }
        }

        public void Dispose()
        {
        }
    }
}
