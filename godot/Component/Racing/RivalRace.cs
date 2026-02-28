
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Component.Racing;

public class RivalRace(Car rival, float startDistance, float raceDistance) {
    public Car Rival { get; init; } = rival;
    public float StartDistance { get; init; } = startDistance;
    public float RaceDistance { get; init; } = raceDistance;
    public bool CheckpointSet { get; set; }
    public string Message { get; set; }
}
