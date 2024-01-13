using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Cars.Init.Part;
using murph9.RallyGame2.godot.Component;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class UpgradeMenu : CenterContainer
{
	private CarDetails _carDetailsPrevious;
	private CarDetails _carDetails;

	private Node[] _upgrades; // each containing a dropdown of level

	private Graph _torqueGraph; //s?
	private Label _stats;

    public override void _Ready() {
		_carDetailsPrevious = CarType.Runner.LoadFromFile(Main.DEFAULT_GRAVITY);
		_carDetails = _carDetailsPrevious.Clone();

		LoadPage();
    }

	private void LoadPage() {
		foreach (var child in GetChildren().ToList()) {
			RemoveChild(child);
		}
		_carDetails.Engine.LoadProps();

		var root = new VBoxContainer();
		AddChild(root);

		root.AddChild(new Label() { Text = "Upgrade Menu", HorizontalAlignment = HorizontalAlignment.Center });
		var mainBox = new HBoxContainer();
		root.AddChild(mainBox);

		var optionsBox = new VBoxContainer();
		mainBox.AddChild(optionsBox);

		var statsBox = new VBoxContainer();
		mainBox.AddChild(statsBox);

		var maxTorque = _carDetails.Engine.MaxTorque();
		_stats = new Label() {
			Text =  $"Max kW (Nm): {double.Round(maxTorque.Item1, 2)} ({double.Round(EngineDetails.TorqueToKw(maxTorque.Item1, maxTorque.Item2), 2)}) @ {maxTorque.Item2} rpm]\n"
		};
		var causes = _carDetails.Engine.GetValueCauses();
		statsBox.AddChild(_stats);
		var prevDetails = _carDetailsPrevious.Engine.AsDict();
		foreach (var entry in _carDetails.Engine.AsDict()) {
			_stats.Text += entry.Key + " -> " + double.Round(entry.Value, 2);
			if (entry.Value != prevDetails[entry.Key]) {
				_stats.Text += " was " + double.Round(prevDetails[entry.Key], 2);
			}
			_stats.Text += "set by " + string.Join(",", causes[entry.Key].Select(x => x.Name));
			_stats.Text += "\n";
		}

        var datasetNew = new Graph.Dataset("Torque", 200, max: 500) {
            Color = Colors.Green
        };
		var datasetKwNew = new Graph.Dataset("kW", 200, max: 500) {
            Color = Colors.Green
        };
		var datasetOld = new Graph.Dataset("TorqueOld", 200, max: 500) {
            Color = Colors.Blue
        };
		var datasetKwOld = new Graph.Dataset("kWOld", 200, max: 500) {
            Color = Colors.Blue
        };
        for (int i = 0; i < 200; i++) {
			if (i*50 < _carDetailsPrevious.Engine.MaxRpm) {
            	datasetNew.Push((float)_carDetails.Engine.CalcTorqueFor(i*50));
            	datasetKwNew.Push((float)_carDetails.Engine.CalcKwFor(i*50));
            	datasetOld.Push((float)_carDetailsPrevious.Engine.CalcTorqueFor(i*50));
            	datasetKwOld.Push((float)_carDetailsPrevious.Engine.CalcKwFor(i*50));
			}
        }
		_torqueGraph = new Graph(new Vector2(300, 250), new [] { datasetNew, datasetOld });
		statsBox.AddChild(_torqueGraph);

		var kWGraph = new Graph(new Vector2(300, 250), new [] { datasetKwNew, datasetKwOld });
		statsBox.AddChild(kWGraph);

		// options to select
		foreach (var part in _carDetails.Engine.Parts) {
			var option = new OptionButton();
			var popup = option.GetPopup();
			int i = 0;
			foreach (var l in part.Levels) {
				popup.AddItem(i.ToString());
				i++;
			}
			option.Selected = part.CurrentLevel;
			option.ItemSelected += (a) => {
				ItemSelected(a, part);
			};

			optionsBox.AddChild(new Label() {
				Text = part.Name + " ---> " + string.Join(",", part.GetLevel().Select(x => x.Key + ": " + x.Value))
			});
			optionsBox.AddChild(option);
		}
		var b = new Button() {
			Text = "Apply"
		};
		b.Pressed += () => {
			_carDetailsPrevious = _carDetails.Clone();
			LoadPage();
		};
		optionsBox.AddChild(b);
	}

	private void ItemSelected(long id, Part part) {
		Console.WriteLine(id + " " + part.Name);
		part.CurrentLevel = (int)id;
		LoadPage();
	}

    public override void _Process(double delta) {}
}
