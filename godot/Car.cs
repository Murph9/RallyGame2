using Godot;
using murph9.RallyGame2.Car;
using murph9.RallyGame2.Car.Init;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Car : Node
{
    private readonly RigidBody3D _rigidBody;
    public RigidBody3D RigidBody => _rigidBody;
    private readonly CarDetails _details;
    public CarDetails Details => _details;

    public readonly Wheel[] Wheels;

    // some temp variables so the copied code can work
    private bool handbrakeCur;
    private float brakingCur;
    private float steeringFake;
    private float driftAngle;

    public const float TRACTION_MAXSLIP = 0.2f;
    public const float TRACTION_MAX = 3f;
    public const float TRACTION_MAXLENGTH = 0.2f;
    public const float TRACTION_DECAY = 3f;

    public Car(CarDetails details) {
        _details = details;

        var uiScene = GD.Load<PackedScene>("res://CarUI.tscn");
        var instance = uiScene.Instantiate<CarUI>();
        instance.Car = this;
        AddChild(instance);

        var scene = GD.Load<PackedScene>("res://assets/" + _details.carModel);
        var carModel = scene.Instantiate<Node3D>();
        _rigidBody = carModel.GetChildren().Single(x => x is RigidBody3D) as RigidBody3D;
        _rigidBody.Mass = _details.mass;
        var parent = _rigidBody.GetParent();
        parent.RemoveChild(_rigidBody); // remove the scene parent
        parent.QueueFree();
        AddChild(_rigidBody);

        Wheels = _details.wheelData.Select(x => {
            var sus = _details.SusByWheelNum(x.id);
            return new Wheel(x) {
                Ray = new RayCast3D() {
                    Position = x.position + new Vector3(0, sus.max_travel, 0),
                    TargetPosition = new Vector3(0, -sus.TravelTotal() - x.radius, 0)
                }
            };
        }).ToArray();
        
        _rigidBody.Position = new Vector3(1, 5, 1);
        // _rigidBody.Rotate(Vector3.Up, Mathf.DegToRad(135));
    }

    public override void _Ready() {
        // all 4 wheels now
        foreach (var w in Wheels) {
            _rigidBody.AddChild(w.Ray);

            var scene = GD.Load<PackedScene>("res://assets/" + w.Details.modelName);
            w.WheelModel = scene.Instantiate<Node3D>();
            if (w.Details.id % 2 == 1)
                w.WheelModel.Rotate(Vector3.Up, Mathf.DegToRad(180));
            w.WheelModel.Position = w.Details.position;
            
            _rigidBody.AddChild(w.WheelModel);
        }
    }

    public override void _Process(double delta) {
        GetViewport().GetCamera3D().LookAt(_rigidBody.GlobalPosition);

        foreach (var w in Wheels) {
            w._Process(delta);
        }
    }
    
    public override void _PhysicsProcess(double delta) {
        foreach (var w in Wheels) {
            CalcSuspension(w);

            if (w.InContact) {
                CalcTraction(w, delta);
                CalcDrag(w);
            } else {
                w.GripDir = new Vector3();
            }

            w._PhysicsProcess(delta);
        }
        ApplyCentralDrag();
    }

    private void CalcSuspension(Wheel w)
    {
        var hitPositionGlobal = w.Ray.GetCollisionPoint();
        var hitNormalGlobal = w.Ray.GetCollisionNormal();

        w.InContact = w.Ray.IsColliding();
        if (!w.Ray.IsColliding()) {
            return;
        }
        w.ContactPoint = _rigidBody.ToLocal(hitPositionGlobal);
        
        var distance = w.Ray.GlobalPosition.DistanceTo(hitPositionGlobal);
        var maxDist = w.Ray.TargetPosition.Length();
        w.SusTravelFraction = Math.Clamp(maxDist - distance, 0, maxDist); // an offset
        
        var hitVelocity = _rigidBody.LinearVelocity + _rigidBody.AngularVelocity.Cross(hitPositionGlobal - _rigidBody.GlobalPosition);
        // then calc other thing velocity if its a rigidbody
        w.ContactRigidBody = w.Ray.GetCollider() as RigidBody3D;
        if (w.ContactRigidBody != null)
            hitVelocity += w.ContactRigidBody.LinearVelocity + w.ContactRigidBody.AngularVelocity.Cross(hitPositionGlobal - w.ContactRigidBody.GlobalPosition);

        var relVel = hitNormalGlobal.Dot(hitVelocity);
        var susDetails = _details.SusByWheelNum(0);
        var damping = -susDetails.Relax() * relVel;
        if (relVel > 0) {
            damping = -susDetails.Compression() * relVel;
        }

        var swayForce = 0f;
        int w_id_other = w.Details.id == 0 ? 1 : w.Details.id == 1 ? 0 : w.Details.id == 2 ? 3 : 2; // fetch the index of the other side
        if (Wheels[w_id_other].InContact) {
            float swayDiff = Wheels[w_id_other].Ray.TargetPosition.Length() - w.Ray.TargetPosition.Length();
            swayForce = swayDiff * susDetails.antiroll;
        }
        
        var totalForce = (swayForce + w.SusTravelFraction) * susDetails.stiffness + damping;
        if (totalForce > 0) {
            // reduce force based on angle to surface
            var rayDirectionGlobal = _rigidBody.GlobalBasis * w.Ray.TargetPosition.Normalized();
            var surfaceNormalFactor = hitNormalGlobal.Dot(-rayDirectionGlobal);
            
            var forceDir = 1000 * -rayDirectionGlobal * totalForce * surfaceNormalFactor;
            _rigidBody.ApplyForce(forceDir, hitPositionGlobal - _rigidBody.GlobalPosition);
            w.SusForce = forceDir;

            w.ContactRigidBody?.ApplyForce(-forceDir, hitPositionGlobal - w.ContactRigidBody.GlobalPosition);
        }
        // TODO suspension keeps sending the car in the -x,+z direction
        // TODO suspension seems to be applying the force on the wrong side of the car or badly during high angles
    }

    private void CalcTraction(Wheel w, double delta) {
        float lastSlipAngle = w.SlipAngle;
        float lastSlipRatio = w.SlipRatio;

        var rotationWorld = _rigidBody.Transform.Basis;
        var angularVelocity = _rigidBody.AngularVelocity;

        var localVel = rotationWorld.Inverse() * _rigidBody.LinearVelocity;
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
            objectRelVelocity = rotationWorld.Inverse() * w.ContactRigidBody.LinearVelocity;
        float groundVelocityZ = localVel.Z - objectRelVelocity.Z;

        float slipr = w.RadSec * w.Details.radius - groundVelocityZ;
        if (groundVelocityZ == 0)
            w.SlipRatio = 0;
        else
            w.SlipRatio = slipr / Mathf.Abs(groundVelocityZ);

        if (handbrakeCur && w.Details.id >= 2) // rearwheels only
            w.RadSec = 0;

        w.SlipAngle = 0;
        if (w.Details.id < 2) {
            // front wheels
            float slipa_front = localVel.X - objectRelVelocity.X + w.Details.position.Z * angVel;
            w.SlipAngle = Mathf.Atan2(slipa_front, Mathf.Abs(groundVelocityZ)) - steeringFake;
        } else {
            // rear wheels
            float slipa_rear = localVel.X - objectRelVelocity.X + w.Details.position.Z * angVel;
            driftAngle = slipa_rear; // set drift angle as the rear amount
            w.SlipAngle = Mathf.Atan2(slipa_rear, Mathf.Abs(groundVelocityZ));
        }

        // some hacks to help the simulation
        SlipSimulationHacks(w, lastSlipRatio, lastSlipAngle, TRACTION_MAXSLIP, TRACTION_MAXSLIP);

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
        var brakeCurrent2 = brakingCur;
        if (Mathf.Abs(w.SlipRatio / TRACTION_MAXSLIP) >= 1 && localVel.Length() > 10 && brakingCur > 0)
            brakeCurrent2 = 0; // very good abs

        // add the wheel force after merging the forces
        var totalLongForce = /*wheelTorque[w_id]*/0 - wheel_force.Z // TODO wheel force from engine
                - (brakeCurrent2 * _details.brakeMaxTorque * Mathf.Sign(w.RadSec));
        // drive wheels have the engine to pull along
        float wheelInertia = _details.Wheel_inertia(w.Details.id);
        if (_details.driveFront && (w.Details.id == 0 || w.Details.id == 1)) {
            wheelInertia = _details.E_inertia();
        }
        if (_details.driveRear && (w.Details.id == 2 || w.Details.id == 3)) {
            wheelInertia = _details.E_inertia();
        }
        var totalLongForceTorque = totalLongForce / wheelInertia * w.Details.radius;
        
        if (brakeCurrent2 != 0 && Mathf.Sign(w.RadSec) != Mathf.Sign(w.RadSec + totalLongForceTorque))
            w.RadSec = 0; // maxed out the forces with braking, so prevent wheels from moving
        else
            w.RadSec += (float)delta * totalLongForceTorque; // so the radSec can be used next frame, to calculate slip ratio

        w.GripDir = wheel_force;
        if (wheel_force.LengthSquared() > 0)
            _rigidBody.ApplyForce(_rigidBody.ToGlobal(wheel_force), _rigidBody.ToGlobal(w.ContactPoint) - _rigidBody.GlobalPosition);
        // TODO apply to other rigid body if it exists
    }

    private void SlipSimulationHacks(Wheel w, float prevSlipRatio, float prevSlipAngle, float maxLatSlipAt, float maxLongSlipAt) {
        // Hack1: prevent 'losing' traction through a large integration step, by detecting jumps past the curve peak
		// this should only affect this class as its only affecting the force by affecting where on curve the current state is
		if (Mathf.Abs(prevSlipAngle) < maxLatSlipAt
				&& Mathf.Abs(w.SlipAngle) > maxLatSlipAt) {
			w.SlipAngle = maxLatSlipAt * Mathf.Sign(w.SlipAngle);
		}
		if (Mathf.Abs(prevSlipAngle) > maxLatSlipAt
				&& Mathf.Abs(w.SlipAngle) < maxLatSlipAt) {
			w.SlipAngle = maxLatSlipAt * Mathf.Sign(prevSlipAngle);
		}
		if (brakingCur == 0) { // needs to be disabled during braking as it prevents you from stopping
			if (Mathf.Abs(prevSlipRatio) < maxLongSlipAt
					&& Mathf.Abs(w.SlipRatio) > maxLongSlipAt) {
				w.SlipRatio = maxLongSlipAt * Mathf.Sign(w.SlipRatio);
			}
			if (Mathf.Abs(prevSlipRatio) > maxLongSlipAt
					&& Mathf.Abs(w.SlipRatio) < maxLongSlipAt) {
				w.SlipRatio = maxLongSlipAt * Mathf.Sign(prevSlipRatio);
			}
		}
		// Hack2: prevent flipping traction over 0 too fast, by always applying 0
		// inbetween
		if (Mathf.Abs(prevSlipAngle) * maxLatSlipAt < 0) { // will be negative if they have both signs
			w.SlipAngle = 0;
		}
		if (Mathf.Abs(prevSlipRatio) * maxLongSlipAt < 0) { // will be negative if they have both signs
			w.SlipRatio = 0;
		}
    }

    private void CalcDrag(Wheel w)
    {
        
    }

    private void ApplyCentralDrag() {
        
    }
}
