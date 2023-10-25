using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;

namespace rtyping
{
    public class IpcController : IDisposable
    {
        private Plugin Plugin;

        /**
         * Returns whether the client is connected to the server or not.
         * */
        private ICallGateProvider<bool> isConnected;

        /**
         * Returns whether the client is detected as typing.
         * */
        private ICallGateProvider<bool> getSelfTypingStatus;


        /**
         * Sets whether the client is typing.
         * The service calling this is responsible of handling typing status timeout.
         * */
        private ICallGateProvider<bool, bool> setSelfTypingStatus;

        /**
         * Returns whether the specified Party Member is typing.
         * Index is Party Index order.
         * */
        private ICallGateProvider<int, bool> getPartyMemberTypingStatusByIndex;


        /**
         * Returns whether the specified Party Member is typing.
         * ContentID is unhashed, RTyping will take care of hashing it.
         * */
        private ICallGateProvider<ulong, bool> getPartyMemberTypingStatusByCid;

        /**
         * Sends whether the client is typing to specific Party Member via a service name.
         * Service name will be returned via onTypingReceive if subscribed.
         * Might be useful for private messaging, as it is sent only between two clients.
         * Index is Party Index order.
         * */
        private ICallGateProvider<string, int, bool, bool> sendTypingByIndex;

        /**
         * Sends whether the client is typing to specific Party Member via a service name.
         * Service name will be returned via onTypingReceive if subscribed.
         * Might be useful for private messaging, as it is sent only between two clients.
         * ContentID is unhashed, RTyping will take care of hashing it.
         * */
        private ICallGateProvider<string, ulong, bool, bool> sendTypingByContentId;

        /**
         * Sends message once client receives a typing packet from the server via a service name.
         * Service name is the one specified via sendTypingByIndex or sendTypingByContentId.
         * It may send unwanted packages from different service names, check for the ones you want.
         * A service of "all" is used when a client disconnects.
         * ContentID has been hashed.
         * */
        private ICallGateProvider<string, string, bool, bool> onTypingReceive;

        /**
         * Returns whether the specified Party Member is trusted.
         * Index is Party Index order.
         * */
        private ICallGateProvider<int, bool> isPartyMemberTrusted;

        /**
         * Returns whether the specified Character is trusted.
         * String is CharacterName@World
         * World is uint
         * */
        private ICallGateProvider<string, bool> isCharacterTrusted;

        /**
         * Returns whether Trust Anyone setting is enabled.
         * */
        private ICallGateProvider<bool> isTrustAnyoneEnabled;

        public IpcController(Plugin plugin)
        {
            Plugin = plugin;

            getSelfTypingStatus = Plugin.PluginInterface.GetIpcProvider<bool>("RTyping.Status.GetSelf");
            setSelfTypingStatus = Plugin.PluginInterface.GetIpcProvider<bool, bool>("RTyping.Status.SetSelf");
            getPartyMemberTypingStatusByIndex = Plugin.PluginInterface.GetIpcProvider<int, bool>("RTyping.Status.PartyMember.Index");
            getPartyMemberTypingStatusByCid = Plugin.PluginInterface.GetIpcProvider<ulong, bool>("RTyping.Status.PartyMember.ContentId");
            sendTypingByIndex = Plugin.PluginInterface.GetIpcProvider<string, int, bool, bool>("RTyping.Client.SendTyping.Index");
            sendTypingByContentId = Plugin.PluginInterface.GetIpcProvider<string, ulong, bool, bool>("RTyping.Client.SendTyping.ContentId");
            onTypingReceive = Plugin.PluginInterface.GetIpcProvider<string, string, bool, bool>("RTyping.Client.GetTyping");
            isPartyMemberTrusted = Plugin.PluginInterface.GetIpcProvider<int, bool>("RTyping.Trusted.PartyMember");
            isCharacterTrusted = Plugin.PluginInterface.GetIpcProvider<string, bool>("RTyping.Trusted.Character");
            isTrustAnyoneEnabled = Plugin.PluginInterface.GetIpcProvider<bool>("RTyping.Trusted.TrustAnyone");
            isConnected = Plugin.PluginInterface.GetIpcProvider<bool>("RTyping.Connected");

            getSelfTypingStatus.RegisterFunc(GetSelfTypingStatus);
            setSelfTypingStatus.RegisterFunc(SetSelfTypingStatus);
            getPartyMemberTypingStatusByIndex.RegisterFunc(GetPartyMemberTypingStatusByIndex);
            getPartyMemberTypingStatusByCid.RegisterFunc(GetPartyMemberTypingStatusByContentId);
            sendTypingByIndex.RegisterFunc(SendTypingByIndex);
            sendTypingByContentId.RegisterFunc(SendTypingByContentId);
            isPartyMemberTrusted.RegisterFunc(IsPartyMemberTrusted);
            isCharacterTrusted.RegisterFunc(IsCharacterTrusted);
            isTrustAnyoneEnabled.RegisterFunc(IsTrustAnyoneEnabled);
            isConnected.RegisterFunc(IsConnected);
        }

