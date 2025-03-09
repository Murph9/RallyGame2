using Godot;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public abstract class Relic(float strength) {
    public float Strength { get; init; } = strength;
    public RelicType Type { get; } = RelicType.BOUNCY;
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
