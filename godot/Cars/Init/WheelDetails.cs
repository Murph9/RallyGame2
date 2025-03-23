using Godot;

namespace murph9.RallyGame2.godot.Cars.Init;

public record WheelDetails {
    public string ModelName;

    public float Radius;
    public float Mass;
    public float Width;

    public float TyreWearRate;

    public int Id;
    public Vector3 Position;
}
