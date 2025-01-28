using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class StopAiInputs : CarAi {
    public StopAiInputs(IRoadManager roadManager) : base(roadManager) {
    }

    public override void _PhysicsProcess(double delta) {
        BrakingCur = 1;
        Steering = 0;
        AccelCur = 0;
        HandbrakeCur = false;
    }
}
