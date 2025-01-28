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

    public bool InReverse { get; init; }

    public TrafficAiInputs(IRoadManager roadManager, bool inReverse) : base(roadManager) {
        InReverse = inReverse;
    }

    public override void _Ready() {
        AddChild(_lineDebug3D);
    }

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition, InReverse, InReverse ? -1 : 1);

        _lineDebug3D.Start = Car.RigidBody.GlobalPosition;
        _lineDebug3D.End = nextCheckPoints.First().Origin;
        _lineDebug3D.Colour = InReverse ? Colors.Blue : Colors.Green;

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
