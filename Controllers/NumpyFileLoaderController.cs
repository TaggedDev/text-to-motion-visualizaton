using Microsoft.AspNetCore.Mvc;
using MotionDataVisualization.IOFactory;
using MotionDataVisualization.Models;

namespace MotionDataVisualization.Controllers;

[ApiController]
public class NumpyFileLoaderController(IIOFactory ioFactory) : ControllerBase
{
    [HttpGet("count")]
    public int GetCount() => ioFactory.GetFileCount();

    [HttpGet("files")]
    public IReadOnlyList<string> GetFiles() => ioFactory.GetFileNames();

    [HttpGet("files/{name}")]
    public ActionResult<MotionArray> GetFile(string name)
    {
        try
        {
            return ioFactory.LoadByName(name);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"File '{name}' not found." });
        }
    }

    [HttpGet("random")]
    public MotionArray GetRandom() => ioFactory.LoadRandom();
}
