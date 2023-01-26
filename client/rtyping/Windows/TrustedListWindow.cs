using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace rtyping.Windows;

public class TrustedListWindow : Window, IDisposable
{
    private Configuration Configuration;

    public TrustedListWindow(Plugin plugin) : base(
        "Trusted Characters", ImGuiWindowFlags.None)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 300),
            MaximumSize = new Vector2(200, 600)
        };

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    private int selected = -1;
    public unsafe override void Draw()
    {
        var trustedList = this.Configuration.TrustedCharacters;
        ImGui.BeginChild("Characters", new Vector2(0, 0), true, ImGuiWindowFlags.None);
        ImGuiListClipperPtr clipper;
        unsafe {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }
        clipper.Begin(trustedList.Count);
        while (clipper.Step()) {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++) {
                if (ImGui.Selectable(trustedList[i], selected == i)) {
                    selected = i;
                    ImGui.OpenPopup($"###Trusted_{trustedList[i]}");
                }
                if (ImGui.BeginPopup($"###Trusted_{trustedList[i]}"))
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.0f, 0.0f, 1.0f));
                    if (ImGui.Button("Remove from Trusted")) {
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
