using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace rtyping
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public bool ShownConsentMenu { get; set; } = false;
        public List<string> TrustedCharacters { get; set; } = new List<string>();
        public bool HideKofi { get; set; } = false;
        public bool DisplaySelfMarker { get; set; } = true;
        public float PartyMarkerOpacity { get; set; } = 1.0f;
        public bool DisplaySelfNamePlateMarker { get; set; } = false;
        public bool DisplayOthersNamePlateMarker { get; set; } = true;
        public int NameplateMarkerStyle { get; set; } = 0;
        public bool ShowOnlyWhenNameplateVisible { get; set; } = true;
        public float NameplateMarkerOpacity { get; set; } = 1.0f;
        public bool ServerChat { get; set; } = true;
        public bool TrustAnyone { get; set; } = false;
        public bool DefaultDisplayParty { get; set; } = true;
        public bool DefaultDisplayNameplate { get; set; } = true;
        public int DefaultNameplateStyle { get; set; } = 0;
        public bool DefaultSendTypingStatus { get; set; } = true;
        public bool DefaultSendPartyless { get; set; } = true;
        public bool DefaultReceivePartyless { get; set; } = true;
        public int TrustedSortType { get; set; } = 0;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
