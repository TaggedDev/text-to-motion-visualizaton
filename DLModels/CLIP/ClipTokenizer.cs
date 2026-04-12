using Tokenizers.DotNet;

namespace MotionDataVisualization.DLModels;

public class ClipTokenizer
{
    private const string TokenizerJSONFilename = "tokenizer.json";
    private const string TokenizerFileFolder = "Weights";
    private const int MaxLength = 77; // CLIP's default context length

    private readonly Tokenizer _tokenizer;

    public ClipTokenizer()
    {
        string tokenizerPath = Path.Combine(TokenizerFileFolder, TokenizerJSONFilename);
        _tokenizer = new Tokenizer(tokenizerPath);
    }

    public int[] Tokenize(string text)
    {
        // Encode text to token IDs
        var tokens = _tokenizer.Encode(text).ToArray();

        // Pad or truncate to MaxLength
        var paddedTokens = new int[MaxLength];

        if (tokens.Length >= MaxLength)
        {
            // Truncate if too long
            Array.Copy(tokens, paddedTokens, MaxLength);
        }
        else
        {
            // Pad with zeros if too short (0 is the padding token)
            Array.Copy(tokens, paddedTokens, tokens.Length);
        }

        return paddedTokens;
    }
}