using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace murph9.RallyGame2.godot.Car.Init;

public enum CarType {
	Survivor,

	Normal,
	Runner,
	Rally,
	Roadster,
	
	Hunter,
	Ricer,
	Muscle,
	Wagon,
	Bus,
	
	Ultra,
	LeMans,
	Inline,
	TouringCar,
	Hill,

	WhiteSloth,
	Rocket,
	
	Debug
}

public static class CarTypeExtensions
{
	public static CarDetails LoadCarDetails(this CarType type, Vector3 gravity) {
		var filePath = Path.Combine(AppContext.BaseDirectory, "Car", "Init", "Type", type.ToString() + ".json");
        var jsonContent = File.ReadAllText(filePath);
		var carDetails = JsonSerializer.Deserialize<CarDetails>(jsonContent, new JsonSerializerOptions() {
			AllowTrailingCommas = true,
			PropertyNameCaseInsensitive = true,
			IncludeFields = true
		});

		// calculate wheel positions based on the model
		Node3D carModel = null;
		try {
			var scene = GD.Load<PackedScene>("res://assets/" + carDetails.carModel);
			carModel = scene.Instantiate<Node3D>();
			var wheelPoss = carModel.GetChildren()
				.OfType<Node3D>()
				.ToDictionary(x => x.Name.ToString());
			carDetails.wheelData[0].id = 0;
			carDetails.wheelData[0].position = wheelPoss[CarPart.wheel_fl.ToString()].Position;
			carDetails.wheelData[1].id = 1;
			carDetails.wheelData[1].position = wheelPoss[CarPart.wheel_fr.ToString()].Position;
			carDetails.wheelData[2].id = 2;
			carDetails.wheelData[2].position = wheelPoss[CarPart.wheel_rl.ToString()].Position;
			carDetails.wheelData[3].id = 3;
			carDetails.wheelData[3].position = wheelPoss[CarPart.wheel_rr.ToString()].Position;
		} catch (Exception e) {
			GD.Print(e);
		} finally {
			carModel?.QueueFree();
		}

		// validate that the wheels are in the correct quadrant for a car
        if (carDetails.wheelData[0].position.X < 0 || carDetails.wheelData[0].position.Z < 0)
            throw new Exception(CarPart.wheel_fl + " should be in pos x and pos z");
        if (carDetails.wheelData[1].position.X > 0 || carDetails.wheelData[1].position.Z < 0)
            throw new Exception(CarPart.wheel_fr + " should be in neg x and pos z");

        if (carDetails.wheelData[2].position.X < 0 || carDetails.wheelData[2].position.Z > 0)
            throw new Exception(CarPart.wheel_rl + " should be in pos x and neg z");
        if (carDetails.wheelData[3].position.X > 0 || carDetails.wheelData[3].position.Z > 0)
            throw new Exception(CarPart.wheel_rr + " should be in neg x and neg z");

		
        // Wheel validation
        float quarterMassForce = Mathf.Abs(gravity.Y) * carDetails.mass / 4f;
        
        // generate the load quadratic value
        carDetails.wheelLoadQuadratic = 1/(quarterMassForce*4);
        for (int i = 0; i < carDetails.wheelData.Length; i++) {
            var sus = carDetails.SusByWheelNum(i);

            // Validate that rest suspension position is within min and max
            float minSusForce = (sus.preload_force + sus.stiffness) * 0 * 1000;
            float maxSusForce = sus.stiffness * (sus.preload_force + sus.max_travel - sus.min_travel) * 1000;
            if (quarterMassForce < minSusForce) {
                throw new Exception("!! Sus min range too high: " + quarterMassForce + " < " + minSusForce + ", decrease pre-load or stiffness");
            }
            if (quarterMassForce > maxSusForce) {
                throw new Exception("!! Sus max range too low: " + quarterMassForce + " > " + maxSusForce + ", increase pre-load or stiffness");
            }
        }
       

		// Output the optimal gear up change point based on the torque curve
        int redlineOffset = 250;
        var changeTimes = new List<(int, float)>();
        float maxTransSpeed = carDetails.SpeedAtRpm(carDetails.trans_gearRatios.Length - 1, carDetails.e_redline - redlineOffset);
        for (float speed = 0; speed < maxTransSpeed; speed += 0.1f) {
            int bestGear = -1;
            float bestTorque = -1;
            for (int gear = 1; gear < carDetails.trans_gearRatios.Length; gear++) {
                int rpm = carDetails.RpmAtSpeed(gear, speed);
                if (rpm > carDetails.e_redline - redlineOffset) //just a bit off of redline because its not that smooth
                    continue;
                float wheelTorque = carDetails.LerpTorque(rpm) * carDetails.trans_gearRatios[gear] * carDetails.trans_finaldrive;
                if (bestTorque < wheelTorque) {
                    bestTorque = wheelTorque;
                    bestGear = gear;
                }
                // This prints a more detailed graph: Log.p(speed * 3.6f, wheelTorque, gear);
            }

            // This prints a nice graph: Log.p(speed * 3.6f, bestTorque, bestGear);
            changeTimes.Add(new (bestGear, speed));
        }

        carDetails.auto_gearDownSpeed = new float[carDetails.trans_gearRatios.Length];
        carDetails.auto_gearDownSpeed[0] = float.MaxValue; // never change out of reverse
        carDetails.auto_gearUpSpeed = new float[carDetails.trans_gearRatios.Length];
        carDetails.auto_gearUpSpeed[0] = float.MaxValue; // never change out of reverse
        // Get the first and last value for each gear
		foreach (var entry in changeTimes.GroupBy(x => x.Item1)) {
			int gear = entry.Key;
			float downValue = entry.First().Item2;
            float upValue = entry.Last().Item2;

			// set the auto up and down changes
            carDetails.auto_gearDownSpeed[gear] = downValue - 2f; // buffer so they overlap a little
            carDetails.auto_gearUpSpeed[gear] = upValue;
		}

        // Checking that there is gear overlap between up and down (as it prevents the
        // car from changing gear):
        // [2>----[3>-<2]---<3] not [2>----<2]--[3>---<3]
        for (int i = 1; i < carDetails.trans_gearRatios.Length - 1; i++) {
            if (carDetails.GetGearUpSpeed(i) < carDetails.GetGearDownSpeed(i + 1)) {
                throw new Exception("Gear overlap test failed for up: " + i + " down: " + (i + 1));
            }
        }

		return carDetails;
	}
}
