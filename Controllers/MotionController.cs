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
}
