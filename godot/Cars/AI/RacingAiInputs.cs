using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class RacingAiInputs : CarAi {

    public float RoadWidth { get; set; } = 5;
    public RacingAiInputs(IRoadManager roadManager) : base(roadManager) { }

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition).ToArray();

        SteerAt(nextCheckPoints.First());

        DebugShapes.INSTANCE.AddLineDebug3D(ToString() + "checkpoint target", nextCheckPoints.First().Origin, Car.RigidBody.GlobalPosition, Colors.Blue);

        AccelCur = 1;
        BrakingCur = 0;

        if (TooFastForNextCheckpoints(nextCheckPoints.Take(3).ToArray())) {
            BrakingCur = 1;
            AccelCur = 0;
        }

        var isDrifting = IsDrifting();
        if (isDrifting) {
            AccelCur = 0;
            Steering /= 2f; // turn less than wanted
        }
    }

    private (Vector3, Vector3) GetOuterWallFromCheckpoints(Vector3 checkpoint1, Vector3 checkpoint2) {
        var wallDir = checkpoint2 - checkpoint1;
        var wallStart = checkpoint1;

        var normal = new Basis(Vector3.Up, Mathf.Pi / 2) * wallDir;
        var newPosX = wallStart + normal.Normalized() * RoadWidth;
        var newPosXM = wallStart + normal.Normalized() * -RoadWidth;

        // pick the futhest pos so its the opposite wall
        if (newPosX.DistanceTo(Car.RigidBody.GlobalPosition) < newPosXM.DistanceTo(Car.RigidBody.GlobalPosition)) {
            return (newPosXM, wallDir);
        }

        return (newPosX, wallDir);
    }

    private bool TooFastForNextCheckpoints(Transform3D[] nextCheckpoints) {

        for (var i = 0; i < nextCheckpoints.Length - 1; i++) {
            var wall = GetOuterWallFromCheckpoints(nextCheckpoints[i].Origin, nextCheckpoints[i + 1].Origin);
            var tooFast = IsTooFastForWall(wall.Item1, wall.Item2);
            if (tooFast) {
                DebugShapes.INSTANCE.AddLineDebug3D(ToString() + "wall", wall.Item1, wall.Item1 + wall.Item2, Colors.Red);
                return true;
            }
        }
        return false;
    }
}
