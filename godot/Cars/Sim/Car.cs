using Godot;
using murph9.RallyGame2.godot.Cars.AI;
using murph9.RallyGame2.godot.Cars.Init;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class Car : Node3D {
    public RigidBody3D RigidBody { get; }
    public CarDetails Details { get; }
    public CarEngine Engine { get; }

    public readonly Wheel[] Wheels;
    private readonly List<WheelSkid> _skids = [];
    private readonly Transform3D _worldSpawn;

    public ICarInputs Inputs { get; }

    public float DriftAngle { get; private set; }

    public Vector3 DragForce;

    public double EngineTorque => Engine.CurrentTorque;
    public double EngineKw => Engine.CurrentTorque * Engine.CurRPM / 9.5488;

    public float DistanceTravelled { get; private set; }
    private Vector3? _lastPos;


    public Car(CarDetails details, ICarInputs inputs = null, Transform3D? worldSpawn = null) {
        Details = details;
        _worldSpawn = worldSpawn ?? Transform3D.Identity;
        Engine = new CarEngine(this);

        Inputs = inputs ?? new HumanCarInputs();
        Inputs.Car = this;

        if (Inputs.IsAi) {
            var ai = Inputs as Node3D;
            AddChild(ai);
        } else {
            var uiScene = GD.Load<PackedScene>("res://Cars/CarUI.tscn");
            var instance = uiScene.Instantiate<CarUI>();
            instance.Car = this;
            AddChild(instance);

            var camera = new CarCamera(this);
            AddChild(camera);
        }

        var scene = GD.Load<PackedScene>("res://assets/car/" + Details.CarModel);
        var carModel = scene.Instantiate<Node3D>();
        RigidBody = carModel.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        var parent = RigidBody.GetParent();
        parent.RemoveChild(RigidBody); // remove the scene parent
        parent.QueueFree();

        // set values from the details
        RigidBody.Mass = (float)Details.TotalMass;
        RigidBody.Transform = _worldSpawn;
        AddChild(RigidBody);

        Wheels = Details.WheelDetails.Select(x => new Wheel(this, x)).ToArray();

        // attach wheels to car
        foreach (var w in Wheels) {
            RigidBody.AddChild(w);

            var skid = new WheelSkid(w);
            _skids.Add(skid);
            AddChild(skid);
        }

        if (!Inputs.IsAi) {
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
    }

    public override void _Process(double delta) {
        foreach (var w in Wheels) {
            // rotate the front wheels (here because the wheels don't have their angle)
            if (w.Details.Id < 2) {
                w.Rotation = new Vector3(0, Inputs.Steering, 0);
            }
        }

        if (!Inputs.IsAi) {
            var audio = GetNode<AudioStreamPlayer>("engineAudioPlayer");
            if (audio != null) {
                // set audio values
                audio.PitchScale = Mathf.Clamp(0.5f + 1.5f * (Engine.CurRPM / (float)Details.Engine.MaxRpm), 0.5f, 2);
                audio.VolumeDb = Mathf.LinearToDb(0.25f + Inputs.AccelCur * 0.25f); // max of .5
            }
        }
    }

    public override void _PhysicsProcess(double delta) {
        Inputs.ReadInputs();

        Engine._PhysicsProcess(delta);

        if (_lastPos.HasValue) {
            DistanceTravelled += RigidBody.GlobalPosition.DistanceTo(_lastPos.Value);
        }
        _lastPos = RigidBody.GlobalPosition;

        var physicsState = GetWorld3D().DirectSpaceState;
        foreach (var w in Wheels) {
            w.DoRaycast(physicsState, RigidBody);
        }

        foreach (var w in Wheels) {
            CalcSuspension(w);

            CalcTraction(w, delta);
            ApplyWheelDrag(w);

            w._PhysicsProcess(delta);
        }
        ApplyCentralDrag();
    }

    public void Reset() {
        RigidBody.Transform = _worldSpawn;
        RigidBody.LinearVelocity = new Vector3();
        RigidBody.AngularVelocity = new Vector3();
        foreach (var w in Wheels) {
            w.RadSec = 0;
        }
    }

    private void CalcSuspension(Wheel w) {
        if (!w.InContact) {
            w.SusForce = new Vector3();
            w.SwayForce = 0;
            w.Damping = 0;
            w.SpringForce = 0;
            return;
        }

        // TODO suspension keeps sending the car in the -x,+z direction

        var hitVelocity = RigidBody.LinearVelocity + RigidBody.AngularVelocity.Cross(w.ContactPointGlobal - RigidBody.GlobalPosition);
        // then calc other thing velocity if its a rigidbody
        if (w.ContactRigidBody != null)
            hitVelocity += w.ContactRigidBody.LinearVelocity + w.ContactRigidBody.AngularVelocity.Cross(w.ContactPointGlobal - w.ContactRigidBody.GlobalPosition);

        // Suspension Dampening
        var relVel = w.ContactNormalGlobal.Dot(hitVelocity);
        var susDetails = Details.SusByWheelNum(w.Details.Id);
        w.Damping = susDetails.Rebound() * relVel;
        if (relVel > 0) {
            w.Damping = susDetails.Compression() * relVel;
        }

        w.SwayForce = 0f;
        var otherSideWheel = GetOtherWheel(w); // fetch the index of the other side
        if (otherSideWheel.InContact) {
            w.SwayForce = (otherSideWheel.SusTravelDistance - w.SusTravelDistance) * susDetails.Antiroll;
        } else if (w.InContact) {
            // in contact but other not in contact, then its basically max sway
            var otherLength = w.RayDirLocal.Length();
            w.SwayForce = (otherLength - w.SusTravelDistance) * susDetails.Antiroll;
        }

        w.SpringForce = (susDetails.PreloadDistance + w.SusTravelDistance) * susDetails.Stiffness;
        var totalForce = w.SpringForce - w.Damping - w.SwayForce;
        if (totalForce > 0) {
            // reduce force based on angle to surface
            var rayDirectionGlobal = RigidBody.GlobalBasis * w.RayDirLocal.Normalized();
            var surfaceNormalFactor = w.ContactNormalGlobal.Dot(-rayDirectionGlobal);

            w.SusForce = 1000 * -rayDirectionGlobal * totalForce * surfaceNormalFactor;
            RigidBody.ApplyForce(w.SusForce, w.ContactPointGlobal - RigidBody.GlobalPosition);

            w.ContactRigidBody?.ApplyForce(-w.SusForce, w.ContactPointGlobal - w.ContactRigidBody.GlobalPosition);
        }
    }

    private void CalcTraction(Wheel w, double delta) {
        w.SlipAngleLast = w.SlipAngle;
        w.SlipRatioLast = w.SlipRatio;

        var localVel = RigidBody.LinearVelocity * RigidBody.GlobalBasis;

        var objectRelVelocity = (w.ContactRigidBody?.LinearVelocity ?? new Vector3()) * RigidBody.GlobalBasis;
        var groundVelocity = localVel - objectRelVelocity;

        var slipr = w.RadSec * w.Details.Radius - groundVelocity.Z;
        w.SlipRatio = slipr / Mathf.Abs(groundVelocity.Z == 0 ? 0.0001f : groundVelocity.Z);

        if (Inputs.HandbrakeCur && w.Details.Id >= 2) // rearwheels only
            w.RadSec = 0;

        var steering = Inputs.Steering;
        if (groundVelocity.Z < 0) { // to flip the steering on moving in reverse
            steering *= -1;
        }

        if (w.Details.Id < 2) {
            // front wheels (player.car.length * player.car.yawrate (in rad/sec))
            var slipa_front = groundVelocity.X - objectRelVelocity.X + w.Details.Position.Z * RigidBody.AngularVelocity.Y;
            w.SlipAngle = Mathf.Atan2(slipa_front, Mathf.Abs(groundVelocity.Z)) - steering;
        } else {
            // rear wheels (player.car.length * player.car.yawrate (in rad/sec))
            var slipa_rear = groundVelocity.X - objectRelVelocity.X + w.Details.Position.Z * RigidBody.AngularVelocity.Y;
            w.SlipAngle = Mathf.Atan2(slipa_rear, Mathf.Abs(groundVelocity.Z));
            DriftAngle = Mathf.RadToDeg(w.SlipAngle); // set drift angle as the rear amount
        }

        // merging the forces into a traction circle
        // normalise based on their independant max values
        var ratiofract = Mathf.Abs(w.SlipRatio / (float)Details.TractionDetails.LongMaxSlip);
        var anglefract = Mathf.Abs(w.SlipAngle / (float)Details.TractionDetails.LatMaxSlip);
        var p = Mathf.Sqrt(ratiofract * ratiofract + anglefract * anglefract);
        w.SkidFraction = p;
        if (p == 0) {
            // if p is zero then both anglefract and ratiofract are 0. So to prevent a 'div by 0' we just make the denominator 1
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

        var td = Details.TractionDetails;

        var wheel_force = new Vector3() {
            // calc the longitudinal force from the slip ratio
            Z = ratiofract * (float)CalcWheelTraction.Calc(w.SlipRatio, td.LongMaxSlip, td.LongGripMax, td.LongPeakLength, td.LongPeakDecay) * w.SusForce.Length(),
            // calc the latitudinal force from the slip angle
            X = -anglefract * (float)CalcWheelTraction.Calc(w.SlipAngle, td.LatMaxSlip, td.LatGripMax, td.LatPeakLength, td.LongPeakDecay) * w.SusForce.Length()
        };

        // braking and abs
        var brakeCurrent2 = Inputs.BrakingCur;
        if (Math.Abs(w.SlipRatioLast - w.SlipRatio) * delta / 4f > td.LongMaxSlip && groundVelocity.Length() > 4)
            brakeCurrent2 = 0; // very good abs (predict slip ratio will run out in 4 frames and stop braking so hard)

        // calcluate traction control
        if (Details.TractionControl && groundVelocity.LengthSquared() > 15 && Math.Abs(Mathf.DegToRad(DriftAngle)) > td.LatMaxSlip * 1.5f) {
            // but only do it on the outer side
            if (w.TractionControlTimeOut > 0) {
                w.TractionControlTimeOut -= delta;
            } else if (w.Details.Id == 0 || w.Details.Id == 2 && w.SlipAngle > 0) {
                brakeCurrent2 = 1f;
                w.TractionControlTimeOut = 0.1f;
            } else if (w.Details.Id == 1 || w.Details.Id == 3 && w.SlipAngle < 0) {
                brakeCurrent2 = 1;
                w.TractionControlTimeOut = 0.1f;
            }
        }

        // add the wheel force after merging the forces
        var totalLongForce = Engine.WheelEngineTorque[w.Details.Id] - wheel_force.Z
                - (brakeCurrent2 * Details.BrakeMaxTorque * Mathf.Sign(w.RadSec));
        // drive wheels have the engine to pull along
        float wheelInertia = Details.WheelInertiaNoEngine(w.Details.Id);
        if (Details.DriveFront && (w.Details.Id == 0 || w.Details.Id == 1)) {
            wheelInertia = Details.WheelInertiaPlusEngine();
        }
        if (Details.DriveRear && (w.Details.Id == 2 || w.Details.Id == 3)) {
            wheelInertia = Details.WheelInertiaPlusEngine();
        }
        var totalLongForceTorque = totalLongForce / wheelInertia * w.Details.Radius;

        if (brakeCurrent2 != 0 && Mathf.Sign(w.RadSec) != Mathf.Sign(w.RadSec + totalLongForceTorque))
            w.RadSec = 0; // maxed out the forces with braking, so prevent wheels from moving
        else
            w.RadSec += (float)delta * totalLongForceTorque; // so the radSec can be used next frame, to calculate slip ratio

        w.GripDir = wheel_force / (float)w.Car.Details.TotalMass;
        if (wheel_force.LengthSquared() > 0)
            RigidBody.ApplyForce(RigidBody.Basis * wheel_force, w.ContactPointGlobal - RigidBody.GlobalPosition);
        if (w.ContactRigidBody != null)
            RigidBody.ApplyForce(w.ContactRigidBody.Basis * wheel_force, w.ContactPointGlobal - w.ContactRigidBody.GlobalPosition);
    }

    private void ApplyWheelDrag(Wheel w) {
        // TODO none for now, when we get better surface types that cause meaningful drag
    }

    private void ApplyCentralDrag() {
        // quadratic drag (air resistance)
        DragForce = Details.QuadraticDrag(RigidBody.LinearVelocity);

        var localVel = RigidBody.LinearVelocity * RigidBody.GlobalBasis;
        float dragDown = -0.5f * Details.AeroDownforce * 1.225f * (localVel.Z * localVel.Z); // formula for downforce from wikipedia
        RigidBody.ApplyCentralForce(DragForce + new Vector3(0, dragDown, 0)); // apply downforce after
    }

    private Wheel GetOtherWheel(Wheel w) => Wheels[w.Details.Id == 0 ? 1 : w.Details.Id == 1 ? 0 : w.Details.Id == 2 ? 3 : 2];
}
