using Godot;

namespace murph9.RallyGame2.godot.Utilities.Audio;

public record Note(int Midi) {
    public const int A2_MIDI = 45;
    public const int C3_MIDI = 48;
    public const int D5_MIDI = 74;
    public static readonly string[] NOTE_NAMES = ["A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#"];

    public static double MidiNoteToFrequency(int midiNumber) {
        return 440 * Mathf.Pow(2, ((double)midiNumber - 69) / 12);
    }

    public override int GetHashCode() {
        return Midi.GetHashCode();
    }
    public override string ToString() {
        return NameFromMidi(Midi);
    }

    public static int OctaveFromMidi(int midi) {
        return Mathf.FloorToInt((midi - C3_MIDI) / 12f) + 3;
    }

    public static string NameFromMidi(int midi) {
        var note = NOTE_NAMES[(midi - A2_MIDI) % 12];
        return $"{note}{OctaveFromMidi(midi)}";
    }
}
