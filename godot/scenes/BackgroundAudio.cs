using Godot;
using murph9.RallyGame2.godot.Utilities.Audio;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.scenes;

public partial class BackgroundAudio : Node {
    private static readonly float SAMPLE_RATE = 44100f;
    private const float BACKGROUND_CHORD_CHANGE_TIME = 3f;
    private static readonly float GLOBAL_VOLUME = 0;

    private readonly List<ISynth> _synths = [];
    private readonly ChordChainer _chordPicker;
    private readonly Envelope _noteEnvelope;

    private AudioStreamGeneratorPlayback _playback;
    private AudioStreamPlayer _player;
    private double _chordChangeTimer;

    public BackgroundAudio() {
        _noteEnvelope = new Envelope(0.02, 0.125, 0.3, 0.5, 0.2);

        var chordEnvelope = new Envelope(0.02, 0.125, BACKGROUND_CHORD_CHANGE_TIME - .2, .7, 0.2);
        _synths.Add(new Synth1() { Volume = GLOBAL_VOLUME * 0.4f, Envelope = chordEnvelope.Copy() });
        _synths.Add(new Synth1() { Volume = GLOBAL_VOLUME * 0.4f, Envelope = chordEnvelope.Copy() });
        _synths.Add(new Synth1() { Volume = GLOBAL_VOLUME * 0.4f, Envelope = chordEnvelope.Copy() });
        _synths.Add(new Synth1() { Volume = GLOBAL_VOLUME * 0.4f, Envelope = chordEnvelope.Copy() });

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

        if (_chordChangeTimer < 0) {
            _chordPicker.SetRandomRelatedChord();
            SetBackgroundChord(_chordPicker.CurrentChord);
            _chordChangeTimer = BACKGROUND_CHORD_CHANGE_TIME;
        }
        _chordChangeTimer -= delta;

        foreach (var synth in _synths.ToArray()) {
            synth._Process(delta);

            // if (synth.EnvelopeEnded) {
                // _synths.Remove(synth);
            // } // TODO figure out how to remove only note synths
        }

        FillBuffers();
    }

    private void SetBackgroundChord(Chord chord) {
        _synths[0].Freq = Note.MidiNoteToFrequency(chord.Notes[0].Midi);
        _synths[0].ResetEnvelope();
        _synths[1].Freq = Note.MidiNoteToFrequency(chord.Notes[1].Midi);
        _synths[0].ResetEnvelope();
        _synths[2].Freq = Note.MidiNoteToFrequency(chord.Notes[2].Midi);
        _synths[0].ResetEnvelope();
        if (chord.Notes.Length > 3) {
            _synths[3].Freq = Note.MidiNoteToFrequency(chord.Notes[3].Midi);
            _synths[3].Playing = true;
            _synths[0].ResetEnvelope();
        } else {
            _synths[3].Playing = false;
        }
    }

    private void PlayNote(Note note, double end) {
        var o = new Synth1() {
            Freq = Note.MidiNoteToFrequency(note.Midi),
            Volume = GLOBAL_VOLUME,
            Envelope = _noteEnvelope.Copy()
        };
        _synths.Add(o);
    }

    private void FillBuffers() {
        int framesAvailable = _playback.GetFramesAvailable();

        var oscs = _synths.SelectMany(x => x.Oscillators).ToArray();
        var buffers = new Vector2[oscs.Length][];

        for (int i = 0; i < oscs.Length; i++) {
            buffers[i] = oscs[i].GenFrames(framesAvailable, SAMPLE_RATE);
        }
        for (var i = 0; i < framesAvailable; i++) {
            var sum = new Vector2();
            for (var j = 0; j < oscs.Length; j++)
                sum += buffers[j][i];
            _playback.PushFrame(sum / buffers.Length);
        }
    }
}
