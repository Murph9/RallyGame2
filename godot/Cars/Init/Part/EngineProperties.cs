namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class EngineProperties {

    // all properties have max value defaults as all parts apply restrictions
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
