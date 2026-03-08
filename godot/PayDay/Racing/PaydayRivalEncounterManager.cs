using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Component.Racing;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.PayDay.Parts;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// PayDay-specific rival manager. Extends the base multi-rival system with:
///  - Rarity-based visual decorations per rival (RivalHighlighter + RivalIndicatorUI).
///  - Per-rival stake routing: Parts rivals reward their wagered part on win;
/// </summary>
public partial class PaydayRivalEncounterManager : RivalEncounterManager {

    [Signal]
    public delegate void RivalWonRewardEventHandler(CollectedPart reward);

    [Signal]
    public delegate void RivalWonMoneyEventHandler();


    private class PerRivalState {
        public PartRarity Rarity;
        public RivalStakeType Stake;
        public Part WageredPart;
        public RivalHighlighter Highlighter;
        public RivalIndicatorUI IndicatorUI;
        public int SlotIndex;
    }

    private readonly Dictionary<Car, PerRivalState> _rivalStates = [];
    private int _dayNumber;

    private PartRarity _currentRivalRarity;

    // Visual highlighting
    private RivalHighlighter _highlighter;
    private RivalIndicatorUI _indicatorUI;

    public PaydayRivalEncounterManager() {
        SpawnedRival += SpawnedRivalHandler;
        RivalRaceStarted += RaceStartedHandler;
        RivalLost += (rival) => RaceEndedHandler(rival, false);
        RivalWon += (rival) => RaceEndedHandler(rival, true);
    }

    public void Init(InfiniteRoadManager roadManager, Car playerCar, int dayNumber) {
        base.Init(roadManager, playerCar);
        _dayNumber = dayNumber;
    }

    private void SpawnedRivalHandler(Car rival) {
        var entry = GetRivalEntry(rival);

        var rarity = PartRarityHelper.RollRarity(_dayNumber);
        var stake = entry?.Stake ?? RivalStakeType.Money;
        var wageredPart = entry?.WageredPart;
        string wageredPartName = stake == RivalStakeType.Parts ? (wageredPart?.Name ?? "Part") : "$$$";

        var state = new PerRivalState {
            Rarity = rarity,
            Stake = stake,
            WageredPart = wageredPart,
            SlotIndex = NextFreeSlot()
        };

        // World-space glow + billboard
        var highlighter = new RivalHighlighter();
        highlighter.Init(rival, rarity);
        rival.AddChild(highlighter);
        state.Highlighter = highlighter;

        // Screen-space HUD indicator — slotIndex controls vertical position
        var indicatorUI = new RivalIndicatorUI();
        indicatorUI.Init(_playerCar, rival, rarity, stake, wageredPartName, state.SlotIndex);
        AddChild(indicatorUI);
        state.IndicatorUI = indicatorUI;

        _rivalStates[rival] = state;
    }

    /// <summary>Returns the lowest slot index (0-based) not already used by an active rival.</summary>
    private int NextFreeSlot() {
        var used = new HashSet<int>();
        foreach (var s in _rivalStates.Values)
            used.Add(s.SlotIndex);
        for (int i = 0; ; i++) {
            if (!used.Contains(i)) return i;
        }
    }

    private void RaceStartedHandler(Car rival) {
        if (_rivalStates.TryGetValue(rival, out var state))
            state.IndicatorUI?.SetRaceActive(true);
    }

    private void RaceEndedHandler(Car rival, bool playerWon) {
        if (!_rivalStates.TryGetValue(rival, out var state))
            return;

        if (playerWon) {
            if (state.Stake == RivalStakeType.Parts && state.WageredPart != null) {
                var reward = new CollectedPart(state.WageredPart, state.Rarity);
                EmitSignal(SignalName.RivalWonReward, reward);
            } else {
                EmitSignal(SignalName.RivalWonMoney);
            }
        }

        // Clean up visuals
        state.Highlighter?.QueueFree();
        state.IndicatorUI?.QueueFree();
        _rivalStates.Remove(rival);
    }


    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        foreach (var (rival, state) in _rivalStates) {
            if (state.IndicatorUI == null || !state.IndicatorUI.IsInsideTree())
                continue;

            var entry = GetRivalEntry(rival);
            if (entry == null)
                continue;

            if (!entry.RaceActive) {
                state.IndicatorUI.UpdateSpeedMatchProgress(entry.SpeedMatchTimer / SPEED_MATCH_WINDOW);
            } else {
                state.IndicatorUI.UpdateRaceProgress(RaceDistanceDrivenFor(entry), RACE_DISTANCE, RaceCheckpointDistanceFor(entry));
            }
        }
    }
}
