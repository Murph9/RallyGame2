using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class EngineDetails {

    // other props that aren't generated from parts
    public string Name { get; set; }
    public int IdleRPM { get; set; }
    public int EngineInertia { get; set; }
    public string Sound { get; set; }
    public float[] BaseTorqueCurve { get; set; }
    public int BaseTorqueCurveMaxRPM => (BaseTorqueCurve.Length - 1) * 1000;

    // all properties have max value defaults as all parts apply restrictions
    public int PistonCount; // count

    public double IdleDrag; // ratio

    public double Compression; // ratio
    public double CombustionEfficiency; // %
    public double CylinderBore; // m
    public double StrokeLength; // m

    public double TurboAirMult; // ratio
    public double TurboAirStartRPM; // int

    public double IntakeAirEffiency; // L/s
    public double ExhaustAirEffiency; // L/s
    public int MaxRpm; // w/min
    public double TransmissionEfficiency; // ratio

    public double CoolingRate; // K / min

    public Dictionary<string, double> Values = new ();
    public List<Part> Parts { get; set; } = new List<Part>();

    public static EngineDetails LoadFromFile(string name) {
        var engineDetails = FileLoader.ReadJsonFile<EngineDetails>("Cars", "Init", "Data", name + ".json");

        engineDetails.LoadProps();
        return engineDetails;
    }

    public void LoadProps() {
        PistonCount = int.MaxValue;
        IdleDrag = double.MaxValue;
        Compression = double.MaxValue;
        CombustionEfficiency = double.MaxValue;
        CylinderBore = double.MaxValue;
        StrokeLength = double.MaxValue;
        TurboAirMult = double.MaxValue;
        TurboAirStartRPM = int.MaxValue;
        IntakeAirEffiency = double.MaxValue;
        ExhaustAirEffiency = double.MaxValue;
        MaxRpm = int.MaxValue;
        TransmissionEfficiency = double.MaxValue;
        CoolingRate = double.MaxValue;

        foreach (var part in Parts) {
            var partValues = part.GetLevel();

            PistonCount = GetAndMin(nameof(PistonCount), partValues, PistonCount);
            IdleDrag = GetAndMin(nameof(IdleDrag), partValues, IdleDrag);
            Compression = GetAndMin(nameof(Compression), partValues, Compression);
            CombustionEfficiency = GetAndMin(nameof(CombustionEfficiency), partValues, CombustionEfficiency);
            CylinderBore = GetAndMin(nameof(CylinderBore), partValues, CylinderBore);
            StrokeLength = GetAndMin(nameof(StrokeLength), partValues, StrokeLength);
            TurboAirMult = GetAndMin(nameof(TurboAirMult), partValues, TurboAirMult);
            TurboAirStartRPM = GetAndMin(nameof(TurboAirStartRPM), partValues, TurboAirStartRPM);
            IntakeAirEffiency = GetAndMin(nameof(IntakeAirEffiency), partValues, IntakeAirEffiency);
            ExhaustAirEffiency = GetAndMin(nameof(ExhaustAirEffiency), partValues, ExhaustAirEffiency);
            MaxRpm = GetAndMin(nameof(MaxRpm), partValues, MaxRpm);
            TransmissionEfficiency = GetAndMin(nameof(TransmissionEfficiency), partValues, TransmissionEfficiency);
            CoolingRate = GetAndMin(nameof(CoolingRate), partValues, CoolingRate);
        }

        // validation
        var engineSet = AreAllSet();
        if (!engineSet) {
            throw new Exception("Engine value not set, see" + this);
        }
    }

    private static double GetAndMin(string name, Dictionary<string, double> dict, double original) {
        if (dict.TryGetValue(name, out double value))
            return Mathf.Min(original, value);
        return original;
    }
    private static int GetAndMin(string name, Dictionary<string, double> dict, int original) {
        if (dict.TryGetValue(name, out double value))
            return Mathf.Min(original, (int)value);
        return original;
    }

    private double AreaOfPiston => Mathf.Pi * CylinderBore * CylinderBore / 4; // m^2
    private double DisplacementVolume => AreaOfPiston * StrokeLength * PistonCount; // m^3 (note that this is usually shown as cm^3 which is /1000)

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


    public bool AreAllSet() {
        return PistonCount != int.MaxValue
            && IdleDrag != double.MaxValue
            && Compression != double.MaxValue
            && CombustionEfficiency != double.MaxValue
            && CylinderBore != double.MaxValue
            && StrokeLength != double.MaxValue
            && TurboAirMult != double.MaxValue
            && TurboAirStartRPM != int.MaxValue
            && IntakeAirEffiency != double.MaxValue
            && ExhaustAirEffiency != double.MaxValue
            && MaxRpm != int.MaxValue
            && TransmissionEfficiency != double.MaxValue
            && CoolingRate != double.MaxValue;
    }

    public Dictionary<string, double> AsDict() {
        return new Dictionary<string, double>() {
            {nameof(PistonCount), PistonCount},
            {nameof(IdleDrag), IdleDrag},
            {nameof(Compression), Compression},
            {nameof(CombustionEfficiency), CombustionEfficiency},
            {nameof(CylinderBore), CylinderBore},
            {nameof(StrokeLength), StrokeLength},
            {nameof(TurboAirMult), TurboAirMult},
            {nameof(TurboAirStartRPM), TurboAirStartRPM},
            {nameof(IntakeAirEffiency), IntakeAirEffiency},
            {nameof(ExhaustAirEffiency), ExhaustAirEffiency},
            {nameof(MaxRpm), MaxRpm},
            {nameof(TransmissionEfficiency), TransmissionEfficiency},
            {nameof(CoolingRate), CoolingRate}
        };
    }
}
