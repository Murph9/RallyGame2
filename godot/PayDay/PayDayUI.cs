using Godot;
using System;

namespace murph9.RallyGame2.godot.PayDay;

/// <summary>HUD overlay shown during a racing run.</summary>
public partial class PayDayUI : CanvasLayer {

    [Signal]
    public delegate void TimerExpiredEventHandler();

    private double _runDurationSeconds;
    private double _elapsed;
    private bool _running;
    private bool _paused;

    public void StartTimer(double durationSeconds) {
        _runDurationSeconds = durationSeconds;
        _elapsed = 0;
        _running = true;
    }

    public void SetPaused(bool paused) => _paused = paused;

    public override void _Process(double delta) {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        GetNode<Label>("HUD/TopBar/MoneyLabel").Text = $"${state.Money:F0}";
        GetNode<Label>("HUD/TopBar/PartsLabel").Text = $"Parts: {state.PartInventory.Count}";

        if (!_running || _paused) return;

        _elapsed += delta;
        double remaining = Math.Max(0, _runDurationSeconds - _elapsed);

        int mins = (int)(remaining / 60);
        int secs = (int)(remaining % 60);
        GetNode<Label>("HUD/TopBar/TimerLabel").Text = $"{mins}:{secs:D2}";

        if (_elapsed >= _runDurationSeconds) {
            _running = false;
            EmitSignal(SignalName.TimerExpired);
        }
    }

    private void _on_button_pressed() {
        EmitSignal(SignalName.TimerExpired);
    }
}
