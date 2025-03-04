using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public partial class RelicManager : Node {

    private readonly HundredGlobalState _hundredGlobalState;

    private readonly List<Relic> _relics = [];

    public RelicManager(HundredGlobalState hundredGlobalState) {
        _hundredGlobalState = hundredGlobalState;

        _hundredGlobalState.TrafficCollision += (node) => {
            // TODO relic stuff
        };
    }

    public void AddRelic<T>(float strength = 1) where T : Relic, new() {
        _relics.Add(new T() { Strength = strength });
    }
}

public abstract class Relic(float strength) {
    public float Strength { get; init; } = strength;
}

public interface IOnEventRelic {

}

public interface IOnPurchaseRelic {

}

public interface IModifyRelic {

}

public interface ITrafficCollisionRelic {
    void TrafficCollision(Car otherCar);
}

public class BouncyRelic(float strength) : Relic(strength), ITrafficCollisionRelic {
    public void TrafficCollision(Car otherCar) {
        otherCar.RigidBody.ApplyCentralImpulse(new Vector3(0, 10000, 0));
    }
}
