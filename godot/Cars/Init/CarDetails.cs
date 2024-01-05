using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Init.Part;

namespace murph9.RallyGame2.godot.Cars.Init;

public record CarDetails
{
    public string name;
    public string carModel;

    public float camLookAtHeight; //from the middle of the model up
	public float camOffsetLength; //from the middle of the model back
	public float camOffsetHeight; //from the middle of the model up
	public float camShake;

    public float mass; //kg (total, do NOT add wheel or engine mass/inertia to this)

	public float aeroDrag;
	public float areoLinearDrag; //0.003 to 0.02 (dimensionless number)
	public float aeroCrossSection; //m^2 front area
	public float aeroDownforce; //not a default yet

	//travel values are relative to wheel offset pos
	public CarSusDetails susF;
	public CarSusDetails susR;

	////////
	//Drivetrain stuff
	public bool driveFront, driveRear;

	public string engineFileName;
    public EngineDetails Engine;

    public float[] autoGearUpSpeed; //m/s for triggering the next gear [calculated]
    public float[] autoGearDownSpeed; // m/s for triggering the next gear [calculated]
	public float autoChangeTime;

	//NOTE: please check torque curves are at the crank before using this value as anything other than 1.0f
	public float transEfficiency; //apparently 0.9 is common (power is lost to rotating the transmission gears)
	public float transFinaldrive; //helps set the total drive ratio
	public float[] transGearRatios; //reverse,gear1,gear2,g3,g4,g5,g6,...

	public float transPowerBalance; //Only used in all drive cars, 0 front <-> 1 rear

	public bool nitroEnabled;
	public float nitroForce;
	public float nitroRate;
	public float nitroMax;

	public bool fuelEnabled;
	public float fuelMax = 80;
	public float fuelRpmRate = 0.00003f;

	public float brakeMaxTorque;

	////////
	//Wheels
	public float wMaxSteerAngle; //in radians

	public float wheelLoadQuadratic;
	public WheelDetails[] wheelData;

	//no idea category
	public float minDriftAngle;
	public Vector3 JUMP_FORCE;

    public CarSusDetails SusByWheelNum(int i) {
        return i < 2 ? susF : susR;
    }

    public float GetGearUpSpeed(int gear) {
        return autoGearUpSpeed[gear];
    }
    public float GetGearDownSpeed(int gear) {
        return autoGearDownSpeed[gear];
    }
    public float SpeedAtRpm(int gear, int rpm) {
        return rpm / (transGearRatios[gear] * transFinaldrive * (60 / (Mathf.Pi*2))) * DriveWheelRadius();
    }
    public int RpmAtSpeed(int gear, float speed) {
        return (int)(speed * (transGearRatios[gear] * transFinaldrive * (60 / (Mathf.Pi*2))) / DriveWheelRadius());
    }

	// https://en.wikipedia.org/wiki/Automobile_drag_coefficient#Drag_area
	public Vector3 QuadraticDrag(Vector3 velocity) {
		float dragx = -1.225f * aeroDrag * aeroCrossSection * velocity.X * Mathf.Abs(velocity.X);
		float dragy = -1.225f * aeroDrag * aeroCrossSection * velocity.Y * Mathf.Abs(velocity.Y);
		float dragz = -1.225f * aeroDrag * aeroCrossSection * velocity.Z * Mathf.Abs(velocity.Z);
        // Optional: use a cross section for each xyz direction to make a realistic drag feeling
        // but what we would really need is angle of attack -> much harder
		return new Vector3(dragx, dragy, dragz);
	}
	//linear drag component (https://en.wikipedia.org/wiki/Rolling_resistance)
	public float RollingResistance(int w_id, float susForce) {
		return susForce*areoLinearDrag/wheelData[w_id].radius;
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
