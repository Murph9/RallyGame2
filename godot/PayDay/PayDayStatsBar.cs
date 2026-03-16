using Godot;

namespace murph9.RallyGame2.godot.PayDay;

/// <summary>
/// Persistent stats overlay shown across all PayDay game phases.
/// Displays Day, Loan Balance, Wallet, and Parts count.
/// Added once by PayDayGame and kept alive for the full session.
/// </summary>
public partial class PayDayStatsBar : CanvasLayer {

    private Label _dayLabel;
    private Label _loanLabel;
    private Label _moneyLabel;
    private Label _partsLabel;

    public override void _Ready() {
        _dayLabel = GetNode<Label>("Panel/VBox/DayLabel");
        _loanLabel = GetNode<Label>("Panel/VBox/LoanLabel");
        _moneyLabel = GetNode<Label>("Panel/VBox/MoneyLabel");
        _partsLabel = GetNode<Label>("Panel/VBox/PartsLabel");

        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        state.MoneyChanged += (_) => Refresh();
        state.DayEnded += (_) => Refresh();
        state.RunEnded += Refresh;

        Refresh();
    }

    private void Refresh() {
        var state = GetNode<PayDayGlobalState>("/root/PayDayGlobalState");
        _dayLabel.Text = $"Day {state.DayNumber}";
        _loanLabel.Text = $"Loan: ${state.Loan.Principal:F0}";
        _moneyLabel.Text = $"Wallet: ${state.Money:F0}";
        _partsLabel.Text = $"Parts: {state.PartInventory.Count}";
    }
}
