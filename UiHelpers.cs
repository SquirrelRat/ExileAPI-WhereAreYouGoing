using ImGuiNET;
using SharpDX;
using Vector4 = System.Numerics.Vector4;

namespace WhereAreYouGoing;

internal static class UiHelpers
{
    public static bool Checkbox(string label, bool value)
    {
        ImGui.Checkbox(label, ref value);
        return value;
    }

    public static int IntDrag(string label, int value, int minValue, int maxValue, float dragSpeed)
    {
        var currentValue = value;
        ImGui.DragInt(label, ref currentValue, dragSpeed, minValue, maxValue);
        return currentValue;
    }

    public static Color ColorPicker(string label, Color inputColor)
    {
        var color = inputColor.ToVector4();
        var pickerColor = new Vector4(color.X, color.Y, color.Z, color.W);
        if (ImGui.ColorEdit4(label, ref pickerColor, ImGuiColorEditFlags.AlphaBar))
        {
            return new Color(pickerColor.X, pickerColor.Y, pickerColor.Z, pickerColor.W);
        }

        return inputColor;
    }

    public static Vector4 GetEntityColor(string key) => key switch
    {
        "Self" => new Vector4(0.4f, 0.8f, 0.4f, 1f),
        "Players" => new Vector4(0.4f, 0.8f, 0.4f, 1f),
        "All Friendlies" => new Vector4(0.7f, 0.4f, 0.9f, 1f),
        "Normal Monster" => new Vector4(0.8f, 0.8f, 0.8f, 1f),
        "Magic Monster" => new Vector4(0.4f, 0.6f, 0.9f, 1f),
        "Rare Monster" => new Vector4(1f, 0.9f, 0.3f, 1f),
        "Unique Monster" => new Vector4(1f, 0.6f, 0.3f, 1f),
        _ => new Vector4(0.8f, 0.8f, 0.8f, 1f)
    };
}
