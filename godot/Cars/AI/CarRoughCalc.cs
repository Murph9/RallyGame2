using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Sim;
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
        // but m cancels out of f as f is (LatGripMax * m)
        // so r = v*v / LatGripmax
        return carVelocity * carVelocity / carDetails.TractionDetails.LatGripMax;
    }

    public static double BestSpeedAtRadius(CarDetails carDetails, double radius) {
        // r = v*v / LatGripMax
        // v*v = r * LatGripMax
        // v = sqrt(r * LatGripMax)
        return Mathf.Sqrt(Math.Abs(radius) * carDetails.TractionDetails.LatGripMax);
    }

    public static bool IfCanHitPoint(Car car, Vector3 target) {
        return IfCanHitPoint(car.Details, target, car.RigidBody.GlobalPosition, car.RigidBody.LinearVelocity);
    }
    public static bool IfCanHitPoint(CarDetails carDetails, Vector3 target, Vector3 carPosition, Vector3 carVelocity) {
        // TODO test

        // Calculates based on the ideal situation whether the car can make the point at the current speed
        var posV2 = V2FromXZ(carPosition);
        var targetV2 = V2FromXZ(target);

        var bestRadius = BestRadiusAtSpeed(carDetails, carVelocity.Length());

        // generate a curved cone that the car can reach using 2 large circles on either side
        // using the speed as the tangent of the circles
        var radiusDir = new Vector2(carVelocity.Y, -carVelocity.X).Normalized() * (float)bestRadius;

        // circle 1
        var center1 = posV2 + radiusDir;
        var distance1 = (targetV2 - center1).Length();

        // circle 2
        var center2 = posV2 - radiusDir;
        var distance2 = (targetV2 - center2).Length();

        // finally return if the cone contains the target
        return distance1 > bestRadius && distance2 > bestRadius;
    }

    private static Vector2 V2FromXZ(Vector3 input) {
        return new Vector2(input.X, input.Z);
    }
}