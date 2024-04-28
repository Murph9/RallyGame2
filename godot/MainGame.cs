using Godot;
using murph9.RallyGame2.godot.Component;
using murph9.RallyGame2.godot.scenes;
using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;

namespace murph9.RallyGame2.godot;

public partial class MainGame : Node {

	// intro -> ready
	// ready -> racing
	// racing -> results
	// results -> on win upgrades, on lose -> intro
	// upgrades -> ready

	private readonly List<Node> _screensAdded = [];
	private RacingScreen _currentRacing;
	private RoadManager _roadManager;

	public override void _Ready() {
		LoadIntro();
	}

	public override void _Process(double delta) {}

	private void Load(Node node) {
		AddChild(node);
		_screensAdded.Add(node);
	}

	private void Unload(Node node, Action after) {
		RemoveChild(node);
		_screensAdded.Remove(node);
		node.QueueFree();

		after();
	}

	private void LoadIntro() {
		foreach (var s in _screensAdded) {
			RemoveChild(s);
			s.QueueFree();
		}
		_screensAdded.Clear();

		if (_currentRacing != null) {
			RemoveChild(_currentRacing);
			_currentRacing.QueueFree();
			_currentRacing = null;
		}

		if (_roadManager != null) {
			RemoveChild(_roadManager);
			_roadManager.QueueFree();
			_roadManager = null;
			Console.WriteLine("uninit");
		}
		Console.WriteLine("init");

		var intro = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(IntroScreen))).Instantiate<IntroScreen>();
		intro.Closed += () => {
			Unload(intro, LoadReady);

			_currentRacing = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(RacingScreen))).Instantiate<RacingScreen>();
			_currentRacing.Finished += () => {
				_currentRacing.StopDriving();
				LoadResults();
			};
			_currentRacing.Restart += () => { Unload(_currentRacing, LoadIntro); };
			Load(_currentRacing);
			_currentRacing.StopDriving(); // to start with no moving

			// don't allow this to null ref on a second call (needs the instance of the delegate created in the lambda below)
			// _roadManager.Loaded -=  intro.RoadLoaded(_roadManager);
		};
		Load(intro);

		_roadManager = new RoadManager();
		_roadManager.Loaded += () => { intro.RoadLoaded(_roadManager); };
		AddChild(_roadManager);
	}

	private void LoadReady() {
		var ready = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(ReadyScreen))).Instantiate<ReadyScreen>();
		ready.Closed += () => { Unload(ready, StartRacing); };
		Load(ready);
	}

	private void StartRacing() {
		_currentRacing.StartDriving();
	}

	private void LoadResults() {
		var results = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(ResultsScreen))).Instantiate<ResultsScreen>();
		results.Closed += () => { Unload(results, LoadUpgrade); };
		results.Restart += () => { Unload(results, LoadIntro); };
		Load(results);
	}

	private void LoadUpgrade() {
		var upgrade = GD.Load<PackedScene>(GodotClassHelper.GetScenePath(typeof(UpgradeScreen))).Instantiate<UpgradeScreen>();
		upgrade.Closed += () => { Unload(upgrade, LoadReady); };
		Load(upgrade);
	}
}
