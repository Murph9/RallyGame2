using Godot;

namespace murph9.RallyGame2.godot.Cars.Sim;

public interface ICarInputs {
    public Car Car { get; set; }
    public bool IsAi { get; }

    public bool HandbrakeCur { get; }
    public float AccelCur { get; }
    public float BrakingCur { get; }
    public float Steering { get; }

    void AcceptInputs();
    void IgnoreInputs();
    void ReadInputs();
}

public class HumanCarInputs : ICarInputs {

    public bool IsAi => false;

    public bool HandbrakeCur { get; private set; }
    public float AccelCur { get; private set; }
    public float BrakingCur { get; private set; }

    private float _steeringLeftRaw;
    private float _steeringRightRaw;
    public float Steering { get; private set; }

    private bool _listeningToInputs = true;

    public Car Car { get; set; }

    public void AcceptInputs() {
        _listeningToInputs = true;
    }

    public void IgnoreInputs() {
        _listeningToInputs = false;

        // and reset everything
        _steeringLeftRaw = 0;
        _steeringRightRaw = 0;
        Steering = 0;

        HandbrakeCur = false;
        BrakingCur = 0.2f; // slow braking
        AccelCur = 0;
    }

    public void ReadInputs() {
        if (!_listeningToInputs) return;

        _steeringLeftRaw = Input.GetActionStrength("car_left") * Car.Details.MaxSteerAngle;
        _steeringRightRaw = Input.GetActionStrength("car_right") * Car.Details.MaxSteerAngle;

        var steeringWant = 0f;
        if (_steeringLeftRaw != 0) //left
            steeringWant += GetBestTurnAngle(_steeringLeftRaw);
        if (_steeringRightRaw != 0) //right
            steeringWant += GetBestTurnAngle(-_steeringRightRaw);
        Steering = Mathf.Clamp(steeringWant, -Car.Details.MaxSteerAngle, Car.Details.MaxSteerAngle);

        HandbrakeCur = Input.IsActionPressed("car_handbrake");

        BrakingCur = Input.GetActionStrength("car_brake");
        AccelCur = Input.GetActionStrength("car_accel");
    }

    private float GetBestTurnAngle(float steeringRaw) {
        var sign = Mathf.Sign(steeringRaw);
        var localVel = Car.RigidBody.LinearVelocity * Car.RigidBody.GlobalBasis;
        if (localVel.Z < 0 || ((-sign * Car.DriftAngle) < 0 && Mathf.Abs(Car.DriftAngle) > Mathf.DegToRad(Car.Details.MinDriftAngle))) {
            //when going backwards, slow or needing to turning against drift, you get no speed factor
            //eg: car is pointing more left than velocity, and is also turning left
            //and drift angle needs to be large enough to matter
            return steeringRaw;
        }

        if (localVel.LengthSquared() < 40) // prevent slow speed weirdness
            return steeringRaw;

        // this is magic, but: minimum should be best slip angle, but it doesn't catch up to the turning angle required
        // so we just add some of the angular vel value to it
        return sign * ((float)Car.Details.TractionDetails.LatMaxSlip + Car.RigidBody.AngularVelocity.Length() * 0.125f);
    }
}