        public void SendOnTypingReceive(string Service, string HashedContentID, bool isTyping)
        {
            var party = Plugin.PartyManager.BuildTrustedPartyDictionary();
            if (!party.ContainsKey(HashedContentID)) return;
            onTypingReceive.SendMessage(Service, HashedContentID, isTyping);
        }

        private bool IsConnected() => Plugin.Client.Status == Client.State.Connected;
        private bool IsTrustAnyoneEnabled() => Plugin.Configuration.TrustAnyone;

        private bool GetSelfTypingStatus()
        {
            return Plugin.TypingManager.SelfTyping;
        }

        private bool SetSelfTypingStatus(bool typing)
        {
            Plugin.TypingManager.IPCTyping = typing;
            return typing;
        }

        private bool GetPartyMemberTypingStatusByIndex(int index)
        {
            if (Plugin.PartyManager.GetPartyMemberCount() <= index) return false;
            var partyMember = Plugin.PartyManager.GetMemberByIndex(index);
            return Plugin.TypingManager.TypingList.ContainsKey(Plugin.HashContentID(partyMember.ContentID));
        }

        private bool GetPartyMemberTypingStatusByContentId(ulong cid)
        {
            var pos = Plugin.PartyManager.GetMemberPosition(cid);
            if (pos < 0) return false;
            return GetPartyMemberTypingStatusByIndex(pos);
        }

        private bool SendTypingByIndex(string service, int index, bool isTyping)
        {
            if (Plugin.Client.Status != Client.State.Connected) return false;
            service = service.ToLower();
            if (service.Length > 12) service = service[..12];
            if (service == "rtyping" || service == "all" || service == "") return false;
            if (Plugin.PartyManager.GetPartyMemberCount() <= index) return false;
            var partyMember = Plugin.PartyManager.GetMemberByIndex(index);
            if (!Plugin.Configuration.TrustedCharacters.Contains($"{partyMember.Name}@{partyMember.World}") && !Plugin.Configuration.TrustAnyone) return false;
            if (isTyping) Plugin.Client.EmitStartTyping(service, new List<string>() { Plugin.HashContentID(partyMember.ContentID) });
            else Plugin.Client.EmitStopTyping(service, new List<string>() { Plugin.HashContentID(partyMember.ContentID) });
            return true;
        }

        private bool SendTypingByContentId(string service, ulong cid, bool isTyping)
        {
            var pos = Plugin.PartyManager.GetMemberPosition(cid);
            if (pos < 0) return false;
            return SendTypingByIndex(service, pos, isTyping);
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
            sendTypingByIndex?.UnregisterFunc();
            sendTypingByContentId?.UnregisterFunc();
            onTypingReceive?.UnregisterFunc();
            isPartyMemberTrusted?.UnregisterFunc();
            isCharacterTrusted?.UnregisterFunc();
            isTrustAnyoneEnabled?.UnregisterFunc();
            isConnected?.UnregisterFunc();
        }
    }
}
