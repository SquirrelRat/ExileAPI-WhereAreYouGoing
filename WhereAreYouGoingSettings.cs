using System.Collections.Generic;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhereAreYouGoing;

public class WhereAreYouGoingSettings : ISettings
{
    public enum UnitType
    {
        None,
        Normal,
        Magic,
        Rare,
        Unique,
        Self,
        Friendly,
        Player,
        UnitTesting
    }

    public ToggleNode Enable { get; set; } = new(false);
    public ToggleNode SmoothPaths { get; set; } = new(true);
    public ToggleNode IgnoreFullscreenPanels { get; set; } = new(false);
    public ToggleNode IgnoreLargePanels { get; set; } = new(false);
    public ToggleNode EnableInPeacefulAreas { get; set; } = new(false);
    public RangeNode<int> MaxCircleDrawDistance { get; set; } = new(120, 0, 500);
    public List<CustomUnitConfig> CustomUnits { get; set; } = [];

    public WAYGConfig NormalMonster { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Normal,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(255, 255, 255, 94),
            MapAttackColor = new Color(255, 0, 0, 255),
            WorldColor = new Color(255, 255, 255, 94),
            WorldAttackColor = new Color(255, 0, 0, 255)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawDestination = true,
            DrawAttackEndPoint = true,
            AlwaysRenderWorldUnit = true,
            RenderCircleThickness = 3,
            LineThickness = 5
        },
        Map = new WAYGConfig.WAYGMap
        {
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 1,
            Enable = false
        }
    };

    public WAYGConfig MagicMonster { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Magic,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(43, 120, 255, 176),
            MapAttackColor = new Color(255, 0, 0, 144),
            WorldColor = new Color(43, 120, 255, 176),
            WorldAttackColor = new Color(255, 0, 0, 144)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawDestination = true,
            DrawAttackEndPoint = true,
            AlwaysRenderWorldUnit = true,
            RenderCircleThickness = 3,
            LineThickness = 5
        },
        Map = new WAYGConfig.WAYGMap
        {
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 1,
            Enable = false
        }
    };

    public WAYGConfig RareMonster { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Rare,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(225, 210, 19, 255),
            MapAttackColor = new Color(255, 0, 0, 140),
            WorldColor = new Color(225, 210, 19, 255),
            WorldAttackColor = new Color(255, 0, 0, 140)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawAttack = true,
            DrawAttackEndPoint = true,
            DrawDestination = true,
            DrawDestinationEndPoint = true,
            DrawLine = true,
            AlwaysRenderWorldUnit = true,
            DrawFilledCircle = true,
            RenderCircleThickness = 5,
            LineThickness = 5
        },
        Map = new WAYGConfig.WAYGMap
        {
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 5,
            Enable = false
        }
    };

    public WAYGConfig UniqueMonster { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Unique,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(226, 122, 33, 255),
            MapAttackColor = new Color(255, 0, 0, 255),
            WorldColor = new Color(226, 122, 33, 255),
            WorldAttackColor = new Color(255, 0, 0, 255)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawAttack = true,
            DrawAttackEndPoint = true,
            DrawDestination = true,
            DrawDestinationEndPoint = true,
            DrawLine = true,
            AlwaysRenderWorldUnit = true,
            DrawFilledCircle = true,
            RenderCircleThickness = 5,
            LineThickness = 5
        },
        Map = new WAYGConfig.WAYGMap
        {
            Enable = true,
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 3
        }
    };

    public WAYGConfig Self { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Self,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(35, 194, 47, 193),
            MapAttackColor = new Color(255, 0, 0, 255),
            WorldColor = new Color(35, 194, 47, 193),
            WorldAttackColor = new Color(255, 0, 0, 255)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawAttack = true,
            DrawAttackEndPoint = true,
            DrawDestination = true,
            DrawDestinationEndPoint = true,
            DrawLine = true,
            AlwaysRenderWorldUnit = true,
            RenderCircleThickness = 3,
            LineThickness = 6
        },
        Map = new WAYGConfig.WAYGMap
        {
            Enable = true,
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 5
        }
    };

    public WAYGConfig Players { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Player,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(35, 194, 47, 193),
            MapAttackColor = new Color(255, 0, 0, 255),
            WorldColor = new Color(35, 194, 47, 193),
            WorldAttackColor = new Color(255, 0, 0, 255)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawAttack = true,
            DrawAttackEndPoint = true,
            DrawDestination = true,
            DrawDestinationEndPoint = true,
            DrawLine = true,
            AlwaysRenderWorldUnit = true,
            RenderCircleThickness = 3,
            LineThickness = 6
        },
        Map = new WAYGConfig.WAYGMap
        {
            Enable = true,
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 5
        }
    };

    public WAYGConfig Minions { get; set; } = new()
    {
        Enable = true,
        UnitType = UnitType.Friendly,
        Colors = new WAYGConfig.WAYGColors
        {
            MapColor = new Color(218, 73, 255, 255),
            MapAttackColor = new Color(255, 73, 115, 121),
            WorldColor = new Color(218, 73, 255, 255),
            WorldAttackColor = new Color(255, 73, 115, 121)
        },
        World = new WAYGConfig.WAYGWorld
        {
            Enable = true,
            DrawAttack = true,
            DrawAttackEndPoint = true,
            DrawDestination = true,
            DrawDestinationEndPoint = true,
            DrawLine = true,
            AlwaysRenderWorldUnit = true,
            RenderCircleThickness = 5,
            LineThickness = 5
        },
        Map = new WAYGConfig.WAYGMap
        {
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 5,
            Enable = false
        }
    };
}
