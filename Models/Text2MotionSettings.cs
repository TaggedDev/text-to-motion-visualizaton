namespace MotionDataVisualization.Models;

public class Text2MotionSettings
{
    public string WeightsDirectory { get; set; } = "Weights/Text2Motion";
    public string NormalizationMeanPath { get; set; } = string.Empty;
    public string NormalizationStdPath { get; set; } = string.Empty;
    public int FixedFrames { get; set; } = 60;
    public int FeatureDim { get; set; } = 263;
    public int TextEmbeddingDim { get; set; } = 512;
    public int HiddenDim { get; set; } = 1024;
    public int NumHiddenLayers { get; set; } = 3;
}
