using Godot;
using murph9.RallyGame2.godot.Cars.Sim;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public abstract class Relic(float strength) {
    public float Strength { get; init; } = strength;
    public abstract RelicType Type { get; }

    // would love to use entity system here
    public double DelaySeconds { get; init; }
    public double Delay { get; set; }

    public void _Process(double delta) {
        if (Delay > 0) Delay -= delta;
    }
}

public interface IOnEventRelic {

}

public interface IOnPurchaseRelic {

}

public interface IModifyRelic {

}

public interface IOnKeyRelic {
    void ActionPressed(Car self, string actionName);
}

public interface ITrafficCollisionRelic {
    void TrafficCollision(Car otherCar);
}

public class BouncyRelic(float strength) : Relic(strength), ITrafficCollisionRelic {
    public override RelicType Type => RelicType.BOUNCY;
    public void TrafficCollision(Car otherCar) {
        otherCar.RigidBody.ApplyCentralImpulse(new Vector3(0, 10000, 0));
    }
}

public class JumpRelic : Relic, IOnKeyRelic {
    public override RelicType Type => RelicType.JUMP;

    public JumpRelic(float strength) : base(strength) {
        DelaySeconds = 5;
    }

    public void ActionPressed(Car self, string actionName) {
        if (Delay <= 0 && actionName == "car_action_1") {
            Delay = DelaySeconds;
            self.RigidBody.ApplyCentralImpulse(new Vector3(0, 10000, 0));
        }
    }
}
