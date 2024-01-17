using Godot;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class EngineDetails {

    // other props that aren't generated from parts
    public string Name { get; set; }
    public int IdleRPM { get; set; }
    public int EngineInertia { get; set; }
    public string Sound { get; set; }
    public float[] BaseTorqueCurve { get; set; }
    public int BaseTorqueCurveMaxRPM => (BaseTorqueCurve.Length - 1) * 1000;

    // all fields have max value defaults as all parts apply restrictions
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

    public Dictionary<string, double> Values { get; private set; }= new ();
    public List<Part> Parts { get; set; } = new List<Part>();

    private static FieldInfo[] FIELD_CACHE;

    public static EngineDetails LoadFromFile(string name) {
        var engineDetails = FileLoader.ReadJsonFile<EngineDetails>("Cars", "Init", "Data", name + ".json");

        engineDetails.LoadSelf();
        return engineDetails;
    }

    public void LoadSelf() {
        var fields = GetFields();

        foreach (var field in fields) {
            if (field.FieldType == typeof(int))
                field.SetValue(this, int.MaxValue);
            if (field.FieldType == typeof(double))
                field.SetValue(this, double.MaxValue);
        }

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
            part.Validate();

            var partValues = part.GetLevel();

            foreach (var field in fields) {
                if (field.FieldType == typeof(int) && partValues.TryGetValue(field.Name, out double value))
                    field.SetValue(this, Mathf.Min((int)field.GetValue(this), (int)value));
                if (field.FieldType == typeof(double) && partValues.TryGetValue(field.Name, out value))
                    field.SetValue(this, Mathf.Min((double)field.GetValue(this), value));
            }

            part.PartColour = Color.FromHtml(part.Color);
        }

        // validation
        var engineSet = AreAllSet();
        if (!engineSet) {
            throw new Exception("Engine value not set, see" + this);
        }
    }

    private double AreaOfPiston => Mathf.Pi * CylinderBore * CylinderBore / 4; // m^2
    private double DisplacementVolume => AreaOfPiston * StrokeLength * PistonCount; // m^3 (note that this is usually shown as cm^3 which is /1000)

    public (double, int) MaxTorque() {
        const int DIVISIONS = 10;
        var max = 0d;
        int atRpm = 0;
        for (int i = 0; i <= MaxRpm/(float)DIVISIONS; i++) {
            var torque = CalcTorqueFor(i*DIVISIONS);
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
        for (int i = 0; i <= MaxRpm/(float)DIVISIONS; i++) {
            var torque = CalcKwFor(i*DIVISIONS);
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

    private FieldInfo[] GetFields() {
        if (FIELD_CACHE != null) return FIELD_CACHE;

        FIELD_CACHE = GetType().GetFields();
        return FIELD_CACHE;
    }

    public bool AreAllSet() {
        var fields = GetFields();

        foreach (var field in fields) {
            if (field.FieldType == typeof(int) && (int)field.GetValue(this) == int.MaxValue)
                return false;
            if (field.FieldType == typeof(double) && (double)field.GetValue(this) == double.MaxValue)
                return false;
        }

        return true;
    }

    public Dictionary<string, double> AsDict() {
        return GetFields().ToDictionary(x => x.Name, x => {
            if (x.FieldType == typeof(double))
                return (double)x.GetValue(this);
            if (x.FieldType == typeof(int))
                return (int)x.GetValue(this);

            return double.MinValue;
        });
    }

    public Dictionary<string, List<Part>> GetValueCauses() {
        var dict = AsDict().ToDictionary(x => x.Key, x => new List<Part>());

        foreach (var part in Parts) {
            var partValues = part.GetLevel();

            foreach (var field in GetFields()) {
                if (partValues.ContainsKey(field.Name))
                    dict[field.Name].Add(part);
            }
        }

        return dict;
    }
}
