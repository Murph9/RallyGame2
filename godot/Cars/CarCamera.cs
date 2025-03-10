using Godot;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarCamera : Node3D {

    private readonly Car _car;

    private readonly Vector3 _offset;
    private readonly Vector3 _lookAt;

    public CarCamera(Car car) {
        _car = car;

        _offset = new Vector3(0, _car.Details.CamOffsetHeight, _car.Details.CamOffsetLength);
        _lookAt = new Vector3(0, _car.Details.CamLookAtHeight, 0);
    }

    public override void _PhysicsProcess(double delta) {
        var cam = GetViewport().GetCamera3D();
        if (cam != null) {
            cam.LookAt(_car.RigidBody.GlobalPosition + _lookAt);
            cam.Position = _car.RigidBody.Position + _car.RigidBody.GlobalBasis * _offset;
        } else {
            GD.PushError(nameof(CarCamera) + ": No camera found");
        }
    }
}
