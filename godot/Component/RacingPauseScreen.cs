using Godot;

namespace murph9.RallyGame2.godot.Component;

public partial class RacingPauseScreen : CenterContainer {

    [Signal]
    public delegate void ResumeEventHandler();
    [Signal]
    public delegate void QuitEventHandler();

    public void ResumeButton_Pressed() {
        EmitSignal(SignalName.Resume);
    }

    public void QuitButton_Pressed() {
        EmitSignal(SignalName.Quit);
    }
}
