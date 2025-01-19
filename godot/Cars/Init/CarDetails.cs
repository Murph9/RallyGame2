using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace murph9.RallyGame2.godot.Cars.Init;

public class CarDetails : IHaveParts {
	private static readonly JsonSerializerOptions CloneSerializeOptions = new () {
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		IncludeFields = true
	};

	public string Name;
    public string CarModel;

    public float CamLookAtHeight; // from the middle of the model up
	public float CamOffsetLength; // from the middle of the model back
	public float CamOffsetHeight; // from the middle of the model up
	public float CamShake;

	[PartField(0f, HowToApply.Add, HigherIs.Bad)]
    public float BodyMass; // kg (total, do NOT add wheel or engine mass/inertia to this)
	public double TotalMass => BodyMass + Engine.EngineMass + WheelDetails.Sum(x => x.Mass);

	public float AreoLinearDrag; //0.003 to 0.02 (dimensionless number)
	public float AeroCrossSection; //m^2 front area
	[PartField(0f, HowToApply.Add, HigherIs.Bad)]
	public float AeroDrag;
	[PartField(0f, HowToApply.Add)]
	public float AeroDownforce; //not a default yet

	// travel values are relative to wheel offset pos
	public string SuspensionFileName;
	public SuspensionDetails SuspensionDetails;

	public bool TractionControl;

	////////
	//Drivetrain stuff
	public bool DriveFront, DriveRear;

	public string EngineFileName;
    public EngineDetails Engine;
	public string TractionFileName;
	public TractionDetails TractionDetails;

	[JsonIgnore]
    public float[] AutoGearUpSpeed; // m/s for triggering the next gear [calculated]
	[JsonIgnore]
    public float[] AutoGearDownSpeed; // m/s for triggering the next gear [calculated]

	[PartField(0f, HowToApply.Set, HigherIs.Bad)]
	public float AutoChangeTime;

	[PartField(0f, HowToApply.Set, HigherIs.Neutral)]
	public float TransFinaldrive; // helps set the total drive ratio
	[PartField(new float[]{2f, 2f, 1f}, HowToApply.Set, HigherIs.Neutral)]
	public float[] TransGearRatios; // reverse,gear1,gear2,g3,g4,g5,g6,...
	[PartField(0f, HowToApply.Set, HigherIs.Neutral)]
	public float TransPowerBalance; // Only used in all wheel drive cars, 0 front <-> 1 rear

	[PartField(false, HowToApply.Set, HigherIs.Good, DefaultIs.Okay)]
	public bool NitroEnabled;
	[PartField(float.MinValue, HowToApply.Set)]
	public float NitroForce;
	[PartField(float.MinValue, HowToApply.Set)]
	public float NitroRate;
	[PartField(float.MinValue, HowToApply.Set)]
	public float NitroMax;

	public bool FuelEnabled;
	public float FuelMax = 80;
	public float FuelRpmRate = 0.00003f;

	[PartField(0f, HowToApply.Set)]
	public float BrakeMaxTorque;

	[JsonIgnore]
	private PartReader PartReader { get; init; }
    public List<Part> Parts { get; set; } = [];

	public float MaxSteerAngle; // radians [0.2 - 0.5]

	public WheelDetails[] WheelDetails;

	// no idea category
	public float MinDriftAngle;
	public Vector3 JUMP_FORCE;

	public CarDetails() {
		PartReader = new PartReader(this);
	}

