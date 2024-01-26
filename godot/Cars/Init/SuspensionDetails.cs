using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init;

#pragma warning disable IDE0044 // Add readonly modifier, because all private fields are loaded by json
#pragma warning disable CS0649 // Will always have the default value, but also loaded by json
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

    public CarSusDetails Front => new (FrontAntiroll, FrontComp, FrontMaxTravel, FrontMinTravel, FrontPreloadDistance, FrontRelax, FrontStiffness);
    public CarSusDetails Rear => new (RearAntiroll, RearComp, RearMaxTravel, RearMinTravel, RearPreloadDistance, RearRelax, RearStiffness);

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
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // will always have default value


public record CarSusDetails(float Antiroll, float Comp, float MaxTravel, float MinTravel, float PreloadDistance, float Relax, float Stiffness)
{
	// travel values are relative to wheel offset pos
	// minTravel m [-0.3 - 0.3] upper travel length - closer to car
	// maxTravel m [-0.3 - 0.3] lower travel length - closer to ground
	// preloadDistance m [~ 0.2] spring travel at max sus travel
	// stiffness kg/mm [10-200] 10 is soft car, 100 is race car
	// antiroll kg/mm [2 - 20], same as stiffness between the axle's other wheel
	// comp [0.2] should be less than relax
	// relax [0.3]

	public float TravelTotal() { return MaxTravel - MinTravel; }
	public float Compression() { return Comp * 2 * Mathf.Sqrt(Stiffness); }
	public float Rebound() { return Relax * 2 * Mathf.Sqrt(Stiffness); }
}
