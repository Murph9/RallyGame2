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
        IntakeAirEffiency = double.MaxValue;
        ExhaustAirEffiency = double.MaxValue;
        MaxRpm = int.MaxValue;
        TransmissionEfficiency = double.MaxValue;
        CoolingRate = double.MaxValue;

        foreach (var part in Parts) {
            double value;
            var partValues = part.GetLevel();
            if (partValues.TryGetValue(nameof(PistonCount), out value))
                PistonCount = Mathf.Min(PistonCount, (int)value);
            if (partValues.TryGetValue(nameof(IdleDrag), out value))
                IdleDrag = Mathf.Min(IdleDrag, value);
            if (partValues.TryGetValue(nameof(Compression), out value))
                Compression = Mathf.Min(Compression, value);
            if (partValues.TryGetValue(nameof(CombustionEfficiency), out value))
                CombustionEfficiency = Mathf.Min(CombustionEfficiency, value);
            if (partValues.TryGetValue(nameof(CylinderBore), out value))
                CylinderBore = Mathf.Min(CylinderBore, value);
            if (partValues.TryGetValue(nameof(StrokeLength), out value))
                StrokeLength = Mathf.Min(StrokeLength, value);
            if (partValues.TryGetValue(nameof(TurboAirMult), out value))
                TurboAirMult = Mathf.Min(TurboAirMult, value);
            if (partValues.TryGetValue(nameof(IntakeAirEffiency), out value))
                IntakeAirEffiency = Mathf.Min(IntakeAirEffiency, value);
            if (partValues.TryGetValue(nameof(ExhaustAirEffiency), out value))
                ExhaustAirEffiency = Mathf.Min(ExhaustAirEffiency, value);
            if (partValues.TryGetValue(nameof(MaxRpm), out value))
                MaxRpm = Mathf.Min(MaxRpm, (int)value);
            if (partValues.TryGetValue(nameof(TransmissionEfficiency), out value))
                TransmissionEfficiency = Mathf.Min(TransmissionEfficiency, value);
            if (partValues.TryGetValue(nameof(CoolingRate), out value))
                CoolingRate = Mathf.Min(CoolingRate, value);
        }

        // validation
        var engineSet = AreAllSet();
        if (!engineSet) {
            throw new Exception("Engine value not set, see" + this);
        }
    }

    private double AreaOfPiston => Mathf.Pi * CylinderBore * CylinderBore / 4; // m^2
    private double DisplacementVolume => AreaOfPiston * StrokeLength * PistonCount; // m^3 (note that this is usually shown as cm^3 which is /1000)

    public double CalcTorqueFor(int rpm) {
        // airflow is a multiplier on torque and anymore 'flow' over the max (intake or exhaust) has a negative effect
        var airFlowEfficiency = Math.Min(IntakeAirEffiency, ExhaustAirEffiency);

        // TODO turbo things

        var torqueBase = LerpTorque(rpm);
        return airFlowEfficiency * CombustionEfficiency * torqueBase; // Nm
    }

    private double LerpTorque(int rpm) {
        if (rpm <= 0) return 0;

        // torque curve will need to be stretched to work with MaxRpm:
        // TODO it reduces torque at lower RPM
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
            {nameof(IntakeAirEffiency), IntakeAirEffiency},
            {nameof(ExhaustAirEffiency), ExhaustAirEffiency},
            {nameof(MaxRpm), MaxRpm},
            {nameof(TransmissionEfficiency), TransmissionEfficiency},
            {nameof(CoolingRate), CoolingRate}
        };
    }
}

/*
TODO

http://www.thecartech.com/subjects/engine/engine_formulas.htm
I think we just need to make up a shape and modify it using the parts

The torque curve of an engine is typically divided into different sections, each influenced by specific factors. Here are the main sections of a torque curve and the factors that affect each one:

1. **Low-End Torque (Low RPMs):**
   - **Factors Influencing Low-End Torque:**
     - **Cylinder Size and Design:** Larger cylinder size and well-designed combustion chambers enhance low-end torque.
     - **Intake and Exhaust Systems:** Efficient intake and exhaust systems promote better airflow at low RPMs.
     - **Forced Induction:** Turbochargers and superchargers can provide additional low-end torque by compressing air.

2. **Mid-Range Torque (Mid RPMs):**
   - **Factors Influencing Mid-Range Torque:**
     - **Valvetrain Configuration:** Properly designed valvetrain systems contribute to good mid-range torque.
     - **Fuel Injection System:** Optimal fuel injection improves combustion efficiency in the mid-range.
     - **Turbocharging or Supercharging:** Forced induction systems play a significant role in boosting mid-range torque.

3. **Peak Torque (Peak RPM):**
   - **Factors Influencing Peak Torque:**
     - **Engine Size and Design:** The overall design and displacement of the engine impact the maximum torque achievable.
     - **Compression Ratio:** A well-optimized compression ratio contributes to peak torque.
     - **Valvetrain and Timing:** Properly tuned valvetrains and ignition timing affect peak torque delivery.

4. **High-End Torque (High RPMs):**
   - **Factors Influencing High-End Torque:**
     - **Valvetrain and Camshaft Design:** High-performance camshafts and optimized valvetrains enhance torque at high RPMs.
     - **Exhaust System:** Efficient exhaust systems are crucial for expelling gases at high RPMs.
     - **Engine Management Systems:** Advanced engine control units (ECUs) can adjust parameters for optimal performance at high RPMs.

5. **Overrevving (Beyond Peak RPM):**
   - **Factors Influencing Overrevving:**
     - **Valvetrain Design:** The ability of the valvetrain to keep up with high RPMs without causing valve float affects overrevving.
     - **Engine Management Systems:** Engine control units may limit or allow overrevving based on the design and purpose of the engine.

It's important to note that the torque curve is a result of a delicate balance between various factors, and engineers aim to design engines that deliver a broad and usable torque band across different driving conditions. Additionally, the specific goals of the engine (e.g., efficiency, power, fuel economy) and the intended use of the vehicle influence the design choices made by manufacturers.


*/