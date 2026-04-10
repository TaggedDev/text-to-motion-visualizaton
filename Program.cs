using NumSharp;

namespace MotionDataVisualization;

internal class Program
{
    private static void Main()
    {
        const string rootWithBins = @"D:\Code\CSharp\text-to-motion\dataset\new_joint_vecs";
        string[] files = Directory.GetFiles(rootWithBins, "*.npy")
            .Where(file => !file.EndsWith(".clip.bin"))
            .ToArray();

        foreach (var file in files)
        {
            Console.WriteLine($"File: {file}");
            NDArray ndArray = np.load(file);
            Console.WriteLine(string.Join(", ", ndArray.shape));
            break;
        }
    }
}
