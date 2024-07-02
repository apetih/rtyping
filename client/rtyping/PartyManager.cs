using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using System.Collections.Generic;

namespace rtyping
{
    public class Member
    {
        public ulong ContentID;
        public uint ObjectID;
        public SeString Name;
        public ushort World;
        public int Position;

        public Member(ulong contentID, uint objectID, SeString name, ushort world, int position)
        {
            ContentID = contentID;
            ObjectID = objectID;
            Name = name;
            World = world;
            Position = position;
        }
    }

    public class PartyManager
    {
        private Plugin Plugin;

        public PartyManager(Plugin plugin)
        {
            this.Plugin = plugin;
        }

        public IDictionary<string, Member> BuildPartyDictionary()
        {
            var PartyDictionary = new Dictionary<string, Member>();
            for (var i = 0; i < GetPartyMemberCount(); i++)
            {
                var member = GetMemberByIndex(i);
                PartyDictionary.Add(Plugin.HashContentID(member.ContentID), member);
            }
            return PartyDictionary;
        }
        public IDictionary<string, Member> BuildTrustedPartyDictionary()
        {
            var PartyDictionary = new Dictionary<string, Member>();
            for (var i = 0; i < GetPartyMemberCount(); i++)
            {
                var member = GetMemberByIndex(i);
                if (!Plugin.Configuration.TrustedCharacters.Contains($"{member.Name}@{member.World}")) continue;
                PartyDictionary.Add(Plugin.HashContentID(member.ContentID), member);
            }
            return PartyDictionary;
        }

        public unsafe int GetPartyMemberCount()
        {
            var manager = (GroupManager*)Plugin.PartyList.GroupManagerAddress;
            if (manager != null)
            {
                if (manager->MainGroup.MemberCount != 0) return manager->MainGroup.MemberCount;
            }
            if (InfoProxyCrossRealm.GetPartyMemberCount() != 0) return InfoProxyCrossRealm.GetPartyMemberCount();
            return 0;
        }

        private string GetPartyType()
        {
            if (InfoProxyCrossRealm.GetPartyMemberCount() != 0) return "CrossRealmParty";
            return "Party";
        }

        public unsafe Member GetMemberByIndex(int i)
        {
            var manager = (GroupManager*)Plugin.PartyList.GroupManagerAddress;
            if (GetPartyType() == "Party")
            {
                var partyMember = manager->MainGroup.GetPartyMemberByIndex(i);
                return new Member((ulong)partyMember->ContentId, partyMember->EntityId, partyMember->NameString, partyMember->HomeWorld, GetMemberPosition((ulong)partyMember->ContentId));
            }
            var crossMember = InfoProxyCrossRealm.GetGroupMember((uint)i);
            return new Member(crossMember->ContentId, crossMember->EntityId, crossMember->NameString, (ushort)crossMember->HomeWorld, crossMember->MemberIndex);
        }

        public unsafe int GetMemberPosition(ulong cid)
        {
            var manager = (GroupManager*)Plugin.PartyList.GroupManagerAddress;
            if (GetPartyType() == "Party")
            {
                var agentHud = Framework.Instance()->UIModule->GetAgentModule()->GetAgentHUD();
                var list = agentHud->PartyMembers;
                var pos = -1;
                for (var i = 0; i < (short)agentHud->PartyMemberCount; i++)
                {
                    if (list[i].ContentId == cid) pos = i;
                    if (pos != -1) break;
                }
                return pos;
            }
            return InfoProxyCrossRealm.GetMemberByContentId(cid)->MemberIndex;
        }
    }
}
