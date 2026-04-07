using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System;

namespace murph9.RallyGame2.godot.Cars.AI;

public class CarRoughCalc {

    public static double CalcBestAccel(CarDetails carDetails, float carVelocity) {
        var engine = carDetails.Engine;
        var curGear = carDetails.GearForSpeed(carVelocity);
        var curRpm = carDetails.RpmAtSpeed(curGear, carVelocity);
        var engineToruque = engine.CalcTorqueFor(curRpm) * carDetails.TransGearRatios[curGear] * carDetails.TransFinaldrive * carDetails.Engine.TransmissionEfficiency * carDetails.WheelDetails[0].Radius;

        var drag = carDetails.QuadraticDrag(new Vector3(carVelocity, 0, 0)).Length();

        return engineToruque - drag;
    }

    public static double BestRadiusAtSpeed(CarDetails carDetails, float carVelocity) {
        // r = (m*v*v)/f
        // f = (LatGripMax * m)
        // r = (m*v*v)/(LatGripMax * m)
        // so r = v*v / LatGripmax
        return carVelocity * carVelocity / carDetails.TractionDetails.LatGripMax / Mathf.Abs(Main.DEFAULT_GRAVITY.Y);
    }

    public static double BestSpeedAtRadius(CarDetails carDetails, double radius) {
        // see above for mass removal
        // r = v*v / LatGripMax
        // v*v = r * LatGripMax
        // v = sqrt(r * LatGripMax)
        return Mathf.Sqrt(Math.Abs(radius) * carDetails.TractionDetails.LatGripMax);
    }

    /// <summary>
    /// Estimates the car's terminal top speed in km/h by stepping through velocities
    /// in the highest gear until engine thrust can no longer overcome aerodynamic drag.
    /// </summary>
    public static float EstimateTopSpeed(CarDetails carDetails) {
        const float STEP = 1f; // m/s
        const float MAX_SEARCH = 200f; // m/s (~720 km/h) — safety upper bound

        for (float v = STEP; v < MAX_SEARCH; v += STEP) {
            if (CalcBestAccel(carDetails, v) <= 0)
                return v;
        }

        return MAX_SEARCH;
    }
}
