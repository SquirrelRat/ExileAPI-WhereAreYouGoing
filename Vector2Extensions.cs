using System.Collections.Generic;
using System.Linq;
using GameOffsets.Native;
using Vector2 = System.Numerics.Vector2;

namespace WhereAreYouGoing;

public static class Vector2Extensions
{
    public static List<Vector2> ConvertToVector2List(this IList<Vector2i> source)
    {
        return source.Select(position => new Vector2(position.X, position.Y)).ToList();
    }
}
