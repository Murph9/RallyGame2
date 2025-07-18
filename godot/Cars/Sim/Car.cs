using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Sim;

public partial class Car : Node3D {
    public RigidBody3D RigidBody { get; } // shouldn't the car class be the rigidbody?
    public CarDetails Details { get; }
    public CarEngine Engine { get; }

    public readonly Wheel[] Wheels;
    private readonly List<WheelSkid> _skids = [];
    private readonly AudioStreamPlayer _engineAudio;
    private readonly bool _isMainCar;
    public readonly Color Colour;

    private readonly Dictionary<CarModelAddition, Node3D> _additions = [];

    public ICarInputs Inputs { get; private set; }

    public float DriftAngle { get; private set; }
    public Vector3 DragForce { get; private set; }

    public double EngineTorque => Engine.CurrentTorque;
    public double EngineKw => Engine.CurrentTorque * Engine.CurRPM / 9.5488;

    public float DistanceTravelled { get; private set; }
    public float Damage { get; set; } // out of 100

    private Vector3? _lastPos;
    private Vector3? _lastVelocity; // to track collision differences

    private Vector3 _frozenVelocity;
    private Vector3 _frozenAngular;

    public Car(CarDetails details, ICarInputs inputs = null, bool isMainCar = false, Transform3D? initialTransform = null, Color? initialColor = null) {
        Details = details;
        Engine = new CarEngine(this);
        Colour = initialColor ?? new Color(0.8f, 0.8f, 0.8f, 1);

        Inputs = inputs ?? new HumanCarInputs();
        Inputs.Car = this;

        if (Inputs is not null) {
            var ai = Inputs as Node3D;
            AddChild(ai);
        }

        _isMainCar = Inputs is null || isMainCar;
        if (_isMainCar) {
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

        // verify the collision bodies are convex collision shapes (for performance)
        // https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/node_type_customization.html
        if (!RigidBody.Name.ToString().Contains("-convcol")) {
            throw new Exception("The rigidbody of " + Details.Name + " doesn't have the correct type, must have the name '-convcol', but has " + RigidBody.Name);
        } else {
            foreach (var child in RigidBody.GetAllChildrenOfType<CollisionShape3D>()) {
                if (!child.Shape.GetType().IsAssignableFrom(typeof(ConvexPolygonShape3D))) {
                    throw new Exception("The rigidbody of " + Details.Name + "doesn't have the correct collision type, should be " + nameof(ConvexPolygonShape3D) + " but is " + child.Shape.GetType());
                }
            }
        }

        if (_isMainCar) {
            // only report contacts on the player car
            RigidBody.ContactMonitor = true;
            RigidBody.MaxContactsReported = 2;
        } else {
            // use a simplified model for the collision if its an ai
            var first = RigidBody.GetAllChildrenOfType<CollisionShape3D>().First();
            foreach (var child in RigidBody.GetAllChildrenOfType<CollisionShape3D>()) {
                child.GetParent().RemoveChild(child);
                child.Owner = null;
            }
            var collisionShape = (ConvexPolygonShape3D)first.Shape;
            var extents = MeshHelper.GetBoxExtents(collisionShape.Points);
            var offset = (extents.Item1 + extents.Item2) / 2f;
            first.Transform = new Transform3D(first.Transform.Basis, first.Transform.Origin + offset);
            first.Shape = new BoxShape3D() {
                Size = (extents.Item2 - extents.Item1) * 0.9f // some reduction to allow for ground clearence
            };
            RigidBody.AddChild(first);
        }

        // update the car colour
        var carModels = RigidBody.GetChildren().Where(x => x is MeshInstance3D);
        foreach (var carModel in carModels.Cast<MeshInstance3D>()) {
            for (var i = 0; i < carModel.Mesh.GetSurfaceCount(); i++) {
                var material = carModel.GetActiveMaterial(i).Duplicate();
                if (material.ResourceName.Contains("[primary]") && material is StandardMaterial3D mat3D) {
                    // clone the material and use it to set an override because the mesh instance is shared between all cars
                    var newMat3D = (StandardMaterial3D)mat3D.Duplicate();
                    newMat3D.AlbedoColor = Colour;
                    carModel.SetSurfaceOverrideMaterial(i, newMat3D);
                }
            }
        }

        // set values from the details
        RigidBody.Mass = (float)Details.TotalMass;
        RigidBody.Transform = initialTransform ?? Transform3D.Identity;
        RigidBody.PhysicsMaterialOverride = new PhysicsMaterial() {
            Friction = 0.4f
        };
        AddChild(RigidBody);

        Wheels = Details.WheelDetails.Select(x => new Wheel(this, x)).ToArray();

        // attach wheels to car
        foreach (var w in Wheels) {
            RigidBody.AddChild(w);

            var skid = new WheelSkid(w);
            _skids.Add(skid);
            AddChild(skid);
        }

        if (_isMainCar) {
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

        var additionNode = carScene.GetChildren().FirstOrDefault(x => x.Name == "additions");
        if (additionNode != null) {
            foreach (var additionType in Enum.GetValues<CarModelAddition>()) {
                var models = additionNode
                    .GetAllChildrenOfType<MeshInstance3D>()
                    .Where(x => x.Name.ToString().Contains(additionType.ToString(), StringComparison.InvariantCultureIgnoreCase));

                if (models.Any()) {
                    _additions[additionType] = new Node3D();
                    foreach (var model in models) {
                        model.GetParent().RemoveChild(model);
                        model.Owner = null;
                        _additions[additionType].AddChild(model);
                    }
                    _additions[additionType].Visible = false;
                    RigidBody.AddChild(_additions[additionType]);
                }
            }
        }
    }

    public override void _Process(double delta) {
        foreach (var w in Wheels) {
            // rotate the front wheels (here because the wheels don't have their angle)
            if (w.Details.Id < 2) {
                w.Rotation = new Vector3(0, Inputs.Steering, 0);
            }
        }

        if (_isMainCar) {
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

        // track last velocity for collision purposes
        _lastVelocity = RigidBody.LinearVelocity;
    }

    public void ToggleAddition(CarModelAddition addition, bool active) {
        if (_additions.TryGetValue(addition, out Node3D? value)) {
            value.Visible = active;
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

        // calculate wear on the tyre based on ratiofract
        if (w.InContact && groundVelocity.LengthSquared() > 4) {
            // traction formula is unstable so we ignore it below 2 m/s completely
            // keep the p value above 0 so we don't gain tyre wear
            var amount = Mathf.Clamp(p - 1, 0, 3);
            w.TyreWear = Mathf.Max(0, w.TyreWear - w.Details.TyreWearRate * (float)delta * amount);
        }

        var td = Details.TractionDetails;
        w.AppliedForces = new Vector3() {
            // calc the longitudinal force from the slip ratio
            Z = ratiofract * (float)CalcWheelTraction.Calc(w.SlipRatio, td.LongMaxSlip, td.LongGripMax, td.LongPeakLength, td.LongPeakDecay),
            // calc the latitudinal force from the slip angle
            X = -anglefract * (float)CalcWheelTraction.Calc(w.SlipAngle, td.LatMaxSlip, td.LatGripMax, td.LatPeakLength, td.LongPeakDecay)
        };

        // reduce total traction based on tyre wear
        w.AppliedForces *= (float)CalcWheelTraction.TotalGripFromWear(w.TyreWear);

        // convert dimensionless values to Nm by adding the normal force
        w.AppliedForces *= w.SusForce.Length();


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
        if (Inputs is not null) {
            RemoveChild(Inputs as Node3D);
        }

        // clone into new car
        var car = new Car(details ?? Details, Inputs, _isMainCar, RigidBody.Transform, Colour);
        car.RigidBody.LinearVelocity = RigidBody.LinearVelocity;
        car.RigidBody.AngularVelocity = RigidBody.AngularVelocity;

        car.DistanceTravelled = DistanceTravelled;
        car._lastPos = _lastPos;
        car.Damage = Damage;
        car.Engine.CloneExistingState(Engine);

        for (var i = 0; i < Wheels.Length; i++) {
            car.Wheels[i].RadSec = Wheels[i].RadSec;
            car.Wheels[i].TyreWear = Wheels[i].TyreWear;
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

    /// <summary>
    /// This method is a hack because the godot RigidBody3D object doesn't tell us more than there 'was' a collision - i want impulse
    /// </summary>
    public Vector3 CalcLastFrameVelocityDiff() {
        if (!_lastVelocity.HasValue) return Vector3.Zero;

        return RigidBody.LinearVelocity - _lastVelocity.Value;
    }

    private const float DRIFT_MIN_ANGLE = 10;
    private const float DRIFT_MIN_SPEED = 5;
    public bool IsDrifting() {
        var speed = RigidBody.LinearVelocity.Length();
        return Mathf.Abs(DriftAngle) > DRIFT_MIN_ANGLE && Mathf.Abs(speed) > DRIFT_MIN_SPEED;
    }
    public double DriftFrameAmount(double delta) {
        var speed = RigidBody.LinearVelocity.Length();
        return (Mathf.Abs(DriftAngle) - DRIFT_MIN_ANGLE) * (float)delta * (speed - DRIFT_MIN_SPEED);
    }
}
