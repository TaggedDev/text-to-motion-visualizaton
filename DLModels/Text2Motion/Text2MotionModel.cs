using Microsoft.Extensions.Options;
using MotionDataVisualization.Models;
using NumSharp;
using Text2Motion.TorchTrainer;
using static TorchSharp.torch;

namespace MotionDataVisualization.DLModels;

public sealed class Text2MotionModel : IDisposable
{
    private readonly BaselineMLPModel _mlp;
    private readonly float[] _mean;
    private readonly float[] _std;
    private readonly int _fixedFrames;
    private readonly int _featureDim;

    public Text2MotionModel(IOptions<Text2MotionSettings> options)
    {
        var cfg = options.Value;

        var trainerSettings = new ModelSettings
        {
            FixedFrames = cfg.FixedFrames,
            FeatureDim = cfg.FeatureDim,
            TextEmbeddingDim = cfg.TextEmbeddingDim,
            HiddenDim = cfg.HiddenDim,
            NumHiddenLayers = cfg.NumHiddenLayers,
        };

        _mlp = new BaselineMLPModel(Options.Create(trainerSettings));

        string weightsDir = Path.IsPathRooted(cfg.WeightsDirectory)
            ? cfg.WeightsDirectory
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cfg.WeightsDirectory);

        if (!Directory.Exists(weightsDir))
            throw new DirectoryNotFoundException($"Weights directory not found: {weightsDir}");

        string[] ptFiles = Directory.GetFiles(weightsDir, "*.pt");
        if (ptFiles.Length == 0)
            throw new FileNotFoundException($"No .pt files found in {weightsDir}");

        try
        {
            _mlp.load(ptFiles[0]);
            _mlp.eval();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to load .pt model from {ptFiles[0]}. Ensure the .pt file was saved by TorchSharp's module.save(), not Python's torch.save(). " +
                $"Details: {ex.Message}", ex);
        }

        _mean = LoadNpy(cfg.NormalizationMeanPath);
        _std = LoadNpy(cfg.NormalizationStdPath);
        _fixedFrames = cfg.FixedFrames;
        _featureDim = cfg.FeatureDim;
    }

    public float[][] GenerateMotion(float[] textEmbedding)
    {
        if (textEmbedding.Length != 512)
            throw new ArgumentException("Text embedding must be 512-dimensional", nameof(textEmbedding));

        using var inputTensor = tensor(textEmbedding).unsqueeze(0);
        using var noGrad = no_grad();
        using var outputTensor = _mlp.forward(inputTensor);

        float[] flat = outputTensor.squeeze(0).data<float>().ToArray();

        float[][] result = new float[_fixedFrames][];
        for (int f = 0; f < _fixedFrames; f++)
        {
            result[f] = new float[_featureDim];
            for (int d = 0; d < _featureDim; d++)
            {
                float normalized = flat[f * _featureDim + d];
                result[f][d] = normalized * _std[d] + _mean[d];
            }
        }

        return result;
    }

    public void Dispose()
    {
        _mlp?.Dispose();
    }

    private static float[] LoadNpy(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Normalization file not found: {path}");

        NDArray arr = np.load(path);
        if (arr.typecode != NPTypeCode.Float)
            arr = arr.astype(NPTypeCode.Float);

        return arr.ToArray<float>();
    }
}
