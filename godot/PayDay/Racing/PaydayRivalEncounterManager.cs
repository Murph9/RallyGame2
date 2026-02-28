using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Component.Racing;
using murph9.RallyGame2.godot.PayDay.Parts;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Base and
/// The rival's rarity hint indicates
/// how hard they are and what reward is on offer.
/// </summary>
public partial class PaydayRivalEncounterManager : RivalEncounterManager {

    [Signal]
    public delegate void RivalWonRewardEventHandler(CollectedPart reward);

    private int _dayNumber;

    private PartRarity _currentRivalRarity;

    // Visual highlighting
    private RivalHighlighter _highlighter;
    private RivalIndicatorUI _indicatorUI;

    public PaydayRivalEncounterManager() {
        SpawnedRival += SpawnedRivalHandler;
        RivalRaceStarted += RaceStartedHandler;
        RivalLost += RaceLostHandler;
        RivalWon += RaceWonHandler;
    }

    public void Init(InfiniteRoadManager roadManager, Car playerCar, int dayNumber) {
        base.Init(roadManager, playerCar);
        _dayNumber = dayNumber;
    }

    public void SpawnedRivalHandler(Car rival) {
        _currentRivalRarity = PartRarityHelper.RollRarity(_dayNumber);

        // Attach world-space glow + billboard to the rival
        _highlighter = new RivalHighlighter();
        _highlighter.Init(_currentRival, _currentRivalRarity);
        _currentRival.AddChild(_highlighter);

        // Attach screen-space HUD indicators
        _indicatorUI = new RivalIndicatorUI();
        _indicatorUI.Init(_playerCar, _currentRival, _currentRivalRarity);
        AddChild(_indicatorUI);
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        if (_roadManager == null || _playerCar == null) return;
        if (_currentRival == null) return;

        if (!_raceActive && _indicatorUI.IsInsideTree()) {
            _indicatorUI?.UpdateSpeedMatchProgress(_speedMatchTimer / SPEED_MATCH_WINDOW); // TODO func lookup in base
        } else if (_raceActive && _indicatorUI.IsInsideTree()) {
            _indicatorUI?.UpdateRaceProgress(RaceDistanceDriven, RACE_DISTANCE, RaceCheckpointDistance);
        }
    }

    private void RaceStartedHandler(Car rival) {
        _indicatorUI?.SetRaceActive(true);
    }

    private void RaceLostHandler() => RaceEndedHandler(false);
    private void RaceWonHandler() => RaceEndedHandler(true);

    private void RaceEndedHandler(bool playerWon) {
        _raceActive = false;

        if (playerWon) {
            var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
            var reward = PartDropTable.Generate(state.CarDetails, _dayNumber);
            // Override the rolled rarity with the advertised rival rarity
            var finalReward = new CollectedPart(reward.Part, _currentRivalRarity);
            EmitSignal(SignalName.RivalWonReward, finalReward);
        }

        // Clean up visual indicators
        _highlighter?.QueueFree();
        _highlighter = null;
        _indicatorUI?.QueueFree();
        _indicatorUI = null;
    }
}
