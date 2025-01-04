using Godot;

namespace murph9.RallyGame2.godot.Hundred;

public partial class HundredUI : Node2D {

    public float DistanceTravelled { get; set; }

    public override void _Ready() {
    }

    public override void _Process(double delta) {
        var progressBar = GetNode<ProgressBar>("ProgressBar");
        if (progressBar != null)
            progressBar.Value = DistanceTravelled;
    }
}
