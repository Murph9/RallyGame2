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

    // all properties have max value defaults as all parts apply restrictions
    public int PistonCount; // count

    public double IdleDrag; // ratio

    public double Compression; // ratio
    public double CombustionEffiency; // %
    public double CylinderBore; // m
    public double StrokeLength; // m
    public double TurboAirMult; // ratio

    public double MaxIntakeAirPiston; // L/s
    public double MaxExhaustAirPiston; // L/s
    public int MaxRpm; // w/min
    public double TransmissionEfficency; // ratio

    public double PeakTorqueRPM; // w/min

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
        CombustionEffiency = double.MaxValue;
        CylinderBore = double.MaxValue;
        StrokeLength = double.MaxValue;
        TurboAirMult = double.MaxValue;
        MaxIntakeAirPiston = double.MaxValue;
        MaxExhaustAirPiston = double.MaxValue;
        MaxRpm = int.MaxValue;
        TransmissionEfficency = double.MaxValue;
        CoolingRate = double.MaxValue;
        PeakTorqueRPM = int.MaxValue;

        foreach (var part in Parts) {
            double value;
            var partValues = part.GetLevel();
            if (partValues.TryGetValue(nameof(PistonCount), out value))
                PistonCount = Mathf.Min(PistonCount, (int)value);
            if (partValues.TryGetValue(nameof(IdleDrag), out value))
                IdleDrag = Mathf.Min(IdleDrag, value);
            if (partValues.TryGetValue(nameof(Compression), out value))
                Compression = Mathf.Min(Compression, value);
            if (partValues.TryGetValue(nameof(CombustionEffiency), out value))
                CombustionEffiency = Mathf.Min(CombustionEffiency, value);
            if (partValues.TryGetValue(nameof(CylinderBore), out value))
                CylinderBore = Mathf.Min(CylinderBore, value);
            if (partValues.TryGetValue(nameof(StrokeLength), out value))
                StrokeLength = Mathf.Min(StrokeLength, value);
            if (partValues.TryGetValue(nameof(TurboAirMult), out value))
                TurboAirMult = Mathf.Min(TurboAirMult, value);
            if (partValues.TryGetValue(nameof(MaxIntakeAirPiston), out value))
                MaxIntakeAirPiston = Mathf.Min(MaxIntakeAirPiston, value);
            if (partValues.TryGetValue(nameof(MaxExhaustAirPiston), out value))
                MaxExhaustAirPiston = Mathf.Min(MaxExhaustAirPiston, value);
            if (partValues.TryGetValue(nameof(MaxRpm), out value))
                MaxRpm = Mathf.Min(MaxRpm, (int)value);
            if (partValues.TryGetValue(nameof(TransmissionEfficency), out value))
                TransmissionEfficency = Mathf.Min(TransmissionEfficency, value);
            if (partValues.TryGetValue(nameof(CoolingRate), out value))
                CoolingRate = Mathf.Min(CoolingRate, value);
            if (partValues.TryGetValue(nameof(PeakTorqueRPM), out value))
                PeakTorqueRPM = Mathf.Min(PeakTorqueRPM, value);
        }

        // validation
        var engineSet = AreAllSet();
        if (!engineSet) {
            throw new Exception("Engine value not set, see" + this);
        }
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


    public bool AreAllSet() {
        return PistonCount != int.MaxValue
            && IdleDrag != double.MaxValue
            && Compression != double.MaxValue
            && CombustionEffiency != double.MaxValue
            && CylinderBore != double.MaxValue
            && StrokeLength != double.MaxValue
            && TurboAirMult != double.MaxValue
            && MaxIntakeAirPiston != double.MaxValue
            && MaxExhaustAirPiston != double.MaxValue
            && MaxRpm != int.MaxValue
            && TransmissionEfficency != double.MaxValue
            && CoolingRate != double.MaxValue
            && PeakTorqueRPM != int.MaxValue;
    }

    public Dictionary<string, double> AsDict() {
        return new Dictionary<string, double>() {
            {nameof(PistonCount), PistonCount},
            {nameof(IdleDrag), IdleDrag},
            {nameof(Compression), Compression},
            {nameof(CombustionEffiency), CombustionEffiency},
            {nameof(CylinderBore), CylinderBore},
            {nameof(StrokeLength), StrokeLength},
            {nameof(TurboAirMult), TurboAirMult},
            {nameof(MaxIntakeAirPiston), MaxIntakeAirPiston},
            {nameof(MaxExhaustAirPiston), MaxExhaustAirPiston},
            {nameof(MaxRpm), MaxRpm},
            {nameof(TransmissionEfficency), TransmissionEfficency},
            {nameof(CoolingRate), CoolingRate},
            {nameof(PeakTorqueRPM), PeakTorqueRPM},
        };
    }
}
