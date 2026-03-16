using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component.Racing;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Screen-space HUD overlay to show Rivals
///
///  1. Status panel (right side) – rarity tier, race state, distance,
///     speed-match progress bar.
///  2. On-screen world marker – "▼ RIVAL" floating above the car in 2D.
///  3. Off-screen edge arrow – a rotated chevron clamped to the screen border
///     pointing toward the rival when it is outside the camera frustum.
///
/// Call Init() before adding to the scene tree.
/// </summary>
public partial class RivalIndicatorUI : CanvasLayer {

    private const float PANEL_WIDTH = 248f;
    private const float PANEL_MARGIN = 16f;
    private const float PANEL_GAP = 8f;

    private Car _playerCar;
    private Car _rivalCar;
    private PartLevel _rarity;
    private RivalStakeType _stake;
    private string _wageredPartName;
    private int _slotIndex;

    // State set by RivalEncounterManager each frame
    private bool _raceActive;
    private double _speedMatchProgress; // fraction

    private Panel _statusPanel;
    private Label _rarityLabel;
    private Label _stateLabel;
    private Label _distanceLabel;
    private Label _raceProgressLabel;
    private ProgressBar _speedMatchBar;
    private Label _speedMatchTitle;
    private Label _onScreenMarker;
    private Label _arrowLabel;

    // Race progress (updated by manager each frame)
    private float _raceDistanceDriven;
    private float _raceTotalDistance;
    private float _raceCheckpointDist = -1f; // -1 = checkpoint not yet placed

    public void Init(Car playerCar, Car rivalCar, PartLevel rarity, RivalStakeType stake, string wageredPartName, int slotIndex = 0) {
        _playerCar = playerCar;
        _rivalCar = rivalCar;
        _rarity = rarity;
        _stake = stake;
        _wageredPartName = wageredPartName;
        _slotIndex = slotIndex;
    }

    public void UpdateSpeedMatchProgress(double progress) => _speedMatchProgress = progress;
    public void SetRaceActive(bool active) => _raceActive = active;
    public void UpdateRaceProgress(float driven, float total, float checkpointDist) {
        _raceDistanceDriven = driven;
        _raceTotalDistance = total;
        _raceCheckpointDist = checkpointDist;
    }

