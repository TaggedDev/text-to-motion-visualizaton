namespace MotionDataVisualization.Models;

public static class MotionDecoder
{
    public const int Joints = 22;

    // Feature indices in (263,) vector
    private const int RootRotVelIdx = 0;
    private const int RootLinVelXIdx = 1;
    private const int RootLinVelZIdx = 2;
    private const int RootHeightIdx = 3;
    private const int JointPosStartIdx = 4;
    private const int JointPosEndIdx = 67;
    private const int LocalJointCount = 21;

    public static readonly int[][] SmplEdges =
    {
        new[] { 0, 1 }, new[] { 0, 2 }, new[] { 0, 3 },
        new[] { 1, 4 }, new[] { 2, 5 }, new[] { 3, 6 },
        new[] { 4, 7 }, new[] { 5, 8 }, new[] { 6, 9 },
        new[] { 7, 10 }, new[] { 8, 11 }, new[] { 9, 12 },
        new[] { 9, 13 }, new[] { 9, 14 }, new[] { 12, 15 },
        new[] { 13, 16 }, new[] { 14, 17 }, new[] { 16, 18 },
        new[] { 17, 19 }, new[] { 18, 20 }, new[] { 19, 21 },
    };

    public static readonly string[] SmplJointGroups =
    {
        "spine", "left_leg", "right_leg", "spine", "left_leg", "right_leg",
        "spine", "left_leg", "right_leg", "spine", "left_leg", "right_leg",
        "spine", "left_arm", "right_arm", "spine",
        "left_arm", "right_arm", "left_arm", "right_arm", "left_arm", "right_arm",
    };

    public static float[][] Decode263ToPositions(float[][] features)
    {
        return Decode263ToPositions(features, mean: null, std: null);
    }

    public static float[][] Decode263ToPositions(float[][] features, float[]? mean, float[]? std)
    {
        int frames = features.Length;

        // Note: Data should be pre-denormalized by caller (.npy files are already denormalized,
        // Text2MotionModel also denormalizes). Only use mean/std if you have normalized data.
        float[][] data = Denormalize(features, mean, std);
        var rootTrajectory = ReconstructRoot(data);
        var joints = ReconstructJoints(data, rootTrajectory);

        return FramesToPositions(joints);
    }

    private static float[][] Denormalize(float[][] features, float[]? mean, float[]? std)
    {
        if (mean == null || std == null)
            return features;

        int frames = features.Length;
        int featureDim = features[0].Length;

        if (mean.Length != featureDim || std.Length != featureDim)
            throw new ArgumentException("Mean and std must match feature dimension");

        float[][] result = new float[frames][];
        for (int f = 0; f < frames; f++)
        {
            result[f] = new float[featureDim];
            for (int d = 0; d < featureDim; d++)
            {
                result[f][d] = features[f][d] * std[d] + mean[d];
            }
        }

        return result;
    }

    private static float[][][] ReconstructRoot(float[][] features)
    {
        int frames = features.Length;
        float[][][] root = new float[frames][][];

        for (int f = 0; f < frames; f++)
        {
            root[f] = new float[1][];
            root[f][0] = new float[3];
        }

        root[0][0][0] = 0f;
        root[0][0][1] = features[0][RootHeightIdx];
        root[0][0][2] = 0f;

        for (int f = 1; f < frames; f++)
        {
            float vx = features[f][RootLinVelXIdx];
            float vz = features[f][RootLinVelZIdx];
            float y = features[f][RootHeightIdx];

            root[f][0][0] = root[f - 1][0][0] + vx;
            root[f][0][1] = y;
            root[f][0][2] = root[f - 1][0][2] + vz;
        }

        return root;
    }

    private static float[][][] ReconstructJoints(float[][] features, float[][][] rootTrajectory)
    {
        int frames = features.Length;
        float[][][] joints = new float[frames][][];

        for (int f = 0; f < frames; f++)
        {
            joints[f] = new float[Joints][];

            // Root joint
            joints[f][0] = new float[3];
            joints[f][0][0] = rootTrajectory[f][0][0];
            joints[f][0][1] = rootTrajectory[f][0][1];
            joints[f][0][2] = rootTrajectory[f][0][2];

            // Other joints: Add root X,Z motion, but Y is absolute (not relative)
            float[] src = features[f];
            for (int j = 1; j < Joints; j++)
            {
                joints[f][j] = new float[3];
                int srcIdx = JointPosStartIdx + (j - 1) * 3;

                joints[f][j][0] = src[srcIdx] + rootTrajectory[f][0][0];      // X: local + root motion
                joints[f][j][1] = src[srcIdx + 1];                             // Y: absolute world position
                joints[f][j][2] = src[srcIdx + 2] + rootTrajectory[f][0][2];   // Z: local + root motion
            }
        }

        return joints;
    }

    private static float[][] FramesToPositions(float[][][] joints)
    {
        int frames = joints.Length;
        float[][] positions = new float[frames][];

        for (int f = 0; f < frames; f++)
        {
            positions[f] = new float[Joints * 3];
            for (int j = 0; j < Joints; j++)
            {
                positions[f][j * 3] = joints[f][j][0];
                positions[f][j * 3 + 1] = joints[f][j][1];
                positions[f][j * 3 + 2] = joints[f][j][2];
            }
        }

        return positions;
    }

    public static (float[][], bool hasNaNs, bool hasInfs) DecodeWithValidation(float[][] features)
    {
        float[][] positions = Decode263ToPositions(features);
        bool hasNaNs = false;
        bool hasInfs = false;

        foreach (var frame in positions)
        {
            foreach (var val in frame)
            {
                if (float.IsNaN(val)) hasNaNs = true;
                if (float.IsInfinity(val)) hasInfs = true;
            }
        }

        return (positions, hasNaNs, hasInfs);
    }

    public static float[][] DenormalizeFeatures(float[][] features, float[] mean, float[] std)
    {
        return Denormalize(features, mean, std);
    }

    public static float[][] GenerateTestData(int frames = 60, int seed = 42)
    {
        var rng = new Random(seed);
        float[][] data = new float[frames][];

        float rootX = 0f, rootZ = 0f;

        for (int f = 0; f < frames; f++)
        {
            data[f] = new float[263];

            float vx = (float)(rng.NextDouble() - 0.5f) * 0.1f;
            float vz = (float)(rng.NextDouble() - 0.5f) * 0.1f;
            float y = 0.9f + (float)(rng.NextDouble() - 0.5f) * 0.1f;

            data[f][0] = (float)(rng.NextDouble() - 0.5f) * 0.01f;
            data[f][1] = vx;
            data[f][2] = vz;
            data[f][3] = y;

            for (int i = 4; i < 67; i++)
            {
                data[f][i] = (float)(rng.NextDouble() - 0.5f) * 0.5f;
            }

            for (int i = 67; i < 263; i++)
            {
                data[f][i] = (float)(rng.NextDouble() - 0.5f) * 0.1f;
            }

            rootX += vx;
            rootZ += vz;
        }

        return data;
    }
}
