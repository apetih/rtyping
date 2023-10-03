using Dalamud.Plugin.Ipc;
using System;

namespace rtyping
{
    public class IpcController : IDisposable
    {
        private Plugin Plugin;

        private ICallGateProvider<bool>? isReady;
        private ICallGateProvider<bool> getSelfTypingStatus;
        private ICallGateProvider<bool, bool> setSelfTypingStatus;
        private ICallGateProvider<int, bool> getPartyMemberTypingStatusByIndex;
        private ICallGateProvider<ulong, bool> getPartyMemberTypingStatusByCid;
        private ICallGateProvider<int, bool> isPartyMemberTrusted;
        private ICallGateProvider<string, bool> isCharacterTrusted;

        public IpcController(Plugin plugin)
        {
            Plugin = plugin;

            getSelfTypingStatus = Plugin.PluginInterface.GetIpcProvider<bool>("RTyping.Status.GetSelf");
            setSelfTypingStatus = Plugin.PluginInterface.GetIpcProvider<bool, bool>("RTyping.Status.SetSelf");
            getPartyMemberTypingStatusByIndex = Plugin.PluginInterface.GetIpcProvider<int, bool>("RTyping.Status.PartyMember.Index");
            getPartyMemberTypingStatusByCid = Plugin.PluginInterface.GetIpcProvider<ulong, bool>("RTyping.Status.PartyMember.ContentId");
            isPartyMemberTrusted = Plugin.PluginInterface.GetIpcProvider<int, bool>("RTyping.Trusted.PartyMember");
            isCharacterTrusted = Plugin.PluginInterface.GetIpcProvider<string, bool>("RTyping.Trusted.Character");
            isReady = Plugin.PluginInterface.GetIpcProvider<bool>("RTyping.Ready");


            getSelfTypingStatus.RegisterFunc(GetSelfTypingStatus);
            setSelfTypingStatus.RegisterFunc(SetSelfTypingStatus);
            getPartyMemberTypingStatusByIndex.RegisterFunc(GetPartyMemberTypingStatusByIndex);
            getPartyMemberTypingStatusByCid.RegisterFunc(GetPartyMemberTypingStatusByContentId);
            isPartyMemberTrusted.RegisterFunc(IsPartyMemberTrusted);
            isCharacterTrusted.RegisterFunc(IsCharacterTrusted);
            isReady.RegisterFunc(IsReady);
        }

        private bool IsReady() => Plugin.PartyTypingUI.IsOpen;

        private bool GetSelfTypingStatus()
        {
            return Plugin.IsTyping;
        }
        private bool SetSelfTypingStatus(bool typing)
        {
            Plugin.IpcTyping = typing;
            return typing;
        }

        private bool GetPartyMemberTypingStatusByIndex(int index)
        {
            if (Plugin.PartyManager.GetPartyMemberCount() <= index) return false;
            var partyMember = Plugin.PartyManager.GetMemberByIndex(index);
            if (Plugin.TypingList.Contains(Plugin.HashContentID(partyMember.ContentID))) return true;
            return false;
        }

        private bool GetPartyMemberTypingStatusByContentId(ulong cid)
        {
            var pos = Plugin.PartyManager.GetMemberPosition(cid);
            if (pos < 0) return false;
            return GetPartyMemberTypingStatusByIndex(pos);
        }

        private bool IsCharacterTrusted(string character) => Plugin.Configuration.TrustedCharacters.Contains(character);

        private bool IsPartyMemberTrusted(int index)
        {
            if (Plugin.PartyManager.GetPartyMemberCount() <= index) return false;
            var partyMember = Plugin.PartyManager.GetMemberByIndex(index);
            return IsCharacterTrusted($"{partyMember.Name}@{partyMember.World}");
        }

        public void Dispose()
        {
            getSelfTypingStatus?.UnregisterFunc();
            setSelfTypingStatus?.UnregisterFunc();
            getPartyMemberTypingStatusByIndex?.UnregisterFunc();
            getPartyMemberTypingStatusByCid?.UnregisterFunc();
            isPartyMemberTrusted?.UnregisterFunc();
            isCharacterTrusted?.UnregisterFunc();
            isReady?.UnregisterFunc();
        }
    }
}
