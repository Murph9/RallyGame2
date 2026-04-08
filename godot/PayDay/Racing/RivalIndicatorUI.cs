using Godot;
using murph9.RallyGame2.godot.Cars.Init.Parts;
using murph9.RallyGame2.godot.Cars.Sim;
using murph9.RallyGame2.godot.Component.Racing;
using murph9.RallyGame2.godot.Component.Rarity;
using murph9.RallyGame2.godot.Utilities;
using System;

namespace murph9.RallyGame2.godot.PayDay.Racing;

/// <summary>
/// Screen-space HUD overlay for a single rival car. Two display modes:
///
///  PRE-RACE  — sidebar panel is hidden. A small rarity-coloured world marker
///              ("▼ RIVAL · wager") floats above the car, plus an off-screen
///              edge arrow when the car is outside the frustum.
///
///  RACING    — sidebar panel appears on the right edge (one slot per active
///              race) showing rarity, wager, race progress and remaining
///              distance. World marker is hidden (RivalHighlighter covers it).
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

    private bool _raceActive;
    private double _speedMatchProgress; // 0–1 fraction, used for world marker pulse

    // Sidebar panel nodes (only visible while racing)
    private Panel _statusPanel;
    private Label _stateLabel;
    private Label _raceProgressLabel;
    private ProgressBar _speedMatchBar;
    private Label _speedMatchTitle;

    // World marker nodes (only visible pre-race)
    private Label _onScreenMarker;
    private Label _arrowLabel;

    // Race progress (fed by manager each frame)
    private float _raceDistanceDriven;
    private float _raceTotalDistance;
    private float _raceCheckpointDist = -1f;

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
        Layer = 10;
        var color = PartLevelHelper.GetColour(_rarity);
        var rarityName = PartLevelHelper.GetDisplayName(_rarity);
        var stakeColor = _stake == RivalStakeType.Parts ? color : new Color(1f, 0.85f, 0.1f);

        // ── Sidebar panel (racing only) ───────────────────────────────────────
        _statusPanel = new Panel { Name = "RivalStatusPanel", Visible = false };
        _statusPanel.AddThemeStyleboxOverride("panel", MakeBoxStyle(new Color(0f, 0f, 0f, 0.72f), color, 3));
        AddChild(_statusPanel);

        var vbox = new VBoxContainer { Name = "VBox" };
        _statusPanel.AddChild(vbox);

        // Header: rarity + wager on one compact line
        var headerLabel = new Label {
            Text = $"★ {rarityName.ToUpper()}  ·  {_wageredPartName}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(headerLabel, color, 14);
        vbox.AddChild(headerLabel);

        // Wager colour accent line
        var wagerLabel = new Label {
            Text = _stake == RivalStakeType.Parts ? "PARTS WAGER" : "MONEY WAGER",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(wagerLabel, stakeColor, 11);
        vbox.AddChild(wagerLabel);

        _stateLabel = new Label {
            Text = "★  RACING  ★",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_stateLabel, color, 13);
        vbox.AddChild(_stateLabel);

        _raceProgressLabel = new Label {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        SetLabelStyle(_raceProgressLabel, new Color(0.9f, 0.9f, 0.5f), 13);
        vbox.AddChild(_raceProgressLabel);

        // ── World marker (pre-race only) ──────────────────────────────────────
        _onScreenMarker = new Label {
            Text = $"▼  RIVAL  ·  {_wageredPartName}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SetLabelStyle(_onScreenMarker, color, 15);
        AddChild(_onScreenMarker);

        _arrowLabel = new Label {
            Text = "▶",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            PivotOffset = new Vector2(14, 14),
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

        if (_raceActive)
            UpdateRaceMode();
        else
            UpdatePreRaceMode();
    }

    // ── Race mode ─────────────────────────────────────────────────────────────

    private void UpdateRaceMode() {
        // Show sidebar, hide world marker
        _statusPanel.Visible = true;
        _onScreenMarker.Visible = false;
        _arrowLabel.Visible = false;

        var viewport = GetViewport();
        var screenSize = viewport.GetVisibleRect().Size;
        var color = PartLevelHelper.GetColour(_rarity);

        float panelH = _statusPanel.GetMinimumSize().Y + 20f;
        _statusPanel.Size = new Vector2(PANEL_WIDTH, Mathf.Max(panelH, 90f));
        float panelY = PANEL_MARGIN + _slotIndex * (_statusPanel.Size.Y + PANEL_GAP);
        _statusPanel.Position = new Vector2(screenSize.X - PANEL_WIDTH - PANEL_MARGIN, panelY);

        var vbox = _statusPanel.GetNode<VBoxContainer>("VBox");
        vbox.Size = _statusPanel.Size - new Vector2(16, 12);
        vbox.Position = new Vector2(8, 6);

        bool checkpointPlaced = _raceCheckpointDist >= 0;
        float remaining, pct;
        string progressText;
        if (!checkpointPlaced) {
            remaining = Mathf.Max(0, _raceTotalDistance - _raceDistanceDriven);
            pct = _raceTotalDistance > 0
                ? Mathf.Clamp(_raceDistanceDriven / _raceTotalDistance, 0f, 1f) : 0f;
            progressText = $"{pct * 100:F0}%  −{remaining:F0} m";
        } else {
            remaining = _raceCheckpointDist;
            float effectiveTotal = _raceDistanceDriven + remaining;
            pct = effectiveTotal > 0
                ? Mathf.Clamp(_raceDistanceDriven / effectiveTotal, 0f, 1f) : 1f;
            progressText = $"{pct * 100:F0}%  −{remaining:F0} m ★";
        }
        _raceProgressLabel.Text = progressText;
    }

    // ── Pre-race mode ─────────────────────────────────────────────────────────

    private void UpdatePreRaceMode() {
        // Hide sidebar entirely
        _statusPanel.Visible = false;

        var viewport = GetViewport();
        var camera = viewport?.GetCamera3D();
        if (camera == null) return;

        var screenSize = viewport.GetVisibleRect().Size;
        var color = PartLevelHelper.GetColour(_rarity);

        // Pulse opacity based on speed-match progress — brighter as it fills
        float alpha = 0.45f + 0.55f * (float)_speedMatchProgress;
        var c = color;
        var pulseColor = new Color(c.R, c.G, c.B, alpha);
        _onScreenMarker.AddThemeColorOverride("font_color", pulseColor);
        _arrowLabel.AddThemeColorOverride("font_color", pulseColor);

        // Project world position above rival car roof
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
            _onScreenMarker.Position = screenPos - new Vector2(_onScreenMarker.Size.X / 2f, 0);
        } else {
            _onScreenMarker.Visible = false;
            _arrowLabel.Visible = true;

            var center = screenSize / 2f;
            var dir = (screenPos - center).Normalized();
            var edgePos = ScreenHelper.ClampToScreenEdge(screenPos, screenSize, 44f);
            _arrowLabel.Position = edgePos - new Vector2(14, 14);
            _arrowLabel.Rotation = dir.Angle();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
        return new StyleBoxFlat {
            BgColor = color,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
        };
    }
}
