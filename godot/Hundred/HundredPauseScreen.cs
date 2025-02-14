using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.Utilities;
using System.Linq;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredPauseScreen : CenterContainer {

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
