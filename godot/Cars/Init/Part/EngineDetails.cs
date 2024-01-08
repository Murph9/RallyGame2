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

public class EngineProperties {

    // all properties have max value defaults so all parts and restrictions
    public int PistonCount = int.MaxValue; // count

    public double IdleDrag = double.MaxValue; // ratio

    public double Compression = double.MaxValue; // ratio
    public double CombustionEffiency = double.MaxValue; // %
    public double CylinderBore = double.MaxValue; // m
    public double StrokeLength = double.MaxValue; // m
    public double TurboAirMult = double.MaxValue; // ratio

    public double MaxIntakeAirPiston = double.MaxValue; // L/s
    public double MaxExhaustAirPiston = double.MaxValue; // L/s
    public int MaxRpm = int.MaxValue; // w/min
    public double TransmissionEfficency = double.MaxValue; // ratio

    public double PeakTorqueRPM = int.MaxValue; // w/min

    public double CoolingRate = double.MaxValue; // K / min

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
}

public class EnginePart : EngineProperties {
    public string Name { get; set; }
    public void LowerRestrictions(EngineProperties engine) {
        engine.PistonCount = Math.Min(engine.PistonCount, PistonCount);

        engine.IdleDrag = Math.Min(engine.IdleDrag, IdleDrag);

        engine.Compression = Math.Min(engine.Compression, Compression);
        engine.CombustionEffiency = Math.Min(engine.CombustionEffiency, CombustionEffiency);
        engine.CylinderBore = Math.Min(engine.CylinderBore, CylinderBore);
        engine.StrokeLength = Math.Min(engine.StrokeLength, StrokeLength);
        engine.TurboAirMult = Math.Min(engine.TurboAirMult, TurboAirMult);

        engine.MaxIntakeAirPiston = Math.Min(engine.MaxIntakeAirPiston, MaxIntakeAirPiston);
        engine.MaxExhaustAirPiston = Math.Min(engine.MaxExhaustAirPiston, MaxExhaustAirPiston);
        engine.MaxRpm = Math.Min(engine.MaxRpm, MaxRpm);
        engine.TransmissionEfficency = Math.Min(engine.TransmissionEfficency, TransmissionEfficency);

        engine.CoolingRate = Math.Min(engine.CoolingRate, CoolingRate);

        engine.PeakTorqueRPM = Math.Min(engine.PeakTorqueRPM, PeakTorqueRPM);
    }
}
