using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using murph9.RallyGame2.godot.Component;
using System;
using System.Linq;

namespace murph9.RallyGame2.godot;

public partial class UpgradeMenu : CenterContainer
{
	private CarDetails _carDetailsPrevious;
	private CarDetails _carDetails;

    public override void _Ready() {
		_carDetailsPrevious = CarMake.Runner.LoadFromFile(Main.DEFAULT_GRAVITY);
		_carDetails = _carDetailsPrevious.Clone();

		LoadPage();
    }

	private void LoadPage() {
		foreach (var child in GetChildren().ToList()) {
			RemoveChild(child);
		}
		_carDetails.LoadSelf(Main.DEFAULT_GRAVITY);

		var root = new VBoxContainer();
		AddChild(root);

		root.AddChild(new Label() { Text = "Upgrade Menu", HorizontalAlignment = HorizontalAlignment.Center });
		var mainBox = new HBoxContainer();
		root.AddChild(mainBox);

		var optionsBox = new VBoxContainer();
		mainBox.AddChild(optionsBox);

		var statsBox = new VBoxContainer();
		mainBox.AddChild(statsBox);

		var stats = new RichTextLabel() {
			LayoutMode = 2,
			BbcodeEnabled = true,
			SizeFlagsHorizontal = SizeFlags.Fill,
			FitContent = true,
			AutowrapMode = TextServer.AutowrapMode.Off
		};
		statsBox.AddChild(stats);

		var maxTorque = _carDetails.Engine.MaxTorque();
		var maxKw = _carDetails.Engine.MaxKw();
		stats.AppendText($"Max Torque (Nm): {double.Round(maxTorque.Item1, 2)} @ {maxTorque.Item2} rpm\n");
		stats.AppendText($"Max Power (kW): {double.Round(maxKw.Item1, 2)} @ {maxKw.Item2} rpm\n");
		stats.PushColor(Colors.White);
		stats.PushTable(4);
		var prevDetails = _carDetailsPrevious.AsDict();
		foreach (var entry in _carDetailsPrevious.Engine.AsDict()) {
			prevDetails.Add(entry.Key, entry.Value);
		}
		var causes = _carDetails.Engine.GetValueCauses();
		foreach (var entry in _carDetails.GetValueCauses()) {
			causes.Add(entry.Key, entry.Value);
		}
		var list = _carDetails.Engine.AsDict();
		foreach (var entry in _carDetails.AsDict()) {
			list.Add(entry.Key, entry.Value);
		}
		foreach (var entry in list) {
			stats.PushCell();
			stats.AppendText(entry.Key);
			stats.Pop();
			stats.PushCell();
			stats.PushColor(Colors.Blue);
			stats.AppendText(ToStringWithRounding(prevDetails[entry.Key], 2));
			stats.Pop();
			stats.Pop();
			if ((dynamic)entry.Value != (dynamic)prevDetails[entry.Key]) {
				// https://stackoverflow.com/a/8855857/9353639
				// TODO support arrays
				stats.PushCell();
				stats.PushColor(Colors.Green);
				stats.AppendText(ToStringWithRounding(entry.Value, 2));
				stats.Pop();
				stats.Pop();
			} else {
				stats.PushCell();
				stats.Pop();
			}
			stats.PushCell();
			stats.PushColor(Colors.Gray);
			stats.AppendText(string.Join(", ", causes[entry.Key].Select(x => $"[color={x.Color}]{x.Name}[/color]")));
			stats.Pop();
			stats.Pop();
		}

		stats.Pop();
		stats.Pop();

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
		var maxRpm = Mathf.Max(_carDetailsPrevious.Engine.MaxRpm, _carDetails.Engine.MaxRpm);
        for (int i = 0; i < 200; i++) {
			if (i*50 < maxRpm) {
            	datasetNew.Push((float)_carDetails.Engine.CalcTorqueFor(i*50));
            	datasetKwNew.Push((float)_carDetails.Engine.CalcKwFor(i*50));
            	datasetOld.Push((float)_carDetailsPrevious.Engine.CalcTorqueFor(i*50));
            	datasetKwOld.Push((float)_carDetailsPrevious.Engine.CalcKwFor(i*50));
			}
        }
		var powerGraph = new Graph(new Vector2(300, 250), [datasetNew, datasetOld, datasetKwNew, datasetKwOld]);
		statsBox.AddChild(powerGraph);

		// options to select
		var parts = _carDetails.Engine.Parts.ToList();
		parts.AddRange(_carDetails.Parts);
		foreach (var part in parts) {
			var option = new OptionButton();
			var popup = option.GetPopup();
			int i = 0;
			foreach (var l in part.GetAllValues()) {
				popup.AddItem("Level: " + i.ToString() + " " + string.Join(", ", l.Select(x => x.Key + ": " + x.Value)));
				i++;
			}
			option.Selected = part.CurrentLevel;
			option.ItemSelected += (id) => {
				part.CurrentLevel = (int)id;
				LoadPage();
			};
			var richLabel = new RichTextLabel() {
				LayoutMode = 2,
				BbcodeEnabled = true,
				SizeFlagsHorizontal = SizeFlags.Fill,
				FitContent = true,
				AutowrapMode = TextServer.AutowrapMode.Off
			};
			richLabel.PushColor(Color.FromHtml(part.Color));
			richLabel.AppendText(part.Name);
			optionsBox.AddChild(richLabel);
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

    public override void _Process(double delta) {}

	private static string ToStringWithRounding(object obj, int length) {
		if (obj is float f)
			return float.Round(f, length).ToString();
		if (obj is double d)
			return double.Round(d, length).ToString();
		return obj.ToString();
	}
}
