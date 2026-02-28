using Godot;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.PayDay.Parts;
using System;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Screen-space HUD overlay that makes it impossible to miss a rival:
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

    // How far the SPEED MATCH window is (must match RivalEncounterManager constant)
    private const float SPEED_MATCH_WINDOW = 3f;

    private Car _playerCar;
    private Car _rivalCar;
    private PartRarity _rarity;

    // State set by RivalEncounterManager each frame
    private bool _raceActive;
    private double _speedMatchProgress; // 0 … 1

    // ─── UI nodes ────────────────────────────────────────────────────────────
    private Panel _statusPanel;
    private Label _rarityLabel;
    private Label _stateLabel;
    private Label _distanceLabel;
    private ProgressBar _speedMatchBar;
    private Label _speedMatchTitle;
    private Label _onScreenMarker;
    private Label _arrowLabel;

    // ─── Init ─────────────────────────────────────────────────────────────────

    public void Init(Car playerCar, Car rivalCar, PartRarity rarity) {
        _playerCar = playerCar;
        _rivalCar = rivalCar;
        _rarity = rarity;
    }

    public void UpdateSpeedMatchProgress(double progress) => _speedMatchProgress = progress;
    public void SetRaceActive(bool active) => _raceActive = active;

    // ─── Ready ────────────────────────────────────────────────────────────────

    public override void _Ready() {
        Layer = 10; // ensure it draws above other game UI
        var color = PartRarityHelper.GetColour(_rarity);
        var rarityName = PartRarityHelper.GetDisplayName(_rarity);

        // ── Status panel ──────────────────────────────────────────────────────
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

        // State – approach / match-speed / racing
        _stateLabel = new Label {
            Text = "APPROACH TO RACE",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_stateLabel, Colors.White, 13);
        vbox.AddChild(_stateLabel);

        // Distance
        _distanceLabel = new Label {
            Text = "Dist: ---",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_distanceLabel, new Color(0.85f, 0.85f, 0.85f), 13);
        vbox.AddChild(_distanceLabel);

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

        // ── On-screen world marker ─────────────────────────────────────────────
        _onScreenMarker = new Label {
            Text = "▼  RIVAL",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SetLabelStyle(_onScreenMarker, color, 18);
        AddChild(_onScreenMarker);

        // ── Off-screen directional arrow ───────────────────────────────────────
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

    // ─── Process ──────────────────────────────────────────────────────────────

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

        // Resize & reposition status panel to the right-centre of the screen
        const float panelW = 248f;
        float panelH = _statusPanel.GetMinimumSize().Y + 20f;
        _statusPanel.Size = new Vector2(panelW, Mathf.Max(panelH, 120f));
        _statusPanel.Position = new Vector2(screenSize.X - panelW - 16f, screenSize.Y / 2f - _statusPanel.Size.Y / 2f);

        // Sync VBox to fill the panel interior
        var vbox = _statusPanel.GetNode<VBoxContainer>("VBox");
        vbox.Size = _statusPanel.Size - new Vector2(16, 12);
        vbox.Position = new Vector2(8, 6);

        // Update labels
        float dist = _rivalCar.RigidBody.GlobalPosition.DistanceTo(_playerCar.RigidBody.GlobalPosition);
        _distanceLabel.Text = $"Dist: {dist:F0} m";

        if (_raceActive) {
            _stateLabel.Text = "★  RACING  ★";
            _stateLabel.AddThemeColorOverride("font_color", PartRarityHelper.GetColour(_rarity));
            _speedMatchBar.Visible = false;
            _speedMatchTitle.Visible = false;
        } else {
            bool inTriggerRange = dist < 12f;
            _stateLabel.Text = inTriggerRange ? "▶  MATCH SPEED!" : "GET CLOSER";
            _stateLabel.AddThemeColorOverride("font_color", inTriggerRange ? Colors.Yellow : Colors.White);
            _speedMatchBar.Visible = true;
            _speedMatchTitle.Visible = true;
            _speedMatchBar.Value = _speedMatchProgress;
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
            var edgePos = ClampToScreenEdge(screenPos, screenSize, 44f);
            _arrowLabel.Position = edgePos - new Vector2(14, 14);
            // Rotate arrow to point toward the rival
            _arrowLabel.Rotation = dir.Angle();
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Clamps a screen-space position to within margin of the screen edges.</summary>
    private static Vector2 ClampToScreenEdge(Vector2 screenPos, Vector2 screenSize, float margin) {
        var center = screenSize / 2f;
        var dir = screenPos - center;
        float halfW = screenSize.X / 2f - margin;
        float halfH = screenSize.Y / 2f - margin;

        float scaleX = dir.X != 0f ? halfW / Math.Abs(dir.X) : float.MaxValue;
        float scaleY = dir.Y != 0f ? halfH / Math.Abs(dir.Y) : float.MaxValue;
        float scale = Math.Min(scaleX, scaleY);

        if (scale < 1f)
            return center + dir * scale;
        // Point is already inside – clamp it to edge
        return center + dir.Normalized() * Math.Min(halfW, halfH);
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
