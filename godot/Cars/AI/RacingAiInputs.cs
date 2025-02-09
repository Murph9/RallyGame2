using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class RacingAiInputs : CarAi {

    public float RoadWidth { get; set; } = 3;
    private LineDebug3D _lineDebug3D = new();
    public RacingAiInputs(IRoadManager roadManager) : base(roadManager) {
    }
    public override void _Ready() {
        AddChild(_lineDebug3D);
    }

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition).ToArray();

        DriveAt(nextCheckPoints.First());

        AccelCur = 1;
        BrakingCur = 0;

        if (TooFastForNextCheckpoints(nextCheckPoints.Take(3).ToArray())) {
            BrakingCur = 1;
            AccelCur = 0;
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
                _lineDebug3D.Start = wall.Item1;
                _lineDebug3D.End = wall.Item1 + wall.Item2;
                _lineDebug3D.Colour = Colors.Red;
                return true;
            }
        }
        _lineDebug3D.Colour = Colors.Transparent;
        return false;
    }
}
