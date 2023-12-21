using System;
using Godot;

namespace murph9.RallyGame2.Car.Init;

public record CarSusDetails
{
	//travel values are relative to wheel offset pos
	public float min_travel; //[-0.3 - 0.3] upper travel length - closer to car
	public float max_travel; //[-0.3 - 0.3] lower travel length - closer to ground
	public float TravelTotal() { return max_travel - min_travel; }
	
	public float preload_force; //kg/mm [2.5ish]
	public float stiffness; //kg/mm [10-200]
	public float max_force; //kg/mm [50*carMass]
	public float antiroll; //??? [12ish]
	
	public float comp; //[0.2] //should be less than relax
	public float relax; //[0.3]
	public float Compression() { return comp * 2 * Mathf.Sqrt(stiffness); }
	public float Relax() { return relax * 2 * Mathf.Sqrt(stiffness); }
}
