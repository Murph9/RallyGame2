using Godot;

namespace murph9.RallyGame2.godot.Component;

public partial class RacingPauseScreen : CenterContainer {

    [Signal]
    public delegate void ResumeEventHandler();
    [Signal]
    public delegate void QuitEventHandler();

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("menu_back")) {
            EmitSignal(SignalName.Resume);
        }
    }

    public void ResumeButton_Pressed() {
        EmitSignal(SignalName.Resume);
    }

    public void QuitButton_Pressed() {
        EmitSignal(SignalName.Quit);
    }
}
