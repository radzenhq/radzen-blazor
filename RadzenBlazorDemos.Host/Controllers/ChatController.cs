using Microsoft.AspNetCore.Mvc;
using Radzen;
using System.Threading.Tasks;

namespace RadzenBlazorDemos
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatStreamingService _chatService;
        public ChatController(ChatStreamingService chatService)
        {
            _chatService = chatService;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost("stream")]
        public async Task Stream([FromBody] ChatRequest request)
        {
            Response.ContentType = "text/plain";
            await foreach (var chunk in _chatService.StreamChatCompletionAsync(request.Message, HttpContext.RequestAborted))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(chunk);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                await Response.Body.FlushAsync();
            }
        }
    }
} 