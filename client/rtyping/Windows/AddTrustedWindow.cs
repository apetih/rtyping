using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System.Linq;
using System.Globalization;
using rtyping.Models;

namespace rtyping.Windows;

public class AddTrustedWindow : Window, IDisposable
{
    private Plugin Plugin;
    private bool displayParty;
    private bool displayNameplate;
    private int nameplateStyle;
    private bool sendTypingStatus;
    private bool sendPartyless;
    private bool receivePartyless;
    private int selected;
    private String characterName = null!;


    public AddTrustedWindow(Plugin plugin) : base(
        "Add Trusted Character",
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.Plugin = plugin;
    }

    public void Dispose() { }

    public void SetDefaults()
    {
        this.displayParty = Plugin.Configuration.DefaultDisplayParty;
        this.displayNameplate = Plugin.Configuration.DefaultDisplayNameplate;
        this.nameplateStyle = Plugin.Configuration.DefaultNameplateStyle;
        this.sendTypingStatus = Plugin.Configuration.DefaultSendTypingStatus;
        this.sendPartyless = Plugin.Configuration.DefaultSendPartyless;
        this.receivePartyless = Plugin.Configuration.DefaultReceivePartyless;
        this.selected = Plugin.ClientState.LocalPlayer == null ? 0 : Plugin.Worlds.Keys.ToList().FindIndex(w => w == Plugin.ClientState.LocalPlayer.HomeWorld.Value.Name);
        this.characterName = "";
    }

    public unsafe override void Draw()
    {
        this.Position = null;
        ImGui.InputText("Character Name", ref characterName, 22);
        ImGui.Combo("Homeworld", ref selected, Plugin.Worlds.Keys.ToArray());
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
        if (characterName == "") ImGui.BeginDisabled();
        if (ImGui.Button("Add Trusted Character"))
        {
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            var CharacterName = textInfo.ToTitleCase(characterName);
            var SelectedWorld = Plugin.Worlds.Keys.ToArray()[selected];
            var SelectedWorldId = Plugin.Worlds[SelectedWorld];
            if (Plugin.TrustedCharacterDb.TrustedCharacters.Any(c => c.CharacterName == CharacterName && c.WorldId == SelectedWorldId))
            {
                Plugin.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
                {
                    Title = "Duplicate Character",
                    Content = $"{CharacterName} is already trusted.",
                    Minimized = false,
                    Type = Dalamud.Interface.ImGuiNotification.NotificationType.Error
                });
                return;
            }
            Plugin.TrustedCharacterDb.Add(new TrustedCharacter
            {
                CharacterName = CharacterName,
                WorldId = SelectedWorldId,
                AddedAt = DateTime.Now,
                DisplayNameplate = displayNameplate,
                DisplayParty = displayParty,
                NameplateStyle = nameplateStyle,
                ReceivePartyless = receivePartyless,
                SendPartyless = sendPartyless,
                SendTypingStatus = sendTypingStatus
            });
            Plugin.TrustedCharacterDb.SaveChanges();
            //Plugin.ChatGui.Print($"{CharacterName} has been added as a trusted character.", "RTyping", 576);
            Plugin.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification
            {
                Title = "Character Added",
                Content = $"{CharacterName} has been added as a trusted character.",
                Minimized = false,
                Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
            });
            this.IsOpen = false;
        }
        if (characterName == "") ImGui.EndDisabled();
        ImGui.PopStyleColor();
    }
}
