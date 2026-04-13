using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ImGuiNET;
using SharpDX;
using static WhereAreYouGoing.WhereAreYouGoingSettings;
using Map = ExileCore.PoEMemory.Elements.Map;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace WhereAreYouGoing;

public class WhereAreYouGoing : BaseSettingsPlugin<WhereAreYouGoingSettings>
{
    private static readonly string[] StandardEntityKeys =
    [
        "Self",
        "Players",
        "All Friendlies",
        "Normal Monster",
        "Magic Monster",
        "Rare Monster",
        "Unique Monster"
    ];

    private const float SmallMapScale = 240f;
    private const float ScreenAllowance = 50f;
    private const ActionFlags IdleActionExtra = (ActionFlags)512;
    private const ActionFlags MovingActionExtra = (ActionFlags)4224;

    private CachedValue<float> _diagonalCache;
    private CachedValue<RectangleF> _mapRectCache;
    private IngameUIElements _ingameUi;
    private bool _isLargeMapVisible;
    private float _largeMapScale;
    private Vector2 _mapCenter;
    private string _selectedEntity = "Self";
    private int _selectedCustomIndex;
    private int _selectedTab;

    private static readonly Dictionary<string, string> SliderEditBuffers = new();
    private static string _activeSliderEditId = null;

    private Camera Camera => GameController.Game.IngameState.Camera;
    private Map MapWindow => GameController.Game.IngameState.IngameUi.Map;

    private RectangleF CurrentMapRect => _mapRectCache?.Value ??
        (_mapRectCache = new TimeCache<RectangleF>(() => MapWindow.GetClientRect(), 100)).Value;

    private float Diagonal => _diagonalCache?.Value ??
        (_diagonalCache = new TimeCache<float>(CalculateDiagonal, 100)).Value;

    public override bool Initialise()
    {
        return true;
    }

    public override Job Tick()
    {
        if (!GameController.InGame)
        {
            return null;
        }

        _ingameUi = GameController.Game.IngameState.IngameUi;
        UpdateMapState();
        return null;
    }

    public override void DrawSettings()
    {
        PushWindowStyle();

        DrawGeneralSettings();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var sidebarHeight = ImGui.GetContentRegionAvail().Y - 60f;
        if (ImGui.BeginChild("Sidebar", new Vector2(140f, sidebarHeight), ImGuiChildFlags.Border, ImGuiWindowFlags.None))
        {
            DrawTabSelector("Standard", 0, new Vector4(0.50f, 0.80f, 1.00f, 1f));
            DrawTabSelector("Custom", 1, new Vector4(0.30f, 0.70f, 0.95f, 1f));
        }

        ImGui.EndChild();
        ImGui.SameLine();

        if (ImGui.BeginChild("Content", new Vector2(ImGui.GetContentRegionAvail().X - 4f, sidebarHeight), ImGuiChildFlags.Border, ImGuiWindowFlags.None))
        {
            switch (_selectedTab)
            {
                case 0:
                    DrawStandardSettings();
                    break;
                case 1:
                    DrawCustomSettings();
                    break;
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleColor(19);
        ImGui.PopStyleVar(9);
    }

    public override void Render()
    {
        if (!ShouldRender())
        {
            return;
        }

        var player = GameController.Player;
        if (player == null || !player.TryGetComponent<Positioned>(out var playerPositioned) || playerPositioned == null)
        {
            return;
        }

        if (MapWindow == null)
        {
            return;
        }

        var playerGridPosition = playerPositioned.GridPosNum;
        var playerWorldZ = player.PosNum.Z;
        var mapZoom = MapWindow.LargeMapZoom;

        RenderEntityCollection(GetEntities(EntityType.Monster), player, playerGridPosition, playerWorldZ, mapZoom);
        RenderEntityCollection(GetEntities(EntityType.Player), player, playerGridPosition, playerWorldZ, mapZoom);

        if (ShouldTrackOtherEntities())
        {
            RenderEntityCollection(GetEntities(EntityType.None), player, playerGridPosition, playerWorldZ, mapZoom);
        }
    }

    private void DrawGeneralSettings()
    {
        ImGui.Text("General");
        ImGui.Separator();
        Settings.Enable.Value = UiHelpers.Checkbox("Master Enable", Settings.Enable.Value);
        Settings.SmoothPaths.Value = UiHelpers.Checkbox("Smooth Paths", Settings.SmoothPaths.Value);

        ImGui.Spacing();
        ImGui.Text("Distance");
        ImGui.Separator();
        var circleDistance = Settings.MaxCircleDrawDistance.Value;
        ModernSlider("Max Circle Distance", ref circleDistance, 0, 500);
        Settings.MaxCircleDrawDistance.Value = circleDistance;

        ImGui.Spacing();
        ImGui.Text("Areas");
        ImGui.Separator();
        Settings.EnableInPeacefulAreas.Value = UiHelpers.Checkbox("Enable In Peaceful Areas", Settings.EnableInPeacefulAreas.Value);
        Settings.IgnoreFullscreenPanels.Value = UiHelpers.Checkbox("Ignore Fullscreen Panels", Settings.IgnoreFullscreenPanels.Value);
        Settings.IgnoreLargePanels.Value = UiHelpers.Checkbox("Ignore Large Panels", Settings.IgnoreLargePanels.Value);
    }

    private void DrawStandardSettings()
    {
        ImGui.Text("Standard Entities");
        ImGui.Separator();
        ImGui.Columns(2, "stdcols", true);
        ImGui.SetColumnWidth(0, 130f);

        foreach (var key in StandardEntityKeys)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiHelpers.GetEntityColor(key));
            if (ImGui.Selectable(key, _selectedEntity == key))
            {
                _selectedEntity = key;
            }
            ImGui.PopStyleColor();
        }

        ImGui.NextColumn();

        var config = GetStandardConfig(_selectedEntity);
        if (config != null)
        {
            ImGui.Text($"Edit: {_selectedEntity}");
            DrawConfigEditor(config);
        }

        ImGui.Columns(1);
    }

