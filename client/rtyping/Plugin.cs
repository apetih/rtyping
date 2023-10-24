using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using rtyping.Windows;
using Dalamud.Game.Command;
using Dalamud.ContextMenu;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Internal;

namespace rtyping
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "RTyping";
        private const string CommandName = "/rtyping";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] public static IPartyList PartyList { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;

        public Configuration Configuration { get; init; }
        public DalamudContextMenu ContextMenu { get; init; }

        public Client Client { get; init; }
        public TypingManager TypingManager { get; init; }
        public PartyManager PartyManager { get; init; }
        public ContextMenuManager ContextMenuManager { get; init; }
        public WindowSystem WindowSystem = new("rtyping");
        public IpcController IPCController;
        public IDalamudTextureWrap TypingTexture;
        public IDalamudTextureWrap TypingNameplateTexture;

        public PartyTypingUI PartyTypingUI;
        public ConfigWindow ConfigWindow;
        public ConsentWindow ConsentWindow;
        public TrustedListWindow TrustedListWindow;
        public AddTrustedWindow AddTrustedWindow;

        public Plugin()
        {
            this.ContextMenu = new DalamudContextMenu(PluginInterface);

            TypingTexture = TextureProvider.GetTextureFromGame("ui/uld/charamake_dataimport.tex")!;
            TypingNameplateTexture = TextureProvider.GetIcon(61397)!;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            this.Client = new Client(this);
            this.TypingManager = new TypingManager(this);
            this.PartyManager = new PartyManager(this);
            this.ContextMenuManager = new ContextMenuManager(this);
            this.IPCController = new IpcController(this);

            PartyTypingUI = new PartyTypingUI(this);
            ConfigWindow = new ConfigWindow(this);
            ConsentWindow = new ConsentWindow(this);
            TrustedListWindow = new TrustedListWindow(this);
            AddTrustedWindow = new AddTrustedWindow(this);

            WindowSystem.AddWindow(PartyTypingUI);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(ConsentWindow);
            WindowSystem.AddWindow(TrustedListWindow);
            WindowSystem.AddWindow(AddTrustedWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens plugin configuration"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            PartyTypingUI.IsOpen = true;
        }

        private void OnCommand(string command, string args)
        {
            ConfigWindow.Toggle();
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            IPCController.Dispose();
            this.Client.Dispose();
            this.ContextMenuManager.Dispose();
            ContextMenu.Dispose();
            TypingManager.Dispose();
            CommandManager.RemoveHandler(CommandName);
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
            if (!this.Configuration.ShownConsentMenu) ConsentWindow.IsOpen = true;
        }

        public void DrawConfigUI()
        {
            ConfigWindow.Toggle();
        }
        public void DrawTrustedListUI()
        {
            TrustedListWindow.IsOpen = true;
        }

        public static string HashContentID(ulong cid)
        {
            var crypt = SHA256.Create();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes($"{cid}"));
            crypt.Clear();
            foreach (var cByte in crypto)
            {
                hash.Append(cByte.ToString("x2"));
            }
            return hash.ToString();
        }

    }
}
