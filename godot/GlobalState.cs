using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot;


public partial class GlobalState : Node {
    public IRoadManager RoadManager { get; set; }
    public Car PlayerCar { get; set; }
}
