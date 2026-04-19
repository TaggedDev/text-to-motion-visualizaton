using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MotionDataVisualization.DLModels;
using MotionDataVisualization.Models;

namespace MotionDataVisualization.Controllers;

[ApiController]
[Route("api/text2motion")]
public class Text2MotionController(
    ClipModel clipModel,
    Text2MotionModel text2MotionModel,
    IOptions<Text2MotionSettings> settings) : ControllerBase
{
    [HttpGet("models")]
    public IActionResult GetModels()
    {
        string dir = ResolveWeightsDir(settings.Value.WeightsDirectory);
        if (!Directory.Exists(dir))
            return Ok(Array.Empty<string>());

        var files = Directory.GetFiles(dir, "*.pt")
            .Concat(Directory.GetFiles(dir, "*.onnx"))
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToArray();

        return Ok(files);
    }

    [HttpPost("generate")]
    public IActionResult Generate([FromBody] GenerateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Text cannot be empty." });

        float[] embedding;
        try
        {
            embedding = clipModel.GetTextEmbedding(request.Text);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"CLIP embedding failed: {ex.Message}" });
        }

        float[][] features;
        try
        {
            features = text2MotionModel.GenerateMotion(embedding);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Motion generation failed: {ex.Message}" });
        }

        float[][] positions = MotionDecoder.Decode263ToPositions(features);

        return Ok(new
        {
            id = "generated",
            split = "generated",
            caption = request.Text,
            frameCount = positions.Length,
            joints = MotionDecoder.Joints,
            positions,
            edges = MotionDecoder.SmplEdges,
            jointGroup = MotionDecoder.SmplJointGroups
        });
    }

    private static string ResolveWeightsDir(string configured) =>
        Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured);
}

public class GenerateRequest
{
    public string Text { get; set; } = string.Empty;
    public string? ModelName { get; set; }
}
