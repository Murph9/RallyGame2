using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot.Component;

public partial class TorqueCurveGraph(CarDetails details, CarDetails compareTo = null) : HBoxContainer {

    private readonly CarDetails _details = details;
    private readonly CarDetails _compareTo = compareTo;

    public override void _Ready() {
        var peakRPM = _details.Engine.MaxRpm;
        if (_compareTo != null) {
            peakRPM = Mathf.Max(_compareTo.Engine.MaxRpm, peakRPM);
        }

        var datasetTorque = new Graph.Dataset("Torque", 200, max: 500) {
            Color = Colors.Green
        };
        var datasetKw = new Graph.Dataset("kW", 200, max: 500) {
            Color = Colors.Green * 0.8f
        };
        var datasetTorqueCompare = new Graph.Dataset("Torque 2", 200, max: 500) {
            Color = Colors.Blue
        };
        var datasetKwCompare = new Graph.Dataset("kW 2", 200, max: 500) {
            Color = Colors.Blue * 0.8f
        };
        for (int i = 0; i < 200; i++) {
            if (i * 50 <= peakRPM) {
                datasetTorque.Push((float)_details.Engine.CalcTorqueFor(i * 50));
                datasetKw.Push((float)_details.Engine.CalcKwFor(i * 50));

                datasetTorqueCompare.Push((float)(_compareTo?.Engine?.CalcTorqueFor(i * 50) ?? 0));
                datasetKwCompare.Push((float)(_compareTo?.Engine?.CalcKwFor(i * 50) ?? 0));
            }
        }

        List<Graph.Dataset> datasets = [datasetTorque, datasetKw];
        if (_compareTo != null) {
            datasets.Add(datasetTorqueCompare);
            datasets.Add(datasetKwCompare);
        }

        AddChild(new Graph(new Vector2(300, 250), datasets));
    }
}
