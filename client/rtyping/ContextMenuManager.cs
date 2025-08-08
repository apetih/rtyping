using Dalamud.Game.Gui.ContextMenu;
using rtyping.Models;
using System;
using System.Linq;

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
            var TargetPlayerName = ((MenuTargetDefault)args.Target).TargetName;
            var TargetPlayerWorldId = ((MenuTargetDefault)args.Target).TargetHomeWorld.RowId;
            if (!Plugin.TrustedCharacterDb.TrustedCharacters.Any(c => c.CharacterName == TargetPlayerName && c.WorldId == TargetPlayerWorldId)) return;

            var TargetCharacter = Plugin.TrustedCharacterDb.TrustedCharacters.First(c => c.CharacterName == TargetPlayerName && c.WorldId == TargetPlayerWorldId);

            Plugin.TrustedCharacterDb.Remove(TargetCharacter);
            Plugin.TrustedCharacterDb.SaveChanges();

            //Plugin.ChatGui.Print($"{TargetPlayerName} has been removed as a trusted character.", "RTyping", 576);

            Plugin.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title = "Character Removed",
                Content = $"{TargetPlayerName} has been removed as a trusted character.",
                Minimized = false,
                Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
            });
        }

        private void AddTrusted(IMenuItemClickedArgs args)
        {
            var TargetPlayerName = ((MenuTargetDefault)args.Target).TargetName;
            var TargetPlayerWorldId = ((MenuTargetDefault)args.Target).TargetHomeWorld.RowId;
            if (Plugin.TrustedCharacterDb.TrustedCharacters.Any(c => c.CharacterName == TargetPlayerName && c.WorldId == TargetPlayerWorldId)) return;

            Plugin.TrustedCharacterDb.Add(new TrustedCharacter
            {
                CharacterName = TargetPlayerName,
                WorldId = TargetPlayerWorldId,
                AddedAt = DateTime.Now,
                DisplayNameplate = Plugin.Configuration.DefaultDisplayNameplate,
                DisplayParty = Plugin.Configuration.DefaultDisplayParty,
                NameplateStyle = Plugin.Configuration.DefaultNameplateStyle,
                ReceivePartyless = Plugin.Configuration.DefaultReceivePartyless,
                SendPartyless = Plugin.Configuration.DefaultSendPartyless,
                SendTypingStatus = Plugin.Configuration.DefaultSendTypingStatus
            });
            Plugin.TrustedCharacterDb.SaveChanges();

            //Plugin.ChatGui.Print($"{TargetPlayerName} has been added as a trusted character.", "RTyping", 576);

            Plugin.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title = "Character Added",
                Content = $"{TargetPlayerName} has been added as a trusted character.",
                Minimized = false,
                Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
            });
        }

        private void ContextMenu_OnOpenGameObjectContextMenu(IMenuOpenedArgs args)
        {
            if (!IsValidAddon(args)) return;
            if (this.Plugin.Configuration.TrustAnyone) return;

            var TargetPlayerName = ((MenuTargetDefault)args.Target).TargetName;
            var TargetPlayerWorldId = ((MenuTargetDefault)args.Target).TargetHomeWorld.RowId;

            if (TargetPlayerName == Plugin.ClientState.LocalPlayer!.Name.TextValue && TargetPlayerWorldId == Plugin.ClientState.LocalPlayer!.HomeWorld.RowId) return;

            if (Plugin.TrustedCharacterDb.TrustedCharacters.Any(c => c.CharacterName == TargetPlayerName && c.WorldId == TargetPlayerWorldId))
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
                    return ((MenuTargetDefault)args.Target).TargetHomeWorld.RowId != 0 && ((MenuTargetDefault)args.Target).TargetHomeWorld.RowId != 65535;
            }
        }

        public void Dispose()
        {
        }
    }
}
