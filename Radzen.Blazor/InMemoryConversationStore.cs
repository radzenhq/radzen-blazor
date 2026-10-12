using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen;

/// <summary>
/// An <see cref="IConversationStore"/> that keeps the conversations in memory for the lifetime of the application. Registered by <see cref="AIChatServiceExtensions.AddAIChatService(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/> when no other store is registered.
/// </summary>
public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationSession> sessions = new();

    /// <summary>
    /// Gets or sets the age after which a conversation is removed when another one is saved. Default is 24 hours; <c>null</c> keeps them forever.
    /// </summary>
    public TimeSpan? MaxAge { get; set; } = TimeSpan.FromHours(24);

    /// <inheritdoc />
    public Task<ConversationSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        return Task.FromResult(sessions.TryGetValue(sessionId, out var session) ? session : null);
    }

    /// <inheritdoc />
    public Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        sessions[session.Id] = session;

        if (MaxAge is TimeSpan maxAge)
        {
            var cutoff = DateTime.Now - maxAge;

            foreach (var stale in sessions.Values.Where(candidate => candidate.LastUpdated < cutoff).Select(candidate => candidate.Id).ToList())
            {
                sessions.TryRemove(stale, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConversationSession>> ListAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ConversationSession> result = sessions.Values
            .Where(session => userId == null || session.UserId == userId)
            .OrderByDescending(session => session.LastUpdated)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionId);

        sessions.TryRemove(sessionId, out _);

        return Task.CompletedTask;
    }
}
