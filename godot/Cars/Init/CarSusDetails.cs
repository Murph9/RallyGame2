using Godot;

namespace murph9.RallyGame2.godot.Cars.Init;

public record CarSusDetails
{
	//travel values are relative to wheel offset pos
	public float minTravel; //[-0.3 - 0.3] upper travel length - closer to car
	public float maxTravel; //[-0.3 - 0.3] lower travel length - closer to ground
	public float TravelTotal() { return maxTravel - minTravel; }

	public float preloadForce; //kg/mm [~0.2 spring travel at max sus travel]
	public float stiffness; //kg/mm [10-200]
	public float antiroll; //kg/mm [2 - 20]

	public float comp; //[0.2] //should be less than relax
	public float relax; //[0.3]
	public float Compression() { return comp * 2 * Mathf.Sqrt(stiffness); }
	public float Relax() { return relax * 2 * Mathf.Sqrt(stiffness); }
}
