using Microsoft.EntityFrameworkCore;
using System;

namespace RadzenBlazorDemos.Data
{
    public class ConversationRecord
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Title { get; set; }

        public DateTime LastUpdated { get; set; }

        public string Json { get; set; }
    }

    public class ConversationContext : DbContext
    {
        public ConversationContext(DbContextOptions<ConversationContext> options) : base(options)
        {
        }

        public DbSet<ConversationRecord> Conversations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("Conversations");
            }
        }
    }
}
