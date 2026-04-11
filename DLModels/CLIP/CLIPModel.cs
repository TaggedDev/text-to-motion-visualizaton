using System.Net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MotionDataVisualization.DLModels;

public class CLIPModel
{
    private const string ModelFileName = "clip-text-vit-32-float32-int32.onnx";
    private const string ModelUrl = "https://huggingface.co/rocca/openai-clip-js/resolve/main/clip-text-vit-32-float32-int32.onnx";
    
    private readonly InferenceSession _session;
    private readonly CLIPTokenizer _tokenizer;

    public CLIPModel()
    {
        string weightsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Weights");
        Directory.CreateDirectory(weightsDir);

        string modelPath = Path.Combine(weightsDir, ModelFileName);

        if (!File.Exists(modelPath))
        {
            using var webClient = new WebClient();
            webClient.DownloadFile(ModelUrl, modelPath);
        }

        _tokenizer = new CLIPTokenizer();
        _session = new InferenceSession(modelPath);
    }

    public float[] GetTextEmbedding(string text)
    {
        if (_session == null)
            throw new InvalidOperationException("Model not initialized");

        // Tokenize text to token IDs
        int[] tokenIds = TokenizeText(text);

        // Create input tensor with shape [1, sequence_length]
        var inputTensor = new DenseTensor<int>(tokenIds, new[] { 1, tokenIds.Length });
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);

        // Extract embeddings from output
        var output = results.FirstOrDefault(r => r.Name == "output");
        if (output == null)
            throw new InvalidOperationException("Model output not found");

        // Convert to float array
        var embeddings = (output.Value as IEnumerable<float>)?.ToArray()
            ?? throw new InvalidOperationException("Failed to extract embeddings");

        return embeddings;
    }

    private int[] TokenizeText(string text)
    {
        return _tokenizer.Tokenize(text);
    }
}