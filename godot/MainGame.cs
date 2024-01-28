using Godot;
using murph9.RallyGame2.godot.scenes;

namespace murph9.RallyGame2.godot;

public partial class MainGame : Node {

	private IntroScreen _introScreen;
	private ReadyScreen _readyScreen;
	private RacingScreen _racingScreen;
	private ResultsScreen _resultsScreen;
	private UpgradeScreen _upgradeScreen;

	public override void _Ready() {
		_introScreen = GD.Load<PackedScene>("res://scenes/IntroScreen.tscn").Instantiate<IntroScreen>();
		_readyScreen = GD.Load<PackedScene>("res://scenes/ReadyScreen.tscn").Instantiate<ReadyScreen>();
		_racingScreen = GD.Load<PackedScene>("res://scenes/RacingScreen.tscn").Instantiate<RacingScreen>();
		_resultsScreen = GD.Load<PackedScene>("res://scenes/ResultsScreen.tscn").Instantiate<ResultsScreen>();
		_upgradeScreen = GD.Load<PackedScene>("res://scenes/UpgradeScreen.tscn").Instantiate<UpgradeScreen>();

		_introScreen.Closed += () => {
			RemoveChild(_introScreen);
			AddChild(_readyScreen);
		};

		_readyScreen.Closed += () => {
			RemoveChild(_readyScreen);
			AddChild(_racingScreen);
		};

		_racingScreen.Closed += () => {
			RemoveChild(_racingScreen);
			AddChild(_resultsScreen);
		};

		_resultsScreen.Closed += () => {
			RemoveChild(_resultsScreen);
			AddChild(_upgradeScreen);
		};

		_upgradeScreen.Closed += () => {
			RemoveChild(_upgradeScreen);
			AddChild(_readyScreen);
		};

		AddChild(_introScreen);
	}

	public override void _Process(double delta) {
	}
}
