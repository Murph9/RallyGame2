using Godot;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Audio;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class BackgroundAudio : Node {
    private static readonly float SAMPLE_RATE = 44100f;
    private const float BACKGROUND_CHORD_CHANGE_TIME = 3f;
    private static readonly float GLOBAL_VOLUME = 0;

    class OscNote(Oscillator oscillator, double timer) {
        public Oscillator Oscillator { get; private set; } = oscillator;
        public double Timer { get; set; } = timer;
    }

    private readonly List<Oscillator> _oscillators = [];
    private readonly ChordChainer _chordPicker;
    private readonly List<OscNote> _oscillatorEnd = [];

    private AudioStreamGeneratorPlayback _playback;
    private AudioStreamPlayer _player;
    private double _chordChangeTimer;

    public BackgroundAudio() {
        _oscillators.Add(new Oscillator() { Wave = WaveType.Saw, Volume = GLOBAL_VOLUME * 0.4f });
        _oscillators.Add(new Oscillator() { Wave = WaveType.Saw, Volume = GLOBAL_VOLUME * 0.4f });
        _oscillators.Add(new Oscillator() { Wave = WaveType.Saw, Volume = GLOBAL_VOLUME * 0.4f });
        _oscillators.Add(new Oscillator() { Wave = WaveType.Saw, Volume = GLOBAL_VOLUME * 0.4f });

        _chordPicker = new ChordChainer();
        _chordPicker.SetRandomChord();
        SetBackgroundChord(_chordPicker.CurrentChord);
    }

    public override void _Ready() {
        var g = new AudioStreamGenerator() {
            MixRate = SAMPLE_RATE,
            BufferLength = 0.1f
        };
        _player = new AudioStreamPlayer() {
            Stream = g,
            Autoplay = true,
            VolumeDb = Mathf.LinearToDb(0.25f),
            Bus = "Music"
        };
        AddChild(_player);

        _player.Play();
        _playback = (AudioStreamGeneratorPlayback)_player.GetStreamPlayback();
    }

    public override void _Process(double delta) {
        if (GLOBAL_VOLUME <= 0) return; // perf

        FillBuffers();

        if (_chordChangeTimer < 0) {
            _chordPicker.SetRandomRelatedChord();
            SetBackgroundChord(_chordPicker.CurrentChord);
            _chordChangeTimer = BACKGROUND_CHORD_CHANGE_TIME;
        }
        _chordChangeTimer -= delta;

        foreach (var osTimer in _oscillatorEnd.ToList()) {
            osTimer.Timer -= delta;
            if (osTimer.Timer < 0) {
                _oscillators.Remove(osTimer.Oscillator);
                _oscillatorEnd.Remove(osTimer);
            }
        }
    }

    private void SetBackgroundChord(Chord chord) {
        _oscillators[0].Freq = Note.MidiNoteToFrequency(chord.Notes[0].Midi);
        _oscillators[1].Freq = Note.MidiNoteToFrequency(chord.Notes[1].Midi);
        _oscillators[2].Freq = Note.MidiNoteToFrequency(chord.Notes[2].Midi);
        if (chord.Notes.Length > 3) {
            _oscillators[3].Freq = Note.MidiNoteToFrequency(chord.Notes[3].Midi);
            _oscillators[3].Playing = true;
        } else {
            _oscillators[3].Playing = false;
        }
    }

    private void PlayNote(Note note, double end) {
        var o = new Oscillator() {
            Freq = Note.MidiNoteToFrequency(note.Midi),
            Wave = WaveType.Saw,
            Volume = GLOBAL_VOLUME
        };
        _oscillators.Add(o);
        _oscillatorEnd.Add(new OscNote(o, end));
    }

    private void FillBuffers() {
        int framesAvailable = _playback.GetFramesAvailable();
        var buffers = new Vector2[_oscillators.Count][];
        foreach (var (osc, index) in _oscillators.WithIndex()) {
            buffers[index] = osc.GenFrames(framesAvailable, SAMPLE_RATE);
        }
        for (var i = 0; i < framesAvailable; i++) {
            var sum = new Vector2();
            for (var j = 0; j < _oscillators.Count; j++)
                sum += buffers[j][i];
            _playback.PushFrame(sum / _oscillators.Count);
        }
    }
}
