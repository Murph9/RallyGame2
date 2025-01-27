using Godot;

namespace murph9.RallyGame2.godot.Utilities.Audio;

public partial class Oscillator {

    public WaveType Wave { get; set; } = WaveType.Sine;
    public double Freq { get; set; }
    public float MasterVolume { get; set; } = 1; // NOT decibels because this is dumb
    public float EnvelopeVolume { get; set; } = 1; // also not decibels
    public bool Playing { get; set; } = true;

    private double _phase = 0;

    public Vector2[] GenFrames(int frames, double sampleRate) {
        var increment = Freq / sampleRate;
        var output = new Vector2[frames];
        for (int i = 0; i < frames; i++) {
            if (!Playing) {
                output[i] = Vector2.Zero;
                break;
            }
            switch (Wave) {
                case WaveType.Sine:
                    output[i] = Vector2.One * (float)Mathf.Sin(_phase * Mathf.Tau);
                    break;
                case WaveType.Saw:
                    output[i] = Vector2.One * (float)(_phase * 2 - 1);
                    break;
                case WaveType.Triangle:
                    var saw = (float)(_phase * 2 - 1);
                    output[i] = Vector2.One * -(Mathf.Abs(saw) - .5f) * 2f;
                    break;
                case WaveType.Square:
                    output[i] = Vector2.One * (_phase < 0.5f ? 1 : -1);
                    break;
                case WaveType.Noise:
                    output[i] = Vector2.One * (GD.Randf() * 2f - 1f);
                    break;
                default:
                    break;
            }
            output[i] *= MasterVolume * EnvelopeVolume;
            _phase = Mathf.PosMod(_phase + increment, 1.0);
        }
        return output;
    }

    public void Sync() {
        _phase = 0;
    }
}
