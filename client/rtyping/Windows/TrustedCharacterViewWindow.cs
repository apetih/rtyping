using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using rtyping.Models;
using System;
using System.Linq;
using System.Numerics;

namespace rtyping.Windows
{
    public class TrustedCharacterViewWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private TrustedCharacter? TargetCharacter;
        private bool displayParty;
        private bool displayNameplate;
        private int nameplateStyle;
        private bool sendTypingStatus;
        private bool sendPartyless;
        private bool receivePartyless;

        public TrustedCharacterViewWindow(Plugin plugin) : base(
        "Trusted Character Details",
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
        {
            this.Plugin = plugin; 
        }

        public void SetCharacter(TrustedCharacter character)
        {
            this.TargetCharacter = character;
            this.displayParty = character.DisplayParty;
            this.displayNameplate = character.DisplayNameplate;
            this.nameplateStyle = character.NameplateStyle;
            this.sendTypingStatus = character.SendTypingStatus;
            this.sendPartyless = character.SendPartyless;
            this.receivePartyless = character.ReceivePartyless;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Draw()
        {
            this.Position = null;
            if (TargetCharacter is null)
            {
                ImGui.Text("No character selected.");
                return;
            }
            var Homeworld = Plugin.DataManager.GetExcelSheet<World>().GetRow(TargetCharacter.WorldId).Name.ExtractText();
            ImGui.Text($"Name: {TargetCharacter.CharacterName}");
            ImGui.Text($"Homeworld: {Homeworld}");
            ImGui.Text($"Trusted Since: {TargetCharacter.AddedAt}");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Checkbox("Display party typing indicator", ref displayParty);
            ImGui.Checkbox("Display nameplate typing indicator", ref displayNameplate);
            ImGui.Checkbox("Send typing status", ref sendTypingStatus);
            /* Maybe someday
            ImGui.Checkbox("Send partyless typing status", ref sendPartyless);
            ImGui.Checkbox("Display partyless typing status", ref receivePartyless);
            */

            ImGui.Text("Nameplate Indicator Position");
            ImGui.RadioButton("Side", ref nameplateStyle, 0); ImGui.SameLine();
            ImGui.RadioButton("Top", ref nameplateStyle, 1);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.0f, 0.5f, 0.0f, 1.0f));
            if (ImGui.Button("Save Changes"))
            {
                TargetCharacter.DisplayParty = this.displayParty;
                TargetCharacter.DisplayNameplate = this.displayNameplate;
                TargetCharacter.NameplateStyle = this.nameplateStyle;
                TargetCharacter.SendTypingStatus = this.sendTypingStatus;
                TargetCharacter.SendPartyless = this.sendPartyless;
                TargetCharacter.ReceivePartyless = this.receivePartyless;
                Plugin.TrustedCharacterDb.SaveChanges();
                this.IsOpen = false;
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.0f, 0.0f, 1.0f));
            if (ImGui.Button("Remove Trusted"))
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter() - (new Vector2(340, 120) / 2), ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(new Vector2(340, 120));
                ImGui.OpenPopup("Remove Trusted");
            }
            ImGui.PopStyleColor();

            var unused_open = true;
            if (ImGui.BeginPopupModal("Remove Trusted", ref unused_open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextWrapped($"Are you sure you wish to remove {TargetCharacter.CharacterName} from your Trusted List?\nYou will no longer be able to see their typing status!");
                ImGui.Separator();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.0f, 0.0f, 1.0f));
                if (ImGui.Button("Remove from Trusted"))
                {
                    var TargetPlayerName = TargetCharacter.CharacterName;
                    var TargetPlayerWorldId = TargetCharacter.WorldId;

                    if (Plugin.TrustedCharacterDb.TrustedCharacters.Any(c => c.CharacterName == TargetPlayerName && c.WorldId == TargetPlayerWorldId))
                    {
                        TargetCharacter = Plugin.TrustedCharacterDb.TrustedCharacters.First(c => c.CharacterName == TargetPlayerName && c.WorldId == TargetPlayerWorldId);

                        Plugin.TrustedCharacterDb.Remove(TargetCharacter);
                        Plugin.TrustedCharacterDb.SaveChanges();
                    }
                    this.IsOpen = false;
                    ImGui.CloseCurrentPopup();
                    //Plugin.ChatGui.Print($"{TargetCharacter.CharacterName} has been removed as a trusted character.", "RTyping", 576);
                    Plugin.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
                    {
                        Title = "Character Removed",
                        Content = $"{TargetCharacter.CharacterName} has been removed as a trusted character.",
                        Minimized = false,
                        Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
                    });
                }
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.0f, 0.5f, 0.0f, 1.0f));
                if (ImGui.Button("Keep in Trusted"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
        }

    }
}
