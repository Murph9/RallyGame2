using System;
using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public abstract class Relic(float inputStrength) {
    public float InputStrength { get; init; } = inputStrength;
    public float OutputStrength { get; protected set; }
    public abstract RelicType Type { get; }

    // would love to use entity system here
    public double DelaySeconds { get; init; }
    public double Delay { get; set; }

    public void _Process(double delta) {
        if (Delay > 0) Delay -= delta;
    }
}

public interface IOnPurchaseRelic {

}

public interface IOnModifyRelic {

}

public interface IOnKeyRelic {
    void ActionPressed(Car self, string actionName);
}

public interface IOnTrafficCollisionRelic {
    void TrafficCollision(Car self, Car otherCar);
}

public interface IOnPhysicsProcessRelic {
    void PhysicsProcess(Car self, double delta);
}

public class BouncyRelic(float strength) : Relic(strength), IOnTrafficCollisionRelic {
    public override RelicType Type => RelicType.BOUNCY;
    private static readonly float MASS_MULT = 10f;

    public void TrafficCollision(Car self, Car otherCar) {
        OutputStrength = (float)otherCar.Details.TotalMass * MASS_MULT;
        otherCar.RigidBody.ApplyCentralImpulse(new Vector3(0, OutputStrength, 0));
    }
}

public class JumpRelic : Relic, IOnKeyRelic {
    public override RelicType Type => RelicType.JUMP;
    private static readonly float MASS_MULT = 4f;

    public JumpRelic(float strength) : base(strength) {
        DelaySeconds = 5;
    }

    public void ActionPressed(Car self, string actionName) {
        if (Delay <= 0 && actionName == "car_action_1") {
            Delay = DelaySeconds;
            OutputStrength = (float)self.Details.TotalMass * MASS_MULT;
            self.RigidBody.ApplyCentralImpulse(new Vector3(0, OutputStrength, 0));
        }
    }
}

public class BigFanRelic : Relic, IOnPhysicsProcessRelic {
    public override RelicType Type => RelicType.BIGFAN;
    private static readonly float MASS_MULT = 0.1f;
    private static readonly float MAX_SPEED = MyMath.KmhToMs(150);
    public BigFanRelic(float strength) : base(strength) { }

    public void PhysicsProcess(Car self, double delta) {
        var dir = self.RigidBody.Basis * Vector3.Forward;
        var mass = self.Details.TotalMass;

        // decay fan at high speeds because otherwise it will overcome any drag
        var currentSpeed = self.RigidBody.LinearVelocity.Length();
        var speedDiff = Math.Max(0, (MAX_SPEED * InputStrength) - currentSpeed);

        self.RigidBody.ApplyCentralImpulse(-dir * (float)mass * MASS_MULT * InputStrength * (float)delta * speedDiff);
    }
}