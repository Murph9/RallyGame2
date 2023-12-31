using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars;

public partial class Car : Node3D
{
    public RigidBody3D RigidBody { get; }
    public CarDetails Details { get; }
    public CarEngine Engine { get; }

    public readonly Wheel[] Wheels;
    private readonly Transform3D _worldSpawn;

    public bool HandbrakeCur { get; private set; }
    public float AccelCur { get; private set; }
    public float BrakingCur { get; private set; }

    private float _steeringLeftRaw;
    private float _steeringRightRaw;
    public float Steering { get; private set; }

    public float DriftAngle { get; private set; }

    public const float TRACTION_MAXSLIP = 0.2f;
    public const float TRACTION_MAX = 2f;
    public const float TRACTION_MAXLENGTH = 0.2f;
    public const float TRACTION_DECAY = 3f;

    public Vector3 DragForce;


    public double EngineTorque => Engine.CurrentTorque;
    public double EngineKw => Engine.CurrentTorque * Engine.CurRPM / 9.5488;

    public Car(CarDetails details, Transform3D worldSpawn) {
        Details = details;
        _worldSpawn = worldSpawn;
        Engine = new CarEngine(this);

        var uiScene = GD.Load<PackedScene>("res://Cars/CarUI.tscn");
        var instance = uiScene.Instantiate<CarUI>();
        instance.Car = this;
        AddChild(instance);

        var camera = new CarCamera(this);
        AddChild(camera);

        var scene = GD.Load<PackedScene>("res://assets/" + Details.carModel);
        var carModel = scene.Instantiate<Node3D>();
        RigidBody = carModel.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        var parent = RigidBody.GetParent();
        parent.RemoveChild(RigidBody); // remove the scene parent
        parent.QueueFree();

        // set values from the details
        RigidBody.Mass = Details.mass;
        RigidBody.Transform = _worldSpawn;
        AddChild(RigidBody);

        Wheels = Details.wheelData.Select(x => {
            var sus = Details.SusByWheelNum(x.id);
            return new Wheel(this, x, new RayCast3D() {
                    Position = x.position + new Vector3(0, sus.maxTravel, 0),
                    TargetPosition = new Vector3(0, -sus.TravelTotal() - x.radius, 0)
                });
        }).ToArray();

        // attach wheels to car
        foreach (var w in Wheels) {
            RigidBody.AddChild(w.Ray); // ray should not be attached to the wheel so it can detect height
            RigidBody.AddChild(w);
        }

        // add audio
        var stream = GD.Load<AudioStreamWav>("res://assets/" + Details.Engine.Sound);
        var engine = new AudioStreamPlayer() {
            Stream = stream,
            Autoplay = true,
            Name = "engineAudioPlayer",
            VolumeDb = Mathf.LinearToDb(0.25f)
        };
        AddChild(engine);
    }

    public override void _Process(double delta) {
        foreach (var w in Wheels) {
            // rotate the front wheels (here because the wheels don't have their angle)
            if (w.Details.id < 2) {
                w.Rotation = new Vector3(0, Steering, 0);
            }
        }

        // set audio values
        var audio = GetNode<AudioStreamPlayer>("engineAudioPlayer");
        audio.PitchScale = Mathf.Clamp(0.5f + 1.5f * (Engine.CurRPM / (float)Details.Engine.MaxRpm), 0.5f, 2);
        audio.VolumeDb = Mathf.LinearToDb(0.25f + AccelCur * 0.25f); // max of .5
    }

    public override void _PhysicsProcess(double delta) {
        ReadInputs();

        Engine._PhysicsProcess(delta);

        foreach (var w in Wheels) {
            CalcSuspension(w);

            CalcTraction(w, delta);
            ApplyWheelDrag(w);

            w._PhysicsProcess(delta);
        }
        ApplyCentralDrag();
    }

