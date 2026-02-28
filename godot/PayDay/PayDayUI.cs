using Godot;
using System;

namespace murph9.RallyGame2.godot.PayDay;

/// <summary>HUD overlay shown during a racing run.</summary>
public partial class PayDayUI : CanvasLayer {

    [Signal]
    public delegate void TimerExpiredEventHandler();

    private Label _timerLabel;
    private Label _moneyLabel;
    private Label _partsLabel;
    private Label _rivalLabel;

    private double _runDurationSeconds;
    private double _elapsed;
    private bool _running;
    private bool _paused;

    public override void _Ready() {
        _timerLabel = GetNodeOrNull<Label>("HUD/TopBar/TimerLabel");
        _moneyLabel = GetNodeOrNull<Label>("HUD/TopBar/MoneyLabel");
        _partsLabel = GetNodeOrNull<Label>("HUD/TopBar/PartsLabel");
        _rivalLabel = GetNodeOrNull<Label>("HUD/SidePanel/RivalLabel");
    }

    public void StartTimer(double durationSeconds) {
        _runDurationSeconds = durationSeconds;
        _elapsed = 0;
        _running = true;
    }

    public void SetPaused(bool paused) => _paused = paused;

    public void UpdateRivalStatus(string text) {
        if (_rivalLabel != null)
            _rivalLabel.Text = text;
    }

    public override void _Process(double delta) {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        if (_moneyLabel != null) _moneyLabel.Text = $"${state.Money:F0}";
        if (_partsLabel != null) _partsLabel.Text = $"Parts: {state.PartInventory.Count}";

        if (!_running || _paused) return;

        _elapsed += delta;
        double remaining = Math.Max(0, _runDurationSeconds - _elapsed);

        if (_timerLabel != null) {
            int mins = (int)(remaining / 60);
            int secs = (int)(remaining % 60);
            _timerLabel.Text = $"{mins}:{secs:D2}";
        }

        if (_elapsed >= _runDurationSeconds) {
            _running = false;
            EmitSignal(SignalName.TimerExpired);
        }
    }
}
