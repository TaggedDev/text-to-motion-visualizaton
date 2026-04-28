using Microsoft.AspNetCore.Mvc;
using MotionDataVisualization.IOFactory;
using MotionDataVisualization.Models;

namespace MotionDataVisualization.Controllers;

[Route("api")]
public class MotionVisualizationController(IIOFactory ioFactory) : ControllerBase
{
    [HttpGet("animations")]
    public IEnumerable<object> GetAnimations()
    {
        // Cap the list so the UI stays responsive; HumanML3D has ~14K files.
        return ioFactory.GetFileNames()
            .Take(500)
            .Select(name => new
            {
                id = Path.GetFileNameWithoutExtension(name),
                split = "all",
                caption = ioFactory.GetAnnotation(name),
                frameCount = 0 // populated on selection
            });
    }

    [HttpGet("test/motion-decoder")]
    public IActionResult TestMotionDecoder()
    {
        float[][] testData = MotionDecoder.GenerateTestData(frames: 60, seed: 42);

        var (positions, hasNaNs, hasInfs) = MotionDecoder.DecodeWithValidation(testData);

        if (hasNaNs || hasInfs)
        {
            return BadRequest(new
            {
                error = "Validation failed",
                hasNaNs,
                hasInfs
            });
        }

        return Ok(new
        {
            status = "success",
            frameCount = positions.Length,
            joints = MotionDecoder.Joints,
            positions,
            edges = MotionDecoder.SmplEdges,
            jointGroup = MotionDecoder.SmplJointGroups,
            message = "Test data generated and decoded successfully. No NaNs or Infs."
        });
    }

    [HttpGet("animation/{split}/{id}")]
    public IActionResult GetAnimation(string split, string id)
    {
        string name = id.EndsWith(".npy", StringComparison.OrdinalIgnoreCase) ? id : id + ".npy";

        MotionArray motion;
        try
        {
            motion = ioFactory.LoadByName(name);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Animation '{id}' not found." });
        }

        if (motion.Shape.Length < 2)
            return BadRequest(new { error = "Unexpected array shape." });

        int frames = motion.Shape[0];
        int featureDim = motion.Shape[1];

        float[][] positions;

        if (featureDim == MotionDecoder.Joints * 3)
        {
            // Raw joint positions: [frames, 66]
            positions = new float[frames][];
            for (int f = 0; f < frames; f++)
            {
                float[] src = motion.Data[f];
                float[] dst = new float[MotionDecoder.Joints * 3];
                Array.Copy(src, dst, MotionDecoder.Joints * 3);
                positions[f] = dst;
            }
        }
        else if (featureDim == 263)
        {
            // HumanML3D new_joint_vecs format: use shared decoder
            positions = MotionDecoder.Decode263ToPositions(motion.Data);
        }
        else
        {
            return BadRequest(new { error = $"Unsupported feature dim {featureDim}." });
        }

        return Ok(new
        {
            id,
            split,
            caption = ioFactory.GetAnnotation(name),
            frameCount = frames,
            joints = MotionDecoder.Joints,
            positions,
            edges = MotionDecoder.SmplEdges,
            jointGroup = MotionDecoder.SmplJointGroups
        });
    }
}