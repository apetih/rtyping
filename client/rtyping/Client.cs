using Dalamud.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

using WatsonWebsocket;
using Newtonsoft.Json;
using Dalamud.Game.Text;

namespace rtyping
{

    class Message
    {
        public int Command;
        public string Content;

        public Message(int command, string content)
        {
            this.Command = command;
            this.Content = content;
        }
    }

    public class Client : IDisposable
    {
        internal enum State
        {
            Disconnected,
            Connecting,
            Error,
            Connected
        }
        private Plugin Plugin { get; }
        private WatsonWsClient wsClient;
        private bool active;
        private string Identifier;
        internal State _status { get; private set; } = State.Disconnected;
        private bool disposed = false;

        private readonly string wsVer = "rplogonTrusted";

        public bool IsConnected => this.wsClient.Connected;
        public bool IsDisposed => this.disposed;
        public Client(Plugin plugin)
        {
            this.Plugin = plugin;

            this.wsClient = new WatsonWsClient("apetih.com", 8443, true);
            this.wsClient.ServerConnected += WsClient_ServerConnected;
            this.wsClient.ServerDisconnected += WsClient_ServerDisconnected;
            this.wsClient.MessageReceived += WsClient_MessageReceived;

            this.Plugin.ClientState.Login += this.Login;
            this.Plugin.ClientState.Logout += this.Logout;

            if (this.Plugin.ClientState.IsLoggedIn)
            {
                this.active = true;
                this.Connect();
            }
        }

        private async Task Connect()
        {
            if (this.Plugin.ClientState.LocalPlayer == null)
            {
                await Task.Delay(3000);
                Connect();
                return;
            }

            this.Identifier = $"{this.Plugin.ClientState.LocalContentId}";

            if (!this.active) return;
            if (this._status == State.Connected || this._status == State.Error) return;

            this._status = State.Connecting;

            await this.wsClient.StartWithTimeoutAsync(30);

            if (!this.wsClient.Connected) await Task.Run(async () =>
            {
                await Task.Delay(3000);
                Connect();
            });
        }

        private void Login(object? sender, EventArgs e)
        {
            this.active = true;
            this.Connect();
        }
        private void Logout(object? sender, EventArgs e)
        {
            this.active = false;
            this._status = State.Disconnected;
            if (this.wsClient.Connected) this.wsClient.Stop();
        }
        public void SendTyping(string Party)
        {
            if (!this.active || !this.wsClient.Connected) return;
            wsClient.SendAsync(JsonConvert.SerializeObject(new Message(1, Party)));
        }
        public void SendStoppedTyping(string Party)
        {
            if (!this.active || !this.wsClient.Connected) return;
            wsClient.SendAsync(JsonConvert.SerializeObject(new Message(2, Party)));
        }
        private void WsClient_MessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            var message = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(e.Data));
            if (message == null) return;
            string[] objectIDs;
            switch (message.Command)
            {
                case 0: // Identify socket
                    wsClient.SendAsync(JsonConvert.SerializeObject(new Message(0, this.Identifier)));
                    break;

                case 1: // Typing
                    objectIDs = message.Content.Split(",");
                    for (var i = 0; i < objectIDs.Length; i++)
                        if (!Plugin.TypingList.Contains(ulong.Parse(objectIDs[i]))) Plugin.TypingList.Add(ulong.Parse(objectIDs[i]));
                    break;

                case 2: // Stopped Typing
                    objectIDs = message.Content.Split(",");
                    for (var i = 0; i < objectIDs.Length; i++)
                        if (Plugin.TypingList.Contains(ulong.Parse(objectIDs[i]))) Plugin.TypingList.Remove(ulong.Parse(objectIDs[i]));
                    break;

                case 3: // Verify server version
                    wsClient.SendAsync(JsonConvert.SerializeObject(new Message(3, this.wsVer)));
                    break;

                case 4: // Server version mismatch
                    this.active = false;
                    this._status = State.Error;
                    this.Dispose();
                    this.Plugin.ChatGui.PrintChat(new XivChatEntry
                    {
                        Message = "Connection to RTyping Server denied. Plugin version does not match.",
                        Type = XivChatType.Urgent,
                    });
                    break;
            }
        }

        private void WsClient_ServerDisconnected(object? sender, EventArgs e)
        {
            Plugin.TypingList.Clear();

            if (this.active)
            {
                if (this.Plugin.Configuration.ServerChat) this.Plugin.ChatGui.PrintChat(new XivChatEntry
                {
                    Message = "Lost connection to RTyping Server. Attempting to reconnect.",
                    Type = XivChatType.Urgent,
                });
                this._status = State.Disconnected;
                Connect();
            }
            else
                if (this._status != State.Error) this._status = State.Disconnected;
        }

        private void WsClient_ServerConnected(object? sender, EventArgs e)
        {
            this._status = State.Connected;
            Plugin.TypingList.Clear();
            if (this.Plugin.Configuration.ServerChat) this.Plugin.ChatGui.PrintChat(new XivChatEntry
            {
                Message = "Connection successful to RTyping Server.",
                Type = XivChatType.Notice,
            });
        }

        public void Dispose()
        {
            if (this.IsDisposed) return;
            this.disposed = true;
            this.active = false;
            this.Plugin.ClientState.Login -= this.Login;
            this.Plugin.ClientState.Logout -= this.Logout;
            wsClient.Dispose();
        }
    }
}
