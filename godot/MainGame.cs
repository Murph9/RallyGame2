using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;

namespace murph9.RallyGame2.godot;

public partial class MainGame : Node {
	public record SceneDetail(Type Type, PackedScene Scene);

	private readonly List<SceneDetail> _sceneCache = [
		new SceneDetail(typeof(IntroScreen), GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(IntroScreen)))),
		new SceneDetail(typeof(ReadyScreen), GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(ReadyScreen)))),
		new SceneDetail(typeof(RacingScreen), GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(RacingScreen)))),
		new SceneDetail(typeof(ResultsScreen), GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(ResultsScreen)))),
		new SceneDetail(typeof(UpgradeScreen), GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(UpgradeScreen))))
	];

	private T GetFromCache<T>() where T : Node => _sceneCache.Single(x => x.Type == typeof(T)).Scene.Instantiate<T>();

	public override void _Ready() {
		var intro = GetFromCache<IntroScreen>();
		intro.Closed += () => IntroClosed(intro);
		AddChild(intro);
	}

	public override void _Process(double delta) {
	}

	private void IntroClosed(IntroScreen intro) {
		RemoveChild(intro);
		intro.QueueFree();

		var ready = GetFromCache<ReadyScreen>();
		ready.Closed += () => ReadyClosed(ready);
		AddChild(ready);
	}

	private void ReadyClosed(ReadyScreen ready) {
		RemoveChild(ready);
		ready.QueueFree();

		var racing = GetFromCache<RacingScreen>();
		racing.Closed += () => RacingClosed(racing);
		AddChild(racing);
	}

	private void RacingClosed(RacingScreen racing) {
		RemoveChild(racing);
		racing.QueueFree();

		var results = GetFromCache<ResultsScreen>();
		results.Closed += () => ResultsClosed(results);
		AddChild(results);
	}

	private void ResultsClosed(ResultsScreen results) {
		RemoveChild(results);
		results.QueueFree();

		var upgrade = GetFromCache<UpgradeScreen>();
		upgrade.Closed += () => UpgradeClosed(upgrade);
		AddChild(upgrade);
	}

	private void UpgradeClosed(UpgradeScreen upgrade) {
		RemoveChild(upgrade);
		upgrade.QueueFree();

		var ready = GetFromCache<ReadyScreen>();
		ready.Closed += () => ReadyClosed(ready);
		AddChild(ready);
	}
}
