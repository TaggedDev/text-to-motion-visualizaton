using MotionDataVisualization.Models;
using NumSharp;

namespace MotionDataVisualization.IOFactory;

public class NpyIOFactory : IIOFactory
{
    private readonly Dictionary<string, string> _filesByName;
    private readonly string[] _names;

    public NpyIOFactory(IConfiguration configuration)
    {
        string rootPath = configuration["Dataset:RootPath"]
            ?? throw new InvalidOperationException("Dataset:RootPath is not configured.");

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Dataset root path not found: {rootPath}");

        _filesByName = Directory.GetFiles(rootPath, "*.npy")
            .ToDictionary(path => Path.GetFileName(path), path => path);
        _names = _filesByName.Keys.OrderBy(n => n).ToArray();
    }

    public int GetFileCount() => _names.Length;

    public IReadOnlyList<string> GetFileNames() => _names;

    public MotionArray LoadByName(string name)
    {
        if (!_filesByName.TryGetValue(name, out string? path))
            throw new FileNotFoundException($"No .npy file named '{name}' in the dataset.");

        return LoadFromPath(name, path);
    }

    public MotionArray LoadRandom()
    {
        if (_names.Length == 0)
            throw new InvalidOperationException("Dataset is empty.");

        string name = _names[Random.Shared.Next(_names.Length)];
        return LoadFromPath(name, _filesByName[name]);
    }

    private static MotionArray LoadFromPath(string name, string path)
    {
        NDArray ndArray = np.load(path);
        if (ndArray.typecode != NPTypeCode.Float)
            ndArray = ndArray.astype(NPTypeCode.Float);

        float[][] data = (float[][])ndArray.ToJaggedArray<float>();
        return new MotionArray(name, ndArray.shape, data);
    }
}
