using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class Main : Node
{
    public override void _Ready() {
        AddChild(new Car());
    }

    public override void _Process(double delta) {
        
    }

    public void _on_button_pressed() {
        AddChild(new Car());
    }
}