    private void DrawCustomSettings()
    {
        ImGui.Text("Custom Units");
        ImGui.Separator();

        if (ImGui.Button("Add", new Vector2(60f, 24f)))
        {
            Settings.CustomUnits.Add(new CustomUnitConfig { Name = $"Unit {Settings.CustomUnits.Count + 1}" });
            _selectedCustomIndex = Settings.CustomUnits.Count - 1;
        }

        ImGui.Columns(2, "custcols", true);
        ImGui.SetColumnWidth(0, 160f);

        for (var index = 0; index < Settings.CustomUnits.Count; index++)
        {
            var unit = Settings.CustomUnits[index];
            var color = unit.Enable
                ? new Vector4(unit.Colors.WorldColor.R / 255f, unit.Colors.WorldColor.G / 255f, unit.Colors.WorldColor.B / 255f, 1f)
                : new Vector4(0.5f, 0.5f, 0.5f, 1f);

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            if (ImGui.Selectable(unit.Name, _selectedCustomIndex == index))
            {
                _selectedCustomIndex = index;
            }
            ImGui.PopStyleColor();
        }

        ImGui.NextColumn();

        if (_selectedCustomIndex >= 0 && _selectedCustomIndex < Settings.CustomUnits.Count)
        {
            var unit = Settings.CustomUnits[_selectedCustomIndex];
            ImGui.Text($"Edit: {unit.Name}");
            ImGui.Separator();

            var name = unit.Name;
            if (ImGui.InputText("Name", ref name, 100))
            {
                unit.Name = name;
            }

            var metadata = unit.MetadataSearch;
            if (ImGui.InputText("Metadata", ref metadata, 256))
            {
                unit.MetadataSearch = metadata;
            }

            unit.Enable = UiHelpers.Checkbox("Enabled", unit.Enable);
            unit.UseRegex = UiHelpers.Checkbox("Use Regex", unit.UseRegex);
            unit.IsRegex = unit.UseRegex;
            DrawConfigEditor(unit);

            if (ImGui.Button("Remove", new Vector2(60f, 24f)))
            {
                Settings.CustomUnits.RemoveAt(_selectedCustomIndex);
                _selectedCustomIndex = Settings.CustomUnits.Count == 0 ? 0 : Math.Min(_selectedCustomIndex, Settings.CustomUnits.Count - 1);
            }
        }

        ImGui.Columns(1);
    }

