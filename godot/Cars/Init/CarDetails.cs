using Godot;
using murph9.RallyGame2.godot.Cars.Init.Part;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

	public void LoadSelf(Vector3 gravity) {
		// calculate wheel positions based on the model
		Node3D carScene = null;
		try {
			var scene = GD.Load<PackedScene>("res://assets/" + carModel);
			carScene = scene.Instantiate<Node3D>();
			var wheelPoss = carScene.GetChildren()
				.OfType<Node3D>()
				.ToDictionary(x => x.Name.ToString());
			wheelData[0].id = 0;
			wheelData[0].position = wheelPoss[CarModelName.wheel_fl.ToString()].Position;
			wheelData[1].id = 1;
			wheelData[1].position = wheelPoss[CarModelName.wheel_fr.ToString()].Position;
			wheelData[2].id = 2;
			wheelData[2].position = wheelPoss[CarModelName.wheel_rl.ToString()].Position;
			wheelData[3].id = 3;
			wheelData[3].position = wheelPoss[CarModelName.wheel_rr.ToString()].Position;
		} catch (Exception e) {
			GD.Print(e);
		} finally {
			carScene?.QueueFree();
		}

		// validate that the wheels are in the correct quadrant for a car
        if (wheelData[0].position.X < 0 || wheelData[0].position.Z < 0)
            throw new Exception(CarModelName.wheel_fl + " should be in pos x and pos z");
        if (wheelData[1].position.X > 0 || wheelData[1].position.Z < 0)
            throw new Exception(CarModelName.wheel_fr + " should be in neg x and pos z");

        if (wheelData[2].position.X < 0 || wheelData[2].position.Z > 0)
            throw new Exception(CarModelName.wheel_rl + " should be in pos x and neg z");
        if (wheelData[3].position.X > 0 || wheelData[3].position.Z > 0)
            throw new Exception(CarModelName.wheel_rr + " should be in neg x and neg z");


        // Wheel validation
        float quarterMassForce = Mathf.Abs(gravity.Y) * mass / 4f;

        // generate the load quadratic value
        wheelLoadQuadratic = 1/(quarterMassForce*4);
        for (int i = 0; i < wheelData.Length; i++) {
            var sus = SusByWheelNum(i);

            // Validate that rest suspension position is within min and max
            float minSusForce = (sus.preloadForce + sus.stiffness) * 0 * 1000;
            float maxSusForce = sus.stiffness * (sus.preloadForce + sus.maxTravel - sus.minTravel) * 1000;
            if (quarterMassForce < minSusForce) {
                throw new Exception("!! Sus min range too high: " + quarterMassForce + " < " + minSusForce + ", decrease pre-load or stiffness");
            }
            if (quarterMassForce > maxSusForce) {
                throw new Exception("!! Sus max range too low: " + quarterMassForce + " > " + maxSusForce + ", increase pre-load or stiffness");
            }
        }


		// Output the optimal gear up change point based on the torque curve
        int redlineOffset = 500;
        var changeTimes = new List<(int, float)>();
        float maxTransSpeed = SpeedAtRpm(transGearRatios.Length - 1, Engine.MaxRpm - redlineOffset);
        for (float speed = 0; speed < maxTransSpeed; speed += 0.1f) {
            int bestGear = -1;
            float bestTorque = -1;
            for (int gear = 1; gear < transGearRatios.Length; gear++) {
                int rpm = RpmAtSpeed(gear, speed);
                if (rpm > Engine.MaxRpm - redlineOffset) //just a bit off of redline because its not that smooth
                    continue;
                float wheelTorque = (float)Engine.CalcTorqueFor(rpm) * transGearRatios[gear] * transFinaldrive;
                if (bestTorque < wheelTorque) {
                    bestTorque = wheelTorque;
                    bestGear = gear;
                }
                // This prints a more detailed graph: Log.p(speed * 3.6f, wheelTorque, gear);
            }

            // This prints a nice graph: Log.p(speed * 3.6f, bestTorque, bestGear);
            changeTimes.Add(new (bestGear, speed));
        }

        autoGearDownSpeed = new float[transGearRatios.Length];
        autoGearDownSpeed[0] = float.MaxValue; // never change out of reverse
        autoGearUpSpeed = new float[transGearRatios.Length];
        autoGearUpSpeed[0] = float.MaxValue; // never change out of reverse
        // Get the first and last value for each gear
		foreach (var entry in changeTimes.GroupBy(x => x.Item1)) {
			int gear = entry.Key;
			float downValue = entry.First().Item2;
            float upValue = entry.Last().Item2;

			// set the auto up and down changes
            autoGearDownSpeed[gear] = downValue - 2f; // buffer so they overlap a little
            autoGearUpSpeed[gear] = upValue;
		}

        // Checking that there is gear overlap between up and down (as it prevents the
        // car from changing gear):
        // [2>----[3>-<2]---<3] not [2>----<2]--[3>---<3]
        for (int i = 1; i < transGearRatios.Length - 1; i++) {
            if (GetGearUpSpeed(i) < GetGearDownSpeed(i + 1)) {
                throw new Exception("Gear overlap test failed for up: " + i + " down: " + (i + 1));
            }
        }
	}

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
