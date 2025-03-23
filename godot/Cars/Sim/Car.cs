using Godot;
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
    private readonly AudioStreamPlayer _engineAudio;
    public readonly Color _colour;

    public ICarInputs Inputs { get; private set; }

    public float DriftAngle { get; private set; }
    public Vector3 DragForce { get; private set; }

    public double EngineTorque => Engine.CurrentTorque;
    public double EngineKw => Engine.CurrentTorque * Engine.CurRPM / 9.5488;

    public float DistanceTravelled { get; private set; }
    private Vector3? _lastPos;

    private Vector3 _frozenVelocity;
    private Vector3 _frozenAngular;

    public Car(CarDetails details, ICarInputs inputs = null, Transform3D? initialTransform = null, Color? initialColor = null) {
        Details = details;
        Engine = new CarEngine(this);
        _colour = initialColor ?? new Color(0.8f, 0.8f, 0.8f, 1);

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
        var carScene = scene.Instantiate<Node3D>();
        RigidBody = carScene.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        var parent = RigidBody.GetParent();
        parent.RemoveChild(RigidBody); // remove the scene parent
        parent.QueueFree();
        RigidBody.Owner = null;
        RigidBody.ContactMonitor = true;
        RigidBody.MaxContactsReported = 2; // TODO perf

        // update the car colour
        var carModels = RigidBody.GetChildren().Where(x => x is MeshInstance3D);
        foreach (var carModel in carModels.Cast<MeshInstance3D>()) {
            for (var i = 0; i < carModel.Mesh.GetSurfaceCount(); i++) {
                var material = carModel.GetActiveMaterial(i).Duplicate();
                if (material.ResourceName.Contains("[primary]") && material is StandardMaterial3D mat3D) {
                    // clone the material and use it to set an override because the mesh instance is shared between all cars
                    var newMat3D = (StandardMaterial3D)mat3D.Duplicate();
                    newMat3D.AlbedoColor = _colour;
                    carModel.SetSurfaceOverrideMaterial(i, newMat3D);
                }
            }
        }

        // set values from the details
        RigidBody.Mass = (float)Details.TotalMass;
        RigidBody.Transform = initialTransform ?? Transform3D.Identity;
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
            _engineAudio = new AudioStreamPlayer() {
                Stream = stream,
                Autoplay = true,
                Name = "engineAudioPlayer",
                VolumeDb = Mathf.LinearToDb(0.25f)
            };
            AddChild(_engineAudio);
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
        var localVel = RigidBody.LinearVelocity * RigidBody.GlobalBasis;

        var objectRelVelocity = (w.ContactRigidBody?.LinearVelocity ?? new Vector3()) * RigidBody.GlobalBasis;
        var groundVelocity = localVel - objectRelVelocity;

        // braking section
        var brakeCurrent = Inputs.BrakingCur;
        // calculate traction control
        if (Details.TractionControl && groundVelocity.LengthSquared() > 15 && Math.Abs(Mathf.DegToRad(DriftAngle)) > Details.TractionDetails.LatMaxSlip * 1.5f) {
            // but only do it on the outer side
            if (w.TractionControlTimeOut > 0) {
                w.TractionControlTimeOut -= delta;
            } else if (w.Details.Id == 0 || w.Details.Id == 2 && w.SlipAngle > 0) {
                brakeCurrent = 1f;
                w.TractionControlTimeOut = 0.1f;
            } else if (w.Details.Id == 1 || w.Details.Id == 3 && w.SlipAngle < 0) {
                brakeCurrent = 1;
                w.TractionControlTimeOut = 0.1f;
            }
        }

        // drive wheels have the engine to pull along
        float wheelInertia = Details.WheelInertiaNoEngine(w.Details.Id);
        if (Details.DriveFront && (w.Details.Id == 0 || w.Details.Id == 1)) {
            wheelInertia = Details.WheelInertiaPlusEngine();
        }
        if (Details.DriveRear && (w.Details.Id == 2 || w.Details.Id == 3)) {
            wheelInertia = Details.WheelInertiaPlusEngine();
        }

        // attmempt to apply abs
        var predictedRadSecDiff = Mathf.Abs(brakeCurrent * Details.BrakeMaxTorque / wheelInertia * w.Details.Radius);
        if (w.ABSControlTimeOut > 0) {
            w.ABSControlTimeOut -= delta;
        } else if (brakeCurrent > 0 && Mathf.Abs(w.RadSec - predictedRadSecDiff) > 10) {
            brakeCurrent = 0f;
            w.ABSControlTimeOut = Details.BrakeAbsTimeout;
        }

        // add the wheel force after merging the forces
        var totalLongForce = Engine.WheelEngineTorque[w.Details.Id] - w.AppliedForces.Z
                - (brakeCurrent * Details.BrakeMaxTorque * Mathf.Sign(w.RadSec));
        var totalLongForceTorque = totalLongForce / wheelInertia * w.Details.Radius;

        if (brakeCurrent != 0 && Mathf.Sign(w.RadSec) != Mathf.Sign(w.RadSec + totalLongForceTorque))
            w.RadSec = 0; // maxed out the forces with braking, so prevent wheels from moving
        else
            w.RadSec += (float)delta * totalLongForceTorque; // so the radSec can be used next frame, to calculate slip ratio

        if (Inputs.HandbrakeCur && w.Details.Id >= 2) // rearwheels only
            w.RadSec = 0;

        var steering = Inputs.Steering;
        if (groundVelocity.Z < 0) { // to flip the steering on moving in reverse
            steering *= -1;
        }

        // calculate the slip ratio and slip angles

        w.SlipRatio = (w.RadSec * w.Details.Radius - groundVelocity.Z) / Mathf.Abs(groundVelocity.Z == 0 ? 0.0001f : groundVelocity.Z);

        // slip angle (player.car.length * player.car.yawrate (in rad/sec))
        var slipAngleTop = groundVelocity.X - objectRelVelocity.X + w.Details.Position.Z * RigidBody.AngularVelocity.Y;
        w.SlipAngle = Mathf.Atan2(slipAngleTop, Mathf.Abs(groundVelocity.Z));
        DriftAngle = Mathf.RadToDeg(w.SlipAngle); // set drift angle as the rear angle amount
        if (w.Details.Id < 2) {
            // front wheels also steer
            w.SlipAngle -= steering;
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
        w.AppliedForces = new Vector3() {
            // calc the longitudinal force from the slip ratio
            Z = ratiofract * (float)CalcWheelTraction.Calc(w.SlipRatio, td.LongMaxSlip, td.LongGripMax, td.LongPeakLength, td.LongPeakDecay) * w.SusForce.Length(),
            // calc the latitudinal force from the slip angle
            X = -anglefract * (float)CalcWheelTraction.Calc(w.SlipAngle, td.LatMaxSlip, td.LatGripMax, td.LatPeakLength, td.LongPeakDecay) * w.SusForce.Length()
        };

        if (w.AppliedForces.LengthSquared() > 0) {
            // Apply the physics to the car
            RigidBody.ApplyForce(RigidBody.Basis * w.AppliedForces, w.ContactPointGlobal - RigidBody.GlobalPosition);
        }

        if (w.ContactRigidBody != null) {
            // Apply a force to the other object
            RigidBody.ApplyForce(w.ContactRigidBody.Basis * w.AppliedForces, w.ContactPointGlobal - w.ContactRigidBody.GlobalPosition);
        }
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


    public Car CloneWithNewDetails(CarDetails details = null) {
        // clone into new car
        var car = new Car(details ?? Details, null, RigidBody.Transform, _colour);
        car.RigidBody.LinearVelocity = RigidBody.LinearVelocity;
        car.RigidBody.AngularVelocity = RigidBody.AngularVelocity;

        car.DistanceTravelled = DistanceTravelled;
        car._lastPos = _lastPos;
        car.Engine.CloneExistingState(Engine);

        for (var i = 0; i < Wheels.Length; i++) {
            car.Wheels[i].RadSec = Wheels[i].RadSec;
        }

        return car;
    }

    public void SetActive(bool active) {
        if (active) {
            RigidBody.Freeze = false;
            RigidBody.LinearVelocity = _frozenVelocity;
            RigidBody.AngularVelocity = _frozenAngular;
            Inputs.AcceptInputs();
            foreach (var w in Wheels) {
                w.Active = active;
            }
        } else {
            _frozenVelocity = RigidBody.LinearVelocity;
            _frozenAngular = RigidBody.AngularVelocity;
            Inputs.IgnoreInputs();
            RigidBody.Freeze = true;
            foreach (var w in Wheels) {
                w.Active = active;
            }
        }

        if (_engineAudio != null) {
            _engineAudio.Playing = active;
        }

        SetProcess(active);
        SetPhysicsProcess(active);

        RigidBody.SetProcess(active);
        RigidBody.SetPhysicsProcess(active);
    }

    public void ResetCarTo(Transform3D? transform = null) {
        // if given a transform teleport there, if not stop in place
        if (transform.HasValue) {
            RigidBody.Position = transform.Value.Origin + new Vector3(0, 0.5f, 0);
            RigidBody.Basis = transform.Value.Basis;
        }

        RigidBody.LinearVelocity *= 0;
        RigidBody.AngularVelocity *= 0;

        for (var i = 0; i < Wheels.Length; i++) {
            Wheels[i].RadSec = 0;
        }
    }

    public void ChangeInputsTo(ICarInputs carInputs) {
        RemoveChild(Inputs as Node3D);

        Inputs = carInputs;
        Inputs.Car = this;
        AddChild(Inputs as Node3D);
    }
}