    private void ReadInputs()
    {
        _steeringLeftRaw = Input.GetActionStrength("car_left") * Details.wMaxSteerAngle;
        _steeringRightRaw = Input.GetActionStrength("car_right") * Details.wMaxSteerAngle;

        var steeringWant = 0f;
        if (_steeringLeftRaw != 0) //left
            steeringWant += GetBestTurnAngle(_steeringLeftRaw, 1);
        if (_steeringRightRaw != 0) //right
            steeringWant -= GetBestTurnAngle(_steeringRightRaw, -1);
        Steering = Mathf.Clamp(steeringWant, -Details.wMaxSteerAngle, Details.wMaxSteerAngle);

        HandbrakeCur = Input.IsActionPressed("car_handbrake");

        BrakingCur = Input.GetActionStrength("car_brake");
        AccelCur = Input.GetActionStrength("car_accel");

        if (Input.IsActionPressed("car_reset")) {
            RigidBody.Transform = _worldSpawn;
            RigidBody.LinearVelocity = new Vector3();
            RigidBody.AngularVelocity = new Vector3();
        }
    }

    private float GetBestTurnAngle(float steeringRaw, int sign) {
        var localVel = RigidBody.LinearVelocity * RigidBody.GlobalBasis;
        if (localVel.Z < 0 || ((-sign * DriftAngle) < 0 && Mathf.Abs(DriftAngle) > Mathf.DegToRad(Details.minDriftAngle))) {
            //when going backwards, slow or needing to turning against drift, you get no speed factor
            //eg: car is pointing more left than velocity, and is also turning left
            //and drift angle needs to be large enough to matter
            return steeringRaw;
        }

        if (localVel.LengthSquared() < 40) // prevent slow speed weirdness
            return steeringRaw;

        // this is magic, but: minimum should be best slip angle, but it doesn't catch up to the turning angle required
        // so we just add some of the angular vel value to it
        return TRACTION_MAXSLIP + RigidBody.AngularVelocity.Length()*0.125f;
    }

    private void CalcSuspension(Wheel w)
    {
        var hitPositionGlobal = w.Ray.GetCollisionPoint();
        var hitNormalGlobal = w.Ray.GetCollisionNormal();

        w.InContact = w.Ray.IsColliding();
        if (!w.Ray.IsColliding()) {
            w.ContactPoint = new Vector3();
            w.ContactNormal = new Vector3();
            w.SusTravelDistance = 0;
            w.ContactRigidBody = null;
            w.SusForce = new Vector3();
            w.SwayForce = 0;
            w.Damping = 0;
            w.SpringForce = 0;
            return;
        }

        w.ContactPoint = RigidBody.ToLocal(hitPositionGlobal);
        w.ContactNormal = RigidBody.ToLocal(hitNormalGlobal);

        var distance = w.Ray.GlobalPosition.DistanceTo(hitPositionGlobal);
        var maxDist = w.Ray.TargetPosition.Length();
        w.SusTravelDistance = Math.Clamp(maxDist - distance, 0, maxDist);

        var hitVelocity = RigidBody.LinearVelocity + RigidBody.AngularVelocity.Cross(hitPositionGlobal - RigidBody.GlobalPosition);
        // then calc other thing velocity if its a rigidbody
        w.ContactRigidBody = w.Ray.GetCollider() as RigidBody3D;
        if (w.ContactRigidBody != null)
            hitVelocity += w.ContactRigidBody.LinearVelocity + w.ContactRigidBody.AngularVelocity.Cross(hitPositionGlobal - w.ContactRigidBody.GlobalPosition);

        // Suspension Dampening
        var relVel = hitNormalGlobal.Dot(hitVelocity);
        var susDetails = Details.SusByWheelNum(w.Details.id);
        w.Damping = susDetails.Relax() * relVel;
        if (relVel > 0) {
            w.Damping = susDetails.Compression() * relVel;
        }

        w.SwayForce = 0f;
        int w_id_other = w.Details.id == 0 ? 1 : w.Details.id == 1 ? 0 : w.Details.id == 2 ? 3 : 2; // fetch the index of the other side
        if (Wheels[w_id_other].InContact) {
            // calc the other wheels distance (perf isn't that important)
            var otherHitPositionGlobal = Wheels[w_id_other].Ray.GetCollisionPoint();
            var otherLength = w.Ray.TargetPosition.Length() - Wheels[w_id_other].Ray.GlobalPosition.DistanceTo(otherHitPositionGlobal);
            w.SwayForce = (otherLength - w.SusTravelDistance) * susDetails.antiroll;
        } else if (w.InContact) {
            // in contact but other not in contact, then its basically max sway
            var otherLength = w.Ray.TargetPosition.Length();
            w.SwayForce = (otherLength - w.SusTravelDistance) * susDetails.antiroll;
        }

        w.SpringForce = (susDetails.preloadForce + w.SusTravelDistance) * susDetails.stiffness;
        var totalForce = w.SpringForce - w.Damping - w.SwayForce;
        if (totalForce > 0) {
            // reduce force based on angle to surface
            var rayDirectionGlobal = RigidBody.GlobalBasis * w.Ray.TargetPosition.Normalized();
            var surfaceNormalFactor = hitNormalGlobal.Dot(-rayDirectionGlobal);

            w.SusForce = 1000 * -rayDirectionGlobal * totalForce * surfaceNormalFactor;
            RigidBody.ApplyForce(w.SusForce, hitPositionGlobal - RigidBody.GlobalPosition);

            w.ContactRigidBody?.ApplyForce(-w.SusForce, hitPositionGlobal - w.ContactRigidBody.GlobalPosition);
        }

        // TODO suspension keeps sending the car in the -x,+z direction
    }