    private void DrawConfigEditor(WAYGConfig config)
    {
        config.Enable = UiHelpers.Checkbox("Enabled", config.Enable);
        config.Colors.MapColor = UiHelpers.ColorPicker("Map Color", config.Colors.MapColor);
        config.Colors.MapAttackColor = UiHelpers.ColorPicker("Map Attack Color", config.Colors.MapAttackColor);
        config.Colors.WorldColor = UiHelpers.ColorPicker("World Color", config.Colors.WorldColor);
        config.Colors.WorldAttackColor = UiHelpers.ColorPicker("World Attack Color", config.Colors.WorldAttackColor);

        config.World.Enable = UiHelpers.Checkbox("Enable World", config.World.Enable);
        config.World.AlwaysRenderWorldUnit = UiHelpers.Checkbox("Always Render Unit", config.World.AlwaysRenderWorldUnit);
        config.World.DrawBoundingBox = UiHelpers.Checkbox("Draw Bounding Box", config.World.DrawBoundingBox);
        config.World.DrawFilledCircle = UiHelpers.Checkbox("Draw Filled Circle", config.World.DrawFilledCircle);
        config.World.DrawAttack = UiHelpers.Checkbox("Draw Attack", config.World.DrawAttack);
        config.World.DrawAttackEndPoint = UiHelpers.Checkbox("Draw Attack Endpoint", config.World.DrawAttackEndPoint);
        config.World.DrawDestination = UiHelpers.Checkbox("Draw Destination", config.World.DrawDestination);
        config.World.DrawDestinationEndPoint = UiHelpers.Checkbox("Draw Destination Endpoint", config.World.DrawDestinationEndPoint);
        config.World.DrawLine = UiHelpers.Checkbox("Draw Line", config.World.DrawLine);

        var worldLineThickness = config.World.LineThickness;
        ModernSlider("World Line Thickness", ref worldLineThickness, 1, 20);
        config.World.LineThickness = worldLineThickness;

        var circleThickness = config.World.RenderCircleThickness;
        ModernSlider("Circle Thickness", ref circleThickness, 1, 20);
        config.World.RenderCircleThickness = circleThickness;

        config.Map.Enable = UiHelpers.Checkbox("Enable Map", config.Map.Enable);
        config.Map.DrawAttack = UiHelpers.Checkbox("Map Draw Attack", config.Map.DrawAttack);
        config.Map.DrawDestination = UiHelpers.Checkbox("Map Draw Destination", config.Map.DrawDestination);

        var mapLineThickness = config.Map.LineThickness;
        ModernSlider("Map Line Thickness", ref mapLineThickness, 1, 20);
        config.Map.LineThickness = mapLineThickness;
    }

    private void DrawTabSelector(string label, int index, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        if (ImGui.Selectable(label, _selectedTab == index))
        {
            _selectedTab = index;
        }
        ImGui.PopStyleColor();
    }

    private bool ShouldRender()
    {
        if (!Settings.Enable.Value || !GameController.InGame)
        {
            return false;
        }

        if (!Settings.EnableInPeacefulAreas.Value && GameController.Area.CurrentArea.IsPeaceful)
        {
            return false;
        }

        var ingameUi = GameController.Game.IngameState.IngameUi;
        if (!Settings.IgnoreFullscreenPanels.Value && ingameUi.FullscreenPanels.Any(panel => panel.IsVisible))
        {
            return false;
        }

        if (!Settings.IgnoreLargePanels.Value && ingameUi.LargePanels.Any(panel => panel.IsVisible))
        {
            return false;
        }

        return true;
    }

