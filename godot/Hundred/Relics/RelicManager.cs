using Godot;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Hundred.Relics;

public partial class RelicManager : Node {

    private readonly HundredGlobalState _hundredGlobalState;

    private readonly List<Relic> _relics = [];

    public RelicManager(HundredGlobalState hundredGlobalState) {
        _hundredGlobalState = hundredGlobalState;

        // _hundredGlobalState.Onaaaa += () => { };
    }

    public void AddRelic<T>() where T : Relic, new() {
        _relics.Add(new T());
    }
}

public abstract class Relic { }

public interface IOnEventRelic {

}

public interface IOnPurchaseRelic {

}

public interface IModifyRelic {

}

public class BouncyRelic : Relic, IOnPurchaseRelic {

}
