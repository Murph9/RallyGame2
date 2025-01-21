using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class TrafficAiInputs : CarAi {

    private static readonly float MAX_SPEED = 50 / 3.6f; //km/h to m/s

    public TrafficAiInputs(IRoadManager roadManager) : base(roadManager) {

    }

    public override void _PhysicsProcess(double delta) {
        if (!_listeningToInputs) return;

        var nextCheckPoints = _roadManager.GetNextCheckpoints(Car.RigidBody.GlobalPosition, 2, true);
        DriveAt(nextCheckPoints.First());

        // if going too fast slow down a little
        if (Car.RigidBody.LinearVelocity.Length() > MAX_SPEED) {
            BrakingCur = 0.3f;
            AccelCur = 0;
        } else {
            BrakingCur = 0f;
            AccelCur = 0.2f;
        }
    }
}
