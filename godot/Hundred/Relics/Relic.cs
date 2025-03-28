using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public abstract class Relic(RelicManager relicManager, float inputStrength) {
    public float InputStrength { get; init; } = inputStrength;
    public float OutputStrength { get; protected set; }
    public abstract RelicType Type { get; }

    // would love to use an entity system here
    public double DelaySeconds { get; init; }
    public double Delay { get; set; }

    protected readonly RelicManager _relicManager = relicManager;

    public virtual void _Process(Car self, double delta) {
        if (Delay > 0) Delay -= delta;
    }

    public virtual void _PhysicsProcess(Car self, double delta) { }
}

public interface IOnPurchaseRelic {
    public bool Applied { get; }
    void CarUpdated(Car self);
}

public interface IOnKeyRelic {
    void ActionPressed(Car self, string actionName);
}

public interface IOnTrafficCollisionRelic {
    void TrafficCollision(Car self, Car otherCar, Vector3 relativeVelocity);
}

public interface IRivalRaceRelic {
    void RivalRaceStarted(Car rival);
    void RivalRaceWon(Car rival, double moneyDiff);
    void RivalRaceLost(Car rival, double moneyDiff);
}
