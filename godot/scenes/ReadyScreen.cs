using System;
using System.Linq;
using Godot;

namespace murph9.RallyGame2.godot.scenes;

public partial class ReadyScreen : CenterContainer, IScene {
    [Signal]
    public delegate void ClosedEventHandler();

    private readonly string[] DIFFICULTIES = ["Easy", "Medium", "Hard"];

    public override void _Ready() {
        // show:
        // - basic car stats
        // - the goal in laps
        // - rewards

        // maybe or later:
        // - the last goal and difference from it

        var root = GetNode<PanelContainer>("PanelContainer");

        var main = new VBoxContainer();
        root.AddChild(main);

        var state = GetNode<GlobalState>("/root/GlobalState");

        main.AddChild(new Label() {
            Text = $"Round {state.RoundResults.Count() + 1}"
        });

        main.AddChild(new Label() {
            Text = "Choose a Reward"
        });

        // pick the choices to show
        for (var i = 0; i < 3; i++) {
            var moneyToWin = 500 * (i + 1);
            var goalTime = state.SecondsToWin() - (i - 1);
            var choiceB = new Button() {
                Text = $"{DIFFICULTIES[i]} Goal: {Math.Round(goalTime, 2)} sec -> Reward: ${moneyToWin}"
            };
            choiceB.Pressed += () => {
                state.SetGoal(new RoundGoal() {
                    Time = goalTime
                });
                state.SetReward(new RoundReward() {
                    Money = moneyToWin,
                    PartCount = 1
                });
                EmitSignal(SignalName.Closed);
            };
            main.AddChild(choiceB);
        }
    }

    public override void _Process(double delta) { }
}
