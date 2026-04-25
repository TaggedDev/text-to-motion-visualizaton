namespace MotionDataVisualization.Models;

public class Text2MotionSettings
{
    public string WeightsDirectory { get; set; } = "Weights/Text2Motion";
    public string NormalizationMeanPath { get; set; } = string.Empty;
    public string NormalizationStdPath { get; set; } = string.Empty;
}
