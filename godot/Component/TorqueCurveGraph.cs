using Godot;
using murph9.RallyGame2.godot.Cars.Init;

namespace murph9.RallyGame2.godot.Component;

public partial class TorqueCurveGraph(CarDetails details, CarDetails compareTo = null) : HBoxContainer {

    private readonly CarDetails _details = details;
    private readonly CarDetails _compareTo = compareTo;

    public override void _Ready() {
        var datasetNew = new Graph.Dataset("Torque", 200, max: 500) {
            Color = Colors.Green
        };
		var datasetKwNew = new Graph.Dataset("kW", 200, max: 500) {
            Color = Colors.Green * 0.8f
        };
		var datasetOld = new Graph.Dataset("TorqueOld", 200, max: 500) {
            Color = Colors.Blue
        };
		var datasetKwOld = new Graph.Dataset("kWOld", 200, max: 500) {
            Color = Colors.Blue * 0.8f
        };
		var maxRpm = Mathf.Max(_compareTo?.Engine?.MaxRpm ?? 0, _details.Engine.MaxRpm);
        for (int i = 0; i < 200; i++) {
			if (i*50 < maxRpm) {
            	datasetNew.Push((float)_details.Engine.CalcTorqueFor(i*50));
            	datasetKwNew.Push((float)_details.Engine.CalcKwFor(i*50));

            	datasetOld.Push((float)(_compareTo?.Engine?.CalcTorqueFor(i*50) ?? 0));
            	datasetKwOld.Push((float)(_compareTo?.Engine?.CalcKwFor(i*50) ?? 0));
			}
        }
		AddChild(new Graph(new Vector2(300, 250), [datasetNew, datasetOld, datasetKwNew, datasetKwOld]));
    }
}
