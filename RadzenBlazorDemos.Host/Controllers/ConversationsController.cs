using Microsoft.AspNetCore.Mvc;
using Radzen;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadzenBlazorDemos
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationsController(IConversationStore store) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var session = await store.LoadAsync(id, cancellationToken);

            return session == null ? NoContent() : Content(ConversationSessionSerializer.Serialize(session), "application/json");
        }

        [HttpGet]
        public async Task<string[]> List(string userId, CancellationToken cancellationToken)
        {
            var sessions = await store.ListAsync(userId, cancellationToken);

            return sessions.Select(ConversationSessionSerializer.Serialize).ToArray();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Save(string id, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(Request.Body);

            var session = ConversationSessionSerializer.Deserialize(await reader.ReadToEndAsync(cancellationToken));

            if (session == null || session.Id != id)
            {
                return BadRequest();
            }

            await store.SaveAsync(session, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await store.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
    }
}
