using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace murph9.RallyGame2.godot.Utilities.Audio;

public partial class ChordChainer {
    private static readonly List<Chord> ALL_CHORDS = [];
    static ChordChainer() {
        for (var i = Note.A2_MIDI; i <= Note.D5_MIDI; i++) {
            ALL_CHORDS.Add(new Chord("Major", i, GenerateChordNotes(ChordType.MAJOR, i)));
            ALL_CHORDS.Add(new Chord("Major inv1", i, GenerateChordNotes(ChordType.MAJOR, i, 1)));
            ALL_CHORDS.Add(new Chord("Major inv2", i, GenerateChordNotes(ChordType.MAJOR, i, 2)));

            ALL_CHORDS.Add(new Chord("Major 7", i, GenerateChordNotes(ChordType.MAJOR7, i)));
            ALL_CHORDS.Add(new Chord("Minor 7 inv1", i, GenerateChordNotes(ChordType.MAJOR7, i, 1)));
            ALL_CHORDS.Add(new Chord("Minor 7 inv2", i, GenerateChordNotes(ChordType.MAJOR7, i, 2)));
            ALL_CHORDS.Add(new Chord("Minor 7 inv3", i, GenerateChordNotes(ChordType.MAJOR7, i, 3)));

            ALL_CHORDS.Add(new Chord("Minor", i, GenerateChordNotes(ChordType.MINOR, i)));
            ALL_CHORDS.Add(new Chord("Major inv1", i, GenerateChordNotes(ChordType.MINOR, i, 1)));
            ALL_CHORDS.Add(new Chord("Major inv2", i, GenerateChordNotes(ChordType.MINOR, i, 2)));

            ALL_CHORDS.Add(new Chord("Minor 7", i, GenerateChordNotes(ChordType.MINOR7, i)));
            ALL_CHORDS.Add(new Chord("Minor 7 inv1", i, GenerateChordNotes(ChordType.MINOR7, i, 1)));
            ALL_CHORDS.Add(new Chord("Minor 7 inv2", i, GenerateChordNotes(ChordType.MINOR7, i, 2)));
            ALL_CHORDS.Add(new Chord("Minor 7 inv3", i, GenerateChordNotes(ChordType.MINOR7, i, 3)));
        }
    }

    public static Note[] GenerateChordNotes(ChordType type, int root, int inversion = 0) {
        int[] indexes;
        if (type == ChordType.MAJOR) {
            indexes = [0, 4, 7];
        } else if (type == ChordType.MINOR) {
            indexes = [0, 3, 7];
        } else if (type == ChordType.MAJOR7) {
            indexes = [0, 4, 7, 11];
        } else if (type == ChordType.MINOR7) {
            indexes = [0, 3, 7, 10];
        } else
            throw new Exception("Invalid chord type: " + type);

        // calc inversions
        if (inversion == 0) {
            // do nothing, yay
        } else if (inversion == 1) {
            indexes[0] += 12;
        } else if (inversion == 2) {
            indexes[0] += 12;
            indexes[1] += 12;
        } else if (inversion == 3 && indexes.Length == 4) {
            indexes[0] += 12;
            indexes[1] += 12;
            indexes[2] += 12;
        } else {
            throw new Exception("Invalid inversion value: " + inversion + " for " + type);
        }
        return indexes.Select(x => new Note(x + root)).ToArray();
    }

    private Queue<Chord> _prevChords = new();
    public Chord CurrentChord { get; private set; }

    public void SetRandomChord() {
        CurrentChord = ALL_CHORDS[GD.RandRange(0, ALL_CHORDS.Count - 1)];
        _prevChords.Enqueue(CurrentChord);
    }

    public void SetRandomRelatedChord(int diff = 1) {
        var similarChords = ALL_CHORDS.Where(x => x.NoteDiff(CurrentChord).Count() == diff).ToArray();
        try {
            if (similarChords.Length < 1) {
                Console.WriteLine("Chord " + CurrentChord + " has no similar chords");
                similarChords = [.. ALL_CHORDS]; // if this happens pick a totally different chord as we don't have options
            }
            CurrentChord = similarChords[GD.RandRange(0, similarChords.Length - 1)];
        } catch (Exception e) {
            Console.WriteLine(e);
        }
        _prevChords.Enqueue(CurrentChord);
    }
}
