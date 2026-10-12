using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Radzen;

/// <summary>
/// Lets an <see cref="IChatClient"/> take messages the user sends while one of its responses is still streaming. Return the implementation from
/// <see cref="IChatClient.GetService"/> and <see cref="AIChatService.TryQueueMessageAsync"/> forwards to it, so an agent that reads new messages between its steps
/// can be plugged into <see cref="Blazor.RadzenAIChat"/> as a chat client without implementing <see cref="IAIChatService"/>.
/// </summary>
public interface IChatMessageQueue
{
    /// <summary>
    /// Offers the client a message sent while its response streams. Return <c>true</c> to take it; the chat then shows it as sent and does not send it again.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="sessionId">The session whose response is streaming.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<bool> TryQueueMessageAsync(ChatMessage message, string? sessionId, CancellationToken cancellationToken = default);
}