    private void UpdateMapState()
    {
        var mapWindow = MapWindow;
        if (mapWindow == null)
        {
            _isLargeMapVisible = false;
            _mapCenter = Vector2.Zero;
            _largeMapScale = 0f;
            return;
        }

        var widthScale = Camera.Width < 1024f ? 1120f : 1024f;

        if (_ingameUi.Map.SmallMiniMap.IsVisibleLocal)
        {
            var mapRect = _ingameUi.Map.SmallMiniMap.GetClientRectCache;
            _mapCenter = new Vector2(mapRect.X + mapRect.Width / 2f, mapRect.Y + mapRect.Height / 2f);
            _isLargeMapVisible = false;
        }
        else if (_ingameUi.Map.LargeMap.IsVisibleLocal)
        {
            _mapCenter = GetLargeMapCenter();
            _isLargeMapVisible = true;
        }
        else
        {
            _mapCenter = Vector2.Zero;
            _isLargeMapVisible = false;
        }

        _largeMapScale = widthScale / Camera.Height * Camera.Width * 3f / 4f / mapWindow.LargeMapZoom;
    }

    private Vector2 GetLargeMapCenter()
    {
        var rect = CurrentMapRect;
        return new Vector2(rect.Width / 2f, rect.Height / 2f - 20f)
               + new Vector2(rect.X, rect.Y)
               + new Vector2(MapWindow.LargeMapShiftX, MapWindow.LargeMapShiftY);
    }

    private float CalculateDiagonal()
    {
        if (_ingameUi?.Map.SmallMiniMap.IsVisibleLocal == true)
        {
            var mapRect = _ingameUi.Map.SmallMiniMap.GetClientRect();
            return (float)(Math.Sqrt(mapRect.Width * mapRect.Width + mapRect.Height * mapRect.Height) / 2f);
        }

        return (float)Math.Sqrt(Camera.Width * Camera.Width + Camera.Height * Camera.Height);
    }

    private void RenderEntityCollection(IEnumerable<Entity> entities, Entity player, Vector2 playerGridPosition, float playerWorldZ, float mapZoom)
    {
        foreach (var entity in entities)
        {
            if (entity == null)
            {
                continue;
            }

            var drawSettings = ResolveDrawSettings(entity, player);
            if (drawSettings == null || !drawSettings.Enable)
            {
                continue;
            }

            if (!entity.TryGetComponent<Render>(out var renderComponent) || renderComponent == null)
            {
                continue;
            }

            if (!entity.TryGetComponent<Pathfinding>(out var pathfindingComponent) || pathfindingComponent == null)
            {
                continue;
            }

            if (!entity.TryGetComponent<Actor>(out var actorComponent) || actorComponent == null)
            {
                continue;
            }

            var shouldDrawCircle = entity.IsAlive && entity.DistancePlayer < Settings.MaxCircleDrawDistance.Value;
            DrawEntityByAction(entity, drawSettings, renderComponent, pathfindingComponent, actorComponent, playerGridPosition, playerWorldZ, mapZoom, shouldDrawCircle);
        }
    }

    private WAYGConfig ResolveDrawSettings(Entity entity, Entity player)
    {
        WAYGConfig standardSettings = entity.Type switch
        {
            EntityType.Monster when entity.IsHostile && entity.Rarity == MonsterRarity.White => Settings.NormalMonster,
            EntityType.Monster when entity.IsHostile && entity.Rarity == MonsterRarity.Magic => Settings.MagicMonster,
            EntityType.Monster when entity.IsHostile && entity.Rarity == MonsterRarity.Rare => Settings.RareMonster,
            EntityType.Monster when entity.IsHostile && entity.Rarity == MonsterRarity.Unique => Settings.UniqueMonster,
            EntityType.Monster when !entity.IsHostile => Settings.Minions,
            EntityType.Player when entity.Address == player?.Address => Settings.Self,
            EntityType.Player => Settings.Players,
            _ => null
        };

        var metadata = entity.Metadata;
        foreach (var customUnit in Settings.CustomUnits)
        {
            if (customUnit.Matches(metadata))
            {
                return customUnit;
            }
        }

        return standardSettings;
    }

