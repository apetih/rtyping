using Microsoft.EntityFrameworkCore;
using System;

namespace rtyping.Models
{
    public class TrustedCharacterContext : DbContext
    {
        public DbSet<TrustedCharacter> TrustedCharacters { get; set; }
        public string DbPath { get; set; }

        public TrustedCharacterContext(string configDir)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var DbName = "trustedcharacters.db";
            DbPath = $"{configDir}{DbName}";
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }

    public class TrustedCharacter
    {
        public int TrustedCharacterId { get; set; }
        public string CharacterName { get; set; } = null!;
        public uint WorldId {  get; set; }
        public DateTime AddedAt { get; set; }
        public bool DisplayParty {  get; set; }
        public bool DisplayNameplate { get; set; }
        public int NameplateStyle { get; set; }
        public bool SendTypingStatus { get; set; }
        public bool SendPartyless {  get; set; }
        public bool ReceivePartyless {  get; set; }
    }
}
