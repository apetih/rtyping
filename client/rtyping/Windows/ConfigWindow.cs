using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace rtyping.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    public ConfigWindow(Plugin plugin) : base(
        "RTyping Configuration",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(262, 220);
        this.SizeCondition = ImGuiCond.Always;
        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Server Status: ");
        ImGui.SameLine();
        switch (Plugin.Client._status)
        {
            case Client.State.Disconnected:
                ImGui.TextColored(new Vector4(0.4f, 0.4f, 0.4f, 1.0f), "Disconnected.");
                break;

            case Client.State.Connecting:
                ImGui.TextColored(new Vector4(0.0f, 0.88f, 0.88f, 1.0f), "Connecting...");
                break;

            case Client.State.Error:
                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Error.");
                break;

            case Client.State.Connected:
                ImGui.TextColored(new Vector4(0.0f, 0.88f, 0.0f, 1.0f), "Connected.");
                break;
        }

        ImGui.Spacing();
        var selfMarkerValue = this.Configuration.DisplaySelfMarker;
        var partyOpacity = this.Configuration.PartyMarkerOpacity;
        var selfNamePlateValue = this.Configuration.DisplaySelfNamePlateMarker;
        var othersNamePlateValue = this.Configuration.DisplayOthersNamePlateMarker;
        var altStyle = this.Configuration.NameplateMarkerStyle;
        var nameplateOpacity = this.Configuration.NameplateMarkerOpacity;
        var chatValue = this.Configuration.ServerChat;
        if (ImGui.BeginTabBar("Config", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("General"))
            {
                this.Size = new Vector2(262, 120);
                if (ImGui.Checkbox("Show server status chat messages", ref chatValue))
                {
                    this.Configuration.ServerChat = chatValue;
                    this.Configuration.Save();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Party"))
            {
                this.Size = new Vector2(262, 148);
                ImGui.SliderFloat("Opacity", ref partyOpacity, 0.5f, 1.0f, "%.1f");
                if (partyOpacity != this.Configuration.PartyMarkerOpacity)
                {

                    this.Configuration.PartyMarkerOpacity = partyOpacity;
                    this.Configuration.Save();
                }
                if (ImGui.Checkbox("Display typing marker on self", ref selfMarkerValue))
                {
                    this.Configuration.DisplaySelfMarker = selfMarkerValue;
                    this.Configuration.Save();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Nameplate"))
            {
                this.Size = new Vector2(262, 220);
                ImGui.SliderFloat("Opacity", ref nameplateOpacity, 0.2f, 1.0f, "%.1f");
                if (nameplateOpacity != this.Configuration.NameplateMarkerOpacity)
                {

                    this.Configuration.NameplateMarkerOpacity = nameplateOpacity;
                    this.Configuration.Save();
                }

                if (ImGui.Checkbox("Display nameplate marker on self", ref selfNamePlateValue))
                {
                    this.Configuration.DisplaySelfNamePlateMarker = selfNamePlateValue;
                    this.Configuration.Save();
                }

                if (ImGui.Checkbox("Display nameplate marker on others", ref othersNamePlateValue))
                {
                    this.Configuration.DisplayOthersNamePlateMarker = othersNamePlateValue;
                    this.Configuration.Save();
                }
                ImGui.Text("Nameplay Marker Position");
                ImGui.RadioButton("Side", ref altStyle, 0); ImGui.SameLine();
                ImGui.RadioButton("Top", ref altStyle, 1);
                if (altStyle != this.Configuration.NameplateMarkerStyle)
                {

                    this.Configuration.NameplateMarkerStyle = altStyle;
                    this.Configuration.Save();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        

    }
}
