using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public class MoneyFromDriftingRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength) {
    private const float MIN_DRIFT_FOR_MONEY = 100;
    public override string DescriptionBBCode => $"Generate small money from drifting, at {InputStrength * 0.05f}x the value above {MIN_DRIFT_FOR_MONEY}";

    public override RelicType[] RequiredRelics => [((RelicType)typeof(DriftScoreRelic).FullName)];

    private float _lastDriftValue;

    public override void _Process(Car self, double delta) {
        base._Process(self, delta);

        var driftScoreRelic = _relicManager.GetRelics().FirstOrDefault(x => x is DriftScoreRelic);
        if (driftScoreRelic == null) {
            return; // filtering probably isn't working
        }

        var lastScore = ((DriftScoreRelic)driftScoreRelic).LastDriftScore;
        if (lastScore != _lastDriftValue) {
            // i.e. its a new score - changed in the last frame, this could collide but its a float
            _lastDriftValue = lastScore;

            if (_lastDriftValue > MIN_DRIFT_FOR_MONEY) {
                // add the last drift score as money (with offset)
                _relicManager.HundredGlobalState.AddMoney((_lastDriftValue - MIN_DRIFT_FOR_MONEY) * InputStrength * 0.05f);
            }
        }
    }
}

public class RivalRaceMoneyIncreaseRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IRivalRaceRelic {
    public override string DescriptionBBCode => $"Get {InputStrength}x more money against rivals";

    public void RivalRaceStarted(Car rival) { }

    public void RivalRaceWon(Car rival, double moneyDiff) {
        _relicManager.HundredGlobalState.AddMoney((float)moneyDiff * InputStrength);
    }

    public void RivalRaceLost(Car rival, double moneyDiff) { }
}

public class DriftScoreRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IAnyCollisionRelic {
    public override string DescriptionBBCode => $"Get a drift score";
    public float LastDriftScore { get; private set; }

    public override void _PhysicsProcess(Car self, double delta) {
        base._PhysicsProcess(self, delta);

        if (self.IsDrifting()) {
            OutputStrength += (float)self.DriftFrameAmount(delta);
        } else {
            if (OutputStrength > 0)
                LastDriftScore = OutputStrength;
            OutputStrength = 0;
        }
    }

    public void AnyCollision(Car self) {
        OutputStrength = 0;
    }
}

public class CollisionDamageReductionRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IDamagedRelic {

    public override string DescriptionBBCode => $"Reduce the damage taken by {Mathf.Round((1 - 0.8f) * (1f / InputStrength) * 100)}%";

    public void DamageTaken(Car self, float amount) {
        self.Damage -= amount * (1 - 0.2f) * (1f / InputStrength);
    }
}

public class BouncyRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IOnTrafficCollisionRelic {
    private static readonly float MASS_MULT = 2f;

    public override string DescriptionBBCode => $"Other cars bounce off you at {Mathf.Round(InputStrength * MASS_MULT)} strength";

    public void TrafficCollision(Car self, Car otherCar, Vector3 relativeVelocity) {
        OutputStrength = (float)otherCar.Details.TotalMass * MASS_MULT;
        otherCar.RigidBody.ApplyCentralImpulse(relativeVelocity * OutputStrength * InputStrength);
    }
}

public class JumpRelic : Relic, IOnKeyRelic {
    private const string ACTION_NAME = "car_action_1";
    private static readonly float MASS_MULT = 4f;

    public override string DescriptionBBCode => $"Allows you to jump on the {ACTION_NAME} button";

    public JumpRelic(RelicManager relicManager, RelicType relicType, float strength) : base(relicManager, relicType, strength) {
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

public class BigFanRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength) {
    private static readonly float MASS_MULT = 0.1f;
    private static readonly float MAX_SPEED = MyMath.KmhToMs(150);
    public override string DescriptionBBCode => $"Adds thrust which pushes you forward up to {Mathf.Round(MAX_SPEED)} km/h";
    public override CarModelAddition[] CarModelAdditions => [CarModelAddition.Fan];

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

public class DashForwardRelic : Relic, IOnKeyRelic {
    private const string ACTION_NAME = "car_action_2";
    private static readonly float MASS_MULT = 10f;

    public override string DescriptionBBCode => $"Gives you a large push fowards when you press the {ACTION_NAME} button";

    public DashForwardRelic(RelicManager relicManager, RelicType relicType, float strength) : base(relicManager, relicType, strength) {
        DelaySeconds = 5;
    }

    public void ActionPressed(Car self, string actionName) {
        if (Delay <= 0 && actionName == ACTION_NAME) {
            Delay = DelaySeconds;
            OutputStrength = (float)self.Details.TotalMass * MASS_MULT;
            self.RigidBody.ApplyCentralImpulse(self.RigidBody.Transform.Basis * new Vector3(0, 0, OutputStrength));
        }
    }
}

public class FuelReductionRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IOnPurchaseRelic {
    public override string DescriptionBBCode => $"Reduces fuel use down by {Mathf.Round((1 - 0.8f) * (1f / InputStrength) * 100)}%";

    public bool Applied { get; private set; }

    public void CarUpdated(Car self) {
        self.Details.Engine.FuelByRpmRate *= 0.8f * (1f / InputStrength);
        Applied = true;
    }
}

public class TyreWearReductionRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IOnPurchaseRelic {
    public override string DescriptionBBCode => $"Reduces tyre wear by {Mathf.Round((1 - 0.8f) * (1f / InputStrength) * 100)}%";

    public bool Applied { get; private set; }

    public void CarUpdated(Car self) {
        foreach (var wheel in self.Wheels) {
            wheel.Details.TyreWearRate *= 0.8f * (1f / InputStrength);
        }
        Applied = true;
    }
}

public class MoneyInRivalRaceRelic(RelicManager relicManager, RelicType relicType, float strength) : Relic(relicManager, relicType, strength), IRivalRaceRelic {

    public override string DescriptionBBCode => $"Generates ${Math.Round(MONEY_MULT * InputStrength, 2)} for every second in a rival race";

    public bool Applied { get; private set; }

    private static readonly float MONEY_MULT = 10f;

    private readonly Dictionary<Car, double> _startTimeTracking = [];

    public override void _Process(Car self, double delta) {
        base._Process(self, delta);

        var strength = InputStrength;
        var moneyIncreasedRelic = _relicManager.GetRelics().FirstOrDefault(x => x is RivalRaceMoneyIncreaseRelic);
        if (moneyIncreasedRelic != null) {
            strength *= moneyIncreasedRelic.InputStrength;
        }

        foreach (var rivalRace in _startTimeTracking) {
            // don't give out after 2 mins
            if (OutputStrength > 0 && _relicManager.HundredGlobalState.TotalTimePassed - rivalRace.Value > 120 * strength) {
                OutputStrength = 0;
                continue;
            }

            _relicManager.HundredGlobalState.AddMoney(MONEY_MULT * strength * (float)delta);
            OutputStrength = MONEY_MULT * strength;
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
