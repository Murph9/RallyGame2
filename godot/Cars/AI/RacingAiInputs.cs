using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities.Debug3D;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class RacingAiInputs(IRoadManager roadManager) : CarAi(roadManager) {

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition)
            .Where(x => x.Origin.DistanceTo(Car.RigidBody.GlobalPosition) > 5)
            .Take(3);
        if (!nextCheckPoints.Any()) {
            return;
        }

        AccelCur = 1;
        BrakingCur = 0;

        SteerAt(nextCheckPoints.First().Origin);

        DebugShapes.INSTANCE.AddLineDebug3D(ToString() + "checkpoint target", nextCheckPoints.First().Origin, Car.RigidBody.GlobalPosition, Colors.Blue);

        if (TooFastForNextCheckpoints((Car.RigidBody.GlobalPosition - nextCheckPoints.First().Origin).Normalized(),
                [.. nextCheckPoints.Prepend(_roadManager.GetPassedCheckpoint(Car.RigidBody.GlobalPosition))])) {
            BrakingCur = 1;
            AccelCur = 0;
        }

        var isDrifting = IsDrifting();
        if (isDrifting) {
            AccelCur = 0;
            BrakingCur = 0.5f;
        }

        FlipIfSlowUpsideDown();
    }

    private (Vector3, Vector3) GetOuterWallFromCheckpoints(Vector3 checkpoint1, Vector3 checkpoint2) {
        var wallDir = checkpoint2 - checkpoint1;
        var wallStart = checkpoint1;

        var normal = new Basis(Vector3.Up, Mathf.Pi / 2) * wallDir.Normalized();

        if (ShouldTurnLeftFor(checkpoint1)) {
            var newPosXM = wallStart + normal * -_roadManager.CurrentRoadWidth;
            return (newPosXM, wallDir);
        }

        var newPosX = wallStart + normal * _roadManager.CurrentRoadWidth;
        return (newPosX, wallDir);
    }

    private bool TooFastForNextCheckpoints(Vector3 targetDir, Transform3D[] nextCheckpoints) {
        for (var i = 0; i < nextCheckpoints.Length - 1; i++) {
            if ((nextCheckpoints[i].Origin - nextCheckpoints[i + 1].Origin).Normalized().Dot(targetDir) > 0.9f) {
                // pretty co-linear
                continue;
            }

            var wall = GetOuterWallFromCheckpoints(nextCheckpoints[i].Origin, nextCheckpoints[i + 1].Origin);
            var tooFast = IsTooFastForWall(nextCheckpoints[i].Origin, wall.Item1, wall.Item2);
            if (tooFast) {
                DebugShapes.INSTANCE.AddLineDebug3D(ToString() + "wall", wall.Item1, wall.Item1 + wall.Item2, Colors.Red);
                return true;
            }
        }
        return false;
    }
}
