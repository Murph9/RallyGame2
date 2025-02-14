using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class TrafficAiInputs : CarAi {

    private const float BASE_TARGET_SPEED_MS = 30 / 3.6f; //km/h to m/s

    public float TargetSpeedMs { get; set; } = BASE_TARGET_SPEED_MS;

    public bool InReverse { get; init; }

    public TrafficAiInputs(IRoadManager roadManager, bool inReverse) : base(roadManager) {
        InReverse = inReverse;
    }

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition, InReverse, InReverse ? -1 : 1);

        DebugShapes.INSTANCE.AddLineDebug3D(ToString() + "target", Car.RigidBody.GlobalPosition, nextCheckPoints.First().Origin, InReverse ? Colors.Blue : Colors.Green);

        SteerAt(nextCheckPoints.First());

        var tooSlowForTarget = IsTooSlowForPoint(nextCheckPoints.First());
        var isDrifting = IsDrifting();
        var targetInfront = true;
        if (tooSlowForTarget && targetInfront) {
            AccelCur = 1f;
            BrakingCur = 0;
            if (isDrifting) {
                AccelCur = 0;
            }
        } else {
            BrakingCur = 0.3f;
            AccelCur = 0;
        }

        // if going too fast slow down a little
        if (Car.RigidBody.LinearVelocity.Length() > TargetSpeedMs) {
            AccelCur = 0;
        }
    }
}
