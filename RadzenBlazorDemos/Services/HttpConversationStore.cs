using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadzenBlazorDemos.Services
{
    public class HttpConversationStore : IConversationStore
    {
        private readonly HttpClient httpClient;

        public HttpConversationStore(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<ConversationSession> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync($"api/conversations/{Uri.EscapeDataString(sessionId)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return ConversationSessionSerializer.Deserialize(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        public async Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default)
        {
            using var content = new StringContent(ConversationSessionSerializer.Serialize(session), Encoding.UTF8, "application/json");
            using var response = await httpClient.PutAsync($"api/conversations/{Uri.EscapeDataString(session.Id)}", content, cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<ConversationSession>> ListAsync(string userId = null, CancellationToken cancellationToken = default)
        {
            var url = userId == null ? "api/conversations" : $"api/conversations?userId={Uri.EscapeDataString(userId)}";

            var items = await httpClient.GetFromJsonAsync<string[]>(url, cancellationToken) ?? Array.Empty<string>();

            return items.Select(ConversationSessionSerializer.Deserialize).Where(s => s != null).ToList();
        }

        public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.DeleteAsync($"api/conversations/{Uri.EscapeDataString(sessionId)}", cancellationToken);

            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
