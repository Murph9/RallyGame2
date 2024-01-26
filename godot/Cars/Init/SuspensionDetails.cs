using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init;

#pragma warning disable IDE0044 // Add readonly modifier, because all private fields are loaded by json
public class SuspensionDetails : IHaveParts {
    [PartField(0, PartReader.APPLY_SET)]
    private float FrontAntiroll;
    [PartField(0, PartReader.APPLY_SET, HigherIs.Neutral)]
    private float FrontComp;
    [PartField(0, PartReader.APPLY_SET)]
    private float FrontMaxTravel;
    [PartField(0, PartReader.APPLY_SET, HigherIs.Bad)]
    private float FrontMinTravel;
    [PartField(0, PartReader.APPLY_SET)]
    private float FrontPreloadDistance;
    [PartField(0, PartReader.APPLY_SET, HigherIs.Neutral)]
    private float FrontRelax;
    [PartField(0, PartReader.APPLY_SET)]
    private float FrontStiffness;
    [PartField(0, PartReader.APPLY_SET)]
    private float RearAntiroll;
    [PartField(0, PartReader.APPLY_SET, HigherIs.Neutral)]
    private float RearComp;
    [PartField(0, PartReader.APPLY_SET)]
    private float RearMaxTravel;
    [PartField(0, PartReader.APPLY_SET, HigherIs.Bad)]
    private float RearMinTravel;
    [PartField(0, PartReader.APPLY_SET)]
    private float RearPreloadDistance;
    [PartField(0, PartReader.APPLY_SET, HigherIs.Neutral)]
    private float RearRelax;
    [PartField(0, PartReader.APPLY_SET)]
    private float RearStiffness;

    [JsonIgnore]
	private PartReader PartReader { get; init; }
    public List<Part> Parts { get; set; } = [];

    public SuspensionDetails() {
        PartReader = new PartReader(this);
    }

    public IEnumerable<PartResult> GetResults() => PartReader.GetResults();

    public CarSusDetails Front => new () {
        antiroll = FrontAntiroll,
        comp = FrontComp,
        maxTravel = FrontMaxTravel,
        minTravel = FrontMinTravel,
        preloadDistance = FrontPreloadDistance,
        relax = FrontRelax,
        stiffness = FrontStiffness
    };
    public CarSusDetails Rear => new () {
        antiroll = RearAntiroll,
        comp = RearComp,
        maxTravel = RearMaxTravel,
        minTravel = RearMinTravel,
        preloadDistance = RearPreloadDistance,
        relax = RearRelax,
        stiffness = RearStiffness
    };

    public void LoadSelf() {
        if (PartReader.ValidateAndSetFields() is string str) {
            throw new Exception("Suspension Details value not set, see: " + str);
        }
    }

    public static SuspensionDetails LoadFromFile(string name) {
        var tractionDetails = FileLoader.ReadJsonFile<SuspensionDetails>("Cars", "Init", "Data", name + ".json");

        tractionDetails.LoadSelf();
        return tractionDetails;
    }
}


public record CarSusDetails
{
	// travel values are relative to wheel offset pos
	public float minTravel; // m [-0.3 - 0.3] upper travel length - closer to car
	public float maxTravel; // m [-0.3 - 0.3] lower travel length - closer to ground
	public float TravelTotal() { return maxTravel - minTravel; }

	public float preloadDistance; // m [~0.2 spring travel at max sus travel]
	public float stiffness; // kg/mm [10-200]
	public float antiroll; // kg/mm [2 - 20]

	public float comp; // [0.2] should be less than relax
	public float relax; // [0.3]
	public float Compression() { return comp * 2 * Mathf.Sqrt(stiffness); }
	public float Relax() { return relax * 2 * Mathf.Sqrt(stiffness); }
}
#pragma warning restore IDE0044 // Add readonly modifier
