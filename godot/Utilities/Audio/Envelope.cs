using Godot;

namespace murph9.RallyGame2.godot.Utilities.Audio;

public record Envelope(double Attack, double Decay, double Sustain, double SustainVolume, double Release) {
    public readonly static Envelope BASIC = new (0, 0, double.MaxValue, 1, 0);

    public double Current { get; set; } // handy inbuilt tracking

    public double TotalLength => Attack + Decay + Sustain + Release;
    public bool Ended => Current > TotalLength;

    public double Volume() {
        if (Current < Attack) {
            return Current / Attack; // ramp up to 1 at attack
        } else if (Current < Attack + Decay) {
            var inPhase = Current - Attack;
            return Mathf.Lerp(1, SustainVolume, inPhase / Decay); // drop down to sustain strength after decay
        } else if (Current < Attack + Decay + Sustain) {
            return SustainVolume; // hold at sustain volume
        } else if (Current < Attack + Decay + Sustain + Release) {
            var inPhase = Current - (Attack + Decay + Sustain);
            return Mathf.Lerp(SustainVolume, 0, inPhase / Release); // decay to 0
        }

        return 0d;
    }

    public Envelope Copy() => this with { Current = 0 };
}