	public void LoadSelf(Vector3 gravity) {
        if (PartReader.ValidateAndSetFields() is string str) {
            throw new Exception("Car Details value not set, see: " + str);
        }

		Engine.LoadSelf();
		TractionDetails.LoadSelf();
		SuspensionDetails.LoadSelf();

		// calculate wheel positions based on the model
		Node3D carScene = null;
		try {
			var scene = GD.Load<PackedScene>("res://assets/car/" + CarModel);
			carScene = scene.Instantiate<Node3D>();
			var modelWheelNodes = carScene.GetChildren()
				.OfType<Node3D>()
				.ToDictionary(x => x.Name.ToString());
			WheelDetails[0].Id = 0;
			WheelDetails[0].Position = modelWheelNodes[CarModelName.wheel_fl.ToString()].Position;
			WheelDetails[1].Id = 1;
			WheelDetails[1].Position = modelWheelNodes[CarModelName.wheel_fr.ToString()].Position;
			WheelDetails[2].Id = 2;
			WheelDetails[2].Position = modelWheelNodes[CarModelName.wheel_rl.ToString()].Position;
			WheelDetails[3].Id = 3;
			WheelDetails[3].Position = modelWheelNodes[CarModelName.wheel_rr.ToString()].Position;
		} catch (Exception e) {
			GD.Print(e);
		} finally {
			carScene?.QueueFree();
		}

		// validate that the wheels are in the correct quadrant for a car
        if (WheelDetails[0].Position.X < 0 || WheelDetails[0].Position.Z < 0)
            throw new Exception(CarModelName.wheel_fl + " should be in pos x and pos z");
        if (WheelDetails[1].Position.X > 0 || WheelDetails[1].Position.Z < 0)
            throw new Exception(CarModelName.wheel_fr + " should be in neg x and pos z");

        if (WheelDetails[2].Position.X < 0 || WheelDetails[2].Position.Z > 0)
            throw new Exception(CarModelName.wheel_rl + " should be in pos x and neg z");
        if (WheelDetails[3].Position.X > 0 || WheelDetails[3].Position.Z > 0)
            throw new Exception(CarModelName.wheel_rr + " should be in neg x and neg z");


        // Wheel validation
        float quarterMassForce = Mathf.Abs(gravity.Y) * (float)TotalMass / 4f;

        // generate the load quadratic value
        for (int i = 0; i < WheelDetails.Length; i++) {
            var sus = SusByWheelNum(i);

            // Validate that rest suspension position is within min and max
            float minSusForce = sus.Stiffness * sus.PreloadDistance * 1000;
            float maxSusForce = sus.Stiffness * (sus.PreloadDistance + Math.Abs(sus.MaxTravel - sus.MinTravel)) * 1000;
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
        float maxTransSpeed = SpeedAtRpm(TransGearRatios.Length - 1, Engine.MaxRpm - redlineOffset);
        for (float speed = 0; speed < maxTransSpeed; speed += 0.1f) {
            int bestGear = -1;
            float bestTorque = -1;
            for (int gear = 1; gear < TransGearRatios.Length; gear++) {
                int rpm = RpmAtSpeed(gear, speed);
                if (rpm > Engine.MaxRpm - redlineOffset) //just a bit off of redline because its not that smooth
                    continue;
                float wheelTorque = (float)Engine.CalcTorqueFor(rpm) * TransGearRatios[gear] * TransFinaldrive;
                if (bestTorque < wheelTorque) {
                    bestTorque = wheelTorque;
                    bestGear = gear;
                }
                // This prints a more detailed graph: Log.p(speed * 3.6f, wheelTorque, gear);
            }

            // This prints a nice graph: Log.p(speed * 3.6f, bestTorque, bestGear);
            changeTimes.Add(new (bestGear, speed));
        }

        AutoGearDownSpeed = new float[TransGearRatios.Length];
        AutoGearDownSpeed[0] = float.MaxValue; // never change out of reverse
        AutoGearUpSpeed = new float[TransGearRatios.Length];
        AutoGearUpSpeed[0] = float.MaxValue; // never change out of reverse
        // Get the first and last value for each gear
		foreach (var entry in changeTimes.GroupBy(x => x.Item1)) {
			int gear = entry.Key;
			float downValue = entry.First().Item2;
            float upValue = entry.Last().Item2;

			// set the auto up and down changes
            AutoGearDownSpeed[gear] = downValue - 2f; // buffer so they overlap a little
            AutoGearUpSpeed[gear] = upValue;
		}

        // Checking that there is gear overlap between up and down (as it prevents the
        // car from changing gear):
        // [2>----[3>-<2]---<3] not [2>----<2]--[3>---<3]
        for (int i = 1; i < TransGearRatios.Length - 1; i++) {
            if (GetGearUpSpeed(i) < GetGearDownSpeed(i + 1)) {
                throw new Exception("Gear overlap test failed for up: " + i + " down: " + (i + 1));
            }
        }
	}

    public CarSusDetails SusByWheelNum(int i) => i < 2 ? SuspensionDetails.Front : SuspensionDetails.Rear;
	public bool IsIdADriveWheel(int i) {
		if (DriveFront && i < 3)
			return true;
		if (DriveRear && i > 2)
			return true;
		return false;
	}

    public float GetGearUpSpeed(int gear) => AutoGearUpSpeed[gear];
    public float GetGearDownSpeed(int gear) => AutoGearDownSpeed[gear];
    public float SpeedAtRpm(int gear, int rpm) {
        return rpm / (TransGearRatios[gear] * TransFinaldrive * (60 / (Mathf.Pi*2))) * DriveWheelRadius();
    }
    public int RpmAtSpeed(int gear, float speed) {
        return (int)(speed * (TransGearRatios[gear] * TransFinaldrive * (60 / (Mathf.Pi*2))) / DriveWheelRadius());
    }
	public int GearForSpeed(float speed) {
		if (speed < 0) return 0; // reverse
		for (var i = 1; i < TransGearRatios.Length; i++) {
			if (speed < GetGearUpSpeed(i) && speed > GetGearDownSpeed(i))
				return i;
		}
		return TransGearRatios.Length - 1; // last gear
	}

	public Vector3 QuadraticDrag(Vector3 velocity) {
		// https://en.wikipedia.org/wiki/Automobile_drag_coefficient#Drag_area
		float dragx = -1.225f * AeroDrag * AeroCrossSection * velocity.X * Mathf.Abs(velocity.X);
		float dragy = -1.225f * AeroDrag * AeroCrossSection * velocity.Y * Mathf.Abs(velocity.Y);
		float dragz = -1.225f * AeroDrag * AeroCrossSection * velocity.Z * Mathf.Abs(velocity.Z);
        // Optional: use a cross section for each xyz direction to make a realistic drag feeling
        // but what we would really need is angle of attack -> much harder
		return new Vector3(dragx, dragy, dragz);
	}

	public float RollingResistance(int w_id, float susForce) {
		// linear drag component (https://en.wikipedia.org/wiki/Rolling_resistance)
		return susForce * AreoLinearDrag / WheelDetails[w_id].Radius;
	}

	public float WheelInertiaNoEngine(int w_id) {
		// this is a thin cylindrical shell
		return WheelDetails[w_id].Mass * WheelDetails[w_id].Radius * WheelDetails[w_id].Radius;
	}

	public float WheelInertiaPlusEngine() { //car internal engine + wheel inertia
		float wheels = 0;
		for (int i = 0; i < WheelDetails.Length; i++)
			wheels += WheelInertiaNoEngine(i);

		if (DriveFront && DriveRear) {
			return Engine.EngineInertia + wheels*4;
		}
		return Engine.EngineInertia + wheels*2;
	}

	public float DriveWheelRadius() {
		if (DriveFront && DriveRear)
			return (WheelDetails[0].Radius + WheelDetails[1].Radius + WheelDetails[2].Radius + WheelDetails[3].Radius) / 4f;
		if (DriveFront)
			return (WheelDetails[0].Radius + WheelDetails[1].Radius) / 2f;
		if (DriveRear)
			return (WheelDetails[2].Radius + WheelDetails[3].Radius) / 2f;

		throw new ArgumentException("No drive wheels set, no wheel radius found.");
    }

    public CarDetails Clone()
    {
        var serialized = JsonSerializer.Serialize(this, CloneSerializeOptions);
		var cloned = JsonSerializer.Deserialize<CarDetails>(serialized, CloneSerializeOptions);
		cloned.LoadSelf(Main.DEFAULT_GRAVITY);
		return cloned;
    }

	public IEnumerable<PartResult> GetResultsInTree() {
		var details = new List<PartResult>();
		foreach (var result in PartReader.GetResults()) {
			details.Add(result);
		}
		foreach (var result in Engine.GetResultsInTree()) {
			details.Add(result);
		}
		foreach (var result in TractionDetails.GetResultsInTree()) {
			details.Add(result);
		}
		foreach (var result in SuspensionDetails.GetResultsInTree()) {
			details.Add(result);
		}
		return details;
	}

	public IEnumerable<Part> GetAllPartsInTree() {
		var parts = new List<Part>();
		foreach (var result in Parts) {
			parts.Add(result);
		}
		foreach (var result in Engine.GetAllPartsInTree()) {
			parts.Add(result);
		}
		foreach (var result in TractionDetails.GetAllPartsInTree()) {
			parts.Add(result);
		}
		foreach (var result in SuspensionDetails.GetAllPartsInTree()) {
			parts.Add(result);
		}
		return parts;
	}
}
