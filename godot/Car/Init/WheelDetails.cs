using System;
using Godot;

namespace murph9.RallyGame2.Car.Init;

public record WheelDetails {
	public string modelName;

	public float radius;
	public float mass;
	public float width;

    public string tractionType;

	public int i;
	public Vector3 position;
    // public WheelTraction traction;
}