    public override void _Ready() {
        Layer = 10; // ensure it draws above other game UI
        var color = PartLevelHelper.GetColour(_rarity);
        var rarityName = PartLevelHelper.GetDisplayName(_rarity);

        _statusPanel = new Panel { Name = "RivalStatusPanel" };
        _statusPanel.AddThemeStyleboxOverride("panel", MakeBoxStyle(new Color(0f, 0f, 0f, 0.72f), color, 3));
        AddChild(_statusPanel);

        var vbox = new VBoxContainer { Name = "VBox" };
        _statusPanel.AddChild(vbox);

        // Header – rarity name
        _rarityLabel = new Label {
            Text = $"★  {rarityName.ToUpper()} RIVAL  ★",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_rarityLabel, color, 16);
        vbox.AddChild(_rarityLabel);

        // Stake – what the rival is racing for
        var stakeColor = _stake == RivalStakeType.Parts ? color : new Color(1f, 0.85f, 0.1f);
        var stakeLabel = new Label {
            Text = $"WAGER: {_wageredPartName}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(stakeLabel, stakeColor, 13);
        vbox.AddChild(stakeLabel);

        // State – approach / match-speed / racing
        _stateLabel = new Label {
            Text = "APPROACH TO RACE",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_stateLabel, Colors.White, 13);
        vbox.AddChild(_stateLabel);

        // Distance to rival
        _distanceLabel = new Label {
            Text = "Dist: ---",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_distanceLabel, new Color(0.85f, 0.85f, 0.85f), 13);
        vbox.AddChild(_distanceLabel);

        // Race progress (shown while race is active)
        _raceProgressLabel = new Label {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
        };
        SetLabelStyle(_raceProgressLabel, new Color(0.9f, 0.9f, 0.5f), 13);
        vbox.AddChild(_raceProgressLabel);

        // Speed-match title + bar
        _speedMatchTitle = new Label {
            Text = "SPEED MATCH",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_speedMatchTitle, new Color(0.7f, 0.7f, 0.7f), 11);
        vbox.AddChild(_speedMatchTitle);

        _speedMatchBar = new ProgressBar {
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            CustomMinimumSize = new Vector2(220, 16),
            ShowPercentage = false,
        };
        _speedMatchBar.AddThemeStyleboxOverride("fill", MakeFillStyle(color));
        _speedMatchBar.AddThemeStyleboxOverride("background", MakeBoxStyle(new Color(0.1f, 0.1f, 0.1f, 0.8f), new Color(0.3f, 0.3f, 0.3f), 1));
        vbox.AddChild(_speedMatchBar);


        // directional arrows
        _onScreenMarker = new Label {
            Text = "▼  RIVAL",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SetLabelStyle(_onScreenMarker, color, 18);
        AddChild(_onScreenMarker);


        _arrowLabel = new Label {
            Text = "▶",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            PivotOffset = new Vector2(14, 14), // approx half of font size 28
        };
        SetLabelStyle(_arrowLabel, color, 28);
        AddChild(_arrowLabel);
    }


    public override void _Process(double delta) {
        if (!IsInstanceValid(_playerCar?.RigidBody) || !IsInstanceValid(_rivalCar?.RigidBody)) {
            Visible = false;
            return;
        }
        Visible = true;

        var viewport = GetViewport();
        var camera = viewport?.GetCamera3D();
        if (camera == null) return;

        var screenSize = viewport.GetVisibleRect().Size;

        // Resize & reposition status panel — stacked down the right edge, one slot per rival
        float panelH = _statusPanel.GetMinimumSize().Y + 20f;
        _statusPanel.Size = new Vector2(PANEL_WIDTH, Mathf.Max(panelH, 120f));
        float panelY = PANEL_MARGIN + _slotIndex * (_statusPanel.Size.Y + PANEL_GAP);
        _statusPanel.Position = new Vector2(screenSize.X - PANEL_WIDTH - PANEL_MARGIN, panelY);

        // Sync VBox to fill the panel interior
        var vbox = _statusPanel.GetNode<VBoxContainer>("VBox");
        vbox.Size = _statusPanel.Size - new Vector2(16, 12);
        vbox.Position = new Vector2(8, 6);

        // Update labels
        float dist = _rivalCar.RigidBody.GlobalPosition.DistanceTo(_playerCar.RigidBody.GlobalPosition);
        _distanceLabel.Text = $"Dist: {dist:F0} m";

        if (_raceActive) {
            _stateLabel.Text = "★  RACING  ★";
            _stateLabel.AddThemeColorOverride("font_color", PartLevelHelper.GetColour(_rarity));
            _speedMatchBar.Visible = false;
            _speedMatchTitle.Visible = false;

            // Phase 1: checkpoint not yet placed — progress toward RACE_DISTANCE
            // Phase 2: checkpoint placed — remaining = direct distance to checkpoint
            bool checkpointPlaced = _raceCheckpointDist >= 0;
            float remaining, pct;
            string progressText;
            if (!checkpointPlaced) {
                remaining = Mathf.Max(0, _raceTotalDistance - _raceDistanceDriven);
                pct = _raceTotalDistance > 0 ? Mathf.Clamp(_raceDistanceDriven / _raceTotalDistance, 0f, 1f) : 0f;
                progressText = $"{pct * 100:F0}%  −{remaining:F0} m";
            } else {
                // Once the checkpoint is placed the remaining distance is authoritative
                remaining = _raceCheckpointDist;
                // Denominator = distance driven so far + remaining to checkpoint
                float effectiveTotal = _raceDistanceDriven + remaining;
                pct = effectiveTotal > 0 ? Mathf.Clamp(_raceDistanceDriven / effectiveTotal, 0f, 1f) : 1f;
                progressText = $"{pct * 100:F0}%  −{remaining:F0} m ★";
            }
            _raceProgressLabel.Visible = true;
            _raceProgressLabel.Text = progressText;
        } else {
            bool inTriggerRange = dist < 12f;
            _stateLabel.Text = inTriggerRange ? "▶  MATCH SPEED!" : "GET CLOSER";
            _stateLabel.AddThemeColorOverride("font_color", inTriggerRange ? Colors.Yellow : Colors.White);
            _speedMatchBar.Visible = true;
            _speedMatchTitle.Visible = true;
            _speedMatchBar.Value = _speedMatchProgress;
            _raceProgressLabel.Visible = false;
        }

        // ── On-screen / off-screen marker ─────────────────────────────────────
        // Project the world position slightly above the rival car's roof
        var worldPos = _rivalCar.RigidBody.GlobalPosition + Vector3.Up * 2.6f;
        bool inFront = camera.IsPositionInFrustum(worldPos);
        var screenPos = camera.UnprojectPosition(worldPos);

        const float markerMargin = 50f;
        bool onScreen = inFront
            && screenPos.X > markerMargin && screenPos.X < screenSize.X - markerMargin
            && screenPos.Y > markerMargin && screenPos.Y < screenSize.Y - markerMargin;

        if (onScreen) {
            _onScreenMarker.Visible = true;
            _arrowLabel.Visible = false;
            _onScreenMarker.Position = screenPos - new Vector2(60, 0);
        } else {
            _onScreenMarker.Visible = false;
            _arrowLabel.Visible = true;

            var center = screenSize / 2f;
            var dir = (screenPos - center).Normalized();
            var edgePos = ScreenHelper.ClampToScreenEdge(screenPos, screenSize, 44f);
            _arrowLabel.Position = edgePos - new Vector2(14, 14);
            // Rotate arrow to point toward the rival
            _arrowLabel.Rotation = dir.Angle();
        }
    }

    private static void SetLabelStyle(Label label, Color color, int fontSize) {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    private static StyleBoxFlat MakeBoxStyle(Color bg, Color border, int borderWidth) {
        return new StyleBoxFlat {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
    }

    private static StyleBoxFlat MakeFillStyle(Color color) {
        return new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 };
    }
}
