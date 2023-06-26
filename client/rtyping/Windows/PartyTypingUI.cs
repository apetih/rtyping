using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
namespace rtyping.Windows;

public class PartyTypingUI : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private GameGui GameGui;
    private PartyManager PartyManager;

    public PartyTypingUI(Plugin plugin, GameGui gameGui) : base(
        "PartyTypingStatus",
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoMouseInputs |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoNav)
    {
        this.Position = ImGui.GetMainViewport().Pos;
        this.Size = ImGui.GetMainViewport().Size;
        this.SizeCondition = ImGuiCond.Always;
        this.ForceMainWindow = true;

        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
        this.GameGui = gameGui;
        this.PartyManager = plugin.PartyManager;
    }

    public void Dispose()
    {
    }

    private unsafe void DrawPartyMemberTyping(int memberIndex)
    {
        if (memberIndex < 0 || memberIndex > 7) return;

        var partyList = (AtkUnitBase*)GameGui.GetAddonByName("_PartyList", 1);
        var memberNodeIndex = 22 - memberIndex;

        if (partyList == null) return;
        if (!partyList->IsVisible) return;

        var memberNode = partyList->UldManager.NodeListCount > memberNodeIndex ? (AtkComponentNode*)partyList->UldManager.NodeList[memberNodeIndex] : (AtkComponentNode*)IntPtr.Zero;

        var partyAlign = partyList->UldManager.NodeList[3]->Y;

        if ((IntPtr)memberNode == IntPtr.Zero) return;
        if (!memberNode->AtkResNode.IsVisible) return;

        var iconNode = memberNode->Component->UldManager.NodeListCount > 4 ? memberNode->Component->UldManager.NodeList[4] : (AtkResNode*)IntPtr.Zero;

        if ((IntPtr)iconNode == IntPtr.Zero) return;

        var iconOffset = new Vector2(-14, 8) * partyList->Scale;
        var iconSize = new Vector2(iconNode->Width / 2, iconNode->Height / 2) * partyList->Scale;
        var iconPos = new Vector2(
            partyList->X + (memberNode->AtkResNode.X * partyList->Scale) + (iconNode->X * partyList->Scale) + (iconNode->Width * partyList->Scale / 2),
            partyList->Y + partyAlign + (memberNode->AtkResNode.Y * partyList->Scale) + (iconNode->Y * partyList->Scale) + (iconNode->Height * partyList->Scale / 2));
        iconPos += iconOffset;

        ImGui.GetWindowDrawList().AddImage(Plugin.TypingTexture.ImGuiHandle, iconPos, iconPos + iconSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, this.Configuration.PartyMarkerOpacity)));
    }

    private unsafe void DrawPartyMemberNameplateTyping(IDictionary<string, Member> party, string cid)
    {
        var ui3DModule = Framework.Instance()->GetUiModule()->GetUI3DModule();
        var oid = party[cid].ObjectID;

        if (cid == Plugin.HashContentID(Plugin.ClientState.LocalContentId) && Plugin.ClientState.LocalPlayer != null) oid = Plugin.ClientState.LocalPlayer.ObjectId;

        AddonNamePlate.NamePlateObject* npObj = null;
        var distance = 0;

        for (var i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++)
        {
            var objectInfo = ((UI3DModule.ObjectInfo**)ui3DModule->NamePlateObjectInfoPointerArray)[i];
            if (objectInfo->GameObject->ObjectID == oid)
            {
                if (objectInfo->GameObject->YalmDistanceFromPlayerX > 35) break;
                distance = objectInfo->GameObject->YalmDistanceFromPlayerX;
                var addonNamePlate = (AddonNamePlate*)GameGui.GetAddonByName("NamePlate", 1);
                npObj = &addonNamePlate->NamePlateObjectArray[objectInfo->NamePlateIndex];
                break;
            }
        }

        if (npObj != null)
        {
            var iconNode = npObj->RootNode->Component->UldManager.NodeList[0];

            if (!iconNode->IsVisible && this.Configuration.ShowOnlyWhenNameplateVisible) return;

            var iconOffset = new Vector2(distance / 1.5f, distance / 3f);
            var iconSize = new Vector2(40.0f * npObj->RootNode->AtkResNode.ScaleX, 40.0f * npObj->RootNode->AtkResNode.ScaleY);
            var iconPos = new Vector2(npObj->RootNode->AtkResNode.X + iconNode->X + iconNode->Width, npObj->RootNode->AtkResNode.Y + iconNode->Y);
            if (iconNode->Height == 24) iconOffset.Y -= 8.0f;

            if (this.Configuration.NameplateMarkerStyle == 1 || (!iconNode->IsVisible && !this.Configuration.ShowOnlyWhenNameplateVisible))
            {
                iconOffset.Y = -16.0f + (distance / 1f);
                iconSize = new Vector2((100.0f * npObj->RootNode->AtkResNode.ScaleX), (100.0f * npObj->RootNode->AtkResNode.ScaleY));
                iconPos = new Vector2(npObj->RootNode->AtkResNode.X + iconNode->X + (iconNode->Width / 4), npObj->RootNode->AtkResNode.Y);
                if (iconNode->Height == 24) iconOffset.Y += 16.0f;
                if (!iconNode->IsVisible && !this.Configuration.ShowOnlyWhenNameplateVisible) iconOffset.Y += 64.0f;
            }

            iconPos += iconOffset;

            ImGui.GetWindowDrawList().AddImage(Plugin.TypingNameplateTexture.ImGuiHandle, iconPos, iconPos + iconSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, this.Configuration.NameplateMarkerOpacity)));

        }
    }

    private unsafe bool DetectTyping()
    {
        var chatlog = (AtkUnitBase*)GameGui.GetAddonByName("ChatLog", 1);

        if (chatlog == null) return false;
        if (!chatlog->IsVisible) return false;

        var textInput = chatlog->UldManager.NodeList[15];
        var chatCursor = textInput->GetAsAtkComponentNode()->Component->UldManager.NodeList[14];

        if (!chatCursor->IsVisible) return false;
        return true;
    }

    private unsafe void DrawPartyTypingStatus(IDictionary<string, Member> party)
    {
        var trustedList = this.Plugin.Configuration.TrustedCharacters;
        var trustAnyone = this.Plugin.Configuration.TrustAnyone;

        foreach (var cid in party.Keys)
        {
            var member = party[cid];
            if (!trustedList.Contains($"{member.Name}@{member.World}") && !trustAnyone) continue;
            if (Plugin.TypingList.Contains(cid))
            {
                DrawPartyMemberTyping(member.Position);
                if (this.Configuration.DisplayOthersNamePlateMarker)
                    DrawPartyMemberNameplateTyping(party, cid);

            }
        }
    }

    private bool wasTyping = false;

    public override void Draw()
    {
        if (Plugin.ClientState.LocalPlayer == null) return;
        var typing = DetectTyping();
        var party = PartyManager.BuildPartyDictionary();

        if (typing)
        {
            if (!wasTyping)
            {
                wasTyping = true;
                if (string.Join(",", party.Keys) != "" && !this.Plugin.Client.IsDisposed) Plugin.Client.SendTyping(string.Join(",", party.Keys));
            }
            if (this.Configuration.DisplaySelfMarker)
                DrawPartyMemberTyping(0);
            if (this.Configuration.DisplaySelfNamePlateMarker)
                DrawPartyMemberNameplateTyping(party, Plugin.HashContentID(Plugin.ClientState.LocalContentId));
        }
        else
        {
            if (wasTyping)
            {
                wasTyping = false;
                if (string.Join(",", party.Keys) != "" && !this.Plugin.Client.IsDisposed) Plugin.Client.SendStoppedTyping(string.Join(",", party.Keys));
            }
        }

        DrawPartyTypingStatus(party);
    }
}