    private void CalcTraction(Wheel w, double delta) {
        var angularVelocity = RigidBody.AngularVelocity;

        var localVel = RigidBody.LinearVelocity * RigidBody.GlobalBasis;
		if (localVel.Z == 0) // NaN on divide avoidance strategy
			localVel.Z = 0.0001f;
		if (localVel.X == 0) // NaN on divide avoidance strategy
			localVel.X = 0.0001f;

        // Linear Accelerations: = player.car.length * player.car.yawrate (in rad/sec)
        float angVel = 0;
        if (!float.IsNaN(angularVelocity.Y))
            angVel = angularVelocity.Y;

        var objectRelVelocity = new Vector3();
        if (w.ContactRigidBody != null) // convert contact object to local co-ords
            objectRelVelocity = w.ContactRigidBody.LinearVelocity * RigidBody.GlobalBasis;
        float groundVelocityZ = localVel.Z - objectRelVelocity.Z;

        float slipr = w.RadSec * w.Details.radius - groundVelocityZ;
        if (groundVelocityZ == 0)
            w.SlipRatio = 0;
        else
            w.SlipRatio = slipr / Mathf.Abs(groundVelocityZ);

        if (HandbrakeCur && w.Details.id >= 2) // rearwheels only
            w.RadSec = 0;

        w.SlipAngle = 0;
        var steering = Steering;
        if (localVel.Z < 0) { // to flip the steering on moving in reverse
            steering *= -1;
        }

        if (w.Details.id < 2) {
            // front wheels
            float slipa_front = localVel.X - objectRelVelocity.X + w.Details.position.Z * angVel;
            w.SlipAngle = Mathf.Atan2(slipa_front, Mathf.Abs(groundVelocityZ)) - steering;
        } else {
            // rear wheels
            float slipa_rear = localVel.X - objectRelVelocity.X + w.Details.position.Z * angVel;
            w.SlipAngle = Mathf.Atan2(slipa_rear, Mathf.Abs(groundVelocityZ));
            DriftAngle = Mathf.RadToDeg(w.SlipAngle); // set drift angle as the rear amount
        }

        // merging the forces into a traction circle
        // normalise based on their independant max values
        float ratiofract = Mathf.Abs(w.SlipRatio / TRACTION_MAXSLIP);
        float anglefract = Mathf.Abs(w.SlipAngle / TRACTION_MAXSLIP);
        float p = Mathf.Sqrt(ratiofract * ratiofract + anglefract * anglefract);
        w.SkidFraction = p;
        if (p == 0) {
            // if p is zero then both anglefract and ratiofract are 0. So to prevent a 'div 0' we just make the denominator 1
            p = 1;
            w.SkidFraction = 0;
        }

        // only apply the merging when the forces are over the peak
        ratiofract /= p;
        anglefract /= p;
        if (Mathf.Abs(p) < 1) {
            ratiofract = Mathf.Sign(ratiofract);
            anglefract = Mathf.Sign(anglefract);
        }

        var wheel_force = new Vector3() {
            // calc the longitudinal force from the slip ratio
            Z = ratiofract * CalcWheelTraction.Calc(w.SlipRatio, TRACTION_MAXSLIP, TRACTION_MAX, TRACTION_MAXLENGTH, TRACTION_DECAY) * w.SusForce.Length(),
            // calc the latitudinal force from the slip angle
            X = -anglefract * CalcWheelTraction.Calc(w.SlipAngle, TRACTION_MAXSLIP, TRACTION_MAX, TRACTION_MAXLENGTH, TRACTION_DECAY) * w.SusForce.Length()
        };

        // braking and abs
        var brakeCurrent2 = BrakingCur;
        if (Mathf.Abs(w.SlipRatio / TRACTION_MAXSLIP) >= 1 && localVel.Length() > 10 && BrakingCur > 0)
            brakeCurrent2 = 0; // very good abs

        // add the wheel force after merging the forces
        var totalLongForce = Engine.WheelEngineTorque[w.Details.id] - wheel_force.Z
                - (brakeCurrent2 * Details.brakeMaxTorque * Mathf.Sign(w.RadSec));
        // drive wheels have the engine to pull along
        float wheelInertia = Details.WheelInertiaNoEngine(w.Details.id);
        if (Details.driveFront && (w.Details.id == 0 || w.Details.id == 1)) {
            wheelInertia = Details.WheelInertiaPlusEngine();
        }
        if (Details.driveRear && (w.Details.id == 2 || w.Details.id == 3)) {
            wheelInertia = Details.WheelInertiaPlusEngine();
        }
        var totalLongForceTorque = totalLongForce / wheelInertia * w.Details.radius;

        if (brakeCurrent2 != 0 && Mathf.Sign(w.RadSec) != Mathf.Sign(w.RadSec + totalLongForceTorque))
            w.RadSec = 0; // maxed out the forces with braking, so prevent wheels from moving
        else
            w.RadSec += (float)delta * totalLongForceTorque; // so the radSec can be used next frame, to calculate slip ratio

        w.GripDir = wheel_force;
        if (wheel_force.LengthSquared() > 0)
            RigidBody.ApplyForce(RigidBody.ToGlobal(wheel_force), RigidBody.ToGlobal(w.ContactPoint) - RigidBody.GlobalPosition);
        if (w.ContactRigidBody != null)
            RigidBody.ApplyForce(w.ContactRigidBody.ToGlobal(wheel_force), w.ContactRigidBody.ToGlobal(w.ContactPoint) - w.ContactRigidBody.GlobalPosition);
    }

    private void ApplyWheelDrag(Wheel w) {
        // TODO none for now, when we get better surface types that cause meaningful drag
    }

    private void ApplyCentralDrag() {
        // quadratic drag (air resistance)
        var localVel = RigidBody.LinearVelocity * RigidBody.GlobalBasis;
        DragForce = Details.QuadraticDrag(RigidBody.LinearVelocity);

		float dragDown = -0.5f * Details.aeroDownforce * 1.225f * (localVel.Z * localVel.Z); // formula for downforce from wikipedia
		RigidBody.ApplyCentralForce(DragForce + new Vector3(0, dragDown, 0)); // apply downforce after
    }
}
