using System;
using System.Text.RegularExpressions;
using SharpDX;

namespace WhereAreYouGoing;

public class CustomUnitConfig : WAYGConfig
{
    private Regex _compiledRegex;
    private string _compiledPattern;
    private bool _compiledWithRegex;

    public string Name { get; set; } = "New Custom Unit";
    public string MetadataSearch { get; set; } = string.Empty;
    public bool UseRegex { get; set; }
    public bool IsRegex { get; set; }

    public bool UsesRegex => UseRegex || IsRegex;

    public CustomUnitConfig()
    {
        Enable = true;
        UnitType = WhereAreYouGoingSettings.UnitType.UnitTesting;
        Colors = new WAYGColors
        {
            MapColor = Color.Magenta,
            MapAttackColor = Color.Red,
            WorldColor = Color.Magenta,
            WorldAttackColor = Color.Red
        };
        World = new WAYGWorld
        {
            Enable = true,
            DrawAttack = true,
            DrawDestination = true,
            DrawLine = true,
            AlwaysRenderWorldUnit = true,
            DrawFilledCircle = true,
            RenderCircleThickness = 3,
            LineThickness = 3
        };
        Map = new WAYGMap
        {
            Enable = true,
            DrawAttack = true,
            DrawDestination = true,
            LineThickness = 3
        };
    }

    public bool Matches(string metadata)
    {
        if (!Enable || string.IsNullOrWhiteSpace(MetadataSearch) || string.IsNullOrEmpty(metadata))
        {
            return false;
        }

        if (!UsesRegex)
        {
            return metadata.Contains(MetadataSearch, StringComparison.OrdinalIgnoreCase);
        }

        EnsureRegex();
        return _compiledRegex?.IsMatch(metadata) ?? metadata.Contains(MetadataSearch, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureRegex()
    {
        if (_compiledRegex != null && _compiledPattern == MetadataSearch && _compiledWithRegex == UsesRegex)
        {
            return;
        }

        _compiledPattern = MetadataSearch;
        _compiledWithRegex = UsesRegex;

        try
        {
            _compiledRegex = new Regex(MetadataSearch, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch
        {
            _compiledRegex = null;
        }
    }
}
