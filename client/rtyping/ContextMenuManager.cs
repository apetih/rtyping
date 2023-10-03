using Dalamud.ContextMenu;
using Dalamud.Game.Text;
using System;

namespace rtyping
{
    public class ContextMenuManager : IDisposable
    {
        private Plugin Plugin;
        private readonly GameObjectContextMenuItem addTrustedItem;
        private readonly GameObjectContextMenuItem removeTrustedItem;

        private string SelectedPlayer;

        public ContextMenuManager(Plugin plugin)
        {
            this.Plugin = plugin;
            this.Plugin.ContextMenu.OnOpenGameObjectContextMenu += this.ContextMenu_OnOpenGameObjectContextMenu;

            this.addTrustedItem = new GameObjectContextMenuItem("Add RTyping Trusted", this.AddTrusted);
            this.removeTrustedItem = new GameObjectContextMenuItem("Remove RTyping Trusted", this.RemoveTrusted);
        }

        private void RemoveTrusted(GameObjectContextMenuItemSelectedArgs args)
        {
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            if (!trustedList.Contains(this.SelectedPlayer)) return;
            this.Plugin.Configuration.TrustedCharacters.Remove(this.SelectedPlayer);
            this.Plugin.Configuration.TrustedCharacters = trustedList;
            this.Plugin.Configuration.Save();

            Plugin.ChatGui.Print(new XivChatEntry
            {
                Message = $"[RTyping] {SelectedPlayer.Split("@")[0]} has been removed as a trusted character.",
                Type = XivChatType.SystemMessage,
            });
        }

        private void AddTrusted(GameObjectContextMenuItemSelectedArgs args)
        {
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            if (trustedList.Contains(this.SelectedPlayer)) return;
            this.Plugin.Configuration.TrustedCharacters.Add(this.SelectedPlayer);
            this.Plugin.Configuration.TrustedCharacters = trustedList;
            this.Plugin.Configuration.Save();

            Plugin.ChatGui.Print(new XivChatEntry
            {
                Message = $"[RTyping] {SelectedPlayer.Split("@")[0]} has been added as a trusted character.",
                Type = XivChatType.SystemMessage,
            });
        }

        private void ContextMenu_OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
        {
            if (!IsValidAddon(args)) return;
            if (this.Plugin.Configuration.TrustAnyone) return;
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            this.SelectedPlayer = $"{args.Text}@{args.ObjectWorld}";
            if (this.SelectedPlayer == $"{Plugin.ClientState.LocalPlayer!.Name}@{Plugin.ClientState.LocalPlayer!.HomeWorld.Id}") return;
            if (trustedList.Contains(this.SelectedPlayer))
                args.AddCustomItem(this.removeTrustedItem);
            else
                args.AddCustomItem(this.addTrustedItem);
        }

        private static bool IsValidAddon(BaseContextMenuArgs args)
        {
            switch (args.ParentAddonName)
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
                    return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;
            }
        }

        public void Dispose()
        {
            this.Plugin.ContextMenu.OnOpenGameObjectContextMenu -= this.ContextMenu_OnOpenGameObjectContextMenu;
        }
    }
}
