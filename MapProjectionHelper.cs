using System;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace WhereAreYouGoing;

internal static class MapProjectionHelper
{
    public static Vector2 DeltaInWorldToMinimapDelta(Vector2 delta, double diagonal, float scale, float deltaZ = 0)
    {
        const float cameraAngle = 38f * MathUtil.Pi / 180f;
        var cos = (float)(diagonal * Math.Cos(cameraAngle) / scale);
        var sin = (float)(diagonal * Math.Sin(cameraAngle) / scale);
        return new Vector2((delta.X - delta.Y) * cos, deltaZ - ((delta.X + delta.Y) * sin));
    }
}
