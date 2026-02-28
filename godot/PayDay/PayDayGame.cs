using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.PayDay.Dialog;
using murph9.RallyGame2.godot.PayDay.Hub;
using murph9.RallyGame2.godot.PayDay.Loan;
using murph9.RallyGame2.godot.PayDay.Parts;
using murph9.RallyGame2.godot.PayDay.Racing;
using murph9.RallyGame2.godot.Utilities;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.PayDay;

/// <summary>
/// Top-level Pay Day Loan game manager. Owns the phase state machine:
///   Hub → Racing → RunEnd → PartApply → Hub (loop)
///                    ↘ DayEnd ↗
/// Win: loan paid off. Lose: debt exceeds 10× original principal.
/// </summary>
public partial class PayDayGame : Node {

    private const double RUN_DURATION_SECONDS = 3 * 60; // 3-minute runs

    private enum Phase { Hub, Racing, RunEnd, PartApply, DayEnd, Win, Lose }

    private Phase _phase = Phase.Hub;
    private Node _currentScene;
    private PayDayUI _activeUI;

    // Accumulated during a single run
    private readonly List<CollectedPart> _runParts = [];
    private float _runMoney;

    public PayDayGame() {
#if DEBUG
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
#endif
    }

    public override void _Ready() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        state.Reset();
        state.SetCarDetails(CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY));

        state.GameWon += () => CallDeferred(MethodName.GoToWin);
        state.GameLost += () => CallDeferred(MethodName.GoToLose);

        ShowIntroDialog();
    }

    public override void _Process(double delta) {
        // Allow escaping back to main menu during a run
        if (_phase == Phase.Racing && Input.IsActionJustPressed("menu_back")) {
            GetTree().ChangeSceneToFile("res://Main.tscn");
        }
    }

    // ─── Phase transitions ────────────────────────────────────────────────────

    private void ShowIntroDialog() {
        var dialog = LoadScene<DialogScreen>();
        dialog.SetDialogLines([
            new DialogLine("Dave", Colors.LightBlue,
                "Hey man... so I heard you got one of those rent-to-own deals on a TV?"),
            new DialogLine("Dave", Colors.LightBlue,
                "The interest rate starts at 8%. Per day. And it goes UP every day."),
            new DialogLine("You", Colors.White,
                "I just needed a TV, Dave."),
            new DialogLine("Dave", Colors.LightBlue,
                "BAHAHAHA. You are so cooked. Well — racing pays decent. Get out there!"),
        ]);
        dialog.Closed += () => {
            RemoveScene();
            CallDeferred(MethodName.GoToHub);
        };
        SwapScene(dialog);
    }

    private void GoToHub() {
        _phase = Phase.Hub;
        var hub = LoadScene<HubScene>();
        hub.StartRacing += () => CallDeferred(MethodName.StartRacingRun);
        hub.OpenLoanPaperwork += () => CallDeferred(MethodName.ShowDayEnd);
        hub.OpenPhone += () => CallDeferred(MethodName.ShowFriendDialog);
        SwapScene(hub);
    }

    private void StartRacingRun() {
        _phase = Phase.Racing;
        _runParts.Clear();
        _runMoney = 0f;

        var racing = LoadScene<PayDayRacingScene>();
        racing.RivalRaceStarted += (rival) => {
            _activeUI?.UpdateRivalStatus("Rival Race Started");
        };
        racing.RivalWon += (reward) => {
            _runParts.Add(reward);
            _runMoney += 200f; // flat rivalry win bonus
            _activeUI?.UpdateRivalStatus($"{PartRarityHelper.GetExclamation(reward.Rarity)} {PartRarityHelper.GetDisplayName(reward.Rarity)} {reward.Part?.Name}!");
            GetNode<PayDayGlobalState>("/root/PayDayGlobalState").AddCollectedPart(reward);
        };
        racing.RivalLost += () => {
            _activeUI?.UpdateRivalStatus("Lost the race...");
        };
        SwapScene(racing);

        _activeUI = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(PayDayUI))).Instantiate<PayDayUI>();
        _activeUI.StartTimer(RUN_DURATION_SECONDS);
        _activeUI.TimerExpired += () => CallDeferred(MethodName.EndRacingRun);
        AddChild(_activeUI);
    }

    private void EndRacingRun() {
        RemoveUI();

        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        state.EndRun(_runMoney);

        _phase = Phase.RunEnd;
        var endScreen = LoadScene<RunEndScreen>();
        endScreen.Populate(_runMoney, _runParts);
        endScreen.ReturnHome += () => CallDeferred(MethodName.GoToHubEvening);
        SwapScene(endScreen);
    }

    private void GoToHubEvening() {
        if (_runParts.Count > 0) {
            _phase = Phase.PartApply;
            var applyScreen = LoadScene<PartApplyScreen>();
            applyScreen.SetParts(_runParts);
            applyScreen.Closed += () => CallDeferred(MethodName.GoToHub);
            SwapScene(applyScreen);
        } else {
            GoToHub();
        }
    }

    private void ShowDayEnd() {
        _phase = Phase.DayEnd;
        var dayEnd = LoadScene<DayEndScreen>();
        dayEnd.DayEnded += (amountPaid) => {
            var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
            state.EndDay(amountPaid);
            // GameWon/GameLost signals will fire from state if terminal; otherwise return to hub
            if (_phase == Phase.DayEnd)
                CallDeferred(MethodName.GoToHub);
        };
        SwapScene(dayEnd);
    }

    private void ShowFriendDialog() {
        var dialog = LoadScene<DialogScreen>();
        dialog.SetDialogLines([
            new DialogLine("Karen", Colors.LightPink,
                "How's the debt going? I heard you got into that EZ Cash thing."),
            new DialogLine("Karen", Colors.LightPink,
                "My cousin did that. He owes them his kidney. Figuratively. Probably."),
            new DialogLine("You", Colors.White,
                "It's fine. It's fine. IT IS FINE."),
        ]);
        dialog.Closed += () => CallDeferred(MethodName.GoToHub);
        SwapScene(dialog);
    }

    private void GoToWin() {
        _phase = Phase.Win;
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        var panel = new PanelContainer();
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var label = new Label {
            Text = $"LOAN PAID OFF!\n\nYou did it in {state.DayNumber - 1} day(s).\n\nThe TV is yours.\n\nYOU WIN!!!",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", Colors.Gold);
        vbox.AddChild(label);

        var btn = new Button { Text = "Back to Menu" };
        btn.Pressed += () => GetTree().ChangeSceneToFile("res://Main.tscn");
        vbox.AddChild(btn);

        SwapScene(panel);
    }

    private void GoToLose() {
        _phase = Phase.Lose;
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");

        var panel = new PanelContainer();
        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var label = new Label {
            Text = $"GAME OVER\n\nThe loan reached ${state.Loan.Principal:F2}.\n"
                  + $"That's {state.Loan.Principal / state.Loan.StartPrincipal:F1}× the original.\n\n"
                  + "Dave tried to warn you.",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", Colors.Red);
        vbox.AddChild(label);

        var btn = new Button { Text = "Back to Menu" };
        btn.Pressed += () => GetTree().ChangeSceneToFile("res://Main.tscn");
        vbox.AddChild(btn);

        SwapScene(panel);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private T LoadScene<T>() where T : Node {
        return GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(T))).Instantiate<T>();
    }

    private void SwapScene(Node newScene) {
        RemoveScene();
        _currentScene = newScene;
        AddChild(_currentScene);
    }

    private void RemoveScene() {
        if (_currentScene == null) return;
        RemoveChild(_currentScene);
        _currentScene.QueueFree();
        _currentScene = null;
    }

    private void RemoveUI() {
        if (_activeUI == null) return;
        RemoveChild(_activeUI);
        _activeUI.QueueFree();
        _activeUI = null;
    }
}
