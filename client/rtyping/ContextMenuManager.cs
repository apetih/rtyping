using Dalamud.ContextMenu;
using System;

namespace rtyping
{
    public class ContextMenuManager : IDisposable
    {
        private Plugin Plugin;
        private readonly GameObjectContextMenuItem addTrustedItem;
        private readonly GameObjectContextMenuItem removeTrustedItem;

        private string SelectedPlayer;

        public ContextMenuManager(Plugin plugin) { 
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
        }

        private void AddTrusted(GameObjectContextMenuItemSelectedArgs args)
        {
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            if (trustedList.Contains(this.SelectedPlayer)) return;
            this.Plugin.Configuration.TrustedCharacters.Add(this.SelectedPlayer);
            this.Plugin.Configuration.TrustedCharacters = trustedList;
            this.Plugin.Configuration.Save();
        }

        private void ContextMenu_OnOpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
        {
            if (!this.IsValidAddon(args)) return;
            var trustedList = this.Plugin.Configuration.TrustedCharacters;
            this.SelectedPlayer = $"{args.Text}@{args.ObjectWorld}";
            if (this.SelectedPlayer == $"{this.Plugin.ClientState.LocalPlayer!.Name}@{this.Plugin.ClientState.LocalPlayer!.HomeWorld.Id}") return;
            if (trustedList.Contains(this.SelectedPlayer))
                args.AddCustomItem(this.removeTrustedItem);
            else
                args.AddCustomItem(this.addTrustedItem);
        }

        private bool IsValidAddon(BaseContextMenuArgs args)
        {
            if (args.ParentAddonName == "BlackList" || args.ParentAddonName == "LookingForGroup") return false;
            return true;
        }

        public void Dispose()
        {
            this.Plugin.ContextMenu.OnOpenGameObjectContextMenu -= this.ContextMenu_OnOpenGameObjectContextMenu;
        }
    }
}
