using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Radzen;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RadzenBlazorDemos
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController(HttpClient httpClient, IOptions<AIChatServiceOptions> chatStreamingServiceOptions) : ControllerBase
    {
        private readonly AIChatServiceOptions options = chatStreamingServiceOptions.Value;
        
        [HttpPost("completions")]
        public async Task<IActionResult> Completions()
        {
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod(Request.Method),
                RequestUri = new Uri(options.Endpoint),
                Content = new StreamContent(Request.Body)
            };

            request.RequestUri = new UriBuilder(request.RequestUri) { Query = Request.QueryString.ToString() }.Uri;

            foreach (var header in Request.Headers)
            {
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // Use configurable API key header

            if (string.Equals(options.ApiKeyHeader, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
            else
            {
                request.Headers.Add(options.ApiKeyHeader, options.ApiKey);
            }

            request.Headers.Host = request.RequestUri.Host;

            try
            {
                var response = await httpClient.SendAsync(request);

                Response.StatusCode = (int)response.StatusCode;

                foreach (var header in response.Headers)
                {
                    Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var contentHeader in response.Content.Headers)
                {
                    Response.Headers[contentHeader.Key] = contentHeader.Value.ToArray();
                }

                Response.Headers.Remove("transfer-encoding");

                if (!response.IsSuccessStatusCode && response.Content.Headers.ContentLength == 0)
                {
                    return new StatusCodeResult((int)response.StatusCode);
                }

                var responseContentStream = await response.Content.ReadAsStreamAsync();

                return new FileStreamResult(responseContentStream, response.Content.Headers.ContentType?.ToString());
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}