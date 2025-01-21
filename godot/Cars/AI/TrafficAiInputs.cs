using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class TrafficAiInputs : CarAi {

    private const float BASE_TARGET_SPEED = 50 / 3.6f; //km/h to m/s

    private LineDebug3D _lineDebug3D = new();

    public float TargetSpeed { get; set; } = BASE_TARGET_SPEED;

    public TrafficAiInputs(IRoadManager roadManager) : base(roadManager) {
    }

    public override void _Ready() {
        AddChild(_lineDebug3D);
    }

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition, 2, true);

        _lineDebug3D.Start = Car.RigidBody.GlobalPosition;
        _lineDebug3D.End = nextCheckPoints.First().Origin;
        _lineDebug3D.Colour = Colors.Blue;

        DriveAt(nextCheckPoints.First());

        // if going too fast slow down a little
        if (Car.RigidBody.LinearVelocity.Length() > TargetSpeed) {
            BrakingCur = 0.3f;
            AccelCur = 0;
        } else {
            BrakingCur = 0f;
            AccelCur = 0.2f;
        }
    }
}
