namespace MotionDataVisualization.Models;

public static class MotionDecoder
{
    public const int Joints = 22;

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
        int frames = features.Length;
        float[][] positions = new float[frames][];

        for (int f = 0; f < frames; f++)
        {
            float[] src = features[f];
            float[] dst = new float[Joints * 3];

            dst[0] = 0f;
            dst[1] = src[3];
            dst[2] = 0f;

            const int jointOffset = 4;
            for (int j = 1; j < Joints; j++)
            {
                int s = jointOffset + (j - 1) * 3;
                int d = j * 3;
                dst[d] = src[s];
                dst[d + 1] = src[s + 1];
                dst[d + 2] = src[s + 2];
            }

            positions[f] = dst;
        }

        return positions;
    }
}
