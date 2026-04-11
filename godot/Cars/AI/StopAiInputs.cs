using murph9.RallyGame2.godot.Component;

namespace murph9.RallyGame2.godot.Cars.AI;

public partial class StopAiInputs(IRoadManager roadManager) : CarAi(roadManager) {
    public override void CarAiPhysicsProcess(double delta) {
        WantBraking = 1;
        WantSteering = 0;
        WantAccel = 0;
        HandbrakeCur = false;
    }
}
