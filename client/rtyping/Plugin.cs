using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using rtyping.Windows;
using Dalamud.Game.Command;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Plugin.Services;
using rtyping.Models;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace rtyping
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "RTyping";
        private const string CommandName = "/rtyping";
        private const string CommandNameConfig = "/rtyping config";

        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IGameGui GameGui { get; private set; } = null!;
        [PluginService] public static IDataManager DataManager { get; private set; } = null!;
        [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] public static IPartyList PartyList { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static IContextMenu ContextMenu { get; private set; } = null!;
        [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
        [PluginService] public static INotificationManager NotificationManager { get; private set; } = null!;

        public Configuration Configuration { get; init; }

        public Client Client { get; init; }
        public TypingManager TypingManager { get; init; }
        public PartyManager PartyManager { get; init; }
        public ContextMenuManager ContextMenuManager { get; init; }
        public WindowSystem WindowSystem = new("rtyping");
        public IpcController IPCController;

        public PartyTypingUI PartyTypingUI;
        public ConfigWindow ConfigWindow;
        public ConsentWindow ConsentWindow;
        public AddTrustedWindow AddTrustedWindow;
        public MainWindow MainWindow;
        public TrustedCharacterViewWindow TrustedCharacterViewWindow;

        public IDictionary<String, uint> Worlds;

        public TrustedCharacterContext TrustedCharacterDb;

        public Plugin()
        {

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            this.Client = new Client(this);
            this.TypingManager = new TypingManager(this);
            this.PartyManager = new PartyManager(this);
            this.ContextMenuManager = new ContextMenuManager(this);
            this.IPCController = new IpcController(this);

            this.TrustedCharacterDb = new TrustedCharacterContext(PluginInterface.GetPluginConfigDirectory() + Path.DirectorySeparatorChar);

            Worlds = DataManager.GetExcelSheet<World>().Where(w => w.IsPublic && w.DataCenter.RowId != 0 && !w.Name.IsEmpty).OrderBy(w => w.Name.ToString()).ToDictionary(w => w.Name.ExtractText(), w => w.RowId);

            PartyTypingUI = new PartyTypingUI(this);
            ConfigWindow = new ConfigWindow(this);
            ConsentWindow = new ConsentWindow(this);
            AddTrustedWindow = new AddTrustedWindow(this);
            MainWindow = new MainWindow(this);
            TrustedCharacterViewWindow = new TrustedCharacterViewWindow(this);

            WindowSystem.AddWindow(PartyTypingUI);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(ConsentWindow);
            WindowSystem.AddWindow(AddTrustedWindow);
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(TrustedCharacterViewWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens main plugin window"
            }); 
            CommandManager.AddHandler(CommandNameConfig, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens plugin configuration window"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            PartyTypingUI.IsOpen = true;

            if (Configuration.Version < 1) MigrateTrusted();
        }

        private void OnCommand(string command, string args)
        {
            if (command == "/rtyping")
            {
                if (args.Contains("config", StringComparison.CurrentCultureIgnoreCase)) ConfigWindow.Toggle();
                else MainWindow.Toggle();
            }
            
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            IPCController.Dispose();
            this.Client.Dispose();
            this.ContextMenuManager.Dispose();
            TypingManager.Dispose();
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(CommandNameConfig);
            this.TrustedCharacterDb.Dispose();
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
            if (!this.Configuration.ShownConsentMenu) ConsentWindow.IsOpen = true;
        }

        public void DrawMainUI()
        {
            MainWindow.Toggle();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.Toggle();
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

        private void MigrateTrusted()
        {
            for (var i = 0; i < Configuration.TrustedCharacters.Count; i++)
            {
                var TargetCharacter = Configuration.TrustedCharacters[i];
                var CharacterName = TargetCharacter.Split('@')[0];
                var CharacterWorld = TargetCharacter.Split('@')[1];
                TrustedCharacterDb.Add(new TrustedCharacter
                {
                    CharacterName = CharacterName,
                    WorldId = uint.Parse(CharacterWorld),
                    AddedAt = DateTime.Now,
                    DisplayNameplate = Configuration.DefaultDisplayNameplate,
                    DisplayParty = Configuration.DefaultDisplayParty,
                    NameplateStyle = Configuration.DefaultNameplateStyle,
                    ReceivePartyless = Configuration.DefaultReceivePartyless,
                    SendPartyless = Configuration.DefaultSendPartyless,
                    SendTypingStatus = Configuration.DefaultSendTypingStatus
                });
            }
            TrustedCharacterDb.SaveChanges();

            Configuration.Version = 1;
            Configuration.Save();
        }

    }
}
