using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class TrafficAiInputs(IRoadManager roadManager, bool inReverse) : CarAi(roadManager) {

    private static readonly float BASE_TARGET_SPEED_MS = MyMath.KmhToMs(30);

    public float TargetSpeedMs { get; set; } = BASE_TARGET_SPEED_MS;

    public bool InReverse { get; init; } = inReverse;

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition, InReverse, InReverse ? -1 : 1);

        DebugShapes.INSTANCE.AddLineDebug3D(ToString() + "target", Car.RigidBody.GlobalPosition, nextCheckPoints.First().Origin, InReverse ? Colors.Blue : Colors.Green);

        SteerAt(nextCheckPoints.First());

        var tooSlowForTarget = IsTooSlowForPoint(nextCheckPoints.First());

        var targetInfront = true; // TODO
        if (tooSlowForTarget && targetInfront) {
            AccelCur = 1f;
            BrakingCur = 0;
        } else {
            BrakingCur = 0.3f;
            AccelCur = 0;
        }

        if (IsDrifting()) {
            AccelCur = 0;
            Steering /= 2f; // turn less than wanted
        }

        // if going too fast slow down a little
        if (Car.RigidBody.LinearVelocity.Length() > TargetSpeedMs) {
            AccelCur = 0;
        }

        FlipIfSlowUpsideDown();
    }
}
