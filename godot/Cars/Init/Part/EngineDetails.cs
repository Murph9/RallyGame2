using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class EngineDetails : EngineProperties {

    // other props that aren't generated
    public string Name { get; set; }
    public int IdleRPM = 1000;
    public int EngineMass = 20; // although this might be a sum all props in the future
    public string Sound { get; set; }

    public List<EnginePart> Parts { get; set; } = new List<EnginePart>();

    public static EngineDetails Load(string name) {
        var engineDetails = FileLoader.ReadJsonFile<EngineDetails>("Cars", "Init", "Data", name + ".json");

        foreach (var part in engineDetails.Parts) {
            part.LowerRestrictions(engineDetails);
        }

        // validation
        var engineSet = engineDetails.AreAllSet();
        if (!engineSet) {
            throw new Exception("Engine value not set, see" + engineDetails);
        }

        return engineDetails;
    }

    private double AreaOfPiston => Mathf.Pi * CylinderBore * CylinderBore / 4; // m^2
    private double DisplacementVolume => AreaOfPiston * StrokeLength * PistonCount; // m^3 (note that this is usually shown as cm^3 which is /1000)
    private double MaxEffectiveAirflowPerPiston => Math.Min(MaxIntakeAirPiston, MaxExhaustAirPiston);
    private double TorquePerStroke => Compression * AreaOfPiston * 10000 * StrokeLength; // Nm

    public double AirflowAtRpm(int rpm) {
        // a inverse quadratic function which peaks at a specific point
        var flowEfficency = Math.Min(1, 1 - Math.Pow((rpm - PeakTorqueRPM)/MaxRpm, 2));

        return flowEfficency * Math.Min(rpm * DisplacementVolume * 1e3, MaxEffectiveAirflowPerPiston) / MaxEffectiveAirflowPerPiston;
    }

    public double CalcTorqueFor(int rpm) {
        var airflowPerPiston = AirflowAtRpm(rpm);
        var torque = PistonCount * TorquePerStroke * airflowPerPiston * CombustionEffiency; // Nm
        return torque; // Nm
    }

    public double CalcKwFor(int rpm) {
        return CalcTorqueFor(rpm) * 2 * Mathf.Pi * rpm / (60 * 1000); //kW
    }
    public static double TorqueToKw(double torque, int rpm) {
        return torque * 2 * Mathf.Pi * rpm / (60 * 1000); //kW
    }
}
