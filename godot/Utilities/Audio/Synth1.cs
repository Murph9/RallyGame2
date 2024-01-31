namespace murph9.RallyGame2.godot.Utilities.Audio;

interface ISynth {
    Oscillator[] Oscillators { get; }
    bool Playing { get; set; }
    float Volume { get; set; }
    double Freq { get; set; }
    Envelope Envelope { get; set; }
    bool EnvelopeEnded { get; }
    void _Process(double delta);
    void ResetEnvelope();
}

public class Synth1 : ISynth {

    public Oscillator[] Oscillators { get; init; }

    public bool Playing {
        get => Oscillators[0].Playing;
        set {
            Oscillators[0].Playing = value;
            Oscillators[1].Playing = value;
        }
    }

    public float Volume {
        get => Oscillators[0].MasterVolume;
        set {
            Oscillators[0].MasterVolume = value;
            Oscillators[1].MasterVolume = value;
        }
    }

    public double Freq {
        get => Oscillators[0].Freq;
        set {
            Oscillators[0].Freq = value;
            Oscillators[1].Freq = value * 0.995f;
        }
    }

    public Envelope Envelope { get; set; } = Envelope.BASIC;
    public bool EnvelopeEnded => Envelope.Ended;

    public Synth1() {
        Oscillators = [
            new Oscillator() { Wave = WaveType.Saw },
            new Oscillator() { Wave = WaveType.Saw }
        ];
    }

    public void _Process(double delta) {
        Envelope.Current += delta;

        Oscillators[0].EnvelopeVolume = (float)Envelope.Volume();
        Oscillators[1].EnvelopeVolume = (float)Envelope.Volume();
    }

    public void ResetEnvelope() => Envelope.Current = 0;
}
