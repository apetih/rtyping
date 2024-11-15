using System;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace rtyping.Windows;

public class AddTrustedWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    public AddTrustedWindow(Plugin plugin) : base(
        "Add Trusted Characters", ImGuiWindowFlags.None)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 300),
            MaximumSize = new Vector2(300, 600)
        };
        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public unsafe override void Draw()
    {
        if (!Plugin.ClientState.IsLoggedIn || Plugin.ClientState.LocalPlayer == null)
        {
            ImGui.TextWrapped("Log into a character to use this window.");
            return;
        }

        var partyCount = Plugin.PartyManager.GetPartyMemberCount();
        if (partyCount == 0)
        {
            ImGui.TextWrapped("Join a party to use this window.");
            return;
        }

        ImGui.Text("Current Party Members");
        ImGui.Separator();

        var partyList = Plugin.PartyManager.BuildPartyDictionary();
        var worldSheet = Plugin.DataManager.GetExcelSheet<World>();

        foreach (var entry in partyList)
        {
            if ($"{Plugin.ClientState.LocalPlayer.Name}@{Plugin.ClientState.LocalPlayer.HomeWorld.RowId}" == $"{entry.Value.Name}@{entry.Value.World}") continue;
            var action = this.Configuration.TrustedCharacters.Contains($"{entry.Value.Name}@{entry.Value.World}") ? "Remove" : "Add";
            if (ImGui.Button($"{action}##{entry.Value.Name}@{entry.Value.World}"))
            {
                if (action == "Add")
                {
                    this.Configuration.TrustedCharacters.Add($"{entry.Value.Name}@{entry.Value.World}");
                    Plugin.ChatGui.Print(new XivChatEntry
                    {
                        Message = $"[RTyping] {entry.Value.Name} has been added as a trusted character.",
                        Type = XivChatType.SystemMessage,
                    });
                }
                else
                {
                    this.Configuration.TrustedCharacters.Remove($"{entry.Value.Name}@{entry.Value.World}");
                    Plugin.ChatGui.Print(new XivChatEntry
                    {
                        Message = $"[RTyping] {entry.Value.Name} has been removed as a trusted character.",
                        Type = XivChatType.SystemMessage,
                    });
                }
                this.Configuration.Save();
            }
            ImGui.SameLine();

            var worldRow = worldSheet!.GetRow(entry.Value.World);
            ImGui.Text($"{entry.Value.Name} @ {worldRow!.Name}");
        }
    }
}
