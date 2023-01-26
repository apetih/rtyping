using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using rtyping.Windows;
using Dalamud.Game.Gui;
using Dalamud.Data;
using ImGuiScene;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.ContextMenu;

namespace rtyping
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "RTyping";
        private const string CommandName = "/rtyping";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public GameGui GameGui { get; init; }
        public DataManager DataManager { get; init; }
        public Configuration Configuration { get; init; }
        public DalamudContextMenu ContextMenu { get; init; }
        [PluginService] public PartyList PartyList { get; init; }
        [PluginService] public ClientState ClientState { get; init; }
        [PluginService] public ChatGui ChatGui { get; init; }
        public Client Client { get; init; }
        public ContextMenuManager ContextMenuManager { get; init; }
        public WindowSystem WindowSystem = new("rtyping");
        public TextureWrap TypingTexture;
        public TextureWrap TypingNameplateTexture;
        public List<ulong> TypingList;

        public Plugin(
            DalamudPluginInterface pluginInterface,
            CommandManager commandManager,
            GameGui gameGui,
            ChatGui chatGui,
            DataManager dataManager,
            PartyList partyList,
            ClientState clientState)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.DataManager = dataManager;
            this.PartyList = partyList;
            this.ClientState = clientState;
            this.ContextMenu = new DalamudContextMenu();

            this.TypingList = new List<ulong>();
            TypingTexture = DataManager.GetImGuiTexture("ui/uld/charamake_dataimport.tex");
            TypingNameplateTexture = DataManager.GetImGuiTextureIcon(61397);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.Client = new Client(this);
            this.ContextMenuManager = new ContextMenuManager(this);

            WindowSystem.AddWindow(new PartyTypingUI(this, GameGui));
            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new ConsentWindow(this));
            WindowSystem.AddWindow(new TrustedListWindow(this));

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens plugin configuration"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            WindowSystem.GetWindow("PartyTypingStatus").IsOpen = true;
        }

        private void OnCommand(string command, string args)
        {
            WindowSystem.GetWindow("RTyping Configuration").IsOpen = !WindowSystem.GetWindow("RTyping Configuration").IsOpen;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.Client.Dispose();
            this.ContextMenuManager.Dispose();
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
            if(!this.Configuration.ShownConsentMenu) WindowSystem.GetWindow("RTyping Welcome").IsOpen = true;
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("RTyping Configuration").IsOpen = true;
        }
        public void DrawTrustedListUI()
        {
            WindowSystem.GetWindow("Trusted Characters").IsOpen = true;
        }
    }
}
