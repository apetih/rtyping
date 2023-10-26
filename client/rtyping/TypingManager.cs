using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace rtyping
{
    public class TypingManager : IDisposable
    {
        private Plugin Plugin;
        public Dictionary<string, long> TypingList = new();
        public bool SelfTyping = false;
        public bool IPCTyping = false;

        private string ChatString = "";
        private bool WasTyping = false;
        private long ResendDelay = 0;
        private int PreviousParty = 0;

        public TypingManager(Plugin plugin)
        {
            this.Plugin = plugin;
            Plugin.Framework.Update += DetectTyping;
            Plugin.Framework.Update += PartyResend;
            Plugin.Framework.Update += TypingListManage;
            Plugin.ClientState.Logout += ClientState_Logout;
        }

        private void TypingListManage(IFramework framework)
        {
            if (TypingList.Count == 0) return;
            foreach (var member in TypingList)
            {
                if (member.Value < Environment.TickCount64) TypingList.Remove(member.Key);
            }
        }

        private void ClientState_Logout()
        {
            SelfTyping = false;
            ChatString = "";
            WasTyping = false;
            ResendDelay = 0;
        }

        private void PartyResend(IFramework framework)
        {
            if (!Plugin.ClientState.IsLoggedIn) return;
            if (!SelfTyping) return;
            var PartyCount = Plugin.PartyManager.GetPartyMemberCount();
            if (PartyCount != PreviousParty)
            {
                PreviousParty = PartyCount;
                if (PartyCount == 0) return;

                if (Plugin.Client.Status == Client.State.Connected)
                {
                    var partyList = Plugin.Configuration.TrustAnyone ? Plugin.PartyManager.BuildPartyDictionary().Keys.ToList<string>() : Plugin.PartyManager.BuildTrustedPartyDictionary().Keys.ToList<string>();
                    if (partyList.Count > 0)
                        Plugin.Client.EmitStopTyping("rtyping", partyList);
                }
            }
        }

        private void DetectTyping(IFramework framework)
        {
            if (!Plugin.ClientState.IsLoggedIn) return;
            List<string> partyList;

            if (!IPCTyping && (!DetectCursor() || GetChatString() == ""))
            {
                if (!SelfTyping) return;
                SelfTyping = false;
                WasTyping = false;
                ChatString = "";
                ResendDelay = 0;
                if (Plugin.Client.Status == Client.State.Connected)
                {
                    partyList = Plugin.Configuration.TrustAnyone ? Plugin.PartyManager.BuildPartyDictionary().Keys.ToList<string>() : Plugin.PartyManager.BuildTrustedPartyDictionary().Keys.ToList<string>();
                    if (partyList.Count > 0)
                        Plugin.Client.EmitStopTyping("rtyping", partyList);
                }
                return;
            }

            if (!WasTyping)
            {
                SelfTyping = true;
                WasTyping = true;
                ChatString = GetChatString();
                ResendDelay = Environment.TickCount64 + 15000;
                if (Plugin.Client.Status == Client.State.Connected)
                {
                    partyList = Plugin.Configuration.TrustAnyone ? Plugin.PartyManager.BuildPartyDictionary().Keys.ToList<string>() : Plugin.PartyManager.BuildTrustedPartyDictionary().Keys.ToList<string>();
                    if (partyList.Count > 0)
                        Plugin.Client.EmitStartTyping("rtyping", partyList);
                }
                return;
            }

            if (Environment.TickCount64 < ResendDelay) return;
            if (IPCTyping) return;

            var currentChat = GetChatString();

            if (ChatString == currentChat)
            {
                SelfTyping = false;
                ResendDelay = Environment.TickCount64 + 1000;
                return;
            }

            SelfTyping = true;
            ChatString = currentChat;
            ResendDelay = Environment.TickCount64 + 15000;

            if (Plugin.Client.Status == Client.State.Connected)
            {
                partyList = Plugin.Configuration.TrustAnyone ? Plugin.PartyManager.BuildPartyDictionary().Keys.ToList<string>() : Plugin.PartyManager.BuildTrustedPartyDictionary().Keys.ToList<string>();
                if (partyList.Count > 0)
                    Plugin.Client.EmitStartTyping("rtyping", partyList);
            }
        }

        private unsafe bool DetectCursor()
        {
            var chatlog = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("ChatLog", 1);

            if (chatlog == null) return false;
            if (!chatlog->IsVisible) return false;

            var textInput = chatlog->UldManager.NodeList[15];
            var chatCursor = textInput->GetAsAtkComponentNode()->Component->UldManager.NodeList[14];

            if (!chatCursor->IsVisible) return false;
            return true;
        }

        private unsafe string GetChatString()
        {
            var chatlog = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("ChatLog", 1);

            if (chatlog == null) return "";
            if (!chatlog->IsVisible) return "";
            var textInput = chatlog->UldManager.NodeList[15];
            var chatInput = textInput->GetComponent()->UldManager.NodeList[1];

            var chatText = chatInput->GetAsAtkTextNode()->GetText();

            return MemoryHelper.ReadSeStringNullTerminated((nint)chatText).ToString();
        }

        public void Dispose()
        {
            Plugin.Framework.Update -= DetectTyping;
            Plugin.Framework.Update -= PartyResend;
            Plugin.ClientState.Logout -= ClientState_Logout;
            TypingList.Clear();
        }
    }
}
