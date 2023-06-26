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
        ImGuiWindowFlags.None)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(262, 246),
            MaximumSize = new Vector2(600, 600)
        };
        this.Size = new Vector2(262, 246);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    private bool understood = false;
    public unsafe override void Draw()
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
        var showHidden = this.Configuration.ShowOnlyWhenNameplateVisible;
        var altStyle = this.Configuration.NameplateMarkerStyle;
        var nameplateOpacity = this.Configuration.NameplateMarkerOpacity;
        var chatValue = this.Configuration.ServerChat;
        var trustAnyone = this.Configuration.TrustAnyone;
        if (ImGui.BeginTabBar("Config", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("General"))
            {
                if (ImGui.Checkbox("Show server status chat messages", ref chatValue))
                {
                    this.Configuration.ServerChat = chatValue;
                    this.Configuration.Save();
                }

                if (trustAnyone) ImGui.BeginDisabled();
                if (ImGui.Button("Manage Trusted Characters"))
                {
                    this.Plugin.DrawTrustedListUI();
                }
                if (trustAnyone) ImGui.EndDisabled();

                if (ImGui.Checkbox("Trust Anyone", ref trustAnyone))
                {
                    if (!this.Configuration.TrustAnyone && trustAnyone)
                    {
                        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter() - (new Vector2(340, 365) / 2), ImGuiCond.Appearing);
                        ImGui.SetNextWindowSize(new Vector2(340, 370));
                        ImGui.OpenPopup("Trust Anyone");
                    }
                    else
                    {
                        this.Configuration.TrustAnyone = trustAnyone;
                        this.Configuration.Save();
                    }
                }

                var unused_open = true;
                if (ImGui.BeginPopupModal("Trust Anyone", ref unused_open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.TextWrapped("You're about to disable the Trusted Characters feature.\n\nBy disabling it, you will be sending typing data from anyone using RTyping within your party, and will allow you to see the typing status of anyone that trusts you or anyone who also has disabled Trusted Characters, regardless of if you trust them or not. Others who have Trusted Characters enabled will still not see your typing status unless they mark you as trusted. While this may sound more convenient, it may also bring unwanted attention to yourself.\nYou will be unable to modify trusted characters while this option is enabled.\n\nMake sure you understand the risks involved before deciding to enable this feature.");
                    ImGui.Separator();
                    ImGui.Checkbox("I understand", ref understood);
                    if (ImGui.Button("Oh heck no, bring me back"))
                    {
                        understood = false;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (!understood) ImGui.BeginDisabled();
                    if (ImGui.Button("I really REALLY understand"))
                    {
                        this.Configuration.TrustAnyone = true;
                        this.Configuration.Save();
                        understood = false;
                        ImGui.CloseCurrentPopup();
                    }
                    if (!understood) ImGui.EndDisabled();
                    ImGui.EndPopup();
                }

                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Party"))
            {
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

                if (ImGui.Checkbox("Hide marker if nameplate not visible", ref showHidden))
                {
                    this.Configuration.ShowOnlyWhenNameplateVisible = showHidden;
                    this.Configuration.Save();
                }
                ImGui.Text("Nameplate Marker Position");
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
