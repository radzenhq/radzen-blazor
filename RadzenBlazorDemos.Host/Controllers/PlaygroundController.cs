using Microsoft.AspNetCore.Mvc;
using RadzenBlazorDemos.Host.Services;
using System.Threading.Tasks;

namespace RadzenBlazorDemos.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaygroundController(PlaygroundService playgroundService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSnippet(string id)
        {
            if (!playgroundService.IsConfigured)
            {
                return StatusCode(503, new { error = "Playground storage is not configured." });
            }

            var snippet = await playgroundService.GetSnippetAsync(id);
            
            if (snippet == null)
            {
                return NotFound(new { error = "Snippet not found." });
            }

            return Ok(new 
            { 
                id = snippet.Id, 
                source = snippet.Source,
                parentId = snippet.ParentId,
                createdAt = snippet.CreatedAt,
                updatedAt = snippet.UpdatedAt
            });
        }

        [HttpPost("save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSnippet([FromBody] SaveSnippetRequest request)
        {
            if (!playgroundService.IsConfigured)
            {
                return StatusCode(503, new { error = "Playground storage is not configured." });
            }

            if (string.IsNullOrWhiteSpace(request.Source))
            {
                return BadRequest(new { error = "Source code is required." });
            }

            var result = await playgroundService.SaveSnippetAsync(request);

            return Ok(result);
        }
    }
}
