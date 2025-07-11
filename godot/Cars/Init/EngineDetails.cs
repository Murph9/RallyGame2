using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init;

public class EngineDetails : IHaveParts {

    public string Name { get; set; }
    public int IdleRPM { get; set; }
    public int EngineInertia { get; set; }
    public string Sound { get; set; }
    public float[] BaseTorqueCurve { get; set; }
    public int BaseTorqueCurveMaxRPM => (BaseTorqueCurve.Length - 1) * 1000;

    [PartField(0d, HowToApply.Add, HigherIs.Bad)]
    public double EngineMass;

    [PartField(int.MaxValue, HowToApply.Min)]
    public int PistonCount; // count
    [PartField(double.MaxValue, HowToApply.Min, HigherIs.Bad)]
    public double IdleDrag; // ratio

    [PartField(double.MaxValue, HowToApply.Min)]
    public double Compression; // ratio
    [PartField(double.MaxValue, HowToApply.Min)]
    public double CombustionEfficiency; // %
    [PartField(double.MaxValue, HowToApply.Min)]
    public double CylinderBore; // m
    [PartField(double.MaxValue, HowToApply.Min)]
    public double StrokeLength; // m

    [PartField(double.MaxValue, HowToApply.Min)]
    public double TurboAirMult; // ratio
    [PartField(double.MaxValue, HowToApply.Min, HigherIs.Bad)]
    public double TurboAirStartRPM; // int

    [PartField(double.MaxValue, HowToApply.Min)]
    public double IntakeAirEffiency; // L/s
    [PartField(double.MaxValue, HowToApply.Min)]
    public double ExhaustAirEffiency; // L/s
    [PartField(int.MaxValue, HowToApply.Min)]
    public int MaxRpm; // w/min
    [PartField(double.MaxValue, HowToApply.Min)]
    public double TransmissionEfficiency; // ratio

    [PartField(double.MaxValue, HowToApply.Min)]
    public double CoolingRate; // K / min

    public bool FuelEnabled;
    [PartField(0f, HowToApply.Add, HigherIs.Bad)]
    public float FuelByRpmRate; // L/s

    [JsonIgnore]
    private PartReader PartReader { get; init; }
    public List<Part> Parts { get; init; } = [];

    public EngineDetails() {
        PartReader = new PartReader(this);
    }

    public static EngineDetails LoadFromFile(string name) {
        var engineDetails = FileLoader.ReadJsonFile<EngineDetails>("Cars", "Init", "Data", name + ".json");

        engineDetails.LoadSelf();
        return engineDetails;
    }

    public void LoadSelf() {
        if (PartReader.ValidateAndSetFields() is string str) {
            throw new Exception("Engine Details value not set, see: " + str);
        }
    }

    private double AreaOfPiston => Mathf.Pi * CylinderBore * CylinderBore / 4; // m^2
    private double DisplacementVolume => AreaOfPiston * StrokeLength * PistonCount; // m^3 (note that this is usually shown as cm^3 which is /1000)

    public (double, int) MaxTorque() {
        const int DIVISIONS = 10;
        var max = 0d;
        int atRpm = 0;
        for (int i = 0; i <= MaxRpm / (float)DIVISIONS; i++) {
            var torque = CalcTorqueFor(i * DIVISIONS);
            if (torque > max) {
                max = torque;
                atRpm = i * DIVISIONS;
            }
        }
        return (max, atRpm);
    }
    public (double, int) MaxKw() {
        const int DIVISIONS = 10;
        var max = 0d;
        int atRpm = 0;
        for (int i = 0; i <= MaxRpm / (float)DIVISIONS; i++) {
            var torque = CalcKwFor(i * DIVISIONS);
            if (torque > max) {
                max = torque;
                atRpm = i * DIVISIONS;
            }
        }
        return (max, atRpm);
    }

    public double CalcTorqueFor(int rpm) {

        // airflow is a multiplier but it needs to support in and out air
        var airFlowEfficiency = Math.Min(IntakeAirEffiency, ExhaustAirEffiency);

        // turbo multiplies airflow about its value, and negative below it
        if (TurboAirMult > 1) {
            if (rpm > TurboAirStartRPM)
                airFlowEfficiency *= TurboAirMult;
            else
                airFlowEfficiency /= TurboAirMult;
        }

        var torqueBase = LerpTorque(rpm);
        return airFlowEfficiency * CombustionEfficiency * torqueBase; // Nm
    }

    private double LerpTorque(int rpm) {
        if (rpm <= 0) return 0;

        // torque curve will need to be stretched to work with MaxRpm:
        var rpmFactor = BaseTorqueCurveMaxRPM / (float)MaxRpm;
        var rpmFloat = (float)rpm / 1000f * rpmFactor;

        var full = Mathf.FloorToInt(rpmFloat);
        var low = BaseTorqueCurve[Math.Clamp(full, 0, BaseTorqueCurve.Length - 1)];
        var high = BaseTorqueCurve[Math.Clamp(full + 1, 0, BaseTorqueCurve.Length - 1)];
        return Mathf.Lerp(low, high, rpmFloat - full);
    }

    public double CalcKwFor(int rpm) {
        return CalcTorqueFor(rpm) * 2 * Mathf.Pi * rpm / (60 * 1000); //kW
    }
    public static double TorqueToKw(double torque, int rpm) {
        return torque * 2 * Mathf.Pi * rpm / (60 * 1000); //kW
    }

    public IEnumerable<PartResult> GetPartResultsInTree() => PartReader.GetResults();
    public IEnumerable<Part> GetAllPartsInTree() => Parts;
}
