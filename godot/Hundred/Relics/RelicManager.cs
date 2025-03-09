using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public partial class RelicManager : Node {
    private readonly HundredGlobalState _hundredGlobalState;

    private readonly List<Relic> _relics = [];

    public RelicManager(HundredGlobalState hundredGlobalState) {
        _hundredGlobalState = hundredGlobalState;

        _hundredGlobalState.TrafficCollision += TrafficCollision;
    }

    public void AddRelic<T>(float strength = 1) where T : Relic, new() {
        _relics.Add(new T() { Strength = strength });
    }
    public void AddRelic(RelicType type, float strength = 1) {
        if (type == RelicType.BOUNCY) {
            _relics.Add(new BouncyRelic(strength));
            return;
        }
        throw new Exception("Unknown relic type: " + type);
    }

    public List<Relic> GetRelics() => _relics;

    private void TrafficCollision(Car otherCar) {
        foreach (var relic in _relics) {
            if (relic is ITrafficCollisionRelic t) {
                t.TrafficCollision(otherCar);
            }
        }
    }

    public List<RelicType> GetValidRelics() {
        return RelicType.ALL_RELIC_TYPES.Except(_relics.Select(x => x.Type)).ToList();
    }
}
