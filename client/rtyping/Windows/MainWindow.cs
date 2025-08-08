using Dalamud.Interface.Components;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Diagnostics;
using System.Numerics;
using Lumina.Excel.Sheets;
using System.Reflection;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace rtyping.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private readonly Vector4[] statusColors = [
            new Vector4(0.4f, 0.4f, 0.4f, 1.0f),
            new Vector4(0.0f, 0.88f, 0.0f, 1.0f),
            new Vector4(0.0f, 0.88f, 0.88f, 1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, 1.0f)
        ];
        public MainWindow(Plugin plugin) : base($"RTyping {Assembly.GetExecutingAssembly().GetName().Version}")
        {
            this.Plugin = plugin;
            this.SizeConstraints = new()
            {
                MinimumSize = new(270, 270),
                MaximumSize = new(400, 400)
            };
            this.AllowPinning = false;
            this.AllowClickthrough = false;
            this.TitleBarButtons.Add(new TitleBarButton { Icon = FontAwesomeIcon.Cog, Click = (a) => { this.Plugin.ConfigWindow.IsOpen = true; } });
        }

        public void Dispose()
        {

        }
        private string filter = "";
        private int selected = -1;

        public override void Draw()
        {
            var WindowPosition = ImGui.GetWindowPos();
            var WindowWidth = ImGui.GetWindowWidth();

            if (!Plugin.Configuration.HideKofi)
            {
                if (ImGuiComponents.IconButton("KoFi", FontAwesomeIcon.Coffee, new Vector4(1.0f, 0.35f, 0.37f, 1.0f)))
                    Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/apetih", UseShellExecute = true });
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Support me on Ko-Fi");

                ImGui.SameLine();
            }

            if (!Plugin.ClientState.IsLoggedIn || Plugin.Client.Status == Client.State.Mismatch || Plugin.Client.Status == Client.State.Reconnecting) ImGui.BeginDisabled();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.PowerOff, Plugin.Client.Status == Client.State.Connected ? new Vector4(1.0f, 0.13f, 0.13f, 1.0f) : new Vector4(0.13f, 0.8f, 0.13f, 1.0f)))
            {
                if (Plugin.Client.Status == Client.State.Connected)
                    Plugin.Client.Disconnect();
                else
                    Plugin.Client.Connect();
            }

            if (!Plugin.ClientState.IsLoggedIn || Plugin.Client.Status == Client.State.Mismatch || Plugin.Client.Status == Client.State.Reconnecting) ImGui.EndDisabled();

            ImGui.SameLine();

            ImGui.Text("Server Status: ");
            ImGui.SameLine();
            ImGui.TextColored(statusColors[(int)Plugin.Client.Status], $"{Plugin.Client.Status}.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (ImGuiComponents.IconButton("Add Trusted Character", FontAwesomeIcon.Plus, new Vector4(0.0f, 0.88f, 0.0f, 1.0f)))
            {
                Plugin.AddTrustedWindow.SetDefaults();
                Plugin.AddTrustedWindow.Position = WindowPosition + new Vector2(WindowWidth, 0);
                Plugin.AddTrustedWindow.IsOpen = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Add new Trusted Character");

            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetWindowContentRegionMax().X - ImGui.GetCursorPosX());
            ImGui.InputTextWithHint("", "Search Trusted Character by Name", ref filter, 22, ImGuiInputTextFlags.AutoSelectAll);
            ImGui.PopItemWidth();
            ImGui.Spacing();
            ImGui.BeginChild("Characters", new Vector2(0, 0), true, ImGuiWindowFlags.None);
            var TrustedList = filter == "" ?
                Plugin.TrustedCharacterDb.TrustedCharacters.OrderByDescending(c => c.AddedAt).ToArray() :
                Plugin.TrustedCharacterDb.TrustedCharacters.Where(c => EF.Functions.Like(c.CharacterName, $"%{filter}%")).OrderByDescending(c => c.AddedAt).ToArray();
            if (TrustedList.Length < 1)
            {
                ImGui.Text(filter == "" ? "No Trusted Characters found..." : "No matching characters found.");
                ImGui.EndChild();
                return;
            }
            var clipper = ImGui.ImGuiListClipper();
            clipper.Begin(TrustedList.Length);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var TargetCharacter = TrustedList[i];
                    var characterName = TargetCharacter.CharacterName;
                    var worldName = Plugin.DataManager.GetExcelSheet<World>().GetRow(TargetCharacter.WorldId).Name.ExtractText();
                    if (!characterName.Contains(filter, StringComparison.CurrentCultureIgnoreCase)) continue;
                    var displayName = $"{characterName}@{worldName}";
                    if (ImGui.Selectable($"{displayName}", selected == i))
                    {
                        Plugin.TrustedCharacterViewWindow.SetCharacter(TargetCharacter);
                        Plugin.TrustedCharacterViewWindow.Position = WindowPosition + new Vector2(WindowWidth, 0);
                        if (!Plugin.TrustedCharacterViewWindow.IsOpen) Plugin.TrustedCharacterViewWindow.IsOpen = true;
                        selected = -1;
                    }
                }
            }
            ImGui.EndChild();
        }
    }
}
