using Microsoft.AspNetCore.Mvc;
using MotionDataVisualization.DLModels;
using MathNet.Numerics.LinearAlgebra;

namespace MotionDataVisualization.Controllers;

/// <summary>
/// Visualization of given sentences embeddings displayed as points using PCA algorithm to downscale each vector.
/// </summary>
/// <param name="clipModel">A model that will compute sentence embedding</param>
[ApiController]
[Route("api/embedding-visualization")]
public class EmbeddingVisualizationController(CLIPModel clipModel) : ControllerBase
{
    [HttpPost("embed")]
    public ActionResult<EmbedResponse> Embed([FromBody] EmbedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text cannot be empty");

        try
        {
            var embedding = clipModel.GetTextEmbedding(request.Text);
            return Ok(new EmbedResponse { Text = request.Text, Embedding = embedding });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating embedding: {ex.Message}");
        }
    }

    [HttpPost("project")]
    public ActionResult<ProjectedPoint[]> Project([FromBody] ProjectRequest request)
    {
        switch (request.Entries.Count)
        {
            case 0:
                return Ok(Array.Empty<ProjectedPoint>());
            case 1:
                return Ok(new[] {
                    new ProjectedPoint { Text = request.Entries[0].Text, X = 0f, Y = 0f }
                });
            default:
                try
                {
                    // Build N×512 matrix
                    var embeddings = request.Entries.Select(e => e.Embedding).ToList();
                    int n = embeddings.Count;
                    var matrix = Matrix<float>.Build.DenseOfRows(embeddings);

                    // Subtract row-wise mean (center the data)
                    var mean = matrix.ColumnSums() / n;
                    for (int i = 0; i < n; i++) 
                        matrix.SetRow(i, matrix.Row(i) - mean);

                    // Thin SVD
                    var svd = matrix.Svd();

                    // VT is the right singular vectors (principal components)
                    var vt = svd.VT;
                    var pc1 = vt.Row(0).ToArray();
                    var pc2 = vt.Row(1).ToArray();

                    // Project each row onto the 2 PCs
                    var results = new ProjectedPoint[n];
                    for (int i = 0; i < n; i++)
                    {
                        var row = matrix.Row(i).ToArray();
                        results[i] = new ProjectedPoint
                        {
                            Text = request.Entries[i].Text,
                            X = row.Select((v, j) => v * pc1[j]).Sum(),
                            Y = row.Select((v, j) => v * pc2[j]).Sum()
                        };
                    }

                    return Ok(results);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error computing PCA: {ex.Message}");
                }
        }
    }
}

public class EmbedRequest
{
    public string Text { get; set; } = string.Empty;
}

public class EmbedResponse
{
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

public class ProjectRequest
{
    public List<ProjectEntry> Entries { get; set; } = new();
}

public class ProjectEntry
{
    public string Text { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

public class ProjectedPoint
{
    public string Text { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
}
