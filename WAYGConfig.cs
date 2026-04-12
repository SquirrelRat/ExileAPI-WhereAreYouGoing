using SharpDX;
using static WhereAreYouGoing.WhereAreYouGoingSettings;

namespace WhereAreYouGoing;

public class WAYGConfig
{
    public bool Enable { get; set; }
    public UnitType UnitType { get; set; } = UnitType.None;
    public WAYGColors Colors { get; set; } = new();
    public WAYGWorld World { get; set; } = new();
    public WAYGMap Map { get; set; } = new();

    public class WAYGMap
    {
        public bool Enable { get; set; } = true;
        public bool DrawAttack { get; set; } = true;
        public bool DrawDestination { get; set; } = true;
        public int LineThickness { get; set; } = 5;
    }

    public class WAYGWorld
    {
        public bool Enable { get; set; } = true;
        public bool DrawAttack { get; set; } = true;
        public bool DrawAttackEndPoint { get; set; } = true;
        public bool DrawDestination { get; set; } = true;
        public bool DrawDestinationEndPoint { get; set; } = true;
        public bool DrawLine { get; set; } = true;
        public bool AlwaysRenderWorldUnit { get; set; } = true;
        public bool DrawBoundingBox { get; set; }
        public bool DrawFilledCircle { get; set; }
        public int RenderCircleThickness { get; set; } = 5;
        public int LineThickness { get; set; } = 5;
    }

    public class WAYGColors
    {
        public Color MapColor { get; set; } = Color.White;
        public Color MapAttackColor { get; set; } = Color.Red;
        public Color WorldColor { get; set; } = Color.White;
        public Color WorldAttackColor { get; set; } = Color.Red;
    }
}