    private void DrawEntityByAction(
        Entity entity,
        WAYGConfig drawSettings,
        Render renderComponent,
        Pathfinding pathfindingComponent,
        Actor actorComponent,
        Vector2 playerGridPosition,
        float playerWorldZ,
        float mapZoom,
        bool shouldDrawCircle)
    {
        switch (actorComponent.Action)
        {
            case IdleActionExtra:
            case ActionFlags.None:
            case ActionFlags.None | ActionFlags.HasMines:
                DrawUnitMarker(entity, drawSettings, renderComponent, shouldDrawCircle, false);
                return;

            case ActionFlags.UsingAbility:
            case ActionFlags.UsingAbilityAbilityCooldown:
            case ActionFlags.UsingAbility | ActionFlags.HasMines:
                DrawAttack(entity, drawSettings, renderComponent, actorComponent, playerGridPosition, playerWorldZ, mapZoom, shouldDrawCircle);
                return;

            case ActionFlags.Moving:
            case MovingActionExtra:
            case ActionFlags.Moving | ActionFlags.HasMines | MovingActionExtra:
                DrawMovement(entity, drawSettings, renderComponent, pathfindingComponent, playerGridPosition, playerWorldZ, mapZoom, shouldDrawCircle);
                return;

            case ActionFlags.AbilityCooldownActive:
            case ActionFlags.Dead:
            case ActionFlags.WashedUpState:
                return;

            default:
                DrawUnitMarker(entity, drawSettings, renderComponent, shouldDrawCircle, false);
                return;
        }
    }

    private void DrawAttack(
        Entity entity,
        WAYGConfig drawSettings,
        Render renderComponent,
        Actor actorComponent,
        Vector2 playerGridPosition,
        float playerWorldZ,
        float mapZoom,
        bool shouldDrawCircle)
    {
        var destination = actorComponent.CurrentAction.Destination.ToVector2Num();

        if (drawSettings.Map.Enable && drawSettings.Map.DrawAttack)
        {
            var startPoint = GetMapPosition(entity.GridPosNum, playerGridPosition, playerWorldZ, mapZoom);
            var endPoint = GetMapPosition(destination, playerGridPosition, playerWorldZ, mapZoom);
            Graphics.DrawLine(startPoint, endPoint, drawSettings.Map.LineThickness, drawSettings.Colors.MapAttackColor);
        }

        if (drawSettings.World.Enable && drawSettings.World.DrawAttack)
        {
            DrawUnitMarker(entity, drawSettings, renderComponent, shouldDrawCircle, true);

            if (drawSettings.World.DrawLine)
            {
                var worldDestination = GameController.IngameState.Data.GetGridScreenPosition(destination);
                var worldOrigin = GameController.IngameState.Data.GetGridScreenPosition(entity.GridPosNum);
                Graphics.DrawLine(worldOrigin, worldDestination, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
            }

            if (drawSettings.World.DrawAttackEndPoint && shouldDrawCircle)
            {
                DrawCircleAtGridPosition(destination, renderComponent.BoundsNum.X / 3f, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor, drawSettings.World.DrawFilledCircle);
            }

            return;
        }

        DrawUnitMarker(entity, drawSettings, renderComponent, shouldDrawCircle, false);
    }

    private void DrawMovement(
        Entity entity,
        WAYGConfig drawSettings,
        Render renderComponent,
        Pathfinding pathfindingComponent,
        Vector2 playerGridPosition,
        float playerWorldZ,
        float mapZoom,
        bool shouldDrawCircle)
    {
        var pathNodes = pathfindingComponent.PathingNodes;
        if (pathNodes.Count == 0)
        {
            DrawUnitMarker(entity, drawSettings, renderComponent, shouldDrawCircle, false);
            return;
        }

        if (drawSettings.Map.Enable && drawSettings.Map.DrawDestination)
        {
            var mapPathNodes = new List<Vector2>(pathNodes.Count);
            foreach (var pathNode in pathNodes)
            {
                mapPathNodes.Add(GetMapPosition(new Vector2(pathNode.X, pathNode.Y), playerGridPosition, playerWorldZ, mapZoom));
            }

            DrawPolyline(mapPathNodes, drawSettings.Colors.MapColor, drawSettings.Map.LineThickness);
        }

        if (drawSettings.World.Enable)
        {
            var worldPathNodes = pathNodes.ConvertToVector2List();

            if (drawSettings.World.DrawLine && drawSettings.World.DrawDestination)
            {
                var screenPathNodes = QueryWorldScreenPositionsWithTerrainHeight(worldPathNodes);
                DrawPolyline(screenPathNodes, drawSettings.Colors.WorldColor, drawSettings.World.LineThickness);
            }

            if (drawSettings.World.DrawDestinationEndPoint && shouldDrawCircle)
            {
                var endPoint = worldPathNodes[^1];
                DrawCircleAtGridPosition(endPoint, renderComponent.BoundsNum.X / 3f, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor, drawSettings.World.DrawFilledCircle);
            }
        }

        DrawUnitMarker(entity, drawSettings, renderComponent, shouldDrawCircle, false);
    }

    private void DrawUnitMarker(Entity entity, WAYGConfig drawSettings, Render renderComponent, bool shouldDrawCircle, bool useAttackColor)
    {
        if (!drawSettings.World.AlwaysRenderWorldUnit || !shouldDrawCircle)
        {
            return;
        }

        var color = useAttackColor ? drawSettings.Colors.WorldAttackColor : drawSettings.Colors.WorldColor;
        if (drawSettings.World.DrawBoundingBox)
        {
            DrawBoundingBoxInWorld(entity.PosNum, color, renderComponent.BoundsNum, renderComponent.RotationNum.X);
            return;
        }

        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, renderComponent.BoundsNum.X, drawSettings.World.RenderCircleThickness, color);
    }

