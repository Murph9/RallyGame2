using Godot;

namespace murph9.RallyGame2.godot.Cars.Init;

public record WheelDetails {
	public string modelName;

	public float radius;
	public float mass;
	public float width;

	public int id;
	public Vector3 position;
    // public WheelTraction traction;
}
