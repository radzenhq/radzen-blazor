using Microsoft.EntityFrameworkCore;
using Radzen;
using RadzenBlazorDemos.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadzenBlazorDemos.Services
{
    public class EfConversationStore : IConversationStore
    {
        private readonly IDbContextFactory<ConversationContext> factory;

        public EfConversationStore(IDbContextFactory<ConversationContext> factory)
        {
            this.factory = factory;
        }

        public async Task<ConversationSession> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var context = await factory.CreateDbContextAsync(cancellationToken);

            var record = await context.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == sessionId, cancellationToken);

            return record == null ? null : ConversationSessionSerializer.Deserialize(record.Json);
        }

        public async Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default)
        {
            using var context = await factory.CreateDbContextAsync(cancellationToken);

            var record = await context.Conversations.FirstOrDefaultAsync(c => c.Id == session.Id, cancellationToken);

            if (record == null)
            {
                record = new ConversationRecord { Id = session.Id };
                context.Conversations.Add(record);
            }

            record.UserId = session.UserId;
            record.Title = session.Title;
            record.LastUpdated = session.LastUpdated;
            record.Json = ConversationSessionSerializer.Serialize(session);

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ConversationSession>> ListAsync(string userId = null, CancellationToken cancellationToken = default)
        {
            using var context = await factory.CreateDbContextAsync(cancellationToken);

            var records = await context.Conversations.AsNoTracking()
                .Where(c => userId == null || c.UserId == userId)
                .OrderByDescending(c => c.LastUpdated)
                .ToListAsync(cancellationToken);

            return records.Select(r => ConversationSessionSerializer.Deserialize(r.Json)).Where(s => s != null).ToList();
        }

        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var context = await factory.CreateDbContextAsync(cancellationToken);

            var record = await context.Conversations.FirstOrDefaultAsync(c => c.Id == sessionId, cancellationToken);

            if (record != null)
            {
                context.Conversations.Remove(record);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