    private void DrawPolyline(List<Vector2> points, Color color, int thickness)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        if (Settings.SmoothPaths.Value && points.Count > 2)
        {
            Graphics.DrawPolyLine(points.ToArray(), color, thickness);
            return;
        }

        for (var index = 0; index < points.Count - 1; index++)
        {
            Graphics.DrawLine(points[index], points[index + 1], thickness, color);
        }
    }

    private Vector2 GetMapPosition(Vector2 gridPosition, Vector2 playerGridPosition, float playerWorldZ, float mapZoom)
    {
        var worldPosition = QueryGridPositionToWorldWithTerrainHeight(gridPosition);
        var verticalOffset = _isLargeMapVisible
            ? (worldPosition.Z - playerWorldZ) / (9f / mapZoom)
            : (worldPosition.Z - playerWorldZ) / 20f;

        return _mapCenter + MapProjectionHelper.DeltaInWorldToMinimapDelta(
            gridPosition - playerGridPosition,
            Diagonal,
            _isLargeMapVisible ? _largeMapScale : SmallMapScale,
            verticalOffset);
    }

    private void DrawCircleAtGridPosition(Vector2 gridPosition, float radius, int thickness, Color color, bool filled)
    {
        var worldGridPosition = new SharpDX.Vector2(gridPosition.X, gridPosition.Y).GridToWorld();
        var worldPosition = new Vector3(worldGridPosition.X, worldGridPosition.Y, GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
        DrawCircleInWorldPos(filled, worldPosition, radius, thickness, color);
    }

    private List<Vector2> QueryWorldScreenPositionsWithTerrainHeight(List<Vector2> gridPositions)
    {
        return gridPositions.Select(gridPosition => Camera.WorldToScreen(QueryGridPositionToWorldWithTerrainHeight(gridPosition))).ToList();
    }

    private Vector3 QueryGridPositionToWorldWithTerrainHeight(Vector2 gridPosition)
    {
        var worldGridPosition = new SharpDX.Vector2(gridPosition.X, gridPosition.Y).GridToWorld();
        return new Vector3(worldGridPosition.X, worldGridPosition.Y, GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
    }

    private void DrawCircleInWorldPos(bool drawFilledCircle, Vector3 position, float radius, int thickness, Color color)
    {
        var screenRect = new RectangleF(
            0f,
            0f,
            GameController.Window.GetWindowRectangleTimeCache.Size.Width,
            GameController.Window.GetWindowRectangleTimeCache.Size.Height);

        var entityScreenPosition = Camera.WorldToScreen(position);
        if (!IsEntityWithinScreen(entityScreenPosition, screenRect, ScreenAllowance))
        {
            return;
        }

        if (drawFilledCircle)
        {
            Graphics.DrawFilledCircleInWorld(position, radius, color);
        }
        else
        {
            Graphics.DrawCircleInWorld(position, radius, color, thickness);
        }
    }

    private void DrawBoundingBoxInWorld(Vector3 position, Color color, Vector3 bounds, float rotationRadians)
    {
        var screenRect = new RectangleF(
            0f,
            0f,
            GameController.Window.GetWindowRectangleTimeCache.Size.Width,
            GameController.Window.GetWindowRectangleTimeCache.Size.Height);

        var entityScreenPosition = Camera.WorldToScreen(position);
        if (IsEntityWithinScreen(entityScreenPosition, screenRect, ScreenAllowance))
        {
            Graphics.DrawBoundingBoxInWorld(position, color, bounds, rotationRadians);
        }
    }

    private static bool IsEntityWithinScreen(Vector2 entityPosition, RectangleF screenRect, float allowancePixels)
    {
        var left = screenRect.Left - allowancePixels;
        var right = screenRect.Right + allowancePixels;
        var top = screenRect.Top - allowancePixels;
        var bottom = screenRect.Bottom + allowancePixels;

        return entityPosition.X >= left && entityPosition.X <= right
               && entityPosition.Y >= top && entityPosition.Y <= bottom;
    }

    private WAYGConfig GetStandardConfig(string key)
    {
        return key switch
        {
            "Self" => Settings.Self,
            "Players" => Settings.Players,
            "All Friendlies" => Settings.Minions,
            "Normal Monster" => Settings.NormalMonster,
            "Magic Monster" => Settings.MagicMonster,
            "Rare Monster" => Settings.RareMonster,
            "Unique Monster" => Settings.UniqueMonster,
            _ => Settings.Self
        };
    }

    private IEnumerable<Entity> GetEntities(EntityType entityType)
    {
        return GameController.EntityListWrapper?.ValidEntitiesByType[entityType] ?? Enumerable.Empty<Entity>();
    }

    private bool ShouldTrackOtherEntities()
    {
        return Settings.CustomUnits.Any(customUnit => customUnit.Enable && !string.IsNullOrWhiteSpace(customUnit.MetadataSearch));
    }

    private void PushWindowStyle()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6f, 3f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 4f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3f);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.02f, 0.08f, 0.15f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.03f, 0.10f, 0.18f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.20f, 0.50f, 0.70f, 0.50f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.06f, 0.18f, 0.30f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.10f, 0.30f, 0.50f, 1f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.15f, 0.40f, 0.65f, 1f));
        ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.40f, 0.85f, 1.00f, 1f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.20f, 0.70f, 0.90f, 1f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.40f, 0.85f, 1.00f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.08f, 0.25f, 0.45f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.15f, 0.40f, 0.65f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.25f, 0.55f, 0.80f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.08f, 0.30f, 0.55f, 1f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.15f, 0.45f, 0.75f, 1f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.25f, 0.60f, 0.90f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.20f, 0.45f, 0.65f, 0.40f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.03f, 0.08f, 0.12f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.20f, 0.50f, 0.70f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.35f, 0.65f, 0.85f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.50f, 0.80f, 1.00f, 1f));
    }

    private bool ModernSlider(string id, ref int value, int min, int max)
    {
        if (_activeSliderEditId == id)
        {
            return HandleSliderTextInput(id, ref value, min, max);
        }

        float floatValue = value;
        if (!ModernSliderFloat(id, ref floatValue, min, max, out var valueClicked))
        {
            if (valueClicked)
            {
                _activeSliderEditId = id;
                SliderEditBuffers[id] = value.ToString();
            }
            return false;
        }

        value = (int)Math.Round((double)floatValue);
        return true;
    }

    private static bool HandleSliderTextInput(string id, ref int value, int min, int max)
    {
        if (!SliderEditBuffers.TryGetValue(id, out var buffer))
        {
            buffer = value.ToString();
            SliderEditBuffers[id] = buffer;
        }

        ImGui.SetKeyboardFocusHere();
        if (ImGui.InputText($"##{id}_edit", ref buffer, 10, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
        {
            if (int.TryParse(buffer, out var newValue))
            {
                value = Math.Clamp(newValue, min, max);
            }
            _activeSliderEditId = null;
            SliderEditBuffers.Remove(id);
            return true;
        }

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsItemHovered())
        {
            _activeSliderEditId = null;
            SliderEditBuffers.Remove(id);
        }

        SliderEditBuffers[id] = buffer;
        return false;
    }

    private static uint PackColor(float red, float green, float blue, float alpha)
    {
        return (uint)(alpha * 255) << 24 | (uint)(blue * 255) << 16 | (uint)(green * 255) << 8 | (uint)(red * 255);
    }

    private static bool ModernSliderFloat(string id, ref float value, float min, float max, out bool valueClicked)
    {
        valueClicked = false;
        var labelSize = ImGui.CalcTextSize(id);
        var valueText = ((int)value).ToString();
        var valueSize = ImGui.CalcTextSize(valueText);
        var height = Math.Max(labelSize.Y, valueSize.Y) + 6f;
        var cursor = ImGui.GetCursorScreenPos();
        var totalWidth = ImGui.GetContentRegionAvail().X;

        var labelPosition = cursor;
        var valuePosition = new Vector2(cursor.X + totalWidth - valueSize.X, cursor.Y + (height - valueSize.Y) * 0.5f);
        var lineStartX = cursor.X + labelSize.X + 12f;
        var lineEndX = cursor.X + totalWidth - valueSize.X - 12f;
        var lineLength = lineEndX - lineStartX;
        var lineY = cursor.Y + height * 0.5f;

        var valueRectMin = valuePosition;
        var valueRectMax = new Vector2(valuePosition.X + valueSize.X, valuePosition.Y + valueSize.Y);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var mousePos = ImGui.GetMousePos();
            if (mousePos.X >= valueRectMin.X && mousePos.X <= valueRectMax.X &&
                mousePos.Y >= valueRectMin.Y && mousePos.Y <= valueRectMax.Y)
            {
                valueClicked = true;
            }
        }

        var sliderButtonWidth = lineLength + 20f;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f, height + 2f));
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        ImGui.PushStyleColor(ImGuiCol.Text, 0);
        var clicked = ImGui.InvisibleButton($"##{id}", new Vector2(sliderButtonWidth, height));
        ImGui.PopStyleColor(4);
        ImGui.PopStyleVar();

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddText(labelPosition, PackColor(0.70f, 0.85f, 1.00f, 1f), id);
        drawList.AddText(valuePosition, PackColor(0.40f, 0.90f, 1.00f, 1f), valueText);

        if (lineLength <= 0f || max <= min)
        {
            return false;
        }

        var normalized = Math.Clamp((value - min) / (max - min), 0f, 1f);
        var dotX = lineStartX + normalized * lineLength;

        drawList.AddLine(new Vector2(lineStartX, lineY), new Vector2(lineEndX, lineY), PackColor(0.08f, 0.25f, 0.40f, 1f), 2f);
        drawList.AddLine(new Vector2(lineStartX, lineY), new Vector2(dotX, lineY), PackColor(0.20f, 0.75f, 0.95f, 0.7f), 2f);

        var isActive = ImGui.IsItemActive();
        var isHovered = ImGui.IsItemHovered();
        var dotRadius = isActive ? 7f : isHovered ? 6.5f : 5.5f;
        var dotColor = isActive
            ? PackColor(0.40f, 0.90f, 1.00f, 1.0f)
            : isHovered
                ? PackColor(0.30f, 0.80f, 0.95f, 1.0f)
                : PackColor(0.20f, 0.70f, 0.90f, 1.0f);

        drawList.AddCircleFilled(new Vector2(dotX, lineY), dotRadius, dotColor);
        drawList.AddCircleFilled(new Vector2(dotX, lineY), dotRadius - 2f, PackColor(0.02f, 0.08f, 0.15f, 1f));
        drawList.AddCircleFilled(new Vector2(dotX, lineY), dotRadius - 3.5f, dotColor);

        if (!valueClicked && (isActive || (clicked && isHovered)))
        {
            var mousePosition = ImGui.GetMousePos();
            var newNormalized = Math.Clamp((mousePosition.X - lineStartX) / lineLength, 0f, 1f);
            value = Math.Clamp(min + newNormalized * (max - min), min, max);
            return true;
        }

        return false;
    }
}
