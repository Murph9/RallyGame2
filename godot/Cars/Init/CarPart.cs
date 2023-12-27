namespace murph9.RallyGame2.godot.Cars.Init;

public enum CarPart {
	Chassis, //main model
	Exhaust1, // only one side
	Exhaust2, //only one side
	wheel_fl, //front left
	wheel_fr, //front right
	wheel_rl, //rear left
	wheel_rr, //rear right
	
	Headlight_L, //only one side
	Taillight_L, //only one side

	Collision // model for collision in bullet physics, must be quite a bit off the ground to prevent clipping
}