using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
namespace rtyping.Windows;

public class PartyTypingUI : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private GameGui GameGui;

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

        this.Plugin = plugin;
        this.Configuration = plugin.Configuration;
        this.GameGui = gameGui;
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

        if ((IntPtr)memberNode == IntPtr.Zero) return;
        if (!memberNode->AtkResNode.IsVisible) return;

        var iconNode = memberNode->Component->UldManager.NodeListCount > 4 ? memberNode->Component->UldManager.NodeList[4] : (AtkResNode*)IntPtr.Zero;

        if ((IntPtr)iconNode == IntPtr.Zero) return;

        var iconOffset = (new Vector2(-14, 8)) * partyList->Scale;
        var iconSize = new Vector2(iconNode->Width / 2, iconNode->Height / 2) * partyList->Scale;
        var iconPos = new Vector2(partyList->X + memberNode->AtkResNode.X * partyList->Scale + iconNode->X * partyList->Scale + iconNode->Width * partyList->Scale / 2, partyList->Y + (memberNode->AtkResNode.Y * partyList->Scale) + iconNode->Y * partyList->Scale + iconNode->Height * partyList->Scale / 2);
        iconPos += iconOffset;

        ImGui.GetWindowDrawList().AddImage(Plugin.TypingTexture.ImGuiHandle, iconPos, iconPos + iconSize, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, this.Configuration.PartyMarkerOpacity)));
    }

    private unsafe void DrawPartyMemberNameplateTyping(ulong cid)
    {
        var ui3DModule = Framework.Instance()->GetUiModule()->GetUI3DModule();
        var oid = GetObjectIDFromContentID(cid);

        if (cid == Plugin.ClientState.LocalContentId && Plugin.ClientState.LocalPlayer != null) oid = Plugin.ClientState.LocalPlayer.ObjectId;
        if (oid == null) return;

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

    private unsafe uint? GetObjectIDFromContentID(ulong cid)
    {
        uint? result = null;
        var manager = (GroupManager*)Plugin.PartyList.GroupManagerAddress;

        for (var i = 0; i < manager->MemberCount; i++)
        {
            var member = manager->GetPartyMemberByIndex(i);
            if ((ulong)member->ContentID != cid) continue;
            result = member->ObjectID;
            break;
        }

        return result;
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

    private unsafe string ObtainPartyMembers()
    {
        var MemberIDs = new List<ulong>();

        var manager = (GroupManager*)Plugin.PartyList.GroupManagerAddress;
        for (var i = 0; i < manager->MemberCount; i++)
        {
            var member = manager->GetPartyMemberByIndex(i);
            MemberIDs.Add((ulong)member->ContentID);
        }

        return string.Join(",", MemberIDs.ToArray());
    }

    private unsafe IDictionary<ulong, int> BuildPartyIndex()
    {
        var partyListLocation = new Dictionary<ulong, int>();
        var agentHud = Framework.Instance()->UIModule->GetAgentModule()->GetAgentHUD();
        var list = (HudPartyMember*)agentHud->PartyMemberList;
        for (var i = 0; i < (short)agentHud->PartyMemberCount; i++)
        {
            partyListLocation.Add(list[i].ContentId, i);
        }
        return partyListLocation;
    }

    private unsafe void DrawPartyTypingStatus()
    {
        var agentHud = Framework.Instance()->UIModule->GetAgentModule()->GetAgentHUD();
        var partyListLocation = BuildPartyIndex();
        var list = (HudPartyMember*)agentHud->PartyMemberList;
        for (var i = 0; i < (short)agentHud->PartyMemberCount; i++)
        {
            var cid = list[i].ContentId;
            if (Plugin.TypingList.Contains(cid))
            {
                DrawPartyMemberTyping(partyListLocation[cid]);
                if (this.Configuration.DisplayOthersNamePlateMarker)
                    DrawPartyMemberNameplateTyping(cid);

            }
        }
    }

    private bool wasTyping = false;

    public override void Draw()
    {
        var typing = DetectTyping();
        string party;

        if (typing)
        {
            if (!wasTyping)
            {
                wasTyping = true;
                party = ObtainPartyMembers();
                if (party != "" && !this.Plugin.Client.IsDisposed) Plugin.Client.SendTyping(party);
            }
            if (this.Configuration.DisplaySelfMarker)
                DrawPartyMemberTyping(0);
            if (this.Configuration.DisplaySelfNamePlateMarker)
                DrawPartyMemberNameplateTyping(Plugin.ClientState.LocalContentId);
        }
        else
        {
            if (wasTyping)
            {
                wasTyping = false;
                party = ObtainPartyMembers();
                if (party != "" && !this.Plugin.Client.IsDisposed) Plugin.Client.SendStoppedTyping(party);
            }
        }

        DrawPartyTypingStatus();
    }
}
