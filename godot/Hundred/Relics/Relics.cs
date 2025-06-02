using System;
using System.Collections.Generic;
using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot.Hundred.Relics;


public class CollisionDamageReductionRelic(RelicManager relicManager, float strength) : Relic(relicManager, strength), IDamagedRelic {

    public override string DescriptionBBCode => $"Reduce the damage taken from collisions to {0.8f * (1f / InputStrength) * 100}%";

    public void DamageTaken(Car self, float amount) {
        self.Damage -= amount * (1 - 0.2f) * (1f / InputStrength);
    }
}

public class BouncyRelic(RelicManager relicManager, float strength) : Relic(relicManager, strength), IOnTrafficCollisionRelic {
    private static readonly float MASS_MULT = 2f;

    public override string DescriptionBBCode => $"Other cars bounce off you at {InputStrength * MASS_MULT} strength";

    public void TrafficCollision(Car self, Car otherCar, Vector3 relativeVelocity) {
        OutputStrength = (float)otherCar.Details.TotalMass * MASS_MULT;
        otherCar.RigidBody.ApplyCentralImpulse(relativeVelocity * OutputStrength * InputStrength);
    }
}

public class JumpRelic : Relic, IOnKeyRelic {
    private const string ACTION_NAME = "car_action_1";
    private static readonly float MASS_MULT = 4f;

    public override string DescriptionBBCode => $"Allows you to jump on the {ACTION_NAME} button";

    public JumpRelic(RelicManager relicManager, float strength) : base(relicManager, strength) {
        DelaySeconds = 5;
    }

    public void ActionPressed(Car self, string actionName) {
        if (Delay <= 0 && actionName == ACTION_NAME) {
            Delay = DelaySeconds;
            OutputStrength = (float)self.Details.TotalMass * MASS_MULT;
            self.RigidBody.ApplyCentralImpulse(new Vector3(0, OutputStrength, 0));
        }
    }
}

public class BigFanRelic : Relic {
    private static readonly float MASS_MULT = 0.1f;
    private static readonly float MAX_SPEED = MyMath.KmhToMs(150);
    public override string DescriptionBBCode => $"Adds thrust which pushes you forward up to {MAX_SPEED} km/h";

    public BigFanRelic(RelicManager relicManager, float strength) : base(relicManager, strength) { }

    public override void _PhysicsProcess(Car self, double delta) {
        base._PhysicsProcess(self, delta);

        var dir = self.RigidBody.Basis * Vector3.Forward;
        var mass = self.Details.TotalMass;

        // decay fan at high speeds because otherwise it will overcome any drag
        var currentSpeed = self.RigidBody.LinearVelocity.Length();
        var speedDiff = Math.Max(0, (MAX_SPEED * InputStrength) - currentSpeed);

        self.RigidBody.ApplyCentralImpulse(-dir * (float)mass * MASS_MULT * InputStrength * (float)delta * speedDiff);
    }
}

public class FuelReductionRelic : Relic, IOnPurchaseRelic {
    public override string DescriptionBBCode => $"Reduces fuel use by {0.8f * (1f / InputStrength) * 100}%";

    public bool Applied { get; private set; }

    public FuelReductionRelic(RelicManager relicManager, float strength) : base(relicManager, strength) { }

    public void CarUpdated(Car self) {
        self.Details.Engine.FuelByRpmRate *= 0.8f * (1f / InputStrength);
        Applied = true;
    }
}

public class TyreWearReductionRelic : Relic, IOnPurchaseRelic {
    public override string DescriptionBBCode => $"Reduces tyre wear by {0.8f * (1f / InputStrength) * 100}%";

    public bool Applied { get; private set; }

    public TyreWearReductionRelic(RelicManager relicManager, float strength) : base(relicManager, strength) { }

    public void CarUpdated(Car self) {
        foreach (var wheel in self.Wheels) {
            wheel.Details.TyreWearRate *= 0.8f * (1f / InputStrength);
        }
        Applied = true;
    }
}

public class MoneyInRivalRaceRelic : Relic, IRivalRaceRelic {

    public override string DescriptionBBCode => $"Generates ${Math.Round(MONEY_MULT * InputStrength, 2)} for every second in a rival race";

    public bool Applied { get; private set; }

    private static readonly float MONEY_MULT = 10f;

    private readonly Dictionary<Car, double> _startTimeTracking = [];

    public MoneyInRivalRaceRelic(RelicManager relicManager, float strength) : base(relicManager, strength) { }

    public override void _Process(Car self, double delta) {
        base._Process(self, delta);

        foreach (var rivalRace in _startTimeTracking) {
            // don't give out after 2 mins
            if (OutputStrength > 0 && _relicManager.HundredGlobalState.TotalTimePassed - rivalRace.Value > 120 * InputStrength) {
                GD.Print(".. but its going too long");
                OutputStrength = 0;
                continue;
            }

            _relicManager.HundredGlobalState.AddMoney(MONEY_MULT * InputStrength * (float)delta);
            OutputStrength = MONEY_MULT * InputStrength;
        }
    }

    public void RivalRaceStarted(Car rival) {
        if (_startTimeTracking.ContainsKey(rival)) return; // probably a bug if this happens

        _startTimeTracking[rival] = _relicManager.HundredGlobalState.TotalTimePassed;
    }

    public void RivalRaceWon(Car rival, double moneyDiff) {
        _startTimeTracking.Remove(rival);
    }

    public void RivalRaceLost(Car rival, double moneyDiff) {
        _startTimeTracking.Remove(rival);
    }
}
