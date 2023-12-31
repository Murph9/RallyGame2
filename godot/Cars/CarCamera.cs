using Godot;

namespace murph9.RallyGame2.godot.Cars;

public partial class CarCamera : Node3D {

    private readonly Car _car;

    private readonly Vector3 _offset;
    private readonly Vector3 _lookAt;

    public CarCamera(Car car) {
        _car = car;

        _offset = new Vector3(0, _car.Details.camOffsetHeight, _car.Details.camOffsetLength);
        _lookAt = new Vector3(0, _car.Details.camLookAtHeight, 0);
    }

    public override void _PhysicsProcess(double delta) {
        GetViewport().GetCamera3D().LookAt(_car.RigidBody.GlobalPosition + _lookAt);
        GetViewport().GetCamera3D().Position = _car.RigidBody.Position + _car.RigidBody.GlobalBasis * _offset;
    }
}
