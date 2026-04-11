using Microsoft.AspNetCore.Mvc;
using MotionDataVisualization.IOFactory;
using MotionDataVisualization.Models;

namespace MotionDataVisualization.Controllers;

[ApiController]
public class MotionController : ControllerBase
{
    private readonly IIOFactory _ioFactory;

    public MotionController(IIOFactory ioFactory)
    {
        _ioFactory = ioFactory;
    }

    [HttpGet("count")]
    public int GetCount() => _ioFactory.GetFileCount();

    [HttpGet("files")]
    public IReadOnlyList<string> GetFiles() => _ioFactory.GetFileNames();

    [HttpGet("files/{name}")]
    public ActionResult<MotionArray> GetFile(string name)
    {
        try
        {
            return _ioFactory.LoadByName(name);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"File '{name}' not found." });
        }
    }

    [HttpGet("random")]
    public MotionArray GetRandom() => _ioFactory.LoadRandom();

    // --- Three.js frontend API ---

    [HttpGet("api/animations")]
    public IEnumerable<object> GetAnimations()
    {
        // Cap the list so the UI stays responsive; HumanML3D has ~14K files.
        return _ioFactory.GetFileNames()
            .Take(500)
            .Select(name => new
            {
                id = Path.GetFileNameWithoutExtension(name),
                split = "all",
                caption = "",
                frameCount = 0 // populated on selection
            });
    }

    [HttpGet("api/animation/{split}/{id}")]
    public IActionResult GetAnimation(string split, string id)
    {
        string name = id.EndsWith(".npy", StringComparison.OrdinalIgnoreCase) ? id : id + ".npy";

        MotionArray motion;
        try
        {
            motion = _ioFactory.LoadByName(name);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Animation '{id}' not found." });
        }

        if (motion.Shape.Length < 2)
            return BadRequest(new { error = "Unexpected array shape." });

        int frames = motion.Shape[0];
        int featureDim = motion.Shape[1];
        const int Joints = 22;

        float[][] positions = new float[frames][];

        if (featureDim == Joints * 3)
        {
            // Raw joint positions: [frames, 66]
            for (int f = 0; f < frames; f++)
            {
                float[] src = motion.Data[f];
                float[] dst = new float[Joints * 3];
                Array.Copy(src, dst, Joints * 3);
                positions[f] = dst;
            }
        }
        else if (featureDim == 263)
        {
            // HumanML3D new_joint_vecs format:
            //   [0]        root rotation velocity
            //   [1..2]     root linear velocity xz
            //   [3]        root y
            //   [4 .. 4+63] (joints-1)*3 local joint positions
            //   remaining  rotations, velocities, foot contacts
            for (int f = 0; f < frames; f++)
            {
                float[] src = motion.Data[f];
                float[] dst = new float[Joints * 3];

                // Joint 0 (root): keep xz at origin, lift by root_y.
                dst[0] = 0f;
                dst[1] = src[3];
                dst[2] = 0f;

                const int JointOffset = 4;
                for (int j = 1; j < Joints; j++)
                {
                    int s = JointOffset + (j - 1) * 3;
                    int d = j * 3;
                    dst[d + 0] = src[s + 0];
                    dst[d + 1] = src[s + 1];
                    dst[d + 2] = src[s + 2];
                }
                positions[f] = dst;
            }
        }
        else
        {
            return BadRequest(new { error = $"Unsupported feature dim {featureDim}." });
        }

        return Ok(new
        {
            id,
            split,
            caption = "",
            frameCount = frames,
            joints = Joints,
            positions,
            edges = SmplEdges,
            jointGroup = SmplJointGroups
        });
    }

    // SMPL 22-joint skeleton (HumanML3D subset).
    private static readonly int[][] SmplEdges =
    {
        new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 },
        new[] { 1, 4 }, new[] { 2, 5 }, new[] { 3, 6 },
        new[] { 4, 7 }, new[] { 5, 8 }, new[] { 6, 9 },
        new[] { 7, 10 }, new[] { 8, 11 }, new[] { 9, 12 },
        new[] { 9, 13 }, new[] { 9, 14 }, new[] { 12, 15 },
        new[] { 13, 16 }, new[] { 14, 17 }, new[] { 16, 18 },
        new[] { 17, 19 }, new[] { 18, 20 }, new[] { 19, 21 },
    };

    private static readonly string[] SmplJointGroups =
    {
        "spine", "left_leg", "right_leg", "spine", "left_leg", "right_leg",
        "spine", "left_leg", "right_leg", "spine", "left_leg", "right_leg",
        "spine", "left_arm", "right_arm", "spine",
        "left_arm", "right_arm", "left_arm", "right_arm", "left_arm", "right_arm",
    };
}
