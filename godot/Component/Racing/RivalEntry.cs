using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Component.Racing;

public partial class RivalEncounterManager {
    protected class RivalEntry {
        public Car Car;
        public CarDetails Details;
        public RivalStakeType Stake;
        public PartDetails WageredPart;
        public bool RaceActive;
        public float PlayerStartDist;
        public bool CheckpointSet;
        public double SpeedMatchTimer;
        public Checkpoint RaceCheckpoint;
    }
}
