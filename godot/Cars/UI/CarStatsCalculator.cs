using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.Cars.UI;

public record CarStats(double Acceleration, double TopSpeed, double Handling, double Braking);

public static class CarStatsCalculator {

    public static CarStats ComputeStats(CarDetails carDetails) {
        if (carDetails == null) {
            return new CarStats(0, 0, 0, 0);
        }

        float maxTorque = (float)carDetails.Engine.MaxTorque().Item1;
        float maxKw = (float)carDetails.Engine.MaxKw().Item1;
        float drag = Mathf.Abs(carDetails.QuadraticDrag(new Vector3(27f, 0f, 0f)).X);

        var mass = carDetails.TotalMass;
        double accel = mass > 0 ? ClampToRange(SkewLog(maxTorque / mass, -1f, 0.75f), 0, 1) : 0;
        double topSpeed = drag > 0 ? ClampToRange(SkewLog(maxKw / drag, -2f, 10f), 0, 1) : 0;

        var longGrip = carDetails.TractionDetails.LongGripMax;
        var handlingRaw = (float)(carDetails.TractionDetails.LatGripMax / longGrip);
        double handling = ClampToRange(SkewLog(handlingRaw, -1f, 1f), 0, 1);

        var brakingRaw = carDetails.BrakeMaxTorque * longGrip / mass;
        double braking = mass > 0 ? ClampToRange(SkewLog(brakingRaw, 0f, 1f), 0, 1) : 0;

        return new CarStats(accel, topSpeed, handling, braking);
    }

    private static double ClampToRange(double value, double min, double max) {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return min;
        return Mathf.Clamp(value, min, max);
    }

    private static double SkewLog(double value, double preMin, double preMax) => SkewLog(value, preMin, preMax, 0, 1);
    private static double SkewLog(double value, double preMin, double preMax, double min, double max) {
        var mx = Mathf.Log(value - preMin) / Mathf.Log(preMax - preMin);
        return mx * (max - min) + min;
    }
}
