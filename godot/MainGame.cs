using Godot;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot;

public partial class MainGame : Node {

	public override void _Ready() {
		LoadIntro();
	}

	public override void _Process(double delta) {
	}

	private void Unload(Node node, System.Action load) {
		RemoveChild(node);
		node.QueueFree();

		load();
	}

	private void LoadIntro() {
		var intro = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(IntroScreen))).Instantiate<IntroScreen>();
		intro.Closed += () => { Unload(intro, LoadReady); };
		AddChild(intro);
	}

	private void LoadReady() {
		var ready = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(ReadyScreen))).Instantiate<ReadyScreen>();
		ready.Closed += () => { Unload(ready, LoadRacing); };
		AddChild(ready);
	}

	private void LoadRacing() {
		var racing = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(RacingScreen))).Instantiate<RacingScreen>();
		racing.Closed += () => { Unload(racing, LoadResults); };
		racing.Quit += () => { Unload(racing, LoadIntro); };
		AddChild(racing);
	}

	private void LoadResults() {
		var results = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(ResultsScreen))).Instantiate<ResultsScreen>();
		results.Closed += () => { Unload(results, LoadUpgrade); };
		results.Quit += () => { Unload(results, LoadIntro); };
		AddChild(results);
	}

	private void LoadUpgrade() {
		var upgrade = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(UpgradeScreen))).Instantiate<UpgradeScreen>();
		upgrade.Closed += () => { Unload(upgrade, LoadReady); };
		AddChild(upgrade);
	}
}
