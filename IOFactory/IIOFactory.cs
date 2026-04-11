using MotionDataVisualization.Models;

namespace MotionDataVisualization.IOFactory;

public interface IIOFactory
{
    int GetFileCount();
    IReadOnlyList<string> GetFileNames();
    MotionArray LoadByName(string name);
    MotionArray LoadRandom();
}
