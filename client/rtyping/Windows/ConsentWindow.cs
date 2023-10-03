using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace rtyping.Windows;

public class ConsentWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConsentWindow(Plugin plugin) : base(
        "RTyping Welcome",
        ImGuiWindowFlags.NoCollapse)
    {
        this.Size = new Vector2(370, 340) * (ImGui.GetFontSize() / 17);
        this.SizeCondition = ImGuiCond.Appearing;
        this.ShowCloseButton = false;
        this.Position = ImGui.GetMainViewport().GetCenter() - (this.Size / 2);
        this.PositionCondition = ImGuiCond.Appearing;

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    private bool understood = false;

    public unsafe override void Draw()
    {
        ImGui.Text("Welcome to RTyping");
        ImGui.Separator();
        ImGui.TextWrapped("This Plugin adds configurable icon indicators for the typing status of others within the same party.\n\nTo be able to see and let someone see your typing status, you will both need to add eachother as a trusted character from any user context menu. This can be removed at any point from the same context menu, or from the Trusted List option found inside the Plugin's configuration window.\n\nKeep in mind that by trusting someone, regardless of if they trust you back, they will receive your typing status while in the same party, although it will not be shown on their end.\n*Make sure to only trust those who you actually do trust.*");
        ImGui.Spacing();
        ImGui.Checkbox("I understand", ref understood);
        if (!understood) ImGui.BeginDisabled();
        if (ImGui.Button("I really understand"))
        {
            Configuration.ShownConsentMenu = true;
            Configuration.Save();
            this.IsOpen = false;
        }

        if (!understood) ImGui.EndDisabled();
    }
}
