using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Utilities.Audio;

public class Chord(string Name, int Root, Note[] Notes) {
    public string Name { get; init; } = Name;
    public int Root { get; init; } = Root;
    public Note[] Notes { get; init; } = Notes;

    public override string ToString() {
        return $"{Note.NameFromMidi(Root)} {Name} -> [{string.Join(", ", Notes.Select(x => x.ToString()))}]";
    }

    public IEnumerable<Note> NoteDiff(Chord other) {
        return Notes.Except(other.Notes).Union(other.Notes.Except(Notes));
    }
}
