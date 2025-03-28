using Godot;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Sim;

public class CarEngine {

    private const int REVERSE_GEAR_INDEX = 0;

    private readonly Car _car;

    public int CurGear { get; private set; }
    public int CurRPM { get; private set; }
    public double CurrentTorque { get; private set; }
    public float[] WheelEngineTorque { get; }

    public float CurrentFuel { get; private set; }
    public float CurrentFuelRate { get; private set; }

    private double _gearChangeTime;
    private bool _changingGear => _gearChangeTime > 0;
    private int _gearChangeTo;

    public CarEngine(Car car) {
        _car = car;
        CurGear = 1;
        CurrentFuel = car.Details.FuelMax;
        WheelEngineTorque = new float[car.Details.WheelDetails.Length];
    }

    public void _PhysicsProcess(double delta) {
        var engineTorque = SetEngineTorque(delta);

        var d = _car.Details;
        var wheelRadius = d.DriveWheelRadius();
        if (d.DriveFront && d.DriveRear) {
            float balance = Mathf.Clamp(d.TransPowerBalance, 0, 1);
            WheelEngineTorque[0] = WheelEngineTorque[1] = (1 - balance) * engineTorque / (4 * wheelRadius);
            WheelEngineTorque[2] = WheelEngineTorque[3] = balance * engineTorque / (4 * wheelRadius);
        } else if (d.DriveFront)
            WheelEngineTorque[0] = WheelEngineTorque[1] = engineTorque / (2 * wheelRadius);
        else if (d.DriveRear)
            WheelEngineTorque[2] = WheelEngineTorque[3] = engineTorque / (2 * wheelRadius);

        var localVelocity = _car.RigidBody.LinearVelocity * _car.RigidBody.GlobalBasis;
        SimulateAutoTransmission(delta, localVelocity);
    }

    private float SetEngineTorque(double delta) {
        var d = _car.Details;
        var w = _car.Wheels;

        if (!d.DriveFront && !d.DriveRear)
            return 0;

        // ignore acceleration while changing gear
        var realAccel = _changingGear ? 0 : _car.Inputs.AccelCur;

        float curGearRatio = d.TransGearRatios[CurGear];
        float diffRatio = d.TransFinaldrive;

        float wheelrot = 0;
        //get the drive wheels rotation speed
        if (d.DriveFront && d.DriveRear)
            wheelrot = (w[0].RadSec + w[1].RadSec + w[2].RadSec + w[3].RadSec) / 4;
        else if (d.DriveFront)
            wheelrot = (w[0].RadSec + w[1].RadSec) / 2;
        else if (d.DriveRear)
            wheelrot = (w[2].RadSec + w[3].RadSec) / 2;

        CurRPM = (int)(wheelrot * curGearRatio * diffRatio * (60 / Mathf.Tau)); //rad/(m*sec) to rad/min and the drive ratios to engine

        // TODO disconnect the engine to the wheels while changing gear
        // by reducing the rpm down to where the next gear wants it

        if (CurRPM > d.Engine.MaxRpm)
            realAccel = 0; // also no accel while above max rpm

        // the perfect anti stall:
        // always keep rpm above Idle
        // at slow speeds make it higher to fake a clutch
        var minRpm = d.Engine.IdleRPM;
        if (CurGear == 1 && realAccel > 0) {
            var minRpmSpeed = d.SpeedAtRpm(1, minRpm * 3);

            if (_car.RigidBody.LinearVelocity.Length() < minRpmSpeed) {
                minRpm = (int)Mathf.Lerp(minRpm * 3, minRpm, _car.RigidBody.LinearVelocity.Length() / minRpmSpeed);
            }
        }
        CurRPM = Mathf.Max(CurRPM, minRpm);


        CurrentFuelRate = (float)(_car.Details.Engine.FuelByRpmRate * (_car.Engine.CurRPM / 1000f) * delta * realAccel);
        CurrentFuel -= CurrentFuelRate;

        if (CurrentFuel < 0 && _car.Details.Engine.FuelEnabled) {
            CurrentFuel = 0;
            realAccel = 0;
        }

        CurrentTorque = d.Engine.CalcTorqueFor(CurRPM) * realAccel;

        double engineDrag = 0;
        if (realAccel < 0.01f) {// so compression only happens with no acceleration
            engineDrag = (CurRPM - d.Engine.IdleRPM) * d.Engine.IdleDrag * Mathf.Sign(wheelrot); // reverse goes the other way
        }

        double engineOutTorque;
        if (Mathf.Abs(CurRPM) > d.Engine.MaxRpm)
            engineOutTorque = -engineDrag; //kill engine if greater than redline, and only apply compression
        else //normal path
            engineOutTorque = CurrentTorque * curGearRatio * diffRatio * d.Engine.TransmissionEfficiency - engineDrag;

        return (float)engineOutTorque;
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

        if (localVelocity.Z > d.GetGearUpSpeed(CurGear) && CurGear < d.TransGearRatios.Length - 1) {
            _gearChangeTime = d.AutoChangeTime;
            _gearChangeTo = CurGear + 1;

            // check the gear down is valid and the redline won't be exceeded by changing gear
        } else if (localVelocity.Z < d.GetGearDownSpeed(CurGear) && CurGear > 1 && CurRPM * d.TransGearRatios[CurGear - 1] / d.TransGearRatios[CurGear] < d.Engine.MaxRpm) {
            _gearChangeTime = d.AutoChangeTime;
            _gearChangeTo = CurGear - 1;
        }
    }

    public void CloneExistingState(CarEngine engine) {
        CurGear = engine.CurGear;
        CurRPM = engine.CurRPM;
        CurrentFuel = engine.CurrentFuel;
    }
}
