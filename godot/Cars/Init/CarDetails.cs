using Godot;
using murph9.RallyGame2.godot.Cars.Init.Part;
using Newtonsoft.Json;
using System;

namespace murph9.RallyGame2.godot.Cars.Init;

public class CarDetails
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

	public float WheelInertiaNoEngine(int w_id) {
		//this is a thin cylindrical shell
		return wheelData[w_id].mass * wheelData[w_id].radius * wheelData[w_id].radius;
	}

	public float WheelInertiaPlusEngine() { //car internal engine + wheel inertia

		float wheels = 0;
		for (int i = 0; i < wheelData.Length; i++)
			wheels += WheelInertiaNoEngine(i);

		if (driveFront && driveRear) {
			return Engine.EngineInertia + wheels*4;
		}
		return Engine.EngineInertia + wheels*2;
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

    public CarDetails Clone()
    {
        var serialized = JsonConvert.SerializeObject(this);
        return JsonConvert.DeserializeObject<CarDetails>(serialized);
    }
}
