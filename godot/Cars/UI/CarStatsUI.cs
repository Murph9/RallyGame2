using Godot;
using murph9.RallyGame2.godot.Cars.Init;
using System;

namespace murph9.RallyGame2.godot.Cars.UI;

public partial class CarStatsUI : Control {

    private CarDetails _carDetails;
    private SimpleCarStats _stagedPublicStats;

    public void SetCarDetails(CarDetails carDetails, SimpleCarStats stagedPublicStats = null) {
        _carDetails = carDetails;
        _stagedPublicStats = stagedPublicStats;
        Refresh();
    }

    public void Refresh() {
        var stats = CarStatsCalculator.ComputeStats(_carDetails);
        var publicStats = CarStatsCalculator.ComputeSimpleStats(_carDetails);
        BuildUI(stats, publicStats);
    }

    private void BuildUI(CarStats stats, SimpleCarStats publicStats) {
        foreach (var child in GetChildren())
            child.QueueFree();

        var vbox = new VBoxContainer();
        AddChild(vbox);

        // Each stat row merges the normalised bar with the real-world value.
        // Staged deltas are shown inline when parts have been staged but not confirmed.
        AddStatRow(vbox, "Acceleration", stats.Acceleration,
            $"{publicStats.MaxKw:0} kW",
            _stagedPublicStats != null ? $"{_stagedPublicStats.MaxKw:0} kW" : null,
            _stagedPublicStats != null ? (double)(_stagedPublicStats.MaxKw - publicStats.MaxKw) : null,
            higherIsBetter: true);

        AddStatRow(vbox, "Top Speed", stats.TopSpeed,
            $"{publicStats.TopSpeedKmh:0} km/h",
            _stagedPublicStats != null ? $"{_stagedPublicStats.TopSpeedKmh:0} km/h" : null,
            _stagedPublicStats != null ? (double)(_stagedPublicStats.TopSpeedKmh - publicStats.TopSpeedKmh) : null,
            higherIsBetter: true);

        AddStatRow(vbox, "Handling", stats.Handling,
            $"{publicStats.LateralG:0.00} G",
            _stagedPublicStats != null ? $"{_stagedPublicStats.LateralG:0.00} G" : null,
            _stagedPublicStats != null ? (double)(_stagedPublicStats.LateralG - publicStats.LateralG) : null,
            higherIsBetter: true);

        AddStatRow(vbox, "Braking", stats.Braking,
            valueText: null, stagedText: null, delta: null, higherIsBetter: true);

        AddFlagRow(vbox, "Nitro", _carDetails.NitroEnabled);
        AddFlagRow(vbox, "Turbo", _carDetails.Engine.TurboAirMult > 1);
    }

    /// <summary>
    /// A stat row with a progress bar, an optional real-world value label, and an optional
    /// staged delta shown as a coloured arrow + new value when staging is active.
    /// </summary>
    private static void AddStatRow(VBoxContainer parent, string labelText, double value,
        string valueText, string stagedText, double? delta, bool higherIsBetter) {

        var hbox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill };

        var label = new Label {
            Text = labelText,
            CustomMinimumSize = new Vector2(90, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        hbox.AddChild(label);

        var bar = new ProgressBar {
            MinValue = 0,
            MaxValue = 100,
            Value = Mathf.Clamp(value * 100, 0, 100),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(120, 0),
        };
        hbox.AddChild(bar);

        if (valueText != null) {
            var valLabel = new Label {
                Text = valueText,
                CustomMinimumSize = new Vector2(72, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            valLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            hbox.AddChild(valLabel);
        }

        if (stagedText != null && delta.HasValue && Math.Abs(delta.Value) > 0.001) {
            bool improved = higherIsBetter ? delta.Value > 0 : delta.Value < 0;
            var arrowColor = improved ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.3f);
            string arrow = delta.Value > 0 ? "▲" : "▼";

            var deltaLabel = new Label {
                Text = $"{arrow} {stagedText}",
                VerticalAlignment = VerticalAlignment.Center,
            };
            deltaLabel.AddThemeColorOverride("font_color", arrowColor);
            hbox.AddChild(deltaLabel);
        }

        parent.AddChild(hbox);
    }

    private static void AddFlagRow(VBoxContainer parent, string labelText, bool flag) {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText };
        var flagLabel = new Label { Text = flag ? "Yes" : "No" };
        hbox.AddChild(label);
        hbox.AddChild(flagLabel);
        parent.AddChild(hbox);
    }
}
