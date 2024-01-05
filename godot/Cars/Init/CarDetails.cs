using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Init.Part;

namespace murph9.RallyGame2.godot.Cars.Init;

public record CarDetails
{
    public string name;
    public string carModel;

    public float cam_lookAtHeight; //from the middle of the model up
	public float cam_offsetLength; //from the middle of the model back
	public float cam_offsetHeight; //from the middle of the model up
	public float cam_shake;

    public float mass; //kg (total, do NOT add wheel or engine mass/inertia to this)

	public float areo_drag;
	public float areo_lineardrag; //0.003 to 0.02 (dimensionless number)
	public float areo_crossSection; //m^2 front area
	public float areo_downforce; //not a default yet

	//travel values are relative to wheel offset pos
	public CarSusDetails susF;
	public CarSusDetails susR;

	////////
	//Drivetrain stuff
	public bool driveFront, driveRear;

	public string engine_file;
    public EngineDetails Engine;

    public float[] auto_gearUpSpeed; //m/s for triggering the next gear [calculated]
    public float[] auto_gearDownSpeed; // m/s for triggering the next gear [calculated]
	public float auto_changeTime;

	//NOTE: please check torque curves are at the crank before using this value as anything other than 1.0f
	public float trans_effic; //apparently 0.9 is common (power is lost to rotating the transmission gears)
	public float trans_finaldrive; //helps set the total drive ratio
	public float[] trans_gearRatios; //reverse,gear1,gear2,g3,g4,g5,g6,...

	public float trans_powerBalance; //Only used in all drive cars, 0 front <-> 1 rear

	public bool nitro_on;
	public float nitro_force;
	public float nitro_rate;
	public float nitro_max;

	public bool fuelEnabled;
	public float fuelMax = 80;
	public float fuelRpmRate = 0.00003f;

	public float brakeMaxTorque;

	////////
	//Wheels
	public float w_steerAngle; //in radians

	public float wheelLoadQuadratic;
	public WheelDetails[] wheelData;

	//no idea category
	public float minDriftAngle;
	public Vector3 JUMP_FORCE;

    public CarSusDetails SusByWheelNum(int i) {
        return i < 2 ? susF : susR;
    }

    public float GetGearUpSpeed(int gear) {
        return auto_gearUpSpeed[gear];
    }
    public float GetGearDownSpeed(int gear) {
        return auto_gearDownSpeed[gear];
    }
    public float SpeedAtRpm(int gear, int rpm) {
        return rpm / (trans_gearRatios[gear] * trans_finaldrive * (60 / (Mathf.Pi*2))) * DriveWheelRadius();
    }
    public int RpmAtSpeed(int gear, float speed) {
        return (int)(speed * (trans_gearRatios[gear] * trans_finaldrive * (60 / (Mathf.Pi*2))) / DriveWheelRadius());
    }

	// https://en.wikipedia.org/wiki/Automobile_drag_coefficient#Drag_area
	public Vector3 QuadraticDrag(Vector3 velocity) {
		float dragx = -1.225f * areo_drag * areo_crossSection * velocity.X * Mathf.Abs(velocity.X);
		float dragy = -1.225f * areo_drag * areo_crossSection * velocity.Y * Mathf.Abs(velocity.Y);
		float dragz = -1.225f * areo_drag * areo_crossSection * velocity.Z * Mathf.Abs(velocity.Z);
        // Optional: use a cross section for each xyz direction to make a realistic drag feeling
        // but what we would really need is angle of attack -> much harder
		return new Vector3(dragx, dragy, dragz);
	}
	//linear drag component (https://en.wikipedia.org/wiki/Rolling_resistance)
	public float RollingResistance(int w_id, float susForce) {
		return susForce*areo_lineardrag/wheelData[w_id].radius;
	}

	public float Wheel_inertia(int w_id) {
		//NOTE: PERF: this is a disc, pls make a thicc pipe so its closer to real life
		return wheelData[w_id].mass*wheelData[w_id].radius*wheelData[w_id].radius/2;
	}
	public float E_inertia() { //car internal engine + wheel inertia

		float wheels = 0;
		for (int i = 0; i < wheelData.Length; i++)
			wheels += Wheel_inertia(i);

		if (driveFront && driveRear) {
			return Engine.EngineMass + wheels*4;
		}
		return Engine.EngineMass + wheels*2;
	}

	//get the max power and rpm
	public (float, float) GetMaxPower() {
		float max = 0;
		float maxrpm = 0;
		for (int i = 0; i < Engine.MaxRpm; i += 10) {
			float prevmax = max;
			max = Mathf.Max(max, (float)Engine.CalcTorqueFor(i) * (1000*i)/9549);
			if (prevmax != max) maxrpm = i;
		} // http://www.autospeed.com/cms/article.html?&title=Power-versus-Torque-Part-1&A=108647
		return (max, maxrpm*1000);
	}

	public float DriveWheelRadius() {
		if (driveFront && driveRear)
			return (wheelData[0].radius + wheelData[1].radius + wheelData[2].radius + wheelData[3].radius) / 4f;
		if (driveFront)
			return (wheelData[0].radius + wheelData[1].radius) / 2f;
		if (driveRear)
			return (wheelData[2].radius + wheelData[3].radius) / 2f;

		throw new ArgumentException("No drive wheels set, no wheel radius found.");
    }
}
