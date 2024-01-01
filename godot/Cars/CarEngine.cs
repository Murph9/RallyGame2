using Godot;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars;

public class CarEngine {

    private const int REVERSE_GEAR_INDEX = 0;

    private readonly Car _car;

    public int CurGear { get; private set; }
    public int CurRPM { get; private set; }
    public float EngineTorque { get; private set; }
    public float[] WheelEngineTorque { get; }

    private double _gearChangeTime;
    private int _gearChangeTo;

    public CarEngine(Car car) {
        _car = car;
        CurGear = 1;
        WheelEngineTorque = new float[car.Details.wheelData.Length];
    }

    public void _PhysicsProcess(double delta) {
        EngineTorque = GetEngineWheelTorque(delta);

        var d = _car.Details;
        var wheelRadius = d.DriveWheelRadius();
        if (d.driveFront && d.driveRear) {
			WheelEngineTorque[0] = WheelEngineTorque[1] = WheelEngineTorque[2] = WheelEngineTorque[3]
             = EngineTorque/(4 * wheelRadius);
        } else if (d.driveFront)
			WheelEngineTorque[0] = WheelEngineTorque[1] = EngineTorque/(2 * wheelRadius);
		else if (d.driveRear)
            WheelEngineTorque[2] = WheelEngineTorque[3] = EngineTorque/(2 * wheelRadius);

        var localVelocity = _car.RigidBody.LinearVelocity * _car.RigidBody.GlobalBasis;
        SimulateAutoTransmission(delta, localVelocity);
    }

    private float GetEngineWheelTorque(double delta) {
        var d = _car.Details;
        var w = _car.Wheels;

        if (!d.driveFront && !d.driveRear)
            return 0;

        float curGearRatio = d.trans_gearRatios[CurGear];
        float diffRatio = d.trans_finaldrive;

        float wheelrot = 0;
		//get the drive wheels rotation speed
		if (d.driveFront && d.driveRear)
			wheelrot = (w[0].RadSec + w[1].RadSec + w[2].RadSec + w[3].RadSec)/4;
		else if (d.driveFront)
			wheelrot = (w[0].RadSec + w[1].RadSec)/2;
		else if (d.driveRear)
			wheelrot = (w[2].RadSec + w[3].RadSec)/2;

        CurRPM = (int)(wheelrot*curGearRatio*diffRatio*(60/Mathf.Tau)); //rad/(m*sec) to rad/min and the drive ratios to engine
        CurRPM = Mathf.Max(CurRPM, d.e_idle);

        float eTorque = d.LerpTorque(CurRPM) * _car.AccelCur;
        float engineDrag = 0;
		if (_car.AccelCur < 0.01f || CurRPM > d.e_redline) //so compression only happens on no accel
			engineDrag = (CurRPM - d.e_idle) * d.e_compression * Mathf.Sign(wheelrot); //reverse goes the other way

        float engineOutTorque;
        if (Mathf.Abs(CurRPM) > d.e_redline)
			engineOutTorque = -engineDrag; //kill engine if greater than redline, and only apply compression
		else //normal path
			engineOutTorque = eTorque * curGearRatio * diffRatio * d.trans_effic - engineDrag;

		return engineOutTorque;
    }

    private void SimulateAutoTransmission(double delta, Vector3 localVelocity) {
        if (_gearChangeTime != 0) {
			_gearChangeTime -= delta;
			if (_gearChangeTime < 0) { // if equal probably shouldn't be trying to set the gear
				CurGear = _gearChangeTo;
				_gearChangeTime = 0;
			}
			return;
		}

		if (CurGear == REVERSE_GEAR_INDEX)
			return; //no changing out of reverse on me please...
        if (!_car.Wheels.Any(x => x.InContact))
			return; //if no contact, no changing of gear

        var d = _car.Details;

		if (localVelocity.Z > d.GetGearUpSpeed(CurGear) && CurGear < d.trans_gearRatios.Length - 1) {
			_gearChangeTime = d.auto_changeTime;
			_gearChangeTo = CurGear + 1;
		} else if (localVelocity.Z < d.GetGearDownSpeed(CurGear) && CurGear > 1) {
			_gearChangeTime = d.auto_changeTime;
			_gearChangeTo = CurGear - 1;
		}
    }
}
