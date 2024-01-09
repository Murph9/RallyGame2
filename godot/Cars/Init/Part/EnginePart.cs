using System;

namespace murph9.RallyGame2.godot.Cars.Init.Part;

public class EnginePart {
    public string Name { get; set; }
    public EngineProperties[] Levels { get; set; }
    public int CurrentLevel { get; set; }
    public void LowerRestrictions(EngineProperties engine) {
        var level = Levels[CurrentLevel];

        engine.PistonCount = Math.Min(engine.PistonCount, level.PistonCount);

        engine.IdleDrag = Math.Min(engine.IdleDrag, level.IdleDrag);

        engine.Compression = Math.Min(engine.Compression, level.Compression);
        engine.CombustionEffiency = Math.Min(engine.CombustionEffiency, level.CombustionEffiency);
        engine.CylinderBore = Math.Min(engine.CylinderBore, level.CylinderBore);
        engine.StrokeLength = Math.Min(engine.StrokeLength, level.StrokeLength);
        engine.TurboAirMult = Math.Min(engine.TurboAirMult, level.TurboAirMult);

        engine.MaxIntakeAirPiston = Math.Min(engine.MaxIntakeAirPiston, level.MaxIntakeAirPiston);
        engine.MaxExhaustAirPiston = Math.Min(engine.MaxExhaustAirPiston, level.MaxExhaustAirPiston);
        engine.MaxRpm = Math.Min(engine.MaxRpm, level.MaxRpm);
        engine.TransmissionEfficency = Math.Min(engine.TransmissionEfficency, level.TransmissionEfficency);

        engine.CoolingRate = Math.Min(engine.CoolingRate, level.CoolingRate);

        engine.PeakTorqueRPM = Math.Min(engine.PeakTorqueRPM, level.PeakTorqueRPM);
    }
}
