using Godot;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Utilities;

public static class ColourHelper {
    public static Color SkidOnRGBScale(float skidFraction) {
        // 0 is white, 0.333f is green, 0.666f is red, 1 is blue
        skidFraction = Mathf.Clamp(Mathf.Abs(skidFraction / 3f), 0, 1);

        if (skidFraction < 1f / 3f)
            return LerpColour(skidFraction * 3, Colors.White, Colors.Green);
        else if (skidFraction < 2f / 3f)
            return LerpColour((skidFraction - 1f / 3f) * 3, Colors.Green, Colors.Red);
        return LerpColour((skidFraction - 2f / 3f) * 3, Colors.Red, Colors.Blue);
    }

    public static Color TyreWearOnGreenToRed(float tyreWear) {
        var realValue = CalcWheelTraction.TotalGripFromWear(tyreWear);

        if (realValue >= 1) {
            return Colors.PaleGreen;
        }

        return LerpColour((float)realValue / 0.2f, Colors.PaleGreen, Colors.MediumVioletRed);
    }

    public static Color LerpColour(float value, Color a, Color b) {
        return new Color(a).Lerp(b, value);
    }
}
