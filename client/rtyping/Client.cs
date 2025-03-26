using System;
using System.Collections.Generic;
using System.Linq;

namespace rtyping
{

    public class Client : IDisposable
    {

        private Plugin Plugin;
        private SocketIOClient.SocketIO wsClient = new("wss://rtyping.apetih.dev:8443", new SocketIOClient.SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        private readonly string wsVer = "api12";

        internal enum State
        {
            Disconnected,
            Connected,
            Reconnecting,
            Error,
            Mismatch
        }

        internal State Status = State.Disconnected;

        public Client(Plugin plugin)
        {
            this.Plugin = plugin;

            Plugin.ClientState.Login += this.Login;
            Plugin.ClientState.Logout += this.Logout;


            wsClient.On("startTyping", OnStartTyping);
            wsClient.On("stopTyping", OnStopTyping);
            wsClient.On("mismatch", OnMismatch);

            wsClient.OnConnected += WsClient_OnConnected;
            wsClient.OnDisconnected += WsClient_OnDisconnected;
            wsClient.OnReconnectAttempt += WsClient_OnReconnectAttempt;
            wsClient.OnReconnectFailed += WsClient_OnReconnectFailed;

            if (Plugin.ClientState.IsLoggedIn)
            {
                Login();
            }
        }

        private void Login()
        {
            if (wsClient.Connected) return;
            Connect();
        }

        private void Logout(int type, int code)
        {
            if (!wsClient.Connected) return;
            Disconnect();
        }

        internal async void Connect()
        {
            wsClient.Options.Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("version", wsVer),
                    new KeyValuePair<string, string>("ContentID", Plugin.HashContentID(Plugin.ClientState.LocalContentId))
                };
            try
            {
                await wsClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error("Unable to connect to RTyping server.");
            }
        }
        internal async void Disconnect()
        {
            if (!wsClient.Connected) return;
            await wsClient.DisconnectAsync();
            Plugin.TypingManager.TypingList.Clear();
        }

        public async void EmitStartTyping(string Service, List<string> Party)
        {
            await wsClient.EmitAsync("startTyping", Service, Party);
        }

        public async void EmitStopTyping(string Service, List<string> Party)
        {
            await wsClient.EmitAsync("stopTyping", Service, Party);
        }

        private void OnStartTyping(SocketIOClient.SocketIOResponse Message)
        {
            var Service = Message.GetValue<string>();
            var ContentID = Message.GetValue<string>(1);
            if (Service != "rtyping")
            {
                //Handle IPC
                Plugin.IPCController.SendOnTypingReceive(Service, ContentID, true);
                if (Service != "all") return;
            }
            if (!Plugin.TypingManager.TypingList.ContainsKey(ContentID))
            {
                Plugin.TypingManager.TypingList.Add(ContentID, 0);
            }
            Plugin.TypingManager.TypingList[ContentID] = Environment.TickCount64 + 20000;
        }

        private void OnStopTyping(SocketIOClient.SocketIOResponse Message)
        {
            var Service = Message.GetValue<string>();
            var ContentID = Message.GetValue<string>(1);
            if (Service != "rtyping")
            {
                //Handle IPC
                Plugin.IPCController.SendOnTypingReceive(Service, ContentID, false);
                if (Service != "all") return;
            }
            if (!Plugin.TypingManager.TypingList.ContainsKey(ContentID)) return;
            Plugin.TypingManager.TypingList.Remove(ContentID);
        }

        private void OnMismatch(SocketIOClient.SocketIOResponse Message)
        {
            Plugin.ChatGui.PrintError("Connection to RTyping Server denied. Plugin version does not match.", "RTyping", 16);
            Status = State.Mismatch;
            Logout(0, 0);
        }

        private void WsClient_OnConnected(object? sender, EventArgs e)
        {
            Status = State.Connected;
            if (Plugin.Configuration.ServerChat) Plugin.ChatGui.Print("Connection successful to RTyping Server.", "RTyping", 60);
            if (Plugin.TypingManager.SelfTyping)
            {
                var partyList = Plugin.Configuration.TrustAnyone ? Plugin.PartyManager.BuildPartyDictionary().Keys.ToList<string>() : Plugin.PartyManager.BuildTrustedPartyDictionary().Keys.ToList<string>();
                if (partyList.Count > 0)
                    EmitStartTyping("rtyping", partyList);
            }
        }

        private void WsClient_OnDisconnected(object? sender, string e)
        {
            if (Status != State.Mismatch) Status = State.Disconnected;
            if (Plugin.Configuration.ServerChat) Plugin.ChatGui.PrintError("Disconnected from RTyping Server.", "RTyping", 16);
        }

        private void WsClient_OnReconnectAttempt(object? sender, int e)
        {
            Status = State.Reconnecting;
            //This would spam chat too much, only here in case I limit reconnection attempts.
            //if (Plugin.Configuration.ServerChat) Plugin.ChatGui.Print("Attempting to reconnect.", "RTyping", 9);
        }

        private void WsClient_OnReconnectFailed(object? sender, EventArgs e)
        {
            Status = State.Error;
            //Same as above.
            //Plugin.ChatGui.PrintError("Failed to reconnect. Please wait a bit before attempting to reconnect.", "RTyping", 16);
        }

        public async void Dispose()
        {
            Plugin.ClientState.Login -= this.Login;
            Plugin.ClientState.Logout -= this.Logout;
            if (Status == State.Connected || Status == State.Reconnecting) await wsClient.DisconnectAsync();
            wsClient.Dispose();
        }
    }
}
