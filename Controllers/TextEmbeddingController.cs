using Microsoft.AspNetCore.Mvc;
using MotionDataVisualization.DLModels;

namespace MotionDataVisualization.Controllers;

[ApiController]
[Route("api/clip")]
public class TextEmbeddingController : ControllerBase
{
    private readonly CLIPModel _clipModel = new();

    [HttpGet]
    public ActionResult<float[]> GetTextEmbedding([FromQuery] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest("Text parameter cannot be empty");
        }

        try
        {
            var embedding = _clipModel.GetTextEmbedding(text);
            return Ok(embedding);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating embedding: {ex.Message}");
        }
    }
}