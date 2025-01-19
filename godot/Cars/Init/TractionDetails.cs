using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init;

public class TractionDetails : IHaveParts {
    [JsonIgnore]
    private PartReader PartReader { get; init; }
    public List<Part> Parts { get; init; } = [];

    [PartField(0d, HowToApply.Set, HigherIs.Neutral)]
    public double LatMaxSlip;
    [PartField(0d, HowToApply.Set)]
    public double LatGripMax;
    [PartField(0d, HowToApply.Set)]
    public double LatPeakLength;
    [PartField(0d, HowToApply.Set, HigherIs.Bad)]
    public double LatPeakDecay;

    [PartField(0d, HowToApply.Set, HigherIs.Neutral)]
    public double LongMaxSlip;
    [PartField(0d, HowToApply.Set)]
    public double LongGripMax;
    [PartField(0d, HowToApply.Set)]
    public double LongPeakLength;
    [PartField(0d, HowToApply.Set, HigherIs.Bad)]
    public double LongPeakDecay;

    public TractionDetails() {
        PartReader = new PartReader(this);
    }

    public static TractionDetails LoadFromFile(string name) {
        var tractionDetails = FileLoader.ReadJsonFile<TractionDetails>("Cars", "Init", "Data", name + ".json");

        tractionDetails.LoadSelf();
        return tractionDetails;
    }

    public void LoadSelf() {
        if (PartReader.ValidateAndSetFields() is string str) {
            throw new Exception("Traction Details value not set, see: " + str);
        }
    }

    public IEnumerable<PartResult> GetResultsInTree() => PartReader.GetResults();
    public IEnumerable<Part> GetAllPartsInTree() => Parts;
}
