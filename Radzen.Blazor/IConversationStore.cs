using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen;

/// <summary>
/// Stores the conversations of <see cref="IAIChatService"/> so that they survive page reloads, circuits and processes.
/// Register an implementation with <see cref="AIChatServiceExtensions.AddConversationStore{TStore}"/>; <see cref="InMemoryConversationStore"/> is used when none is registered.
/// Use <see cref="ConversationSessionSerializer"/> to turn a session into JSON and back when the store keeps it as text.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Loads a conversation. Returns <c>null</c> when it does not exist.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<ConversationSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a conversation, creating or replacing it.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the conversations, most recently updated first.
    /// </summary>
    /// <param name="userId">When specified only the conversations of that user are returned.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyList<ConversationSession>> ListAsync(string? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}
