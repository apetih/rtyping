using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace rtyping.Windows;

public class TrustedListWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    public TrustedListWindow(Plugin plugin) : base(
        "Trusted Characters", ImGuiWindowFlags.None)
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

    private int selected = -1;
    public unsafe override void Draw()
    {
        var trustedList = this.Configuration.TrustedCharacters;
        if (ImGui.Button("Add Trusted Character"))
        {
            Plugin.AddTrustedWindow.IsOpen = true;
        }
        ImGui.BeginChild("Characters", new Vector2(0, 0), true, ImGuiWindowFlags.None);
        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }
        clipper.Begin(trustedList.Count);
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var worldSheet = Plugin.DataManager.GetExcelSheet<World>();
                var worldRow = worldSheet!.GetRow(uint.Parse(trustedList[i].Split("@")[1]));
                var worldName = worldRow.Name.ExtractText();
                var characterName = trustedList[i].Split("@")[0];
                var displayName = $"{characterName}@{worldName}";
                if (ImGui.Selectable(displayName, selected == i))
                {
                    selected = i;
                    ImGui.OpenPopup($"###Trusted_{trustedList[i]}");
                }
                if (ImGui.BeginPopup($"###Trusted_{trustedList[i]}"))
                {
                    ImGui.Text(characterName);
                    ImGui.Text(worldName);
                    ImGui.Separator();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.0f, 0.0f, 1.0f));
                    if (ImGui.Button("Remove from Trusted"))
                    {
                        trustedList.Remove(trustedList[i]);
                        this.Configuration.TrustedCharacters = trustedList;
                        this.Configuration.Save();
                        selected = -1;
                    }
                    ImGui.PopStyleColor();
                    ImGui.EndPopup();
                }
            }
        }
        ImGui.EndChild();
    }
}
